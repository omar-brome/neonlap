using System.Collections.Generic;
using NeonLap.Audio;
using NeonLap.Camera;
using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Input;
using NeonLap.Race;
using NeonLap.Track;
using NeonLap.UI;
using NeonLap.VFX;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NeonLap.Core
{
    public class NeonLapSceneSetup : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] VehicleProfile vehicleProfile;
        [SerializeField] TrackDefinition trackDefinition;
        [SerializeField] InputActionAsset inputActions;

        [Header("Materials")]
        [SerializeField] Material trackSurfaceMaterial;
        [SerializeField] Material trackEdgeMaterial;
        [SerializeField] Material carBodyMaterial;
        [SerializeField] Material carAccentMaterial;

        [Header("Options")]
        [SerializeField] bool spawnAiRivals = true;
        [SerializeField] int aiRivalCount = 9;
        [SerializeField] bool createUi = true;

        static readonly Color PlayerBodyColor = new(0.1f, 0.35f, 0.45f);
        static readonly Color PlayerAccentColor = new(0f, 3.5f, 4f);

        static readonly Color[] RivalBodyColors =
        {
            new(0.45f, 0.08f, 0.08f),
            new(0.45f, 0.22f, 0.05f),
            new(0.4f, 0.38f, 0.06f),
            new(0.08f, 0.38f, 0.12f),
            new(0.08f, 0.15f, 0.42f),
            new(0.28f, 0.08f, 0.42f),
            new(0.42f, 0.1f, 0.32f),
            new(0.38f, 0.32f, 0.08f),
            new(0.35f, 0.35f, 0.38f),
        };

        static readonly Color[] RivalAccentColors =
        {
            new(4f, 0.3f, 0.3f),
            new(4f, 1.6f, 0.2f),
            new(3.8f, 3.5f, 0.3f),
            new(0.4f, 4f, 0.8f),
            new(0.5f, 1.2f, 4f),
            new(2.5f, 0.4f, 4f),
            new(4f, 0.5f, 2.8f),
            new(3.5f, 2.8f, 0.4f),
            new(3f, 3f, 3.5f),
        };

        GameObject playerCar;
        OvalTrackBuilder trackBuilder;
        RaceManager raceManager;
        RaceEnvironmentBuilder environmentBuilder;

        void Awake()
        {
            GameQualitySettings.Load();
            GameDifficultySettings.Load();
            GameLapSettings.Load();
            GamePoliceSettings.Load();
            EnsureRuntimeDefaults();
            ApplyEnvironmentSettings();
            EnsurePhysicsLayerCollisions();
            EnsureGameManager();
            ResolveActiveTrackDefinition();
            BuildTrack();
            BuildEnvironment();
            BuildTrackHazards();
            BuildNitroPickups();
            BuildBananaHazards();
            SpawnRacers();
            SetupCamera();
            SetupRaceSystems();
            SpawnPatrolHelicopter();
        }

        void EnsureRuntimeDefaults()
        {
            if (vehicleProfile == null)
            {
                vehicleProfile = ScriptableObject.CreateInstance<VehicleProfile>();
                Debug.LogWarning("NeonLapSceneSetup: vehicleProfile was not assigned. Using runtime defaults.", this);
            }

            if (carBodyMaterial == null || carAccentMaterial == null)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                if (carBodyMaterial == null)
                {
                    carBodyMaterial = new Material(lit);
                    carBodyMaterial.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f));
                }

                if (carAccentMaterial == null)
                {
                    carAccentMaterial = new Material(lit);
                    carAccentMaterial.EnableKeyword("_EMISSION");
                    carAccentMaterial.SetColor("_EmissionColor", new Color(0f, 2.5f, 3f));
                }
            }

            if (trackSurfaceMaterial == null || trackEdgeMaterial == null)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                if (trackSurfaceMaterial == null)
                {
                    trackSurfaceMaterial = new Material(lit);
                    TrackRoadMarkingBuilder.ApplyAsphaltLook(trackSurfaceMaterial, null);
                }

                if (trackEdgeMaterial == null)
                {
                    trackEdgeMaterial = new Material(lit);
                    TrackRoadMarkingBuilder.ApplyAsphaltLook(null, trackEdgeMaterial);
                }
            }
        }

        void ApplyEnvironmentSettings()
        {
            var preset = GameQualitySettings.Preset;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.12f, 0.14f, 0.22f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.09f, 0.14f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.06f);
            GameQualitySettings.ApplyFogAndLighting(preset.FogDensity, preset.LightIntensity);
            SkyGraphicsSystem.Ensure(UnityEngine.Camera.main);
            EnsureDynamicWeather(UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null, preset);
        }

        static void EnsureDynamicWeather(Transform cameraTransform, QualityPreset preset)
        {
            var existing = Object.FindAnyObjectByType<DynamicWeatherSystem>();
            if (existing != null)
            {
                existing.Configure(cameraTransform, preset);
                return;
            }

            var weatherGo = new GameObject("DynamicWeather");
            var weather = weatherGo.AddComponent<DynamicWeatherSystem>();
            weather.Configure(cameraTransform, preset);
        }

        void EnsurePhysicsLayerCollisions()
        {
            Physics.IgnoreLayerCollision(NeonLapLayers.Vehicle, NeonLapLayers.Obstacle, false);
            Physics.IgnoreLayerCollision(NeonLapLayers.Vehicle, NeonLapLayers.Track, false);
            Physics.IgnoreLayerCollision(NeonLapLayers.Obstacle, NeonLapLayers.Track, false);
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 2;
            Physics.defaultContactOffset = 0.01f;
            Physics.defaultMaxDepenetrationVelocity = 2.5f;
        }

        void EnsureGameManager()
        {
            if (FindAnyObjectByType<GameManager>() != null)
                return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        void ResolveActiveTrackDefinition()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.SetFallbackTrack(trackDefinition);
            var activeTrack = GameManager.Instance.GetCurrentTrackDefinition();
            if (activeTrack != null)
                trackDefinition = activeTrack;
        }

        void BuildEnvironment()
        {
            var envGo = new GameObject("RaceEnvironment");
            environmentBuilder = envGo.AddComponent<RaceEnvironmentBuilder>();
            environmentBuilder.Build(trackDefinition, trackBuilder.StartPosition, trackBuilder.StartRotation,
                trackBuilder.EnvironmentHalfExtents, GameQualitySettings.Preset);
        }

        void BuildTrackHazards()
        {
            var hazardBuilder = trackBuilder.gameObject.AddComponent<TrackHazardBuilder>();
            hazardBuilder.Build(trackBuilder.AiWaypointTransforms, trackDefinition, carBodyMaterial, carAccentMaterial);
        }

        void BuildNitroPickups()
        {
            var pickupBuilder = trackBuilder.gameObject.AddComponent<NitroPickupBuilder>();
            pickupBuilder.Build(trackBuilder.AiWaypointTransforms, trackDefinition);
        }

        void BuildBananaHazards()
        {
            var bananaBuilder = trackBuilder.gameObject.AddComponent<BananaHazardBuilder>();
            bananaBuilder.Build(trackBuilder.AiWaypointTransforms, trackDefinition);
        }

        void BuildTrack()
        {
            var raceRoot = new GameObject("Race");
            trackBuilder = raceRoot.AddComponent<OvalTrackBuilder>();
            trackBuilder.Configure(trackDefinition, trackSurfaceMaterial, trackEdgeMaterial, trackEdgeMaterial);
            trackBuilder.BuildTrack();
        }

        void SpawnRacers()
        {
            const int gridColumns = 5;
            const float columnSpacing = 4.2f;
            const float rowSpacing = 6f;
            const int playerGridIndex = 2;

            var waypointList = trackBuilder.AiWaypointTransforms;
            var waypoints = new Transform[waypointList.Count];
            for (var i = 0; i < waypoints.Length; i++)
                waypoints[i] = waypointList[i];

            var playerPosition = GetGridSpawnPosition(playerGridIndex, gridColumns, columnSpacing, rowSpacing);
            playerCar = BuildCar("HoverCar", vehicleProfile, true, PlayerBodyColor, PlayerAccentColor);
            if (playerCar == null)
            {
                Debug.LogError("NeonLapSceneSetup: Failed to build player car.", this);
                return;
            }

            playerCar.transform.SetPositionAndRotation(playerPosition, trackBuilder.StartRotation);
            var playerReset = playerCar.GetComponent<VehicleReset>();
            if (playerReset != null)
                playerReset.SetSpawnPoint(playerPosition, trackBuilder.StartRotation);
            playerCar.SetActive(true);

            if (!spawnAiRivals)
                return;

            var rivalCount = Mathf.Min(aiRivalCount, GameQualitySettings.Preset.AiRivalCount);
            for (var rivalIndex = 0; rivalIndex < rivalCount; rivalIndex++)
            {
                var gridIndex = rivalIndex >= playerGridIndex ? rivalIndex + 1 : rivalIndex;
                var spawnPosition = GetGridSpawnPosition(gridIndex, gridColumns, columnSpacing, rowSpacing);
                var bodyColor = RivalBodyColors[rivalIndex % RivalBodyColors.Length];
                var accentColor = RivalAccentColors[rivalIndex % RivalAccentColors.Length];
                var aiCar = BuildCar("AIRival_" + (rivalIndex + 1), vehicleProfile, false, bodyColor, accentColor);
                if (aiCar == null)
                    continue;

                aiCar.transform.SetPositionAndRotation(spawnPosition, trackBuilder.StartRotation);

                var ai = aiCar.GetComponent<AIVehicleController>();
                if (ai == null)
                    continue;

                ai.SetWaypoints(waypoints);
                ai.ConfigureTrack(trackDefinition != null ? trackDefinition.trackWidth * 0.5f : 7f);

                var difficulty = GameDifficultySettings.Preset;
                ai.ApplyDifficulty(difficulty);
                ai.SetRivalVariation(rivalIndex * 2,
                    difficulty.RivalSpeedBase + rivalIndex * difficulty.RivalSpeedStep);
                ai.SetPlayerTarget(playerCar.transform);

                var aiReset = aiCar.GetComponent<VehicleReset>();
                aiReset.SetSpawnPoint(spawnPosition, trackBuilder.StartRotation);
                aiReset.ConfigureForAi(ai);
                aiCar.SetActive(true);
            }
        }

        Vector3 GetGridSpawnPosition(int gridIndex, int columns, float columnSpacing, float rowSpacing)
        {
            var row = gridIndex / columns;
            var col = gridIndex % columns;
            var lateral = (col - (columns - 1) * 0.5f) * columnSpacing;
            var right = trackBuilder.StartRotation * Vector3.right;
            var back = trackBuilder.StartRotation * Vector3.back;
            return trackBuilder.StartPosition + right * lateral + back * (row * rowSpacing);
        }

        GameObject BuildCar(string carName, VehicleProfile profile, bool isPlayer, Color bodyColor, Color accentColor)
        {
            if (profile == null)
            {
                Debug.LogError("NeonLapSceneSetup: Cannot build car without a VehicleProfile.", this);
                return null;
            }

            var car = new GameObject(carName);
            car.SetActive(false);
            car.tag = isPlayer ? "Player" : "Untagged";
            car.layer = NeonLapLayers.Vehicle;

            var rb = car.GetComponent<Rigidbody>();
            if (rb == null)
                rb = car.AddComponent<Rigidbody>();
            rb.mass = 750f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            ObstaclePhysics.ConfigureVehicle(rb);

            var buildArgs = new HoverCarVisualBuilder.BuildArgs(carBodyMaterial, carAccentMaterial, bodyColor,
                accentColor, isPlayer);
            HoverCarVisualBuilder.Build(car.transform, buildArgs);

            var appearance = car.AddComponent<VehicleAppearance>();
            appearance.Configure(buildArgs);
            var hoverPods = car.AddComponent<VehicleHoverPodSystem>();
            hoverPods.Configure(isPlayer);
            car.AddComponent<VehicleWheelSteerVisual>();
            car.AddComponent<VehicleSlipEffect>();
            car.AddComponent<VehicleTaillightController>();
            car.AddComponent<VehicleDriftMarkEmitter>();
            car.AddComponent<VehicleDamageSystem>();

            VehicleCollisionBody.Build(car);
            foreach (var vehicleCollider in car.GetComponents<BoxCollider>())
                ObstaclePhysics.ApplyVehicleColliderMaterial(vehicleCollider);

            car.AddComponent<PodiumJumpController>();

            if (isPlayer)
            {
                car.AddComponent<VehicleCollisionHandler>();
                car.AddComponent<CollisionProximitySensor>();
            }

            car.AddComponent<VehicleGroundProbe>();

            PlayerInputReader inputReader = null;
            if (isPlayer)
            {
                inputReader = car.AddComponent<PlayerInputReader>();
                if (inputActions != null)
                    inputReader.Configure(inputActions);
                else
                    Debug.LogError("NeonLapSceneSetup: inputActions is not assigned.", this);
            }

            if (isPlayer)
            {
                car.AddComponent<VehicleNitroBoost>();
                var controller = car.AddComponent<VehicleController>();
                controller.Configure(profile, inputReader);
                var barrelRoll = car.AddComponent<VehicleBarrelRoll>();
                barrelRoll.Configure(profile);
            }

            var reset = car.AddComponent<VehicleReset>();
            if (inputReader != null)
                reset.Configure(inputReader);

            car.AddComponent<RaceStartGate>();
            car.AddComponent<RacerProgress>().Configure(isPlayer);
            car.AddComponent<ExhaustSmokeVFX>();

            if (isPlayer)
            {
                car.AddComponent<GtrExhaustPopVFX>();
                car.AddComponent<VehicleUnderglowVFX>();
                car.AddComponent<VehicleTurnSignalController>();
                car.AddComponent<RaceScoreSystem>();
                car.AddComponent<VehicleAudioController>();
                car.AddComponent<VehicleFuelSystem>();
                AddDriftTrails(car);
            }
            else
            {
                var ai = car.AddComponent<AIVehicleController>();
                SetPrivateField(ai, "profile", profile);
                car.AddComponent<AIVehicleHealthSystem>();
                reset.ConfigureForAi(ai);
            }

            return car;
        }

        void AddDriftTrails(GameObject car)
        {
            var vfx = car.AddComponent<DriftTrailVFX>();
            var left = CreateTrail(car.transform, new Vector3(-0.7f, 0.1f, -1f), "LeftTrail");
            var right = CreateTrail(car.transform, new Vector3(0.7f, 0.1f, -1f), "RightTrail");
            SetPrivateField(vfx, "vehicle", car.GetComponent<VehicleController>());
            SetPrivateField(vfx, "leftTrail", left);
            SetPrivateField(vfx, "rightTrail", right);
        }

        TrailRenderer CreateTrail(Transform parent, Vector3 localPos, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.startWidth = 0.35f;
            trail.endWidth = 0.05f;
            trail.emitting = false;
            if (carAccentMaterial != null)
                trail.material = carAccentMaterial;
            return trail;
        }

        void SetupCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null || playerCar == null)
                return;

            var follow = cam.gameObject.GetComponent<FollowCamera>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.Target = playerCar.transform;

            var weather = FindAnyObjectByType<DynamicWeatherSystem>();
            if (weather != null)
                weather.Configure(cam.transform, GameQualitySettings.Preset);
        }

        void SpawnPatrolHelicopter()
        {
            if (playerCar == null || !GameQualitySettings.Preset.EnableHelicopter)
                return;

            var helicopterGo = new GameObject("PatrolHelicopter");
            helicopterGo.transform.SetPositionAndRotation(
                playerCar.transform.position + playerCar.transform.forward * 48f + Vector3.up * 24f,
                playerCar.transform.rotation);

            var visual = HelicopterVisualBuilder.Build(helicopterGo.transform, carBodyMaterial, carAccentMaterial);
            var patrol = helicopterGo.AddComponent<PatrolHelicopter>();
            patrol.Configure(playerCar.transform, visual, raceManager);
        }

        void SetupRaceSystems()
        {
            var raceRoot = trackBuilder.transform;
            raceManager = raceRoot.gameObject.AddComponent<RaceManager>();
            SetPrivateField(raceManager, "totalLaps", GameLapSettings.CurrentLaps);
            SetPrivateField(raceManager, "trackBuilder", trackBuilder);
            SetPrivateField(raceManager, "playerReset", playerCar.GetComponent<VehicleReset>());

            if (raceRoot.GetComponent<DriftMarkSystem>() == null)
                raceRoot.gameObject.AddComponent<DriftMarkSystem>();

            var policeChase = raceRoot.gameObject.AddComponent<PoliceChaseSystem>();
            policeChase.Configure(raceManager, playerCar, trackBuilder, vehicleProfile, carBodyMaterial,
                carAccentMaterial);

            if (!createUi)
            {
                environmentBuilder?.ConfigureScoreboard(raceManager);
                return;
            }

            BuildRaceUi();
            BuildPauseMenu(raceRoot.gameObject);
            environmentBuilder?.ConfigureScoreboard(raceManager);
        }

        void BuildPauseMenu(GameObject raceRoot)
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("PauseMenuUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(canvasGo.transform, false);
            var panelRect = pausePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = pausePanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.65f);
            pausePanel.SetActive(false);

            var resume = CreateMenuButton(pausePanel.transform, "ResumeButton", "RESUME", new Vector2(0f, 40f));
            var restart = CreateMenuButton(pausePanel.transform, "RestartButton", "RESTART", new Vector2(0f, -30f));
            var quit = CreateMenuButton(pausePanel.transform, "QuitButton", "MAIN MENU", new Vector2(0f, -100f));

            var pause = canvasGo.AddComponent<PauseMenuController>();
            pause.Configure(pausePanel, raceManager, resume, restart, quit);
        }

        Button CreateMenuButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 50f);
            rect.anchoredPosition = pos;
            go.AddComponent<Image>().color = new Color(0.1f, 0.05f, 0.18f, 0.95f);
            var button = go.AddComponent<Button>();
            var text = CreateCenteredText(go.transform, "Label", label, 24);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        void BuildRaceUi()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("RaceUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var raceScaler = canvasGo.AddComponent<CanvasScaler>();
            raceScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            raceScaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var raceUi = canvasGo.AddComponent<RaceUI>();
            SetPrivateField(raceUi, "raceManager", raceManager);

            var lapText = CreateText(canvasGo.transform, "LapText", new Vector2(20f, -20f), TextAnchor.UpperLeft, 28);
            var lapTimer = CreateText(canvasGo.transform, "LapTimer", new Vector2(-20f, -20f), TextAnchor.UpperRight, 28);
            var raceTimer = CreateText(canvasGo.transform, "RaceTimer", new Vector2(-20f, -60f), TextAnchor.UpperRight, 24);
            var bestLap = CreateText(canvasGo.transform, "BestLap", new Vector2(20f, -60f), TextAnchor.UpperLeft, 24);
            var positionText = CreateText(canvasGo.transform, "PositionText", new Vector2(20f, -100f), TextAnchor.UpperLeft, 28);
            positionText.color = Color.white;
            var scoreText = CreateText(canvasGo.transform, "ScoreText", new Vector2(20f, -140f), TextAnchor.UpperLeft, 28);
            scoreText.color = new Color(0.45f, 1f, 1f);
            scoreText.fontStyle = FontStyle.Bold;

            var scoreSystem = playerCar.GetComponent<RaceScoreSystem>();
            scoreSystem?.Configure(raceManager, playerCar);

            var dashboardCluster = canvasGo.AddComponent<VehicleDashboardCluster>();
            dashboardCluster.Build(canvasGo.transform);
            dashboardCluster.Configure(raceManager, playerCar.GetComponent<VehicleController>());

            var commentaryPanel = CreateCommentaryPanel(canvasGo.transform, out var commentarySubtitle);

            var directionGuide = canvasGo.AddComponent<RaceDirectionGuide>();
            directionGuide.Configure(raceManager, playerCar.transform, trackBuilder.CheckpointTransforms,
                canvasGo.transform);

            var collisionHud = canvasGo.AddComponent<CollisionWarningHud>();
            collisionHud.Build(canvasGo.transform);
            var proximitySensor = playerCar.GetComponent<CollisionProximitySensor>();
            if (proximitySensor != null)
            {
                proximitySensor.Configure(raceManager);
                collisionHud.Configure(proximitySensor);
            }

            var countdownPanel = new GameObject("CountdownPanel");
            countdownPanel.transform.SetParent(canvasGo.transform, false);
            var panelRect = countdownPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            countdownPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            countdownPanel.SetActive(false);

            var countdownSubtitle = CreateText(countdownPanel.transform, "CountdownSubtitle", Vector2.zero, TextAnchor.MiddleCenter, 36);
            countdownSubtitle.alignment = TextAnchor.MiddleCenter;
            countdownSubtitle.color = new Color(0.75f, 0.85f, 1f);
            var subtitleRect = countdownSubtitle.GetComponent<RectTransform>();
            ConfigureScreenTextRect(subtitleRect, 50f, new Vector2(0f, 90f));
            countdownSubtitle.text = "GET READY";

            var countdownText = CreateText(countdownPanel.transform, "CountdownText", Vector2.zero, TextAnchor.MiddleCenter, 120);
            countdownText.alignment = TextAnchor.MiddleCenter;
            countdownText.color = new Color(0.45f, 1f, 1f);
            ConfigureScreenTextRect(countdownText.GetComponent<RectTransform>(), 160f, Vector2.zero);
            countdownText.text = "3";

            var finishPanel = new GameObject("FinishPanel");
            finishPanel.transform.SetParent(canvasGo.transform, false);
            var finishPanelRect = finishPanel.AddComponent<RectTransform>();
            finishPanelRect.anchorMin = new Vector2(0f, 0f);
            finishPanelRect.anchorMax = new Vector2(1f, 0f);
            finishPanelRect.pivot = new Vector2(0.5f, 0f);
            finishPanelRect.sizeDelta = new Vector2(0f, 340f);
            finishPanelRect.anchoredPosition = Vector2.zero;
            finishPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            finishPanel.SetActive(false);

            var jumpHint = CreatePodiumJumpHint(canvasGo.transform);

            var finishTitle = CreateText(finishPanel.transform, "FinishTitle", new Vector2(0f, 250f), TextAnchor.LowerCenter, 64);
            finishTitle.alignment = TextAnchor.LowerCenter;
            finishTitle.color = new Color(0.4f, 1f, 1f);
            var finishTitleRect = finishTitle.GetComponent<RectTransform>();
            finishTitleRect.anchorMin = new Vector2(0.5f, 0f);
            finishTitleRect.anchorMax = new Vector2(0.5f, 0f);
            finishTitleRect.pivot = new Vector2(0.5f, 0f);
            finishTitleRect.anchoredPosition = new Vector2(0f, 250f);
            finishTitleRect.sizeDelta = new Vector2(900f, 80f);

            var finishDetail = CreateText(finishPanel.transform, "FinishDetail", new Vector2(0f, 205f), TextAnchor.LowerCenter, 28);
            finishDetail.alignment = TextAnchor.LowerCenter;
            finishDetail.color = Color.white;
            var finishDetailRect = finishDetail.GetComponent<RectTransform>();
            finishDetailRect.anchorMin = new Vector2(0.5f, 0f);
            finishDetailRect.anchorMax = new Vector2(0.5f, 0f);
            finishDetailRect.pivot = new Vector2(0.5f, 0f);
            finishDetailRect.anchoredPosition = new Vector2(0f, 205f);
            finishDetailRect.sizeDelta = new Vector2(900f, 50f);

            var nextLevelButton = CreateMenuButton(finishPanel.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(0f, 130f));
            nextLevelButton.gameObject.SetActive(false);
            var nextLevelLabel = nextLevelButton.GetComponentInChildren<Text>();

            var finishRestart = CreateMenuButton(finishPanel.transform, "FinishRestartButton", "RESTART", new Vector2(0f, 70f));
            var finishMenu = CreateMenuButton(finishPanel.transform, "FinishMenuButton", "MAIN MENU", new Vector2(0f, 10f));

            var finishMenuController = canvasGo.AddComponent<FinishMenuController>();
            finishMenuController.Configure(
                raceManager,
                finishPanel,
                finishMenu,
                finishRestart,
                nextLevelButton,
                nextLevelLabel);

            raceUi.Configure(
                raceManager,
                playerCar.GetComponent<VehicleController>(),
                lapText,
                lapTimer,
                raceTimer,
                bestLap,
                positionText,
                scoreText,
                scoreSystem,
                dashboardCluster,
                countdownText,
                countdownSubtitle,
                countdownPanel,
                finishPanel,
                finishTitle,
                finishDetail);

            var commentary = canvasGo.AddComponent<RaceCommentarySystem>();
            commentary.Configure(raceManager, commentarySubtitle, commentaryPanel);

            var followCamera = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<FollowCamera>()
                : null;

            if (followCamera != null)
            {
                var cameraModeText = CreateText(canvasGo.transform, "CameraModeText",
                    new Vector2(0f, -18f), TextAnchor.UpperCenter, 22);
                cameraModeText.alignment = TextAnchor.UpperCenter;
                cameraModeText.color = new Color(0.75f, 0.88f, 1f, 0.9f);
                cameraModeText.gameObject.SetActive(false);
                var cameraModeRect = cameraModeText.GetComponent<RectTransform>();
                cameraModeRect.anchorMin = new Vector2(0.5f, 1f);
                cameraModeRect.anchorMax = new Vector2(0.5f, 1f);
                cameraModeRect.pivot = new Vector2(0.5f, 1f);
                cameraModeRect.anchoredPosition = new Vector2(0f, -18f);
                cameraModeRect.sizeDelta = new Vector2(420f, 36f);

                var cameraModeIndicator = canvasGo.AddComponent<CameraModeIndicator>();
                cameraModeIndicator.Configure(followCamera, cameraModeText);
            }

            var replaySystem = raceManager.gameObject.AddComponent<RaceReplaySystem>();
            replaySystem.Configure(raceManager, followCamera, playerCar, canvasGo.transform);

            var minimapPanel = CreateMinimapPanel(canvasGo.transform, new Vector2(20f, 96f));
            var minimap = minimapPanel.AddComponent<RaceMinimap>();
            minimap.Configure(raceManager, trackBuilder.CenterlinePoints);
            minimapPanel.transform.SetAsLastSibling();

            var podiumSequence = raceManager.gameObject.AddComponent<RacePodiumSequence>();
            podiumSequence.Configure(
                raceManager,
                trackBuilder,
                playerCar,
                followCamera,
                raceUi,
                finishPanel,
                jumpHint,
                replaySystem);
        }

        static void ConfigureScreenTextRect(RectTransform rect, float height, Vector2 anchoredPosition)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(40f, anchoredPosition.y - height * 0.5f);
            rect.offsetMax = new Vector2(-40f, anchoredPosition.y + height * 0.5f);
        }

        Text CreateCenteredText(Transform parent, string name, string content, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        GameObject CreateMinimapPanel(Transform parent, Vector2 anchoredPos)
        {
            var go = new GameObject("Minimap");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(300f, 210f);
            go.SetActive(true);
            return go;
        }

        static GameObject CreateCommentaryPanel(Transform parent, out Text subtitleText)
        {
            var panel = new GameObject("CommentaryPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 272f);
            panelRect.sizeDelta = new Vector2(980f, 92f);
            panel.AddComponent<Image>().color = new Color(0.02f, 0.05f, 0.1f, 0.82f);
            panel.SetActive(false);

            var labelGo = new GameObject("CommentaryLabel");
            labelGo.transform.SetParent(panel.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -6f);
            labelRect.sizeDelta = new Vector2(-32f, 22f);
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.UpperCenter;
            label.color = new Color(1f, 0.55f, 0.85f, 0.9f);
            label.text = "COMMENTARY";
            label.raycastTarget = false;

            var subtitleGo = new GameObject("CommentarySubtitle");
            subtitleGo.transform.SetParent(panel.transform, false);
            var subtitleRect = subtitleGo.AddComponent<RectTransform>();
            subtitleRect.anchorMin = Vector2.zero;
            subtitleRect.anchorMax = Vector2.one;
            subtitleRect.offsetMin = new Vector2(24f, 12f);
            subtitleRect.offsetMax = new Vector2(-24f, -28f);
            subtitleText = subtitleGo.AddComponent<Text>();
            subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitleText.fontSize = 30;
            subtitleText.fontStyle = FontStyle.Bold;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.85f, 1f, 1f);
            subtitleText.text = string.Empty;
            subtitleText.raycastTarget = false;

            return panel;
        }

        Text CreatePodiumJumpHint(Transform parent)
        {
            var go = new GameObject("PodiumJumpHint");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -42f);
            rect.sizeDelta = new Vector2(920f, 48f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.92f, 0.35f);
            text.text = string.Empty;
            text.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            go.SetActive(false);
            return text;
        }

        Text CreateBottomCenterText(Transform parent, string name, Vector2 anchoredPos, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(320f, 72f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = string.Empty;
            text.fontStyle = FontStyle.Bold;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        Text CreateText(Transform parent, string name, Vector2 anchoredPos, TextAnchor anchor, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rect.anchorMax = anchor == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rect.pivot = anchor == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(400f, 60f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = name;
            return text;
        }

        static void EnsureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var es = new GameObject("EventSystem");
                eventSystem = es.AddComponent<EventSystem>();
            }

            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
                Object.Destroy(legacyModule);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            eventSystem.sendNavigationEvents = false;
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
