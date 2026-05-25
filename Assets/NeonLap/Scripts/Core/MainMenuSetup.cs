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
            EnsureGameManager();
            PrepareCamera();
            var showcase = gameObject.AddComponent<MainMenuShowcase>();
            showcase.Build();
            BuildMenuUi();
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

            var tagline = CreateText(canvasGo.transform, "Tagline", "3 LEVELS  •  10 RACERS  •  1 CHAMPION", 22, new Vector2(0f, 108f));
            tagline.color = new Color(0.75f, 0.88f, 1f);
            tagline.raycastTarget = false;

            var startButton = CreateNeonButton(canvasGo.transform, "StartButton", "START RACE", new Vector2(0f, 24f),
                new Color(0.2f, 1f, 1f));
            var optionsButton = CreateNeonButton(canvasGo.transform, "OptionsButton", "OPTIONS", new Vector2(0f, -48f),
                new Color(0.55f, 0.75f, 1f));
            var controlsButton = CreateNeonButton(canvasGo.transform, "ControlsButton", "CONTROLS", new Vector2(0f, -120f),
                new Color(0.55f, 0.75f, 1f));
            var quitButton = CreateNeonButton(canvasGo.transform, "QuitButton", "QUIT", new Vector2(0f, -192f),
                new Color(1f, 0.45f, 0.55f));

            var levelSelectPanel = BuildLevelSelectPanel(canvasGo.transform, out var levelOneButton,
                out var levelTwoButton, out var levelThreeButton, out var levelSelectBackButton);

            var controlsPanel = BuildControlsPanel(canvasGo.transform);
            var controlsBackButton = controlsPanel.transform.Find("BackButton")?.GetComponent<Button>();

            var optionsHubPanel = BuildOptionsHubPanel(canvasGo.transform, out var gameSettingsNavButton,
                out var accessibilityNavButton, out var optionsHubBackButton);
            var gameSettingsPanel = BuildGameSettingsPanel(canvasGo.transform, out var lapOneButton,
                out var lapTwoButton, out var lapThreeButton, out var lapFiveButton, out var lapLabel,
                out var policeOnButton, out var policeOffButton, out var policeLabel, out var gameSettingsBackButton);
            var accessibilityPanel = BuildAccessibilitySettingsPanel(canvasGo.transform, out var lowButton,
                out var mediumButton, out var highButton, out var qualityLabel, out var difficultyEasyButton,
                out var difficultyMediumButton, out var difficultyHardButton, out var difficultyLabel,
                out var accessibilityBackButton);

            var menu = canvasGo.AddComponent<MainMenuController>();
            menu.Configure(startButton, optionsButton, controlsButton, quitButton, levelSelectPanel,
                levelSelectBackButton, levelOneButton, levelTwoButton, levelThreeButton, controlsPanel,
                controlsBackButton, optionsHubPanel, optionsHubBackButton, gameSettingsNavButton,
                accessibilityNavButton, gameSettingsPanel, gameSettingsBackButton, lapOneButton, lapTwoButton,
                lapThreeButton, lapFiveButton, lapLabel, policeOnButton, policeOffButton, policeLabel,
                accessibilityPanel, accessibilityBackButton, lowButton,
                mediumButton, highButton, qualityLabel, difficultyEasyButton, difficultyMediumButton,
                difficultyHardButton, difficultyLabel);
        }

        GameObject BuildLevelSelectPanel(Transform parent, out Button levelOneButton, out Button levelTwoButton,
            out Button levelThreeButton, out Button backButton)
        {
            var panel = CreateOverlayPanel(parent, "LevelSelectPanel");

            var title = CreateText(panel.transform, "LevelSelectTitle", "SELECT LEVEL", 56, new Vector2(0f, 260f));
            title.color = new Color(0.2f, 1f, 1f);
            title.raycastTarget = false;

            var subtitle = CreateText(panel.transform, "LevelSelectSubtitle", "CHOOSE YOUR TRACK", 24,
                new Vector2(0f, 200f));
            subtitle.color = new Color(0.75f, 0.88f, 1f);
            subtitle.raycastTarget = false;

            levelOneButton = CreateNeonButton(panel.transform, "LevelOneButton", GetLevelButtonLabel(0),
                new Vector2(0f, 100f), new Color(0.2f, 1f, 1f), 560f);
            levelTwoButton = CreateNeonButton(panel.transform, "LevelTwoButton", GetLevelButtonLabel(1),
                new Vector2(0f, 20f), new Color(0.55f, 0.75f, 1f), 560f);
            levelThreeButton = CreateNeonButton(panel.transform, "LevelThreeButton", GetLevelButtonLabel(2),
                new Vector2(0f, -60f), new Color(1f, 0.72f, 0.35f), 560f);

            var hint = CreateText(panel.transform, "LevelSelectHint", "Jump straight to your favorite circuit.", 20,
                new Vector2(0f, -140f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(920f, 40f);

            backButton = CreateNeonButton(panel.transform, "LevelSelectBackButton", "BACK", new Vector2(0f, -250f),
                new Color(0.55f, 0.75f, 1f));
            return panel;
        }

        static string GetLevelButtonLabel(int levelIndex)
        {
            var manager = GameManager.Instance != null
                ? GameManager.Instance
                : Object.FindAnyObjectByType<GameManager>();
            var track = manager != null ? manager.GetTrackDefinition(levelIndex) : null;
            var trackName = track != null ? track.trackName.ToUpperInvariant() : $"TRACK {levelIndex + 1}";
            return $"LEVEL {levelIndex + 1}  •  {trackName}";
        }

        void BuildVignette(Transform parent)
        {
            CreatePanel(parent, "VignetteTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -240f),
                Vector2.zero, new Color(0.02f, 0.02f, 0.06f, 0.78f));
            CreatePanel(parent, "VignetteBottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 280f),
                new Color(0.02f, 0.02f, 0.06f, 0.85f));

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

        GameObject BuildOptionsHubPanel(Transform parent, out Button gameSettingsButton,
            out Button accessibilityButton, out Button backButton)
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
                new Vector2(0f, 80f), new Color(0.2f, 1f, 1f));
            accessibilityButton = CreateNeonButton(panel.transform, "AccessibilityNavButton", "ACCESSIBILITY SETTINGS",
                new Vector2(0f, 0f), new Color(0.55f, 0.75f, 1f));
            backButton = CreateNeonButton(panel.transform, "OptionsHubBackButton", "BACK", new Vector2(0f, -250f),
                new Color(0.55f, 0.75f, 1f));
            return panel;
        }

        GameObject BuildGameSettingsPanel(Transform parent, out Button lapOneButton, out Button lapTwoButton,
            out Button lapThreeButton, out Button lapFiveButton, out Text lapLabel, out Button policeOnButton,
            out Button policeOffButton, out Text policeLabel, out Button backButton)
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

            var policeSubtitle = CreateText(panel.transform, "PoliceSubtitle", "POLICE CHASES", 26,
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

            var hint = CreateText(panel.transform, "GameSettingsHint",
                "Applies to every track in career and quick races.", 20, new Vector2(0f, -160f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(920f, 60f);

            backButton = CreateNeonButton(panel.transform, "GameSettingsBackButton", "BACK", new Vector2(0f, -250f),
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

            backButton = CreateNeonButton(panel.transform, "AccessibilityBackButton", "BACK", new Vector2(0f, -250f),
                new Color(0.55f, 0.75f, 1f));
            return panel;
        }

        GameObject BuildControlsPanel(Transform parent)
        {
            var panel = new GameObject("ControlsPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
            panel.SetActive(false);

            var title = CreateText(panel.transform, "ControlsTitle", "CONTROLS", 56, new Vector2(0f, 260f));
            title.color = new Color(0.2f, 1f, 1f);
            title.raycastTarget = false;

            var lines = new[]
            {
                "W / Up Arrow       Accelerate",
                "S / Down Arrow     Brake / Reverse",
                "A / D or Left / Right     Steer",
                "Space              Drift",
                "C                  Switch Camera",
                "R                  Reset Car",
                "Esc                Pause Menu",
            };

            for (var i = 0; i < lines.Length; i++)
            {
                var line = CreateText(panel.transform, "ControlLine" + i, lines[i], 30, new Vector2(0f, 150f - i * 52f));
                line.alignment = TextAnchor.MiddleCenter;
                line.raycastTarget = false;
                line.color = new Color(0.88f, 0.92f, 1f);
                var lineRect = line.GetComponent<RectTransform>();
                lineRect.sizeDelta = new Vector2(900f, 44f);
            }

            var hint = CreateText(panel.transform, "ControlsHint", "Win races to unlock harder tracks!", 24, new Vector2(0f, -170f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;

            CreateNeonButton(panel.transform, "BackButton", "BACK", new Vector2(0f, -250f),
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

        Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos)
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
