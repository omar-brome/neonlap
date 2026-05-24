using NeonLap.Audio;
using NeonLap.Camera;
using NeonLap.Core;
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

        void Awake()
        {
            ApplyEnvironmentSettings();
            BuildTrack();
            SpawnRacers();
            SetupCamera();
            SetupRaceSystems();
        }

        void ApplyEnvironmentSettings()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.02f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.04f, 0f, 0.08f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.008f;

            var light = FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.intensity = 0.35f;
                light.color = new Color(0.7f, 0.8f, 1f);
            }
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
            const float columnSpacing = 2.6f;
            const float rowSpacing = 4.5f;
            const int playerGridIndex = 2;

            var waypointList = trackBuilder.AiWaypointTransforms;
            var waypoints = new Transform[waypointList.Count];
            for (var i = 0; i < waypoints.Length; i++)
                waypoints[i] = waypointList[i];

            var playerPosition = GetGridSpawnPosition(playerGridIndex, gridColumns, columnSpacing, rowSpacing);
            playerCar = BuildCar("HoverCar", vehicleProfile, true, PlayerBodyColor, PlayerAccentColor);
            playerCar.transform.SetPositionAndRotation(playerPosition, trackBuilder.StartRotation);
            var playerReset = playerCar.GetComponent<VehicleReset>();
            playerReset.SetSpawnPoint(playerPosition, trackBuilder.StartRotation);
            playerCar.SetActive(true);

            if (!spawnAiRivals)
                return;

            for (var rivalIndex = 0; rivalIndex < aiRivalCount; rivalIndex++)
            {
                var gridIndex = rivalIndex >= playerGridIndex ? rivalIndex + 1 : rivalIndex;
                var spawnPosition = GetGridSpawnPosition(gridIndex, gridColumns, columnSpacing, rowSpacing);
                var bodyColor = RivalBodyColors[rivalIndex % RivalBodyColors.Length];
                var accentColor = RivalAccentColors[rivalIndex % RivalAccentColors.Length];
                var aiCar = BuildCar("AIRival_" + (rivalIndex + 1), vehicleProfile, false, bodyColor, accentColor);
                aiCar.transform.SetPositionAndRotation(spawnPosition, trackBuilder.StartRotation);

                var ai = aiCar.GetComponent<AIVehicleController>();
                ai.SetWaypoints(waypoints);
                ai.SetRivalVariation(rivalIndex * 3, 0.92f + rivalIndex * 0.018f);
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
            var car = new GameObject(carName);
            car.SetActive(false);
            car.tag = isPlayer ? "Player" : "Untagged";
            car.layer = NeonLapLayers.Vehicle;

            HoverCarVisualBuilder.Build(car.transform,
                new HoverCarVisualBuilder.BuildArgs(carBodyMaterial, carAccentMaterial, bodyColor, accentColor));

            var rb = car.AddComponent<Rigidbody>();
            rb.mass = 750f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var collider = car.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.6f, 0.6f, 2.8f);
            collider.center = new Vector3(0f, 0.2f, 0f);

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

            var controller = car.AddComponent<VehicleController>();
            if (isPlayer)
                controller.Configure(profile, inputReader);
            else
                SetPrivateField(controller, "profile", profile);

            var reset = car.AddComponent<VehicleReset>();
            if (inputReader != null)
                reset.Configure(inputReader);

            car.AddComponent<RaceStartGate>();
            car.AddComponent<RacerProgress>().Configure(isPlayer);
            car.AddComponent<ExhaustSmokeVFX>();

            if (isPlayer)
            {
                car.AddComponent<VehicleAudioController>();
                AddDriftTrails(car);
            }
            else
            {
                var ai = car.AddComponent<AIVehicleController>();
                SetPrivateField(ai, "profile", profile);
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
            if (cam == null)
                return;

            var follow = cam.gameObject.GetComponent<FollowCamera>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.Target = playerCar.transform;
        }

        void SetupRaceSystems()
        {
            var raceRoot = trackBuilder.transform;
            raceManager = raceRoot.gameObject.AddComponent<RaceManager>();
            SetPrivateField(raceManager, "totalLaps", trackDefinition != null ? trackDefinition.lapCount : 3);
            SetPrivateField(raceManager, "trackBuilder", trackBuilder);
            SetPrivateField(raceManager, "playerReset", playerCar.GetComponent<VehicleReset>());

            if (!createUi)
                return;

            BuildRaceUi();
            BuildPauseMenu(raceRoot.gameObject);
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
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var raceUi = canvasGo.AddComponent<RaceUI>();
            SetPrivateField(raceUi, "raceManager", raceManager);

            var lapText = CreateText(canvasGo.transform, "LapText", new Vector2(20f, -20f), TextAnchor.UpperLeft, 28);
            var lapTimer = CreateText(canvasGo.transform, "LapTimer", new Vector2(-20f, -20f), TextAnchor.UpperRight, 28);
            var raceTimer = CreateText(canvasGo.transform, "RaceTimer", new Vector2(-20f, -60f), TextAnchor.UpperRight, 24);
            var bestLap = CreateText(canvasGo.transform, "BestLap", new Vector2(20f, -60f), TextAnchor.UpperLeft, 24);

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
            finishPanelRect.anchorMin = Vector2.zero;
            finishPanelRect.anchorMax = Vector2.one;
            finishPanelRect.offsetMin = Vector2.zero;
            finishPanelRect.offsetMax = Vector2.zero;
            finishPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            finishPanel.SetActive(false);

            var finishTitle = CreateText(finishPanel.transform, "FinishTitle", Vector2.zero, TextAnchor.MiddleCenter, 72);
            finishTitle.alignment = TextAnchor.MiddleCenter;
            finishTitle.color = new Color(0.4f, 1f, 1f);
            ConfigureScreenTextRect(finishTitle.GetComponent<RectTransform>(), 120f, new Vector2(0f, 40f));

            var finishDetail = CreateText(finishPanel.transform, "FinishDetail", Vector2.zero, TextAnchor.MiddleCenter, 32);
            finishDetail.alignment = TextAnchor.MiddleCenter;
            finishDetail.color = Color.white;
            ConfigureScreenTextRect(finishDetail.GetComponent<RectTransform>(), 60f, new Vector2(0f, -40f));

            raceUi.Configure(
                raceManager,
                lapText,
                lapTimer,
                raceTimer,
                bestLap,
                countdownText,
                countdownSubtitle,
                countdownPanel,
                finishPanel,
                finishTitle,
                finishDetail);
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
