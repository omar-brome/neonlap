using NeonLap.Audio;
using NeonLap.Race;
using NeonLap.UI;
using NeonLap.VFX;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NeonLap.Core
{
    public class MainMenuSetup : MonoBehaviour
    {
        void Awake()
        {
            GameQualitySettings.Load();
            GameDifficultySettings.Load();
            GameLapSettings.Load();
            GamePoliceSettings.Load();
            GameRaceModeSettings.Load();
            GameTeamRaceSettings.Load();
            GameTrackOptions.Load();
            TimeTrialSettings.Load();
            GameAccessibilitySettings.Load();
            GameAudioSettings.Load();
            GameTouchSettings.Load();
            EnsureGameManager();
            PrepareCamera();
            var showcase = gameObject.AddComponent<MainMenuShowcase>();
            showcase.Build();
            BuildMenuUi();
            MenuMusicController.Setup(transform);
        }

        public static void ApplyMenuAtmosphere()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.01f, 0.05f);

            var preset = GameQualitySettings.Preset;
            GameQualitySettings.ApplyFogAndLighting(preset.FogDensity, preset.LightIntensity);
            EnsureMenuRainEffect(cam.transform, preset);
            SkyGraphicsSystem.Ensure(cam);
        }

        static void PrepareCamera()
        {
            ApplyMenuAtmosphere();
        }

        static void EnsureMenuRainEffect(Transform cameraTransform, QualityPreset preset)
        {
            if (cameraTransform == null)
                return;

            var existing = Object.FindAnyObjectByType<RainEffect>();
            if (!preset.EnableRain || preset.RainIntensity <= 0.01f)
            {
                if (existing != null)
                    Object.Destroy(existing.gameObject);
                return;
            }

            if (existing != null)
            {
                existing.Configure(cameraTransform, preset.RainIntensity);
                return;
            }

            var rainGo = new GameObject("RainEffect");
            var rain = rainGo.AddComponent<RainEffect>();
            rain.Configure(cameraTransform, preset.RainIntensity);
        }

        void EnsureGameManager()
        {
            EnsureGameManagerExists();
        }

        public static void EnsureGameManagerExists()
        {
            if (FindAnyObjectByType<GameManager>() != null)
                return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        void BuildMenuUi()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("MainMenuUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildVignette(canvasGo.transform);
            BuildDecorativeLines(canvasGo.transform);

            var title = CreateText(canvasGo.transform, "Title", "NEONLAP", 92, new Vector2(0f, 210f));
            title.color = new Color(0.2f, 1f, 1f);
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<MainMenuTitlePulse>();

            var subtitle = CreateText(canvasGo.transform, "Subtitle", "HOVER  •  DRIFT  •  DOMINATE", 28, new Vector2(0f, 145f));
            subtitle.color = new Color(1f, 0.55f, 0.85f);
            subtitle.raycastTarget = false;

            var tagline = CreateText(canvasGo.transform, "Tagline", "7 TRACKS  •  TRIAL GHOSTS  •  CAREER ★  •  CHASE  •  SCORE", 22,
                new Vector2(0f, 108f));
            tagline.color = new Color(0.75f, 0.88f, 1f);
            tagline.raycastTarget = false;

            var startButton = CreateNeonButton(canvasGo.transform, "StartButton", "START RACE", new Vector2(0f, 36f),
                new Color(0.2f, 1f, 1f));
            var garageButton = CreateNeonButton(canvasGo.transform, "GarageButton", "GARAGE", new Vector2(0f, -36f),
                new Color(0.85f, 0.55f, 1f));
            var optionsButton = CreateNeonButton(canvasGo.transform, "OptionsButton", "OPTIONS", new Vector2(0f, -108f),
                new Color(0.55f, 0.75f, 1f));
            var quitButton = CreateNeonButton(canvasGo.transform, "QuitButton", "QUIT", new Vector2(0f, -180f),
                new Color(1f, 0.45f, 0.55f));

            var garagePanel = BuildGaragePanel(canvasGo.transform, out var garageBackButton, out var garageBuildOneButton,
                out var garageBuildTwoButton, out var garageBuildThreeButton, out var garageBuildFourButton,
                out var garageBuildFiveButton, out var garageStatsText, out var garageDetailText, out var garageUnlockText,
                out var garageEquipButton, out var garageCreditsText, out var garagePaintButton, out var garageDecalButton,
                out var garageRimButton, out var garageTrailButton, out var garageHornButton);

            var levelSelectPanel = BuildLevelSelectPanel(canvasGo.transform, out var careerModeButton,
                out var timeTrialModeButton, out var eliminationModeButton, out var chaseModeButton,
                out var scoreAttackModeButton, out var practiceModeButton, out var customModeButton,
                out var teamRaceModeButton, out var ghostDuelModeButton, out var demolitionModeButton,
                out var hardcoreModeButton,
                out var levelOneButton,
                out var levelTwoButton, out var levelThreeButton, out var levelFourButton,
                out var levelFiveButton, out var levelSixButton, out var levelSevenButton, out var careerProgressText,
                out var dailyChallengeText, out var endlessModeButton, out var trackPreviewText, out var goRaceButton,
                out var levelSelectBackButton);

            var controlsPanel = ControlsOverlayBuilder.Build(canvasGo.transform, "ControlsPanel", out var controlsBackButton);

            var optionsHubPanel = BuildOptionsHubPanel(canvasGo.transform, out var gameSettingsNavButton,
                out var controlsNavButton, out var accessibilityNavButton, out var audioNavButton,
                out var optionsHubBackButton);
            var audioSettingsPanel = BuildAudioSettingsPanel(canvasGo.transform, out var masterVolumeSlider,
                out var sfxVolumeSlider, out var musicVolumeSlider, out var audioSettingsLabel,
                out var audioSettingsBackButton);
            var gameSettingsPanel = BuildGameSettingsPanel(canvasGo.transform, out var lapOneButton,
                out var lapTwoButton, out var lapThreeButton, out var lapFiveButton, out var lapLabel,
                out var policeOnButton, out var policeOffButton, out var policeLabel,
                out var reverseOffButton, out var reverseOnButton, out var nightOffButton, out var nightOnButton,
                out var weatherForecastButton, out var weatherDryButton, out var weatherRainButton,
                out var weatherFogButton, out var weatherSandButton,
                out var trackOptionsLabel, out var teamBlueButton, out var teamRedButton, out var teamLabel,
                out var ttRivalsButton, out var ttSettingsLabel, out var ghostCollisionOnButton,
                out var ghostCollisionOffButton, out var ttPoliceOnButton, out var ttPoliceOffButton,
                out var ttRanksOnButton, out var ttRanksOffButton, out var gameSettingsBackButton);
            var accessibilityPanel = BuildAccessibilitySettingsPanel(canvasGo.transform, out var lowButton,
                out var mediumButton, out var highButton, out var qualityLabel, out var difficultyEasyButton,
                out var difficultyMediumButton, out var difficultyHardButton, out var difficultyLabel,
                out var accessibilityBackButton);

            var menu = canvasGo.AddComponent<MainMenuController>();
            menu.Configure(startButton, garageButton, optionsButton, quitButton, garagePanel, garageBackButton,
                garageBuildOneButton, garageBuildTwoButton, garageBuildThreeButton, garageBuildFourButton,
                garageBuildFiveButton, garageStatsText, garageDetailText, garageUnlockText, garageEquipButton,
                garageCreditsText, garagePaintButton, garageDecalButton, garageRimButton, garageTrailButton,
                garageHornButton,
                levelSelectPanel, levelSelectBackButton, careerModeButton, timeTrialModeButton, eliminationModeButton,
                chaseModeButton, scoreAttackModeButton, practiceModeButton, customModeButton, teamRaceModeButton,
                ghostDuelModeButton, demolitionModeButton, hardcoreModeButton, levelOneButton,
                levelTwoButton, levelThreeButton, levelFourButton, levelFiveButton, levelSixButton, levelSevenButton,
                careerProgressText, dailyChallengeText, endlessModeButton, trackPreviewText, goRaceButton, controlsPanel,
                controlsBackButton, optionsHubPanel, optionsHubBackButton, gameSettingsNavButton, controlsNavButton,
                accessibilityNavButton, audioNavButton, audioSettingsPanel, audioSettingsBackButton,
                masterVolumeSlider, sfxVolumeSlider, musicVolumeSlider, audioSettingsLabel,
                gameSettingsPanel, gameSettingsBackButton, lapOneButton, lapTwoButton,
                lapThreeButton, lapFiveButton, lapLabel, policeOnButton, policeOffButton, policeLabel,
                reverseOffButton, reverseOnButton, nightOffButton, nightOnButton,
                weatherForecastButton, weatherDryButton, weatherRainButton, weatherFogButton, weatherSandButton,
                trackOptionsLabel, teamBlueButton,
                teamRedButton, teamLabel, ttRivalsButton, ttSettingsLabel, ghostCollisionOnButton,
                ghostCollisionOffButton, ttPoliceOnButton, ttPoliceOffButton, ttRanksOnButton, ttRanksOffButton,
                accessibilityPanel, accessibilityBackButton, lowButton,
                mediumButton, highButton, qualityLabel, difficultyEasyButton, difficultyMediumButton,
                difficultyHardButton, difficultyLabel);

            canvasGo.AddComponent<PlatformMenuExtension>();
        }

        GameObject BuildGaragePanel(Transform parent, out Button backButton, out Button buildOne, out Button buildTwo,
            out Button buildThree, out Button buildFour, out Button buildFive, out Text statsText, out Text detailText,
            out Text unlockText, out Button equipButton, out Text creditsText, out Button paintButton,
            out Button decalButton, out Button rimButton, out Button trailButton, out Button hornButton)
        {
            var panel = CreateOverlayPanel(parent, "GaragePanel");

            var title = CreateText(panel.transform, "GarageTitle", "GARAGE", 52, new Vector2(0f, 280f));
            title.color = new Color(0.85f, 0.55f, 1f);
            title.raycastTarget = false;

            var subtitle = CreateText(panel.transform, "GarageSubtitle", "BUILDS  •  PAINT  •  DECALS  •  RIMS", 22, new Vector2(0f, 232f));
            subtitle.color = new Color(0.75f, 0.88f, 1f);
            subtitle.raycastTarget = false;

            buildOne = CreateNeonButton(panel.transform, "GarageBuild1", "BUILD 1", new Vector2(-280f, 150f),
                new Color(0.2f, 1f, 1f), 200f);
            buildTwo = CreateNeonButton(panel.transform, "GarageBuild2", "BUILD 2", new Vector2(-140f, 150f),
                new Color(0.85f, 0.55f, 1f), 200f);
            buildThree = CreateNeonButton(panel.transform, "GarageBuild3", "BUILD 3", new Vector2(0f, 150f),
                new Color(1f, 0.72f, 0.35f), 200f);
            buildFour = CreateNeonButton(panel.transform, "GarageBuild4", "BUILD 4", new Vector2(140f, 150f),
                new Color(0.65f, 1f, 0.55f), 200f);
            buildFive = CreateNeonButton(panel.transform, "GarageBuild5", "BUILD 5", new Vector2(280f, 150f),
                new Color(1f, 0.45f, 0.55f), 200f);

            statsText = CreateText(panel.transform, "GarageStats", "Stats", 24, new Vector2(0f, 72f));
            statsText.color = new Color(0.45f, 1f, 1f);
            statsText.alignment = TextAnchor.MiddleCenter;
            var statsRect = statsText.GetComponent<RectTransform>();
            statsRect.sizeDelta = new Vector2(900f, 36f);

            detailText = CreateText(panel.transform, "GarageDetail", "Detail", 20, new Vector2(-220f, 8f));
            detailText.color = Color.white;
            detailText.alignment = TextAnchor.UpperLeft;
            var detailRect = detailText.GetComponent<RectTransform>();
            detailRect.sizeDelta = new Vector2(420f, 110f);

            unlockText = CreateText(panel.transform, "GarageUnlock", "Unlock", 18, new Vector2(220f, 8f));
            unlockText.color = new Color(0.75f, 0.85f, 1f);
            unlockText.alignment = TextAnchor.UpperLeft;
            var unlockRect = unlockText.GetComponent<RectTransform>();
            unlockRect.sizeDelta = new Vector2(420f, 110f);

            creditsText = CreateText(panel.transform, "GarageCredits", "Credits", 20, new Vector2(0f, -20f));
            creditsText.color = new Color(1f, 0.92f, 0.35f);
            creditsText.alignment = TextAnchor.MiddleCenter;
            var creditsRect = creditsText.GetComponent<RectTransform>();
            creditsRect.sizeDelta = new Vector2(900f, 28f);

            paintButton = CreateNeonButton(panel.transform, "GaragePaintButton", "PAINT", new Vector2(-280f, -58f),
                new Color(0.35f, 1f, 0.75f), 180f);
            decalButton = CreateNeonButton(panel.transform, "GarageDecalButton", "DECAL", new Vector2(-140f, -58f),
                new Color(1f, 0.55f, 0.85f), 180f);
            rimButton = CreateNeonButton(panel.transform, "GarageRimButton", "RIMS", new Vector2(0f, -58f),
                new Color(0.75f, 0.85f, 1f), 180f);
            trailButton = CreateNeonButton(panel.transform, "GarageTrailButton", "UNDERGLOW", new Vector2(140f, -58f),
                new Color(0.45f, 1f, 1f), 180f);
            hornButton = CreateNeonButton(panel.transform, "GarageHornButton", "HORN", new Vector2(280f, -58f),
                new Color(0.85f, 0.55f, 1f), 180f);

            equipButton = CreateNeonButton(panel.transform, "GarageEquipButton", "EQUIP BUILD", new Vector2(120f, -120f),
                new Color(0.2f, 1f, 1f), 260f);
            backButton = CreateNeonButton(panel.transform, "GarageBackButton", "BACK", new Vector2(-120f, -120f),
                new Color(0.55f, 0.75f, 1f), 260f);

            panel.SetActive(false);
            return panel;
        }

        GameObject BuildLevelSelectPanel(Transform parent, out Button careerModeButton, out Button timeTrialModeButton,
            out Button eliminationModeButton, out Button chaseModeButton, out Button scoreAttackModeButton,
            out Button practiceModeButton, out Button customModeButton, out Button teamRaceModeButton,
            out Button ghostDuelModeButton, out Button demolitionModeButton, out Button hardcoreModeButton,
            out Button levelOneButton,
            out Button levelTwoButton, out Button levelThreeButton, out Button levelFourButton,
            out Button levelFiveButton, out Button levelSixButton, out Button levelSevenButton,
            out Text careerProgressText, out Text dailyChallengeText, out Button endlessModeButton,
            out Text trackPreviewText, out Button goRaceButton, out Button backButton)
        {
            var panel = CreateOverlayPanel(parent, "LevelSelectPanel");

            var title = CreateText(panel.transform, "LevelSelectTitle", "SELECT MODE & TRACK", 48, new Vector2(0f, 292f));
            title.color = new Color(0.2f, 1f, 1f);
            title.raycastTarget = false;

            careerModeButton = CreateNeonButton(panel.transform, "CareerModeButton", "CAREER",
                new Vector2(-250f, 236f), new Color(0.55f, 0.75f, 1f), 150f);
            timeTrialModeButton = CreateNeonButton(panel.transform, "TimeTrialModeButton", "TRIAL",
                new Vector2(-125f, 236f), new Color(0.85f, 0.55f, 1f), 150f);
            eliminationModeButton = CreateNeonButton(panel.transform, "EliminationModeButton", "ELIM",
                new Vector2(0f, 236f), new Color(1f, 0.45f, 0.55f), 150f);
            chaseModeButton = CreateNeonButton(panel.transform, "ChaseModeButton", "CHASE",
                new Vector2(125f, 236f), new Color(1f, 0.72f, 0.35f), 150f);
            scoreAttackModeButton = CreateNeonButton(panel.transform, "ScoreAttackModeButton", "SCORE",
                new Vector2(250f, 236f), new Color(0.45f, 1f, 1f), 150f);
            practiceModeButton = CreateNeonButton(panel.transform, "PracticeModeButton", "PRACTICE",
                new Vector2(-165f, 188f), new Color(0.65f, 1f, 0.55f), 150f);
            teamRaceModeButton = CreateNeonButton(panel.transform, "TeamRaceModeButton", "TEAM",
                new Vector2(0f, 188f), new Color(0.35f, 0.65f, 1f), 150f);
            customModeButton = CreateNeonButton(panel.transform, "CustomModeButton", "CUSTOM",
                new Vector2(165f, 188f), new Color(0.95f, 0.75f, 0.35f), 150f);
            ghostDuelModeButton = CreateNeonButton(panel.transform, "GhostDuelModeButton", "GHOST",
                new Vector2(0f, 140f), new Color(0.55f, 0.95f, 1f), 150f);
            demolitionModeButton = CreateNeonButton(panel.transform, "DemolitionModeButton", "DEMOL",
                new Vector2(-90f, 92f), new Color(1f, 0.55f, 0.2f), 150f);
            hardcoreModeButton = CreateNeonButton(panel.transform, "HardcoreModeButton", "HARD",
                new Vector2(90f, 92f), new Color(1f, 0.38f, 0.42f), 150f);

            var subtitle = CreateText(panel.transform, "LevelSelectSubtitle", "CAREER MAP  •  EARN STARS TO UNLOCK THE CIRCUIT", 22,
                new Vector2(0f, 84f));
            subtitle.color = new Color(0.75f, 0.88f, 1f);
            subtitle.raycastTarget = false;

            careerProgressText = CreateText(panel.transform, "CareerProgressText", CareerProgressionGate.GetStarProgressLine(),
                20, new Vector2(0f, 52f));
            careerProgressText.color = new Color(1f, 0.92f, 0.35f);
            careerProgressText.raycastTarget = false;

            dailyChallengeText = CreateText(panel.transform, "DailyChallengeText", DailyChallengeService.GetMenuBannerLine(),
                18, new Vector2(0f, 22f));
            dailyChallengeText.color = new Color(0.75f, 0.88f, 1f);
            dailyChallengeText.raycastTarget = false;
            var dailyRect = dailyChallengeText.GetComponent<RectTransform>();
            dailyRect.sizeDelta = new Vector2(920f, 48f);

            var map = CareerMapUiBuilder.Build(panel.transform, CreateMapLevelButton);
            levelOneButton = map.LevelButtons[0];
            levelTwoButton = map.LevelButtons[1];
            levelThreeButton = map.LevelButtons[2];
            levelFourButton = map.LevelButtons[3];
            levelFiveButton = map.LevelButtons[4];
            levelSixButton = map.LevelButtons[5];
            levelSevenButton = map.LevelButtons[6];

            endlessModeButton = CreateNeonButton(panel.transform, "EndlessModeButton", "STUNT PARK",
                new Vector2(0f, -188f), new Color(1f, 0.55f, 0.85f), 280f);
            endlessModeButton.gameObject.SetActive(StuntProgressionGate.IsUnlocked());

            trackPreviewText = CreateText(panel.transform, "TrackPreview", "Track browser", 20, new Vector2(0f, -218f));
            trackPreviewText.color = new Color(0.75f, 0.88f, 1f);
            trackPreviewText.alignment = TextAnchor.UpperCenter;
            var previewRect = trackPreviewText.GetComponent<RectTransform>();
            previewRect.sizeDelta = new Vector2(920f, 88f);

            goRaceButton = CreateNeonButton(panel.transform, "GoRaceButton", "GO RACE", new Vector2(140f, -250f),
                new Color(0.2f, 1f, 1f), 240f);

            var hint = CreateText(panel.transform, "LevelSelectHint",
                "Career = stars. Trial/Ghost = PB ghosts. Elim/Demo = last standing. Outrun = survive. Practice = sectors. Stunt = pure fun.",
                16,
                new Vector2(0f, -300f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(920f, 36f);

            backButton = CreateNeonButton(panel.transform, "LevelSelectBackButton", "BACK", new Vector2(-140f, -250f),
                new Color(0.55f, 0.75f, 1f), 240f);
            return panel;
        }

        Button CreateMapLevelButton(Transform parent, int levelIndex, float y, Color accent)
        {
            return CreateNeonButton(parent, "Level" + (levelIndex + 1) + "Button", GetLevelButtonLabel(levelIndex),
                new Vector2(0f, y), accent, 300f);
        }

        static string GetLevelButtonLabel(int levelIndex)
        {
            var manager = GameManager.Instance != null
                ? GameManager.Instance
                : Object.FindAnyObjectByType<GameManager>();
            var track = manager != null ? manager.GetTrackDefinition(levelIndex) : null;
            var trackName = track != null ? track.trackName.ToUpperInvariant() : $"TRACK {levelIndex + 1}";

            if (GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel)
                return $"L{levelIndex + 1}  {TimeTrialMedalStore.GetTrackSummary(levelIndex)}";

            if (GameRaceModeSettings.IsScoreAttack)
                return $"L{levelIndex + 1}  {trackName}  •  {ScoreAttackRecordStore.GetTrackSummary(levelIndex)}";

            if (GameRaceModeSettings.IsCareer)
                return CareerScoreStore.GetLevelButtonLabel(levelIndex, trackName);

            return $"L{levelIndex + 1}  {trackName}";
        }

        void BuildVignette(Transform parent)
        {
            CreatePanel(parent, "VignetteTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -200f),
                Vector2.zero, new Color(0.02f, 0.02f, 0.06f, 0.52f));
            CreatePanel(parent, "VignetteBottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 220f),
                new Color(0.02f, 0.02f, 0.06f, 0.58f));

            CreateSideVignette(parent, "VignetteLeft", true);
            CreateSideVignette(parent, "VignetteRight", false);
        }

        static void CreateSideVignette(Transform parent, string name, bool left)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.sizeDelta = new Vector2(460f, 0f);
            rect.anchoredPosition = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.02f, 0.02f, 0.06f, 0.5f);
            image.raycastTarget = false;
        }

        void BuildDecorativeLines(Transform parent)
        {
            CreateLineBar(parent, "TitleLineGlow", new Vector2(0f, 178f), new Vector2(520f, 12f),
                new Color(0.2f, 1f, 1f, 0.15f));
            CreateLineBar(parent, "TitleLine", new Vector2(0f, 180f), new Vector2(440f, 3f),
                new Color(0.2f, 1f, 1f, 0.8f));
        }

        static void CreateLineBar(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        static void CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        static GameObject CreateOverlayPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildOptionsHubPanel(Transform parent, out Button gameSettingsButton, out Button controlsButton,
            out Button accessibilityButton, out Button audioButton, out Button backButton)
        {
            var panel = CreateOverlayPanel(parent, "OptionsHubPanel");

            var title = CreateText(panel.transform, "OptionsTitle", "OPTIONS", 56, new Vector2(0f, 260f));
            title.color = new Color(0.2f, 1f, 1f);
            title.raycastTarget = false;

            var subtitle = CreateText(panel.transform, "OptionsSubtitle", "CHOOSE A SETTINGS CATEGORY", 24,
                new Vector2(0f, 200f));
            subtitle.color = new Color(0.75f, 0.88f, 1f);
            subtitle.raycastTarget = false;

            gameSettingsButton = CreateNeonButton(panel.transform, "GameSettingsNavButton", "GAME SETTINGS",
                new Vector2(0f, 110f), new Color(0.2f, 1f, 1f));
            audioButton = CreateNeonButton(panel.transform, "AudioNavButton", "AUDIO",
                new Vector2(0f, 40f), new Color(0.85f, 0.55f, 1f));
            accessibilityButton = CreateNeonButton(panel.transform, "AccessibilityNavButton", "ACCESSIBILITY SETTINGS",
                new Vector2(0f, -30f), new Color(0.55f, 0.75f, 1f));
            controlsButton = CreateNeonButton(panel.transform, "ControlsNavButton", "CONTROLS",
                new Vector2(0f, -100f), new Color(1f, 0.72f, 0.35f));
            CreateNeonButton(panel.transform, "PlatformNavButton", "SHARE / SAVE",
                new Vector2(0f, -170f), new Color(0.45f, 1f, 0.65f));
            backButton = CreateNeonButton(panel.transform, "OptionsHubBackButton", "BACK", new Vector2(0f, -250f),
                new Color(0.55f, 0.75f, 1f));
            return panel;
        }

        GameObject BuildAudioSettingsPanel(Transform parent, out Slider masterSlider, out Slider sfxSlider,
            out Slider musicSlider, out Text summaryLabel, out Button backButton)
        {
            var panel = CreateOverlayPanel(parent, "AudioSettingsPanel");

            var title = CreateText(panel.transform, "AudioTitle", "AUDIO", 56, new Vector2(0f, 260f));
            title.color = new Color(0.85f, 0.55f, 1f);
            title.raycastTarget = false;

            summaryLabel = CreateText(panel.transform, "AudioSummaryLabel", GameAudioSettings.GetSummaryLine(), 22,
                new Vector2(0f, 200f));
            summaryLabel.color = new Color(0.75f, 0.88f, 1f);
            summaryLabel.raycastTarget = false;
            var summaryRect = summaryLabel.GetComponent<RectTransform>();
            summaryRect.sizeDelta = new Vector2(920f, 48f);

            masterSlider = CreateVolumeSlider(panel.transform, "MasterVolumeSlider", "MASTER VOLUME",
                new Vector2(0f, 110f), GameAudioSettings.MasterVolume);
            sfxSlider = CreateVolumeSlider(panel.transform, "SfxVolumeSlider", "SFX VOLUME", new Vector2(0f, 20f),
                GameAudioSettings.SfxVolume);
            musicSlider = CreateVolumeSlider(panel.transform, "MusicVolumeSlider", "MUSIC VOLUME",
                new Vector2(0f, -70f), GameAudioSettings.MusicVolume);

            var hint = CreateText(panel.transform, "AudioHint",
                "Music crossfades: calm → racing → chase / final lap. Drop WAVs into AudioClips/ to replace procedural audio.",
                20, new Vector2(0f, -150f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(920f, 72f);

            backButton = CreateNeonButton(panel.transform, "AudioSettingsBackButton", "BACK", new Vector2(0f, -250f),
                new Color(0.55f, 0.75f, 1f));
            return panel;
        }

        static Slider CreateVolumeSlider(Transform parent, string name, string label, Vector2 position, float value)
        {
            var labelText = CreateText(parent, name + "Label", label, 24, position + new Vector2(0f, 34f));
            labelText.color = new Color(0.75f, 0.88f, 1f);
            labelText.raycastTarget = false;
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(640f, 36f);

            var sliderGo = new GameObject(name);
            sliderGo.transform.SetParent(parent, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(640f, 26f);
            sliderRect.anchoredPosition = position;
            var sliderBg = sliderGo.AddComponent<Image>();
            sliderBg.color = new Color(0.05f, 0.06f, 0.12f, 0.96f);
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(8f, 6f);
            fillAreaRect.offsetMax = new Vector2(-8f, -6f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.85f, 0.55f, 1f, 0.85f);
            slider.fillRect = fillRect;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(sliderGo.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 38f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.72f, 0.35f, 0.95f);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            return slider;
        }

        GameObject BuildGameSettingsPanel(Transform parent, out Button lapOneButton, out Button lapTwoButton,
            out Button lapThreeButton, out Button lapFiveButton, out Text lapLabel, out Button policeOnButton,
            out Button policeOffButton, out Text policeLabel, out Button reverseOffButton, out Button reverseOnButton,
            out Button nightOffButton, out Button nightOnButton, out Button weatherForecastButton,
            out Button weatherDryButton, out Button weatherRainButton, out Button weatherFogButton,
            out Button weatherSandButton, out Text trackOptionsLabel,
            out Button teamBlueButton, out Button teamRedButton, out Text teamLabel,
            out Button ttRivalsButton, out Text ttSettingsLabel, out Button ghostCollisionOnButton,
            out Button ghostCollisionOffButton, out Button ttPoliceOnButton, out Button ttPoliceOffButton,
            out Button ttRanksOnButton, out Button ttRanksOffButton, out Button backButton)
        {
            var panel = CreateOverlayPanel(parent, "GameSettingsPanel");

            var title = CreateText(panel.transform, "GameSettingsTitle", "GAME SETTINGS", 56, new Vector2(0f, 280f));
            title.color = new Color(0.2f, 1f, 1f);
            title.raycastTarget = false;

            var lapSubtitle = CreateText(panel.transform, "LapSubtitle", "RACE LAPS", 26, new Vector2(0f, 210f));
            lapSubtitle.color = new Color(0.75f, 0.88f, 1f);
            lapSubtitle.raycastTarget = false;

            lapLabel = CreateText(panel.transform, "LapLabel",
                "Current: " + GameLapSettings.GetDisplayName(GameLapSettings.CurrentLaps), 22,
                new Vector2(0f, 170f));
            lapLabel.color = new Color(1f, 0.55f, 0.85f);
            lapLabel.raycastTarget = false;

            lapOneButton = CreateNeonButton(panel.transform, "LapOneButton", "1 LAP", new Vector2(-330f, 90f),
                new Color(1f, 0.45f, 0.55f), 200f);
            lapTwoButton = CreateNeonButton(panel.transform, "LapTwoButton", "2 LAPS", new Vector2(-110f, 90f),
                new Color(0.55f, 0.75f, 1f), 200f);
            lapThreeButton = CreateNeonButton(panel.transform, "LapThreeButton", "3 LAPS", new Vector2(110f, 90f),
                new Color(0.2f, 1f, 1f), 200f);
            lapFiveButton = CreateNeonButton(panel.transform, "LapFiveButton", "5 LAPS", new Vector2(330f, 90f),
                new Color(1f, 0.72f, 0.35f), 200f);

            var policeSubtitle = CreateText(panel.transform, "PoliceSubtitle", "POLICE (CAREER / CUSTOM)", 26,
                new Vector2(0f, 10f));
            policeSubtitle.color = new Color(0.75f, 0.88f, 1f);
            policeSubtitle.raycastTarget = false;

            policeLabel = CreateText(panel.transform, "PoliceLabel",
                "Current: " + GamePoliceSettings.GetDisplayName(GamePoliceSettings.Enabled), 22,
                new Vector2(0f, -25f));
            policeLabel.color = new Color(0.55f, 0.75f, 1f);
            policeLabel.raycastTarget = false;

            policeOnButton = CreateNeonButton(panel.transform, "PoliceOnButton", "ON", new Vector2(-120f, -80f),
                new Color(0.15f, 0.55f, 1f), 220f);
            policeOffButton = CreateNeonButton(panel.transform, "PoliceOffButton", "OFF", new Vector2(120f, -80f),
                new Color(1f, 0.45f, 0.55f), 220f);

            var variantSubtitle = CreateText(panel.transform, "VariantSubtitle", "TRACK VARIANTS", 26,
                new Vector2(0f, -150f));
            variantSubtitle.color = new Color(0.75f, 0.88f, 1f);
            variantSubtitle.raycastTarget = false;

            trackOptionsLabel = CreateText(panel.transform, "TrackOptionsLabel",
                "Variants: " + GameTrackOptions.GetSummaryLine(), 20, new Vector2(0f, -185f));
            trackOptionsLabel.color = new Color(0.55f, 0.95f, 1f);
            trackOptionsLabel.raycastTarget = false;

            reverseOffButton = CreateNeonButton(panel.transform, "ReverseOffButton", "FORWARD", new Vector2(-220f, -235f),
                new Color(0.55f, 0.75f, 1f), 200f);
            reverseOnButton = CreateNeonButton(panel.transform, "ReverseOnButton", "REVERSE", new Vector2(0f, -235f),
                new Color(0.2f, 1f, 1f), 200f);
            nightOffButton = CreateNeonButton(panel.transform, "NightOffButton", "DAY", new Vector2(220f, -235f),
                new Color(1f, 0.72f, 0.35f), 200f);
            nightOnButton = CreateNeonButton(panel.transform, "NightOnButton", "NIGHT", new Vector2(-220f, -295f),
                new Color(0.85f, 0.55f, 1f), 200f);
            weatherForecastButton = CreateNeonButton(panel.transform, "WeatherForecastButton", "FORECAST",
                new Vector2(-300f, -355f), new Color(0.55f, 0.75f, 1f), 150f);
            weatherDryButton = CreateNeonButton(panel.transform, "WeatherDryButton", "DRY", new Vector2(-150f, -355f),
                new Color(0.65f, 1f, 0.55f), 150f);
            weatherRainButton = CreateNeonButton(panel.transform, "WeatherRainButton", "RAIN", new Vector2(0f, -355f),
                new Color(0.15f, 0.55f, 1f), 150f);
            weatherFogButton = CreateNeonButton(panel.transform, "WeatherFogButton", "FOG", new Vector2(150f, -355f),
                new Color(0.72f, 0.78f, 0.88f), 150f);
            weatherSandButton = CreateNeonButton(panel.transform, "WeatherSandButton", "SAND", new Vector2(300f, -355f),
                new Color(0.95f, 0.62f, 0.25f), 150f);

            var ttSubtitle = CreateText(panel.transform, "TimeTrialSubtitle", "TIME TRIAL / GHOST", 26,
                new Vector2(0f, -430f));
            ttSubtitle.color = new Color(0.75f, 0.88f, 1f);
            ttSubtitle.raycastTarget = false;

            ttSettingsLabel = CreateText(panel.transform, "TimeTrialSettingsLabel", TimeTrialSettings.GetSummaryLine(),
                18, new Vector2(0f, -465f));
            ttSettingsLabel.color = new Color(0.55f, 0.95f, 1f);
            ttSettingsLabel.raycastTarget = false;
            var ttSettingsRect = ttSettingsLabel.GetComponent<RectTransform>();
            ttSettingsRect.sizeDelta = new Vector2(920f, 48f);

            ttRivalsButton = CreateNeonButton(panel.transform, "TimeTrialRivalsButton", "CYCLE RIVALS",
                new Vector2(0f, -520f), new Color(0.55f, 0.95f, 1f), 220f);
            ghostCollisionOnButton = CreateNeonButton(panel.transform, "GhostCollisionOnButton", "CLIP ON",
                new Vector2(-120f, -575f), new Color(1f, 0.55f, 0.2f), 220f);
            ghostCollisionOffButton = CreateNeonButton(panel.transform, "GhostCollisionOffButton", "CLIP OFF",
                new Vector2(120f, -575f), new Color(0.55f, 0.75f, 1f), 220f);
            ttPoliceOnButton = CreateNeonButton(panel.transform, "TimeTrialPoliceOnButton", "TT POLICE ON",
                new Vector2(-120f, -620f), new Color(0.15f, 0.55f, 1f), 220f);
            ttPoliceOffButton = CreateNeonButton(panel.transform, "TimeTrialPoliceOffButton", "TT POLICE OFF",
                new Vector2(120f, -620f), new Color(0.55f, 0.75f, 1f), 220f);
            ttRanksOnButton = CreateNeonButton(panel.transform, "TimeTrialRanksOnButton", "TIME RANKS ON",
                new Vector2(-120f, -675f), new Color(1f, 0.92f, 0.35f), 220f);
            ttRanksOffButton = CreateNeonButton(panel.transform, "TimeTrialRanksOffButton", "TIME RANKS OFF",
                new Vector2(120f, -675f), new Color(0.55f, 0.75f, 1f), 220f);

            var teamSubtitle = CreateText(panel.transform, "TeamSubtitle", "TEAM RACE (BLUE VS RED)", 26,
                new Vector2(0f, -730f));
            teamSubtitle.color = new Color(0.75f, 0.88f, 1f);
            teamSubtitle.raycastTarget = false;

            teamLabel = CreateText(panel.transform, "TeamLabel",
                "Team: " + GameTeamRaceSettings.GetDisplayName(GameTeamRaceSettings.PlayerTeam), 22,
                new Vector2(0f, -765f));
            teamLabel.color = new Color(0.35f, 0.65f, 1f);
            teamLabel.raycastTarget = false;

            teamBlueButton = CreateNeonButton(panel.transform, "TeamBlueButton", "TEAM BLUE", new Vector2(-120f, -820f),
                new Color(0.35f, 0.65f, 1f), 220f);
            teamRedButton = CreateNeonButton(panel.transform, "TeamRedButton", "TEAM RED", new Vector2(120f, -820f),
                new Color(1f, 0.38f, 0.42f), 220f);

            var hint = CreateText(panel.transform, "GameSettingsHint",
                "Time Trial: optional police chase, ghost PB, S/A/B time ranks (not career score).", 20,
                new Vector2(0f, -790f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(920f, 60f);

            backButton = CreateNeonButton(panel.transform, "GameSettingsBackButton", "BACK", new Vector2(0f, -940f),
                new Color(0.55f, 0.75f, 1f));
            return panel;
        }

        GameObject BuildAccessibilitySettingsPanel(Transform parent, out Button lowButton, out Button mediumButton,
            out Button highButton, out Text qualityLabel, out Button difficultyEasyButton,
            out Button difficultyMediumButton, out Button difficultyHardButton, out Text difficultyLabel,
            out Button backButton)
        {
            var panel = CreateOverlayPanel(parent, "AccessibilitySettingsPanel");

            var title = CreateText(panel.transform, "AccessibilityTitle", "ACCESSIBILITY SETTINGS", 52,
                new Vector2(0f, 300f));
            title.color = new Color(0.2f, 1f, 1f);
            title.raycastTarget = false;

            var qualitySubtitle = CreateText(panel.transform, "QualitySubtitle", "GRAPHICS QUALITY", 26,
                new Vector2(0f, 230f));
            qualitySubtitle.color = new Color(0.75f, 0.88f, 1f);
            qualitySubtitle.raycastTarget = false;

            qualityLabel = CreateText(panel.transform, "QualityLabel",
                "Current: " + GameQualitySettings.GetDisplayName(GameQualitySettings.Current), 22,
                new Vector2(0f, 190f));
            qualityLabel.color = new Color(1f, 0.55f, 0.85f);
            qualityLabel.raycastTarget = false;

            lowButton = CreateNeonButton(panel.transform, "QualityLowButton", "LOW", new Vector2(-220f, 130f),
                new Color(1f, 0.45f, 0.55f));
            mediumButton = CreateNeonButton(panel.transform, "QualityMediumButton", "MEDIUM", new Vector2(0f, 130f),
                new Color(0.55f, 0.75f, 1f));
            highButton = CreateNeonButton(panel.transform, "QualityHighButton", "HIGH", new Vector2(220f, 130f),
                new Color(0.2f, 1f, 1f));

            var difficultySubtitle = CreateText(panel.transform, "DifficultySubtitle", "RACE DIFFICULTY", 26,
                new Vector2(0f, 40f));
            difficultySubtitle.color = new Color(0.75f, 0.88f, 1f);
            difficultySubtitle.raycastTarget = false;

            difficultyLabel = CreateText(panel.transform, "DifficultyLabel",
                "Current: " + GameDifficultySettings.GetDisplayName(GameDifficultySettings.Current), 22,
                new Vector2(0f, 0f));
            difficultyLabel.color = new Color(1f, 0.55f, 0.85f);
            difficultyLabel.raycastTarget = false;

            difficultyEasyButton = CreateNeonButton(panel.transform, "DifficultyEasyButton", "EASY",
                new Vector2(-220f, -60f), new Color(1f, 0.45f, 0.55f));
            difficultyMediumButton = CreateNeonButton(panel.transform, "DifficultyMediumButton", "MEDIUM",
                new Vector2(0f, -60f), new Color(0.55f, 0.75f, 1f));
            difficultyHardButton = CreateNeonButton(panel.transform, "DifficultyHardButton", "HARD",
                new Vector2(220f, -60f), new Color(0.2f, 1f, 1f));

            var hint = CreateText(panel.transform, "AccessibilityHint",
                "Lower graphics reduce visual density. Easier difficulty slows rivals and softens racing lines.",
                20, new Vector2(0f, -150f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(920f, 80f);

            var assistTitle = CreateText(panel.transform, "SteeringAssistTitle", "STEERING ASSIST", 26,
                new Vector2(0f, -210f));
            assistTitle.color = new Color(0.75f, 0.88f, 1f);
            assistTitle.raycastTarget = false;

            var assistSliderGo = new GameObject("SteeringAssistSlider");
            assistSliderGo.transform.SetParent(panel.transform, false);
            var sliderRect = assistSliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(640f, 26f);
            sliderRect.anchoredPosition = new Vector2(0f, -250f);
            var sliderBg = assistSliderGo.AddComponent<Image>();
            sliderBg.color = new Color(0.05f, 0.06f, 0.12f, 0.96f);
            var slider = assistSliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(assistSliderGo.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(8f, 6f);
            fillAreaRect.offsetMax = new Vector2(-8f, -6f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 1f, 1f, 0.85f);
            slider.fillRect = fillRect;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(assistSliderGo.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 38f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.72f, 0.35f, 0.95f);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            var autoTitle = CreateText(panel.transform, "AutoAccelerateTitle", "AUTO-ACCELERATE", 26,
                new Vector2(0f, -310f));
            autoTitle.color = new Color(0.75f, 0.88f, 1f);
            autoTitle.raycastTarget = false;

            CreateNeonButton(panel.transform, "AutoAccelOffButton", "OFF", new Vector2(-120f, -360f),
                new Color(1f, 0.45f, 0.55f), 220f);
            CreateNeonButton(panel.transform, "AutoAccelOnButton", "ON", new Vector2(120f, -360f),
                new Color(0.65f, 1f, 0.55f), 220f);

            backButton = CreateNeonButton(panel.transform, "AccessibilityBackButton", "BACK", new Vector2(0f, -460f),
                new Color(0.55f, 0.75f, 1f));
            return panel;
        }

        static readonly Color ButtonFill = new(0.05f, 0.06f, 0.12f, 0.96f);
        static readonly Color ButtonText = new(0.97f, 0.98f, 1f);

        Button CreateNeonButton(Transform parent, string name, string label, Vector2 pos, Color accent,
            float width = 320f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 58f);
            rect.anchoredPosition = pos;

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var glowRect = glow.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-4f, -4f);
            glowRect.offsetMax = new Vector2(4f, 4f);
            glow.AddComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.55f);

            var image = go.AddComponent<Image>();
            image.color = ButtonFill;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = ButtonFill;
            colors.highlightedColor = Color.Lerp(ButtonFill, accent, 0.22f);
            colors.pressedColor = Color.Lerp(ButtonFill, accent, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var text = CreateText(go.transform, "Label", label, 30, Vector2.zero);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = ButtonText;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 120f);
            rect.anchoredPosition = pos;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
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
    }
}
