using NeonLap.Core;
using UnityEngine;
using QualityLevel = NeonLap.Core.QualityLevel;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button quitButton;
        [SerializeField] GameObject levelSelectPanel;
        [SerializeField] Button levelSelectBackButton;
        [SerializeField] Button levelOneButton;
        [SerializeField] Button levelTwoButton;
        [SerializeField] Button levelThreeButton;
        [SerializeField] GameObject controlsPanel;
        [SerializeField] Button controlsBackButton;
        [SerializeField] GameObject optionsHubPanel;
        [SerializeField] Button optionsHubBackButton;
        [SerializeField] Button gameSettingsNavButton;
        [SerializeField] Button accessibilityNavButton;
        [SerializeField] GameObject gameSettingsPanel;
        [SerializeField] Button gameSettingsBackButton;
        [SerializeField] Button lapOneButton;
        [SerializeField] Button lapTwoButton;
        [SerializeField] Button lapThreeButton;
        [SerializeField] Button lapFiveButton;
        [SerializeField] Text lapLabel;
        [SerializeField] Button policeOnButton;
        [SerializeField] Button policeOffButton;
        [SerializeField] Text policeLabel;
        [SerializeField] GameObject accessibilityPanel;
        [SerializeField] Button accessibilityBackButton;
        [SerializeField] Button qualityLowButton;
        [SerializeField] Button qualityMediumButton;
        [SerializeField] Button qualityHighButton;
        [SerializeField] Text qualityLabel;
        [SerializeField] Button difficultyEasyButton;
        [SerializeField] Button difficultyMediumButton;
        [SerializeField] Button difficultyHardButton;
        [SerializeField] Text difficultyLabel;

        static readonly Color ButtonFill = new(0.05f, 0.06f, 0.12f, 0.96f);
        static readonly Color ButtonFillSelected = new(0.1f, 0.12f, 0.22f, 0.98f);
        static readonly Color ButtonText = new(0.97f, 0.98f, 1f);
        static readonly Color ButtonTextSelected = Color.white;
        static readonly Color LowAccent = new(1f, 0.45f, 0.55f);
        static readonly Color MediumAccent = new(0.55f, 0.75f, 1f);
        static readonly Color HighAccent = new(0.2f, 1f, 1f);

        public void Configure(
            Button start,
            Button options,
            Button controls,
            Button quit,
            GameObject levelSelectPanelObject,
            Button levelSelectBack,
            Button levelOne,
            Button levelTwo,
            Button levelThree,
            GameObject controlsPanelObject,
            Button controlsBack,
            GameObject optionsHubPanelObject,
            Button optionsHubBack,
            Button gameSettingsNav,
            Button accessibilityNav,
            GameObject gameSettingsPanelObject,
            Button gameSettingsBack,
            Button lapOne,
            Button lapTwo,
            Button lapThree,
            Button lapFive,
            Text lapLabelText,
            Button policeOn,
            Button policeOff,
            Text policeLabelText,
            GameObject accessibilityPanelObject,
            Button accessibilityBack,
            Button lowQuality,
            Button mediumQuality,
            Button highQuality,
            Text qualityLabelText,
            Button easyDifficulty,
            Button mediumDifficulty,
            Button hardDifficulty,
            Text difficultyLabelText)
        {
            startButton = start;
            optionsButton = options;
            controlsButton = controls;
            quitButton = quit;
            levelSelectPanel = levelSelectPanelObject;
            levelSelectBackButton = levelSelectBack;
            levelOneButton = levelOne;
            levelTwoButton = levelTwo;
            levelThreeButton = levelThree;
            controlsPanel = controlsPanelObject;
            controlsBackButton = controlsBack;
            optionsHubPanel = optionsHubPanelObject;
            optionsHubBackButton = optionsHubBack;
            gameSettingsNavButton = gameSettingsNav;
            accessibilityNavButton = accessibilityNav;
            gameSettingsPanel = gameSettingsPanelObject;
            gameSettingsBackButton = gameSettingsBack;
            lapOneButton = lapOne;
            lapTwoButton = lapTwo;
            lapThreeButton = lapThree;
            lapFiveButton = lapFive;
            lapLabel = lapLabelText;
            policeOnButton = policeOn;
            policeOffButton = policeOff;
            policeLabel = policeLabelText;
            accessibilityPanel = accessibilityPanelObject;
            accessibilityBackButton = accessibilityBack;
            qualityLowButton = lowQuality;
            qualityMediumButton = mediumQuality;
            qualityHighButton = highQuality;
            qualityLabel = qualityLabelText;
            difficultyEasyButton = easyDifficulty;
            difficultyMediumButton = mediumDifficulty;
            difficultyHardButton = hardDifficulty;
            difficultyLabel = difficultyLabelText;

            HideAllSubPanels();
            WireButtons();
            RefreshSettingsUi();
        }

        void Start()
        {
            HideAllSubPanels();
            WireButtons();
            RefreshSettingsUi();
        }

        void HideAllSubPanels()
        {
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(false);
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(false);
            if (gameSettingsPanel != null)
                gameSettingsPanel.SetActive(false);
            if (accessibilityPanel != null)
                accessibilityPanel.SetActive(false);
        }

        void WireButtons()
        {
            Bind(startButton, OnStartClicked);
            Bind(optionsButton, OnOptionsClicked);
            Bind(controlsButton, OnControlsClicked);
            Bind(quitButton, OnQuitClicked);
            Bind(levelSelectBackButton, OnLevelSelectBackClicked);
            Bind(levelOneButton, () => OnLevelSelected(0));
            Bind(levelTwoButton, () => OnLevelSelected(1));
            Bind(levelThreeButton, () => OnLevelSelected(2));
            Bind(controlsBackButton, OnControlsBackClicked);
            Bind(optionsHubBackButton, OnOptionsHubBackClicked);
            Bind(gameSettingsNavButton, OnGameSettingsNavClicked);
            Bind(accessibilityNavButton, OnAccessibilityNavClicked);
            Bind(gameSettingsBackButton, OnGameSettingsBackClicked);
            Bind(accessibilityBackButton, OnAccessibilityBackClicked);
            Bind(lapOneButton, () => OnLapsSelected(1));
            Bind(lapTwoButton, () => OnLapsSelected(2));
            Bind(lapThreeButton, () => OnLapsSelected(3));
            Bind(lapFiveButton, () => OnLapsSelected(5));
            Bind(policeOnButton, () => OnPoliceSelected(true));
            Bind(policeOffButton, () => OnPoliceSelected(false));
            Bind(qualityLowButton, () => OnQualitySelected(QualityLevel.Low));
            Bind(qualityMediumButton, () => OnQualitySelected(QualityLevel.Medium));
            Bind(qualityHighButton, () => OnQualitySelected(QualityLevel.High));
            Bind(difficultyEasyButton, () => OnDifficultySelected(DifficultyLevel.Easy));
            Bind(difficultyMediumButton, () => OnDifficultySelected(DifficultyLevel.Medium));
            Bind(difficultyHardButton, () => OnDifficultySelected(DifficultyLevel.Hard));
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        void OnStartClicked()
        {
            HideAllSubPanels();
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(true);
            else if (GameManager.Instance != null)
                GameManager.Instance.StartNewCareer();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }

        void OnLevelSelected(int levelIndex)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartLevel(levelIndex);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }

        void OnLevelSelectBackClicked()
        {
            if (levelSelectPanel != null)
                levelSelectPanel.SetActive(false);
        }

        void OnOptionsClicked()
        {
            HideAllSubPanels();
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(true);
        }

        void OnControlsClicked()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(true);
        }

        void OnControlsBackClicked()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
        }

        void OnOptionsHubBackClicked()
        {
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(false);
        }

        void OnGameSettingsNavClicked()
        {
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(false);
            if (gameSettingsPanel != null)
                gameSettingsPanel.SetActive(true);
            RefreshSettingsUi();
        }

        void OnAccessibilityNavClicked()
        {
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(false);
            if (accessibilityPanel != null)
                accessibilityPanel.SetActive(true);
            RefreshSettingsUi();
        }

        void OnGameSettingsBackClicked()
        {
            if (gameSettingsPanel != null)
                gameSettingsPanel.SetActive(false);
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(true);
        }

        void OnAccessibilityBackClicked()
        {
            if (accessibilityPanel != null)
                accessibilityPanel.SetActive(false);
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(true);
        }

        void OnLapsSelected(int laps)
        {
            GameLapSettings.SetLaps(laps);
            RefreshSettingsUi();
        }

        void OnPoliceSelected(bool enabled)
        {
            GamePoliceSettings.SetEnabled(enabled);
            RefreshSettingsUi();
        }

        void OnQualitySelected(QualityLevel level)
        {
            GameQualitySettings.SetLevel(level);
            MainMenuSetup.ApplyMenuAtmosphere();
            RefreshSettingsUi();
        }

        void OnDifficultySelected(DifficultyLevel level)
        {
            GameDifficultySettings.SetLevel(level);
            RefreshSettingsUi();
        }

        void RefreshSettingsUi()
        {
            if (lapLabel != null)
                lapLabel.text = "Current: " + GameLapSettings.GetDisplayName(GameLapSettings.CurrentLaps);

            StyleTierButton(lapOneButton, GameLapSettings.CurrentLaps == 1, LowAccent);
            StyleTierButton(lapTwoButton, GameLapSettings.CurrentLaps == 2, MediumAccent);
            StyleTierButton(lapThreeButton, GameLapSettings.CurrentLaps == 3, HighAccent);
            StyleTierButton(lapFiveButton, GameLapSettings.CurrentLaps == 5, new Color(1f, 0.72f, 0.35f));

            if (policeLabel != null)
                policeLabel.text = "Current: " + GamePoliceSettings.GetDisplayName(GamePoliceSettings.Enabled);

            StyleTierButton(policeOnButton, GamePoliceSettings.Enabled, new Color(0.15f, 0.55f, 1f));
            StyleTierButton(policeOffButton, !GamePoliceSettings.Enabled, LowAccent);

            if (qualityLabel != null)
                qualityLabel.text = "Current: " + GameQualitySettings.GetDisplayName(GameQualitySettings.Current);
            if (difficultyLabel != null)
                difficultyLabel.text = "Current: " + GameDifficultySettings.GetDisplayName(GameDifficultySettings.Current);

            StyleTierButton(qualityLowButton, GameQualitySettings.Current == QualityLevel.Low, LowAccent);
            StyleTierButton(qualityMediumButton, GameQualitySettings.Current == QualityLevel.Medium, MediumAccent);
            StyleTierButton(qualityHighButton, GameQualitySettings.Current == QualityLevel.High, HighAccent);

            StyleTierButton(difficultyEasyButton, GameDifficultySettings.Current == DifficultyLevel.Easy, LowAccent);
            StyleTierButton(difficultyMediumButton, GameDifficultySettings.Current == DifficultyLevel.Medium,
                MediumAccent);
            StyleTierButton(difficultyHardButton, GameDifficultySettings.Current == DifficultyLevel.Hard, HighAccent);
        }

        static void StyleTierButton(Button button, bool selected, Color accent)
        {
            if (button == null)
                return;

            var fill = selected ? ButtonFillSelected : ButtonFill;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = fill;

            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = Color.Lerp(fill, accent, 0.22f);
            colors.pressedColor = Color.Lerp(fill, accent, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var glow = button.transform.Find("Glow")?.GetComponent<Image>();
            if (glow != null)
                glow.color = new Color(accent.r, accent.g, accent.b, selected ? 0.85f : 0.55f);

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.color = selected ? ButtonTextSelected : ButtonText;
        }

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
