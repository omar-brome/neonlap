using System.Collections.Generic;
using NeonLap.Audio;
using NeonLap.Camera;
using NeonLap.Core.Content;
using NeonLap.Input;
using NeonLap.Core;
using NeonLap.Services;
using NeonLap.Services.Race;
using NeonLap.Environment;
using NeonLap.Race;
using NeonLap.Track;
using NeonLap.UI;
using NeonLap.Vehicle;
using NeonLap.Rendering;
using NeonLap.VFX;
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

        [Header("Production (optional prefabs)")]
        [SerializeField] NeonLapContentCatalog contentCatalog;

        static readonly Color PlayerBodyColor = new(0.1f, 0.35f, 0.45f);
        static readonly Color PlayerAccentColor = new(0f, 3.5f, 4f);

        GameObject playerCar;
        OvalTrackBuilder trackBuilder;
        RaceManager raceManager;
        PoliceChaseSystem policeChase;
        RaceEnvironmentBuilder environmentBuilder;

        void Awake()
        {
            GameAudioSettings.Load();
            NeonLap.Audio.NeonLapAudioLibrary.Preload();
            GameQualitySettings.Load();
            GameDifficultySettings.Load();
            GameLapSettings.Load();
            GamePoliceSettings.Load();
            GameHapticsSettings.Load();
            GameMinimapSettings.Load();
            GameRaceModeSettings.Load();
            GameTeamRaceSettings.Load();
            GameTrackOptions.Load();
            GameAccessibilitySettings.Load();
            NeonLapServicesBootstrap.EnsureInitialized();
            contentCatalog ??= NeonLapContentCatalog.LoadDefault();
            EnsureRuntimeDefaults();
            EnsurePhysicsLayerCollisions();
            EnsureGameManager();
            ResolveActiveTrackDefinition();
            ApplyEnvironmentSettings();
            BuildTrack();
            EnsurePerformanceSystems();
            ApplyTrackVisualTheme();
            WetTrackSurfaceController.Register(trackSurfaceMaterial);
            var nightLevelIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            NightTrackVisuals.Apply(trackEdgeMaterial, nightLevelIndex);
            BuildEnvironment();
            var modeRules = GameRaceModeSettings.Rules;
            if (modeRules.SpawnTrackObstacles)
            {
                BuildTrackHazards();
                BuildBananaHazards();
            }

            BuildNitroPickups();
            BuildFuelPads();
            if (RaceModeDamageRules.GetDamageProfile().SpawnRepairPads)
                BuildRepairPads();
            SpawnRacers();
            ConfigureElevationProbes();
            SetupCamera();
            SetupRaceSystems();
            if (GameRaceModeSettings.Rules.SpawnHelicopter)
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
            var mainCamera = UnityEngine.Camera.main;
            TrackThemeApplicator.Apply(trackDefinition, preset, mainCamera);
            EnsureDynamicWeather(mainCamera != null ? mainCamera.transform : null, preset);
        }

        void ApplyTrackVisualTheme()
        {
            if (trackDefinition == null)
                return;

            var profile = TrackThemeProfile.ForDefinition(trackDefinition);
            TrackRoadMarkingBuilder.ApplyAsphaltLook(trackSurfaceMaterial, trackEdgeMaterial, profile.AsphaltColor,
                profile.CurbColor);
            TrackRoadMarkingBuilder.ApplyNeonEdgeLook(trackEdgeMaterial, profile.EdgeEmissionColor,
                profile.EdgeBaseColor);
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
            if (activeTrack == null)
                return;

            trackDefinition = activeTrack;
            trackDefinition.layout = TrackLayoutUtility.LayoutForLevelIndex(GameManager.Instance.CurrentLevelIndex);
        }

        void BuildEnvironment()
        {
            var envGo = new GameObject("RaceEnvironment");
            environmentBuilder = envGo.AddComponent<RaceEnvironmentBuilder>();
            environmentBuilder.Build(trackDefinition, trackBuilder.StartPosition, trackBuilder.StartRotation,
                trackBuilder.EnvironmentHalfExtents, GameQualitySettings.Preset, trackBuilder.CenterlinePoints);
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
            pickupBuilder.BuildAiNitroZones(trackBuilder.AiWaypointTransforms, trackDefinition);
        }

        void BuildFuelPads()
        {
            var padBuilder = trackBuilder.gameObject.AddComponent<FuelPadPickupBuilder>();
            padBuilder.Build(trackBuilder.AiWaypointTransforms, trackDefinition);
        }

        void BuildRepairPads()
        {
            var padBuilder = trackBuilder.gameObject.AddComponent<RepairPadPickupBuilder>();
            padBuilder.Build(trackBuilder.AiWaypointTransforms, trackDefinition);
        }

        void EnsurePerformanceSystems()
        {
            if (trackBuilder == null)
                return;

            var raceRoot = trackBuilder.transform;
            VehicleDamageDebrisPool.Ensure(raceRoot);
            BananaHazardPool.Ensure(raceRoot);
            NeonTrackEdgePulseDriver.Ensure(trackEdgeMaterial);
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

        void ConfigureElevationProbes()
        {
            if (trackDefinition == null || !TrackLayoutUtility.HasElevation(trackDefinition.layout))
                return;

            foreach (var probe in FindObjectsByType<VehicleGroundProbe>(FindObjectsInactive.Exclude))
                probe.SetRayLength(8f);
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
            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            if (GameRaceModeSettings.IsCareer)
                PlayerGarageStore.EnsureLegalBuildForTrack(trackIndex);

            var selectedBuild = PlayerGarageStore.GetSelectedBuild();
            var playerProfile = selectedBuild != null && selectedBuild.profile != null
                ? selectedBuild.profile
                : PlayerVehicleProfileStore.GetSelectedProfile();
            if (playerProfile == null)
                playerProfile = vehicleProfile;
            Color playerBody;
            Color playerAccent;
            if (selectedBuild != null)
                VehicleCustomizationStore.GetResolvedColors(selectedBuild, out playerBody, out playerAccent);
            else
            {
                playerBody = PlayerVehicleProfileStore.GetBodyColor(PlayerVehicleProfileStore.SelectedKind);
                playerAccent = PlayerVehicleProfileStore.GetAccentColor(PlayerVehicleProfileStore.SelectedKind);
            }

            PlayerVehicleProfileStore.SyncFromGarageSelection(selectedBuild);
            playerCar = SpawnCar("HoverCar", playerProfile, true, playerBody, playerAccent);
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
            ApplyPlayerModeSettings(playerCar);

            if (GameRaceModeSettings.IsTeamRace)
            {
                var playerTeam = playerCar.GetComponent<RacerTeamMarker>();
                if (playerTeam == null)
                    playerTeam = playerCar.AddComponent<RacerTeamMarker>();
                playerTeam.Configure(GameTeamRaceSettings.PlayerTeam);
            }

            var modeRules = GameRaceModeSettings.Rules;
            var rivalCount = 0;
            if (spawnAiRivals && modeRules.SpawnAiRivals)
            {
                rivalCount = Mathf.Min(
                    modeRules.AiRivalCount > 0 ? modeRules.AiRivalCount : aiRivalCount,
                    GameQualitySettings.Preset.AiRivalCount);
            }
            else if (spawnAiRivals && GameRaceModeSettings.IsTimeTrial)
            {
                TimeTrialSettings.Load();
                rivalCount = Mathf.Min(TimeTrialSettings.RivalCount, GameQualitySettings.Preset.AiRivalCount);
            }

            if (rivalCount <= 0)
                return;
            for (var rivalIndex = 0; rivalIndex < rivalCount; rivalIndex++)
            {
                var gridIndex = rivalIndex >= playerGridIndex ? rivalIndex + 1 : rivalIndex;
                var spawnPosition = GetGridSpawnPosition(gridIndex, gridColumns, columnSpacing, rowSpacing);
                var rivalProfile = RivalIdentityCatalog.Get(rivalIndex);
                var bodyColor = rivalProfile.BodyColor;
                var accentColor = rivalProfile.AccentColor;
                var aiCar = SpawnCar("AIRival_" + rivalProfile.ShortName, vehicleProfile, false, bodyColor, accentColor);
                if (aiCar == null)
                    continue;

                aiCar.transform.SetPositionAndRotation(spawnPosition, trackBuilder.StartRotation);

                var ai = aiCar.GetComponent<AIVehicleController>();
                if (ai == null)
                    continue;

                ai.SetWaypoints(waypoints);
                ai.ConfigureTrack(trackDefinition != null ? trackDefinition.trackWidth * 0.5f : 7f);

                var difficulty = GameDifficultySettings.Preset;
                var personality = AIPersonalityCatalog.GetForRivalIndex(rivalIndex);
                ai.ApplyDifficulty(difficulty);
                ai.ApplyPersonality(personality);
                ai.SetRivalVariation(rivalIndex * 2,
                    difficulty.RivalSpeedBase + rivalIndex * difficulty.RivalSpeedStep);
                ai.SetPlayerTarget(playerCar.transform);

                var identity = aiCar.GetComponent<RivalIdentity>();
                if (identity == null)
                    identity = aiCar.AddComponent<RivalIdentity>();
                identity.Configure(rivalIndex, rivalProfile);

                var grudge = aiCar.GetComponent<RivalGrudgeController>();
                if (grudge == null)
                    grudge = aiCar.AddComponent<RivalGrudgeController>();
                grudge.Configure(ai, playerCar.transform);

                if (GameRaceModeSettings.IsTeamRace)
                {
                    var team = aiCar.GetComponent<RacerTeamMarker>();
                    if (team == null)
                        team = aiCar.AddComponent<RacerTeamMarker>();
                    team.Configure(rivalIndex % 2 == 0 ? RaceTeam.Blue : RaceTeam.Red);
                }

                var combat = aiCar.GetComponent<AICombatController>();
                if (combat == null)
                    combat = aiCar.AddComponent<AICombatController>();
                combat.Configure(playerCar.transform, rivalIndex, personality);

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

        GameObject SpawnCar(string carName, VehicleProfile profile, bool isPlayer, Color bodyColor, Color accentColor)
        {
            var request = new NeonLapCarSpawnRequest
            {
                CarName = carName,
                Profile = profile,
                IsPlayer = isPlayer,
                BodyColor = bodyColor,
                AccentColor = accentColor,
                BodyMaterial = carBodyMaterial,
                AccentMaterial = carAccentMaterial,
                InputActions = inputActions,
                DamageMode = RaceModeDamageRules.GetDamageMode(),
            };

            return NeonLapCarSpawner.Spawn(in request, contentCatalog, BuildCar);
        }

        GameObject BuildCar(in NeonLapCarSpawnRequest request) =>
            BuildCar(request.CarName, request.Profile, request.IsPlayer, request.BodyColor, request.AccentColor);

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

            var buildArgs = isPlayer
                ? VehicleCustomizationStore.CreateBuildArgs(carBodyMaterial, carAccentMaterial, bodyColor, accentColor, true)
                : new HoverCarVisualBuilder.BuildArgs(carBodyMaterial, carAccentMaterial, bodyColor, accentColor, false);
            HoverCarVisualBuilder.Build(car.transform, buildArgs);

            var appearance = car.AddComponent<VehicleAppearance>();
            appearance.Configure(buildArgs);
            var hoverPods = car.AddComponent<VehicleHoverPodSystem>();
            hoverPods.Configure(isPlayer);
            car.AddComponent<VehicleWheelSteerVisual>();
            car.AddComponent<VehicleSlipEffect>();
            car.AddComponent<VehicleTaillightController>();
            car.AddComponent<VehicleDriftMarkEmitter>();
            var damageProfile = RaceModeDamageRules.GetDamageProfile();
            var damageSystem = car.AddComponent<VehicleDamageSystem>();
            damageSystem.Configure(damageProfile);
            car.AddComponent<VehicleHealthSystem>();
            car.AddComponent<RepairPadLapTracker>();

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
            car.AddComponent<VehicleTrackZoneResponder>();

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
                car.AddComponent<VehicleCombatShield>();
                car.AddComponent<PlayerCombatController>();
                car.AddComponent<PlayerNitroController>();
                var controller = car.AddComponent<VehicleController>();
                controller.Configure(profile, inputReader);
                var barrelRoll = car.AddComponent<VehicleBarrelRoll>();
                barrelRoll.Configure(profile);
                VehicleHapticsController.Setup(car);
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
                car.AddComponent<RaceShortcutTracker>();
                car.AddComponent<RaceScoreSystem>();
                car.AddComponent<DriftZonePresence>();
                VehicleAudioController.Setup(car);
                car.AddComponent<VehicleCollisionAudio>();
                car.AddComponent<VehicleFuelSystem>();
                AddDriftTrails(car);
            }
            else
            {
                car.AddComponent<VehicleNitroBoost>();
                var ai = car.AddComponent<AIVehicleController>();
                SetPrivateField(ai, "profile", profile);
                car.AddComponent<AICombatController>();
                VehicleAudioController.Setup(car, spatial3D: true, useRivalMix: true);
                car.AddComponent<VehicleCollisionAudio>();
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

        static void EnsureAudioListener()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            if (cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();
        }

        void SetupCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null || playerCar == null)
                return;

            EnsureAudioListener();

            var follow = cam.gameObject.GetComponent<FollowCamera>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.Target = playerCar.transform;
            if (cam.gameObject.GetComponent<DriftCameraShake>() == null)
                cam.gameObject.AddComponent<DriftCameraShake>();
            VFX.SpeedLinesPostEffect.Ensure(cam, playerCar.transform);

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
            patrol.Configure(playerCar.transform, visual, raceManager, policeChase);
        }

        void ApplyPlayerModeSettings(GameObject player)
        {
            if (player == null)
                return;

            var rules = GameRaceModeSettings.Rules;
            var fuel = player.GetComponent<VehicleFuelSystem>();
            if (fuel != null)
            {
                fuel.Configure(GameFuelEconomy.GetTankDuration(GameLapSettings.CurrentLaps), null);
                fuel.SetInfinite(rules.InfiniteFuel);
            }
        }

        void SetupRaceSystems()
        {
            var rules = GameRaceModeSettings.Rules;
            var raceRoot = trackBuilder.transform;
            raceManager = raceRoot.gameObject.AddComponent<RaceManager>();
            if (rules.SpawnAiRivals)
                RivalBlockerCoordinator.Ensure(raceManager);
            SetPrivateField(raceManager, "totalLaps", GameLapSettings.CurrentLaps);
            SetPrivateField(raceManager, "trackBuilder", trackBuilder);
            SetPrivateField(raceManager, "playerReset", playerCar.GetComponent<VehicleReset>());
            raceManager.SetPlayerLapFinishEnabled(rules.UseLapFinish);

            if (raceRoot.GetComponent<DriftMarkSystem>() == null)
                raceRoot.gameObject.AddComponent<DriftMarkSystem>();

            policeChase = null;
            TimeTrialSettings.Load();
            var allowPolice = rules.AllowPolice || rules.ForcePolice
                || ((GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel)
                    && TimeTrialSettings.PoliceEnabled);
            if (allowPolice)
            {
                policeChase = raceRoot.gameObject.AddComponent<PoliceChaseSystem>();
                policeChase.Configure(raceManager, playerCar, trackBuilder, vehicleProfile, carBodyMaterial,
                    carAccentMaterial);
                if (rules.ForcePolice)
                    policeChase.SetOutrunMode(true);
                PoliceChaseAudio.Setup(raceRoot, raceManager, policeChase);
            }

            DynamicRaceMusicController.Setup(raceRoot, raceManager, policeChase);
            BlackoutLapController.Setup(raceRoot, raceManager, trackEdgeMaterial);
            RaceAudioController.Setup(raceRoot, raceManager);
            RaceSessionReporter.Setup(raceManager, playerCar);
            RaceAchievementBridge.Setup(raceManager, playerCar);
            RaceMetagameBridge.Setup(raceManager, playerCar);
            RaceNegativeAchievementBridge.Setup(raceManager, playerCar);
            RaceLeaderboardBridge.Setup(raceManager, playerCar);
            FindAnyObjectByType<DynamicWeatherSystem>()?.BindRace(raceManager);

            var aiWaypoints = trackBuilder.AiWaypointTransforms;
            if (aiWaypoints.Count > 0 && aiWaypoints[0] != null)
                CatchUpGhostController.Setup(aiWaypoints[0].parent, raceManager);

            var trackLevelIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            playerCar.GetComponent<RaceShortcutTracker>()?.Configure(trackLevelIndex);
            EnsureAudioListener();
            NeonLap.VFX.VehicleHornSfx.Setup(playerCar);

            if (!createUi)
            {
                playerCar.GetComponent<PlayerCombatController>()
                    ?.Configure(playerCar.GetComponent<PlayerInputReader>(), raceManager);
                environmentBuilder?.ConfigureScoreboard(raceManager, policeChase);
                return;
            }

            BuildRaceUi();
            BuildPauseMenu(raceRoot.gameObject);
            environmentBuilder?.ConfigureScoreboard(raceManager, policeChase);
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

            var pauseTitle = CreateCenteredText(pausePanel.transform, "PauseTitle", "PAUSED", 52);
            pauseTitle.color = new Color(0.45f, 1f, 1f);
            pauseTitle.fontStyle = FontStyle.Bold;
            var pauseTitleRect = pauseTitle.GetComponent<RectTransform>();
            pauseTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            pauseTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            pauseTitleRect.sizeDelta = new Vector2(520f, 64f);
            pauseTitleRect.anchoredPosition = new Vector2(0f, 150f);

            var pauseStatus = CreateCenteredText(pausePanel.transform, "PauseStatus", string.Empty, 22);
            pauseStatus.alignment = TextAnchor.MiddleCenter;
            pauseStatus.color = new Color(0.8f, 0.9f, 1f);
            var pauseStatusRect = pauseStatus.GetComponent<RectTransform>();
            pauseStatusRect.anchorMin = new Vector2(0.5f, 0.5f);
            pauseStatusRect.anchorMax = new Vector2(0.5f, 0.5f);
            pauseStatusRect.sizeDelta = new Vector2(720f, 72f);
            pauseStatusRect.anchoredPosition = new Vector2(0f, 95f);

            var resume = CreateMenuButton(pausePanel.transform, "ResumeButton", "RESUME", new Vector2(0f, 70f));
            var restart = CreateMenuButton(pausePanel.transform, "RestartButton", "RESTART", new Vector2(0f, 10f));
            var controls = CreateMenuButton(pausePanel.transform, "ControlsButton", "CONTROLS", new Vector2(0f, -50f));
            var quit = CreateMenuButton(pausePanel.transform, "QuitButton", "MAIN MENU", new Vector2(0f, -110f));

            var controlsPanel = ControlsOverlayBuilder.Build(canvasGo.transform, "PauseControlsPanel",
                out var controlsBack, "BACK TO PAUSE");

            var pause = canvasGo.AddComponent<PauseMenuController>();
            pause.Configure(pausePanel, raceManager, resume, restart, controls, quit, controlsPanel, controlsBack,
                pauseStatus);

            var modeRules = GameRaceModeSettings.Rules;
            if (modeRules.UseTimeTrialGhost || modeRules.UseGhostDuel)
            {
                var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
                var ghostExport = CreateMenuButton(pausePanel.transform, "GhostExportButton", "EXPORT GHOST",
                    new Vector2(0f, -170f));
                var ghostImport = CreateMenuButton(pausePanel.transform, "GhostImportButton", "IMPORT GHOST",
                    new Vector2(0f, -230f));
                var ghostShareStatus = CreateCenteredText(pausePanel.transform, "GhostShareStatus",
                    "Share PB ghosts via clipboard", 18);
                ghostShareStatus.alignment = TextAnchor.MiddleCenter;
                ghostShareStatus.color = new Color(0.75f, 0.88f, 1f);
                var shareStatusRect = ghostShareStatus.GetComponent<RectTransform>();
                shareStatusRect.anchorMin = new Vector2(0.5f, 0.5f);
                shareStatusRect.anchorMax = new Vector2(0.5f, 0.5f);
                shareStatusRect.sizeDelta = new Vector2(520f, 48f);
                shareStatusRect.anchoredPosition = new Vector2(0f, -290f);
                GhostShareController.Setup(pausePanel.transform, trackIndex, ghostExport, ghostImport, ghostShareStatus);
            }
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
            Text pbLapHeader = null;
            Text pbLapTimer = null;
            if (GameRaceModeSettings.IsTimeTrial)
            {
                pbLapHeader = CreateText(canvasGo.transform, "PbLapHeader", new Vector2(-250f, -12f), TextAnchor.UpperRight, 20);
                pbLapHeader.alignment = TextAnchor.UpperRight;
                pbLapHeader.color = new Color(0.65f, 0.85f, 1f, 0.9f);
                pbLapHeader.text = "PB LAP";
                var pbHeaderRect = pbLapHeader.GetComponent<RectTransform>();
                pbHeaderRect.sizeDelta = new Vector2(160f, 24f);

                pbLapTimer = CreateText(canvasGo.transform, "PbLapTimer", new Vector2(-250f, -38f), TextAnchor.UpperRight, 40);
                pbLapTimer.alignment = TextAnchor.UpperRight;
                pbLapTimer.fontStyle = FontStyle.Bold;
                pbLapTimer.color = new Color(0.55f, 0.95f, 1f);
                pbLapTimer.text = "--:--.--";
                var pbTimerRect = pbLapTimer.GetComponent<RectTransform>();
                pbTimerRect.sizeDelta = new Vector2(220f, 48f);

                var lapTimerRect = lapTimer.GetComponent<RectTransform>();
                lapTimerRect.anchoredPosition = new Vector2(-20f, -72f);
            }

            var sectorSplit = CreateText(canvasGo.transform, "SectorSplit", new Vector2(-20f, -52f), TextAnchor.UpperRight, 22);
            if (GameRaceModeSettings.IsTimeTrial)
            {
                var sectorRect = sectorSplit.GetComponent<RectTransform>();
                sectorRect.anchoredPosition = new Vector2(-20f, -104f);
            }
            sectorSplit.color = new Color(0.75f, 0.88f, 1f);
            var ghostDelta = CreateText(canvasGo.transform, "GhostDelta", new Vector2(0f, -24f), TextAnchor.UpperCenter, 30);
            ghostDelta.alignment = TextAnchor.UpperCenter;
            ghostDelta.color = new Color(0.55f, 0.95f, 1f);
            ghostDelta.fontStyle = FontStyle.Bold;
            var ghostDeltaRect = ghostDelta.GetComponent<RectTransform>();
            ghostDeltaRect.sizeDelta = new Vector2(320f, 40f);
            ghostDelta.gameObject.SetActive(false);
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
            var playerControllerForHud = playerCar.GetComponent<VehicleController>();
            dashboardCluster.Configure(raceManager, playerControllerForHud);
            dashboardCluster.ApplyProfileSkin(playerControllerForHud != null ? playerControllerForHud.Profile : null);

            var commentaryPanel = CreateCommentaryPanel(canvasGo.transform, out var commentarySubtitle);

            var directionGuide = canvasGo.AddComponent<RaceDirectionGuide>();
            var trackHalfWidth = trackDefinition != null ? trackDefinition.trackWidth * 0.5f : 7f;
            directionGuide.Configure(
                raceManager,
                playerCar.transform,
                trackBuilder.CheckpointTransforms,
                canvasGo.transform,
                trackBuilder.CenterlinePoints,
                trackHalfWidth);

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
            finishPanelRect.sizeDelta = new Vector2(0f, 520f);
            finishPanelRect.anchoredPosition = Vector2.zero;
            finishPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            finishPanel.SetActive(false);

            var jumpHint = CreatePodiumJumpHint(canvasGo.transform);

            var finishTitle = CreateText(finishPanel.transform, "FinishTitle", new Vector2(0f, 430f), TextAnchor.LowerCenter, 64);
            finishTitle.alignment = TextAnchor.LowerCenter;
            finishTitle.color = new Color(0.4f, 1f, 1f);
            var finishTitleRect = finishTitle.GetComponent<RectTransform>();
            finishTitleRect.anchorMin = new Vector2(0.5f, 0f);
            finishTitleRect.anchorMax = new Vector2(0.5f, 0f);
            finishTitleRect.pivot = new Vector2(0.5f, 0f);
            finishTitleRect.anchoredPosition = new Vector2(0f, 430f);
            finishTitleRect.sizeDelta = new Vector2(900f, 80f);

            var finishStarOne = CreateText(finishPanel.transform, "FinishStar1", new Vector2(-72f, 360f), TextAnchor.LowerCenter, 56);
            finishStarOne.text = "☆";
            finishStarOne.color = new Color(0.28f, 0.32f, 0.42f);
            var finishStarOneRect = finishStarOne.GetComponent<RectTransform>();
            finishStarOneRect.sizeDelta = new Vector2(72f, 72f);

            var finishStarTwo = CreateText(finishPanel.transform, "FinishStar2", new Vector2(0f, 360f), TextAnchor.LowerCenter, 56);
            finishStarTwo.text = "☆";
            finishStarTwo.color = new Color(0.28f, 0.32f, 0.42f);
            var finishStarTwoRect = finishStarTwo.GetComponent<RectTransform>();
            finishStarTwoRect.sizeDelta = new Vector2(72f, 72f);

            var finishStarThree = CreateText(finishPanel.transform, "FinishStar3", new Vector2(72f, 360f), TextAnchor.LowerCenter, 56);
            finishStarThree.text = "☆";
            finishStarThree.color = new Color(0.28f, 0.32f, 0.42f);
            var finishStarThreeRect = finishStarThree.GetComponent<RectTransform>();
            finishStarThreeRect.sizeDelta = new Vector2(72f, 72f);

            var finishRewardsLine = CreateText(finishPanel.transform, "FinishRewardsLine", new Vector2(0f, 312f), TextAnchor.LowerCenter, 24);
            finishRewardsLine.alignment = TextAnchor.LowerCenter;
            finishRewardsLine.color = new Color(1f, 0.92f, 0.35f);
            var finishRewardsRect = finishRewardsLine.GetComponent<RectTransform>();
            finishRewardsRect.sizeDelta = new Vector2(960f, 36f);

            var finishPlacementLine = CreateText(finishPanel.transform, "FinishPlacementLine", new Vector2(0f, 282f), TextAnchor.LowerCenter, 22);
            finishPlacementLine.alignment = TextAnchor.LowerCenter;
            finishPlacementLine.color = new Color(0.75f, 0.95f, 1f);
            var finishPlacementRect = finishPlacementLine.GetComponent<RectTransform>();
            finishPlacementRect.sizeDelta = new Vector2(960f, 32f);

            var finishScreenView = finishPanel.AddComponent<RaceFinishScreenView>();
            finishScreenView.Configure(finishStarOne, finishStarTwo, finishStarThree, finishRewardsLine, finishPlacementLine);

            var finishDetail = CreateText(finishPanel.transform, "FinishDetail", new Vector2(0f, 248f), TextAnchor.LowerCenter, 22);
            finishDetail.alignment = TextAnchor.LowerCenter;
            finishDetail.color = Color.white;
            var finishDetailRect = finishDetail.GetComponent<RectTransform>();
            finishDetailRect.anchorMin = new Vector2(0.5f, 0f);
            finishDetailRect.anchorMax = new Vector2(0.5f, 0f);
            finishDetailRect.pivot = new Vector2(0.5f, 0f);
            finishDetailRect.anchoredPosition = new Vector2(0f, 248f);
            finishDetailRect.sizeDelta = new Vector2(960f, 64f);

            var finishBreakdown = CreateText(finishPanel.transform, "FinishBreakdown", new Vector2(0f, 188f), TextAnchor.LowerCenter, 20);
            finishBreakdown.alignment = TextAnchor.LowerCenter;
            finishBreakdown.color = new Color(0.55f, 0.95f, 1f);
            var finishBreakdownRect = finishBreakdown.GetComponent<RectTransform>();
            finishBreakdownRect.anchorMin = new Vector2(0.5f, 0f);
            finishBreakdownRect.anchorMax = new Vector2(0.5f, 0f);
            finishBreakdownRect.pivot = new Vector2(0.5f, 0f);
            finishBreakdownRect.anchoredPosition = new Vector2(0f, 188f);
            finishBreakdownRect.sizeDelta = new Vector2(720f, 100f);
            finishBreakdown.gameObject.SetActive(false);

            var nextLevelButton = CreateMenuButton(finishPanel.transform, "NextLevelButton", "NEXT TRACK", new Vector2(0f, 120f));
            nextLevelButton.gameObject.SetActive(false);
            var nextLevelLabel = nextLevelButton.GetComponentInChildren<Text>();

            var finishRestart = CreateMenuButton(finishPanel.transform, "FinishRestartButton", "RETRY", new Vector2(0f, 60f));
            var finishRestartLabel = finishRestart.GetComponentInChildren<Text>();
            var itchExportButton = CreateMenuButton(finishPanel.transform, "ItchExportButton", "COPY PB FOR ITCH",
                new Vector2(0f, 60f));
            itchExportButton.gameObject.SetActive(false);
            var finishMenu = CreateMenuButton(finishPanel.transform, "FinishMenuButton", "MAIN MENU", new Vector2(0f, 30f));

            var finishMenuController = canvasGo.AddComponent<FinishMenuController>();
            finishMenuController.Configure(
                raceManager,
                finishPanel,
                finishMenu,
                finishRestart,
                nextLevelButton,
                nextLevelLabel,
                finishRestartLabel);
            finishMenuController.ConfigureItchExportButton(itchExportButton);

            var ghostToggle = CreateMenuButton(canvasGo.transform, "GhostToggleButton", "GHOST ON", new Vector2(-20f, -100f));
            var ghostToggleRect = ghostToggle.GetComponent<RectTransform>();
            ghostToggleRect.anchorMin = new Vector2(1f, 1f);
            ghostToggleRect.anchorMax = new Vector2(1f, 1f);
            ghostToggleRect.pivot = new Vector2(1f, 1f);
            ghostToggleRect.anchoredPosition = new Vector2(-20f, -100f);
            ghostToggleRect.sizeDelta = new Vector2(180f, 42f);
            var ghostToggleLabel = ghostToggle.GetComponentInChildren<Text>();
            ghostToggle.gameObject.SetActive(false);

            raceUi.Configure(
                raceManager,
                playerCar.GetComponent<VehicleController>(),
                lapText,
                lapTimer,
                pbLapHeader,
                pbLapTimer,
                sectorSplit,
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
                finishDetail,
                finishBreakdown,
                finishScreenView);

            var voiceover = CommentaryVoiceover.Setup(raceManager.transform);
            var commentary = canvasGo.AddComponent<RaceCommentarySystem>();
            commentary.Configure(raceManager, commentarySubtitle, commentaryPanel, voiceover);
            StadiumCrowdAudio.Setup(raceManager.transform, commentary);

            var touchUi = TouchDrivingUI.Build(canvasGo.transform);
            var playerInput = playerCar.GetComponent<PlayerInputReader>();
            var compositeInput = CompositeVehicleInputProvider.Setup(playerCar, playerInput, touchUi);
            var playerController = playerCar.GetComponent<VehicleController>();
            var activeProfile = playerController != null && playerController.Profile != null
                ? playerController.Profile
                : vehicleProfile;
            playerController.Configure(activeProfile, compositeInput);
            playerCar.GetComponent<PlayerCombatController>()?.Configure(playerInput, raceManager);

            var photoHint = CreateText(canvasGo.transform, "PhotoModeHint", new Vector2(0f, 120f), TextAnchor.LowerCenter, 20);
            photoHint.alignment = TextAnchor.LowerCenter;
            photoHint.color = new Color(0.75f, 0.88f, 1f, 0.95f);
            var photoHintRect = photoHint.GetComponent<RectTransform>();
            photoHintRect.anchorMin = new Vector2(0.5f, 0f);
            photoHintRect.anchorMax = new Vector2(0.5f, 0f);
            photoHintRect.pivot = new Vector2(0.5f, 0f);
            photoHintRect.anchoredPosition = new Vector2(0f, 120f);
            photoHintRect.sizeDelta = new Vector2(900f, 48f);
            photoHint.gameObject.SetActive(false);

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

            var mainCam = followCamera != null ? followCamera.GetComponent<UnityEngine.Camera>() : UnityEngine.Camera.main;
            Camera.CameraSpectacleDirector.Setup(mainCam, followCamera, playerCar.transform, policeChase, raceManager);

            if (followCamera != null)
            {
                var photoMode = canvasGo.AddComponent<PhotoModeController>();
                photoMode.Configure(followCamera, raceManager, playerCar, photoHint, photoHint.gameObject, canvasGo);
            }

            var minimapPanel = CreateMinimapPanel(canvasGo.transform, new Vector2(20f, 96f));
            var minimap = minimapPanel.AddComponent<RaceMinimap>();
            minimap.Configure(
                raceManager,
                trackBuilder.CenterlinePoints,
                trackBuilder.ShortcutPaths,
                policeChase);
            minimapPanel.transform.SetAsLastSibling();
            RivalStandingsHud.Setup(canvasGo.transform, raceManager);

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

            var modeRules = GameRaceModeSettings.Rules;

            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var showGhostHud = modeRules.UseTimeTrialGhost || modeRules.UseGhostDuel;
            if (showGhostHud)
            {
                ghostDelta.gameObject.SetActive(true);
                ghostToggle.gameObject.SetActive(true);
            }

            var ghostHud = GhostHudController.Setup(
                raceManager,
                playerCar.transform,
                ghostDelta,
                ghostToggle,
                ghostToggleLabel);

            dashboardCluster.Configure(
                raceManager,
                playerControllerForHud,
                ghostHud,
                scoreSystem);
            dashboardCluster.ApplyProfileSkin(playerControllerForHud != null ? playerControllerForHud.Profile : null);
            collisionHud.Configure(proximitySensor, ghostHud, playerCar.transform);
            directionGuide.BindGhostHud(ghostHud);
            minimap.BindGhostHud(ghostHud);

            TimeTrialController.Setup(
                raceManager,
                replaySystem,
                raceUi,
                podiumSequence,
                trackBuilder,
                playerCar,
                carBodyMaterial,
                carAccentMaterial,
                ghostHud);

            GhostDuelController.Setup(
                raceManager,
                replaySystem,
                raceUi,
                trackBuilder,
                playerCar,
                carBodyMaterial,
                carAccentMaterial,
                ghostHud);

            if (!modeRules.UsePodiumSequence && podiumSequence != null)
                podiumSequence.enabled = false;

            EliminationModeController.Setup(raceManager, raceUi, podiumSequence);
            DemolitionModeController.Setup(raceManager, raceUi);
            ChaseModeController.Setup(raceManager, raceUi, podiumSequence, playerCar, policeChase);

            if (jumpHint != null)
                jumpHint.text = GameRaceModeSettings.IsStuntFreestyle
                    ? "SPACE / DRIFT — CELEBRATE   •   P — PHOTO MODE   •   ENTER — END SESSION"
                    : "SPACE / DRIFT — CELEBRATE   •   P — PHOTO MODE   •   ENTER — CONTINUE";
            ScoreAttackModeController.Setup(raceManager, raceUi, scoreSystem, podiumSequence);
            StuntFreestyleController.Setup(raceManager, raceUi, playerCar);
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
