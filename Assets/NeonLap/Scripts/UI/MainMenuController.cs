using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Track;
using NeonLap.Vehicle;
using NeonLap.VFX;
using UnityEngine;
using QualityLevel = NeonLap.Core.QualityLevel;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class MainMenuController : MonoBehaviour
    {
        const string SelectedLevelPrefsKey = "NeonLap.Menu.SelectedLevel";

        [SerializeField] Button startButton;
        [SerializeField] Button garageButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button quitButton;
        [SerializeField] GameObject garagePanel;
        [SerializeField] Button garageBackButton;
        [SerializeField] Button garageBuildOneButton;
        [SerializeField] Button garageBuildTwoButton;
        [SerializeField] Button garageBuildThreeButton;
        [SerializeField] Button garageBuildFourButton;
        [SerializeField] Button garageBuildFiveButton;
        [SerializeField] Text garageStatsText;
        [SerializeField] Text garageDetailText;
        [SerializeField] Text garageUnlockText;
        [SerializeField] Button garageEquipButton;
        [SerializeField] Text garageCreditsText;
        [SerializeField] Button garagePaintButton;
        [SerializeField] Button garageDecalButton;
        [SerializeField] Button garageRimButton;
        [SerializeField] Button garageTrailButton;
        [SerializeField] Button garageHornButton;
        [SerializeField] GameObject levelSelectPanel;
        [SerializeField] Button levelSelectBackButton;
        [SerializeField] Button careerModeButton;
        [SerializeField] Button timeTrialModeButton;
        [SerializeField] Button eliminationModeButton;
        [SerializeField] Button chaseModeButton;
        [SerializeField] Button scoreAttackModeButton;
        [SerializeField] Button practiceModeButton;
        [SerializeField] Button customModeButton;
        [SerializeField] Button teamRaceModeButton;
        [SerializeField] Button ghostDuelModeButton;
        [SerializeField] Button demolitionModeButton;
        [SerializeField] Button hardcoreModeButton;
        [SerializeField] Button levelOneButton;
        [SerializeField] Button levelTwoButton;
        [SerializeField] Button levelThreeButton;
        [SerializeField] Button levelFourButton;
        [SerializeField] Button levelFiveButton;
        [SerializeField] Button levelSixButton;
        [SerializeField] Button levelSevenButton;
        [SerializeField] Text careerProgressText;
        [SerializeField] Text dailyChallengeText;
        [SerializeField] Button endlessModeButton;
        [SerializeField] Text trackPreviewText;
        [SerializeField] Button goRaceButton;
        [SerializeField] GameObject controlsPanel;
        [SerializeField] Button controlsBackButton;
        [SerializeField] GameObject optionsHubPanel;
        [SerializeField] Button optionsHubBackButton;
        [SerializeField] Button gameSettingsNavButton;
        [SerializeField] Button controlsNavButton;
        [SerializeField] Button accessibilityNavButton;
        [SerializeField] Button audioNavButton;
        [SerializeField] GameObject audioSettingsPanel;
        [SerializeField] Button audioSettingsBackButton;
        [SerializeField] Text audioSettingsLabel;
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
        [SerializeField] Button reverseOffButton;
        [SerializeField] Button reverseOnButton;
        [SerializeField] Button nightOffButton;
        [SerializeField] Button nightOnButton;
        [SerializeField] Button weatherForecastButton;
        [SerializeField] Button weatherDryButton;
        [SerializeField] Button weatherRainButton;
        [SerializeField] Button weatherFogButton;
        [SerializeField] Button weatherSandButton;
        [SerializeField] Text trackOptionsLabel;
        [SerializeField] Button teamBlueButton;
        [SerializeField] Button teamRedButton;
        [SerializeField] Text teamLabel;
        [SerializeField] Button ttRivalsButton;
        [SerializeField] Text ttSettingsLabel;
        [SerializeField] Button ghostCollisionOnButton;
        [SerializeField] Button ghostCollisionOffButton;
        [SerializeField] Button ttPoliceOnButton;
        [SerializeField] Button ttPoliceOffButton;
        [SerializeField] Button ttRanksOnButton;
        [SerializeField] Button ttRanksOffButton;
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

        Slider steeringAssistSlider;
        Slider masterVolumeSlider;
        Slider sfxVolumeSlider;
        Slider musicVolumeSlider;
        Button autoAccelOffButton;
        Button autoAccelOnButton;

        static readonly Color ButtonFill = new(0.05f, 0.06f, 0.12f, 0.96f);
        static readonly Color ButtonFillSelected = new(0.1f, 0.12f, 0.22f, 0.98f);
        static readonly Color ButtonText = new(0.97f, 0.98f, 1f);
        static readonly Color ButtonTextSelected = Color.white;
        static readonly Color LowAccent = new(1f, 0.45f, 0.55f);
        static readonly Color MediumAccent = new(0.55f, 0.75f, 1f);
        static readonly Color HighAccent = new(0.2f, 1f, 1f);
        static readonly Color GarageAccent = new(0.85f, 0.55f, 1f);

        int selectedLevelIndex;
        int previewBuildIndex;
        bool garagePreviewInitialized;

        public void Configure(
            Button start,
            Button garage,
            Button options,
            Button quit,
            GameObject garagePanelObject,
            Button garageBack,
            Button garageBuildOne,
            Button garageBuildTwo,
            Button garageBuildThree,
            Button garageBuildFour,
            Button garageBuildFive,
            Text garageStats,
            Text garageDetail,
            Text garageUnlock,
            Button garageEquip,
            Text garageCredits,
            Button garagePaint,
            Button garageDecal,
            Button garageRim,
            Button garageTrail,
            Button garageHorn,
            GameObject levelSelectPanelObject,
            Button levelSelectBack,
            Button careerMode,
            Button timeTrialMode,
            Button eliminationMode,
            Button chaseMode,
            Button scoreAttackMode,
            Button practiceMode,
            Button customMode,
            Button teamRaceMode,
            Button ghostDuelMode,
            Button demolitionMode,
            Button hardcoreMode,
            Button levelOne,
            Button levelTwo,
            Button levelThree,
            Button levelFour,
            Button levelFive,
            Button levelSix,
            Button levelSeven,
            Text careerProgress,
            Text dailyChallenge,
            Button endlessMode,
            Text trackPreview,
            Button goRace,
            GameObject controlsPanelObject,
            Button controlsBack,
            GameObject optionsHubPanelObject,
            Button optionsHubBack,
            Button gameSettingsNav,
            Button controlsNav,
            Button accessibilityNav,
            Button audioNav,
            GameObject audioSettingsPanelObject,
            Button audioSettingsBack,
            Slider masterVolume,
            Slider sfxVolume,
            Slider musicVolume,
            Text audioSettingsLabelText,
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
            Button reverseOff,
            Button reverseOn,
            Button nightOff,
            Button nightOn,
            Button weatherForecast,
            Button weatherDry,
            Button weatherRain,
            Button weatherFog,
            Button weatherSand,
            Text trackOptionsLabelText,
            Button teamBlue,
            Button teamRed,
            Text teamLabelText,
            Button ttRivals,
            Text ttSettings,
            Button ghostCollisionOn,
            Button ghostCollisionOff,
            Button ttPoliceOn,
            Button ttPoliceOff,
            Button ttRanksOn,
            Button ttRanksOff,
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
            garageButton = garage;
            optionsButton = options;
            quitButton = quit;
            garagePanel = garagePanelObject;
            garageBackButton = garageBack;
            garageBuildOneButton = garageBuildOne;
            garageBuildTwoButton = garageBuildTwo;
            garageBuildThreeButton = garageBuildThree;
            garageBuildFourButton = garageBuildFour;
            garageBuildFiveButton = garageBuildFive;
            garageStatsText = garageStats;
            garageDetailText = garageDetail;
            garageUnlockText = garageUnlock;
            garageEquipButton = garageEquip;
            garageCreditsText = garageCredits;
            garagePaintButton = garagePaint;
            garageDecalButton = garageDecal;
            garageRimButton = garageRim;
            garageTrailButton = garageTrail;
            garageHornButton = garageHorn;
            levelSelectPanel = levelSelectPanelObject;
            trackPreviewText = trackPreview;
            goRaceButton = goRace;
            selectedLevelIndex = PlayerPrefs.GetInt(SelectedLevelPrefsKey, 0);
            levelSelectBackButton = levelSelectBack;
            careerModeButton = careerMode;
            timeTrialModeButton = timeTrialMode;
            eliminationModeButton = eliminationMode;
            chaseModeButton = chaseMode;
            scoreAttackModeButton = scoreAttackMode;
            practiceModeButton = practiceMode;
            customModeButton = customMode;
            teamRaceModeButton = teamRaceMode;
            ghostDuelModeButton = ghostDuelMode;
            demolitionModeButton = demolitionMode;
            hardcoreModeButton = hardcoreMode;
            levelOneButton = levelOne;
            levelTwoButton = levelTwo;
            levelThreeButton = levelThree;
            levelFourButton = levelFour;
            levelFiveButton = levelFive;
            levelSixButton = levelSix;
            levelSevenButton = levelSeven;
            careerProgressText = careerProgress;
            dailyChallengeText = dailyChallenge;
            endlessModeButton = endlessMode;
            controlsPanel = controlsPanelObject;
            controlsBackButton = controlsBack;
            optionsHubPanel = optionsHubPanelObject;
            optionsHubBackButton = optionsHubBack;
            gameSettingsNavButton = gameSettingsNav;
            controlsNavButton = controlsNav;
            accessibilityNavButton = accessibilityNav;
            audioNavButton = audioNav;
            audioSettingsPanel = audioSettingsPanelObject;
            audioSettingsBackButton = audioSettingsBack;
            audioSettingsLabel = audioSettingsLabelText;
            masterVolumeSlider = masterVolume;
            sfxVolumeSlider = sfxVolume;
            musicVolumeSlider = musicVolume;
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
            reverseOffButton = reverseOff;
            reverseOnButton = reverseOn;
            nightOffButton = nightOff;
            nightOnButton = nightOn;
            weatherForecastButton = weatherForecast;
            weatherDryButton = weatherDry;
            weatherRainButton = weatherRain;
            weatherFogButton = weatherFog;
            weatherSandButton = weatherSand;
            trackOptionsLabel = trackOptionsLabelText;
            teamBlueButton = teamBlue;
            teamRedButton = teamRed;
            teamLabel = teamLabelText;
            ttRivalsButton = ttRivals;
            ttSettingsLabel = ttSettings;
            ghostCollisionOnButton = ghostCollisionOn;
            ghostCollisionOffButton = ghostCollisionOff;
            ttPoliceOnButton = ttPoliceOn;
            ttPoliceOffButton = ttPoliceOff;
            ttRanksOnButton = ttRanksOn;
            ttRanksOffButton = ttRanksOff;
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

            steeringAssistSlider = accessibilityPanel != null
                ? accessibilityPanel.transform.Find("SteeringAssistSlider")?.GetComponent<Slider>()
                : null;
            autoAccelOffButton = accessibilityPanel != null
                ? accessibilityPanel.transform.Find("AutoAccelOffButton")?.GetComponent<Button>()
                : null;
            autoAccelOnButton = accessibilityPanel != null
                ? accessibilityPanel.transform.Find("AutoAccelOnButton")?.GetComponent<Button>()
                : null;

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
            if (garagePanel != null)
                garagePanel.SetActive(false);
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
            if (audioSettingsPanel != null)
                audioSettingsPanel.SetActive(false);
        }

        void WireButtons()
        {
            Bind(startButton, OnStartClicked);
            Bind(garageButton, OnGarageClicked);
            Bind(optionsButton, OnOptionsClicked);
            Bind(quitButton, OnQuitClicked);
            Bind(garageBackButton, OnGarageBackClicked);
            Bind(garageEquipButton, OnGarageEquipClicked);
            Bind(garagePaintButton, OnGaragePaintClicked);
            Bind(garageDecalButton, OnGarageDecalClicked);
            Bind(garageRimButton, OnGarageRimClicked);
            Bind(garageTrailButton, OnGarageTrailClicked);
            Bind(garageHornButton, OnGarageHornClicked);
            Bind(garageBuildOneButton, () => PreviewBuildIndex(0));
            Bind(garageBuildTwoButton, () => PreviewBuildIndex(1));
            Bind(garageBuildThreeButton, () => PreviewBuildIndex(2));
            Bind(garageBuildFourButton, () => PreviewBuildIndex(3));
            Bind(garageBuildFiveButton, () => PreviewBuildIndex(4));
            Bind(levelSelectBackButton, OnLevelSelectBackClicked);
            Bind(goRaceButton, OnGoRaceClicked);
            if (endlessModeButton != null)
            {
                Bind(endlessModeButton, OnEndlessModeClicked);
            }
            Bind(careerModeButton, () => OnModeSelected(RaceMode.Career));
            Bind(timeTrialModeButton, () => OnModeSelected(RaceMode.TimeTrial));
            Bind(eliminationModeButton, () => OnModeSelected(RaceMode.Elimination));
            Bind(chaseModeButton, () => OnModeSelected(RaceMode.Chase));
            Bind(scoreAttackModeButton, () => OnModeSelected(RaceMode.ScoreAttack));
            Bind(practiceModeButton, () => OnModeSelected(RaceMode.Practice));
            Bind(customModeButton, () => OnModeSelected(RaceMode.Custom));
            Bind(teamRaceModeButton, () => OnModeSelected(RaceMode.TeamRace));
            Bind(ghostDuelModeButton, () => OnModeSelected(RaceMode.GhostDuel));
            Bind(demolitionModeButton, () => OnModeSelected(RaceMode.Demolition));
            Bind(hardcoreModeButton, () => OnModeSelected(RaceMode.Hardcore));
            Bind(teamBlueButton, () => OnTeamSelected(RaceTeam.Blue));
            Bind(teamRedButton, () => OnTeamSelected(RaceTeam.Red));
            Bind(levelOneButton, () => SelectLevel(0));
            Bind(levelTwoButton, () => SelectLevel(1));
            Bind(levelThreeButton, () => SelectLevel(2));
            Bind(levelFourButton, () => SelectLevel(3));
            Bind(levelFiveButton, () => SelectLevel(4));
            Bind(levelSixButton, () => SelectLevel(5));
            Bind(levelSevenButton, () => SelectLevel(6));
            Bind(controlsBackButton, OnControlsBackClicked);
            Bind(optionsHubBackButton, OnOptionsHubBackClicked);
            Bind(gameSettingsNavButton, OnGameSettingsNavClicked);
            Bind(controlsNavButton, OnControlsNavClicked);
            Bind(accessibilityNavButton, OnAccessibilityNavClicked);
            Bind(audioNavButton, OnAudioNavClicked);
            Bind(audioSettingsBackButton, OnAudioSettingsBackClicked);
            Bind(gameSettingsBackButton, OnGameSettingsBackClicked);
            Bind(accessibilityBackButton, OnAccessibilityBackClicked);
            Bind(lapOneButton, () => OnLapsSelected(1));
            Bind(lapTwoButton, () => OnLapsSelected(2));
            Bind(lapThreeButton, () => OnLapsSelected(3));
            Bind(lapFiveButton, () => OnLapsSelected(5));
            Bind(policeOnButton, () => OnPoliceSelected(true));
            Bind(policeOffButton, () => OnPoliceSelected(false));
            Bind(reverseOffButton, () => OnReverseSelected(false));
            Bind(reverseOnButton, () => OnReverseSelected(true));
            Bind(nightOffButton, () => OnNightSelected(false));
            Bind(nightOnButton, () => OnNightSelected(true));
            Bind(weatherForecastButton, () => OnWeatherSelected(TrackWeatherChoice.Forecast));
            Bind(weatherDryButton, () => OnWeatherSelected(TrackWeatherChoice.ForceDry));
            Bind(weatherRainButton, () => OnWeatherSelected(TrackWeatherChoice.ForceRain));
            Bind(weatherFogButton, () => OnWeatherSelected(TrackWeatherChoice.ForceFog));
            Bind(weatherSandButton, () => OnWeatherSelected(TrackWeatherChoice.ForceSandstorm));
            Bind(ttRivalsButton, CycleTimeTrialRivals);
            Bind(ghostCollisionOnButton, () => OnGhostCollisionSelected(true));
            Bind(ghostCollisionOffButton, () => OnGhostCollisionSelected(false));
            Bind(ttPoliceOnButton, () => OnTimeTrialPoliceSelected(true));
            Bind(ttPoliceOffButton, () => OnTimeTrialPoliceSelected(false));
            Bind(ttRanksOnButton, () => OnTimeTrialRanksSelected(true));
            Bind(ttRanksOffButton, () => OnTimeTrialRanksSelected(false));
            Bind(qualityLowButton, () => OnQualitySelected(QualityLevel.Low));
            Bind(qualityMediumButton, () => OnQualitySelected(QualityLevel.Medium));
            Bind(qualityHighButton, () => OnQualitySelected(QualityLevel.High));
            Bind(difficultyEasyButton, () => OnDifficultySelected(DifficultyLevel.Easy));
            Bind(difficultyMediumButton, () => OnDifficultySelected(DifficultyLevel.Medium));
            Bind(difficultyHardButton, () => OnDifficultySelected(DifficultyLevel.Hard));

            if (steeringAssistSlider != null)
            {
                steeringAssistSlider.onValueChanged.RemoveListener(OnSteeringAssistChanged);
                steeringAssistSlider.onValueChanged.AddListener(OnSteeringAssistChanged);
            }

            WireAudioSliders();

            Bind(autoAccelOffButton, () => OnAutoAccelSelected(false));
            Bind(autoAccelOnButton, () => OnAutoAccelSelected(true));
        }

        void OnSteeringAssistChanged(float value)
        {
            GameAccessibilitySettings.SetSteeringAssist(value);
            RefreshSettingsUi();
        }

        void OnAutoAccelSelected(bool enabled)
        {
            GameAccessibilitySettings.SetAutoAccelerate(enabled);
            RefreshSettingsUi();
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        void OnGarageClicked()
        {
            HideAllSubPanels();
            if (garagePanel != null)
            {
                garagePanel.SetActive(true);
                RefreshGarageUi();
            }
        }

        void OnGarageBackClicked()
        {
            if (garagePanel != null)
                garagePanel.SetActive(false);
        }

        void OnGarageEquipClicked()
        {
            var build = GetPreviewBuild();
            if (build == null)
                return;

            if (!PlayerGarageStore.IsUnlocked(build))
            {
                if (!PlayerGarageStore.TryPurchaseUnlock(build))
                {
                    RefreshGarageUi();
                    return;
                }
            }

            PlayerGarageStore.SelectedIndex = previewBuildIndex;
            var showcase = Object.FindAnyObjectByType<MainMenuShowcase>();
            showcase?.ApplyGaragePreview(build);
            RefreshGarageUi();
        }

        void PreviewBuildIndex(int index)
        {
            var registry = PlayerGarageStore.Registry;
            if (registry == null || registry.Count == 0)
                return;

            previewBuildIndex = Mathf.Clamp(index, 0, registry.Count - 1);
            garagePreviewInitialized = true;
            RefreshGarageUi();
            var showcase = Object.FindAnyObjectByType<MainMenuShowcase>();
            showcase?.ApplyGaragePreview(GetPreviewBuild());
        }

        HoverBuildDefinition GetPreviewBuild()
        {
            var registry = PlayerGarageStore.Registry;
            return registry != null ? registry.GetBuild(previewBuildIndex) : null;
        }

        void RefreshGarageUi()
        {
            var registry = PlayerGarageStore.Registry;
            if (registry == null || registry.Count == 0)
                return;

            if (!garagePreviewInitialized)
                previewBuildIndex = PlayerGarageStore.SelectedIndex;

            previewBuildIndex = Mathf.Clamp(previewBuildIndex, 0, registry.Count - 1);
            var build = registry.GetBuild(previewBuildIndex);
            if (build == null)
                return;

            RefreshBuildButton(garageBuildOneButton, 0);
            RefreshBuildButton(garageBuildTwoButton, 1);
            RefreshBuildButton(garageBuildThreeButton, 2);
            RefreshBuildButton(garageBuildFourButton, 3);
            RefreshBuildButton(garageBuildFiveButton, 4);

            var profile = build.profile;
            if (garageStatsText != null)
            {
                var classLine = VehicleClassRules.GetClassBadge(build);
                garageStatsText.text = profile != null
                    ? $"{build.displayName.ToUpperInvariant()}  •  {classLine}  •  {VehicleStatsFormatter.FormatSummary(profile)}"
                    : $"{build.displayName.ToUpperInvariant()}  •  {classLine}";
            }

            if (garageDetailText != null)
            {
                garageDetailText.text = build.tagline;
                if (profile != null)
                    garageDetailText.text += "\n\n" + VehicleStatsFormatter.FormatDetail(profile);
            }

            if (garageUnlockText != null)
            {
                var unlocked = PlayerGarageStore.GetUnlockedCount();
                var total = registry.Count;
                var status = PlayerGarageStore.GetUnlockStatus(build);
                var paint = VehicleCustomizationCatalog.GetPaintPreset(VehicleCustomizationStore.PaintPresetIndex);
                garageUnlockText.text =
                    $"Status: {status}\nGarage {unlocked}/{total} builds\nPaint: {paint.DisplayName}  •  Decal: {VehicleCustomizationCatalog.GetDecal(VehicleCustomizationStore.DecalIndex).DisplayName}\nCareer ★ {CareerScoreStore.GetTotalStars()}  •  Credits {CareerCurrencyStore.Balance:N0}";
            }

            if (garageEquipButton != null)
            {
                var equipLabel = garageEquipButton.GetComponentInChildren<Text>();
                if (equipLabel != null)
                    equipLabel.text = PlayerGarageStore.GetEquipButtonLabel(build, previewBuildIndex);

                var canInteract = PlayerGarageStore.IsUnlocked(build)
                                  || PlayerGarageStore.CanPurchaseWithCredits(build);
                garageEquipButton.interactable = canInteract;
            }

            RefreshGarageCosmeticsUi();
        }

        void RefreshGarageCosmeticsUi()
        {
            if (garageCreditsText != null)
            {
                garageCreditsText.text =
                    $"Credits {CareerCurrencyStore.Balance:N0}  •  Career ★ {CareerScoreStore.GetTotalStars()}  •  Builds {PlayerGarageStore.GetUnlockedCount()}/{PlayerGarageStore.Registry?.Count ?? 0}";
            }

            RefreshCustomizationButton(
                garagePaintButton,
                "PAINT",
                VehicleCustomizationCatalog.GetPaintPreset(VehicleCustomizationStore.PaintPresetIndex).DisplayName);
            RefreshCustomizationButton(
                garageDecalButton,
                "DECAL",
                VehicleCustomizationCatalog.GetDecal(VehicleCustomizationStore.DecalIndex).DisplayName);
            RefreshCustomizationButton(
                garageRimButton,
                "RIMS",
                VehicleCustomizationCatalog.GetRim(VehicleCustomizationStore.RimIndex).DisplayName);

            if (garageTrailButton != null)
            {
                var index = VehicleUnderglowUnlockStore.SelectedIndex;
                var option = VehicleUnderglowUnlockStore.GetOption(index);
                var unlocked = VehicleUnderglowUnlockStore.IsUnlocked(index);
                garageTrailButton.interactable = unlocked;
                var label = garageTrailButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = unlocked
                        ? $"UNDERGLOW: {option.DisplayName}"
                        : $"UNDERGLOW: {option.DisplayName}  •  {option.GetUnlockHint()}";
                }
            }

            if (garageHornButton != null)
            {
                garageHornButton.interactable = true;
                var label = garageHornButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = CareerCosmeticStore.HornUnlocked ? "HORN: UNLOCKED" : "BUY HORN (1,200)";
            }
        }

        void RefreshCustomizationButton(Button button, string prefix, string optionName)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = $"{prefix}: {optionName}";
        }

        void OnGaragePaintClicked()
        {
            VehicleCustomizationStore.CyclePaint();
            RefreshGarageCustomizationPreview();
        }

        void OnGarageDecalClicked()
        {
            VehicleCustomizationStore.CycleDecal();
            RefreshGarageCustomizationPreview();
        }

        void OnGarageRimClicked()
        {
            VehicleCustomizationStore.CycleRim();
            RefreshGarageCustomizationPreview();
        }

        void RefreshGarageCustomizationPreview()
        {
            RefreshGarageCosmeticsUi();
            var showcase = Object.FindAnyObjectByType<MainMenuShowcase>();
            var build = GetPreviewBuild();
            if (build != null)
                showcase?.ApplyGaragePreview(build);
        }

        void OnGarageTrailClicked()
        {
            VehicleUnderglowUnlockStore.CycleToNextUnlocked();
            RefreshGarageCosmeticsUi();
        }

        void OnGarageHornClicked()
        {
            if (!CareerCosmeticStore.HornUnlocked)
                CareerCosmeticStore.TryUnlockHorn();

            RefreshGarageCosmeticsUi();
        }

        void RefreshBuildButton(Button button, int buildIndex)
        {
            if (button == null)
                return;

            var registry = PlayerGarageStore.Registry;
            if (registry == null || buildIndex >= registry.Count)
            {
                button.gameObject.SetActive(false);
                return;
            }

            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = PlayerGarageStore.GetBuildButtonLabel(buildIndex);

            var selected = buildIndex == previewBuildIndex;
            StyleTierButton(button, selected, GarageAccent);
            button.interactable = true;
        }

        void OnStartClicked()
        {
            HideAllSubPanels();
            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(true);
                RefreshLevelSelectUi();
                RefreshTrackPreview();
            }
            else if (GameManager.Instance != null)
                GameManager.Instance.StartNewCareer();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }

        void OnModeSelected(RaceMode mode)
        {
            GameRaceModeSettings.SetMode(mode);
            RefreshLevelSelectUi();
        }

        void SelectLevel(int levelIndex)
        {
            if (GameRaceModeSettings.Rules.RequiresCareerUnlock
                && !CareerScoreStore.IsTrackUnlocked(levelIndex))
                return;

            selectedLevelIndex = levelIndex;
            PlayerPrefs.SetInt(SelectedLevelPrefsKey, selectedLevelIndex);
            PlayerPrefs.Save();
            RefreshTrackPreview();
        }

        void OnGoRaceClicked()
        {
            if (GameRaceModeSettings.Rules.RequiresCareerUnlock
                && !CareerScoreStore.IsTrackUnlocked(selectedLevelIndex))
                return;

            if (GameRaceModeSettings.IsCareer)
                PlayerGarageStore.EnsureLegalBuildForTrack(selectedLevelIndex);

            if (GameManager.Instance == null)
                MainMenuSetup.EnsureGameManagerExists();

            DailyChallengeService.ApplyTodayModifiersForRace(selectedLevelIndex);

            if (GameManager.Instance != null)
                GameManager.Instance.StartRace(selectedLevelIndex, GameRaceModeSettings.Current);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }

        void OnEndlessModeClicked()
        {
            if (!StuntProgressionGate.IsUnlocked())
                return;

            GameRaceModeSettings.SetMode(RaceMode.StuntFreestyle);
            GameLapSettings.SetLaps(1);
            GamePoliceSettings.SetEnabled(false);
            GameTrackOptions.SetWeatherChoice(TrackWeatherChoice.ForceDry);

            if (GameManager.Instance == null)
                MainMenuSetup.EnsureGameManagerExists();

            if (GameManager.Instance != null)
                GameManager.Instance.StartRace(selectedLevelIndex, RaceMode.StuntFreestyle);
        }

        void RefreshTrackPreview()
        {
            if (trackPreviewText == null)
                return;

            if (GameManager.Instance == null)
                MainMenuSetup.EnsureGameManagerExists();

            var track = GameManager.Instance != null
                ? GameManager.Instance.GetTrackDefinition(selectedLevelIndex)
                : null;

            if (track == null)
            {
                trackPreviewText.text = "Select a track to see layout stats.";
                return;
            }

            var modifier = Track.TrackLevelConfig.GetModifierNote(selectedLevelIndex);
            var variants = GameTrackOptions.GetSummaryLine();
            var medalHint = GameRaceModeSettings.IsCareer
                ? CareerMedalTables.GetMedalHint(selectedLevelIndex, RaceMedal.Gold)
                : GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel
                    ? TimeTrialMedalUtility.GetRequirementHint(selectedLevelIndex)
                    : string.Empty;
            var modeLine =
                $"Race mode: {GameRaceModeSettings.GetDisplayName(GameRaceModeSettings.Current)} — pick CAREER or TIME TRIAL above, then START.";
            var reverseHint = GameTrackOptions.GetReverseRaceHint();
            var classHint = GameRaceModeSettings.IsCareer
                ? VehicleClassRules.GetTrackClassLimitLine(selectedLevelIndex)
                : string.Empty;
            trackPreviewText.text =
                $"{GameTrackOptions.FormatTrackName(track.trackName).ToUpperInvariant()}  •  {track.GetBrowserSummary()}\n{track.description}\n{modeLine}\nVariants: {variants}"
                + (string.IsNullOrWhiteSpace(classHint) ? string.Empty : $"\n{classHint}")
                + (string.IsNullOrWhiteSpace(reverseHint) ? string.Empty : $"\n{reverseHint}")
                + (string.IsNullOrWhiteSpace(modifier) ? string.Empty : $"\n{modifier}")
                + (string.IsNullOrWhiteSpace(medalHint) ? string.Empty : $"\n{medalHint}");
        }

        void RefreshLevelSelectUi()
        {
            var mode = GameRaceModeSettings.Current;
            StyleTierButton(careerModeButton, mode == RaceMode.Career, MediumAccent);
            StyleTierButton(timeTrialModeButton, mode == RaceMode.TimeTrial, new Color(0.85f, 0.55f, 1f));
            StyleTierButton(eliminationModeButton, mode == RaceMode.Elimination, new Color(1f, 0.45f, 0.55f));
            StyleTierButton(chaseModeButton, mode == RaceMode.Chase, new Color(1f, 0.72f, 0.35f));
            StyleTierButton(scoreAttackModeButton, mode == RaceMode.ScoreAttack, new Color(0.45f, 1f, 1f));
            StyleTierButton(practiceModeButton, mode == RaceMode.Practice, new Color(0.65f, 1f, 0.55f));
            StyleTierButton(customModeButton, mode == RaceMode.Custom, new Color(0.95f, 0.75f, 0.35f));
            StyleTierButton(teamRaceModeButton, mode == RaceMode.TeamRace, new Color(0.35f, 0.65f, 1f));
            StyleTierButton(ghostDuelModeButton, mode == RaceMode.GhostDuel, new Color(0.55f, 0.95f, 1f));
            StyleTierButton(demolitionModeButton, mode == RaceMode.Demolition, new Color(1f, 0.55f, 0.2f));
            StyleTierButton(hardcoreModeButton, mode == RaceMode.Hardcore, new Color(1f, 0.38f, 0.42f));
            RefreshCareerProgressionUi();
            RefreshLevelButtonLabels();
            RefreshLevelButtonInteractable();
            RefreshTrackPreview();
        }

        void RefreshCareerProgressionUi()
        {
            if (careerProgressText != null)
                careerProgressText.text = CareerProgressionGate.GetStarProgressLine();

            if (dailyChallengeText != null)
                dailyChallengeText.text = DailyChallengeService.GetMenuBannerLine();

            if (endlessModeButton != null)
            {
                var showStunt = StuntProgressionGate.IsUnlocked() && GameRaceModeSettings.IsCareer;
                endlessModeButton.gameObject.SetActive(showStunt);
            }
        }

        void RefreshLevelButtonInteractable()
        {
            var requiresUnlock = GameRaceModeSettings.Rules.RequiresCareerUnlock;
            SetLevelButtonInteractable(levelOneButton, !requiresUnlock || CareerScoreStore.IsTrackUnlocked(0));
            SetLevelButtonInteractable(levelTwoButton, !requiresUnlock || CareerScoreStore.IsTrackUnlocked(1));
            SetLevelButtonInteractable(levelThreeButton, !requiresUnlock || CareerScoreStore.IsTrackUnlocked(2));
            SetLevelButtonInteractable(levelFourButton, !requiresUnlock || CareerScoreStore.IsTrackUnlocked(3));
            SetLevelButtonInteractable(levelFiveButton, !requiresUnlock || CareerScoreStore.IsTrackUnlocked(4));
            SetLevelButtonInteractable(levelSixButton, !requiresUnlock || CareerScoreStore.IsTrackUnlocked(5));
            SetLevelButtonInteractable(levelSevenButton, !requiresUnlock || CareerScoreStore.IsTrackUnlocked(6));
        }

        static void SetLevelButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        void RefreshLevelButtonLabels()
        {
            SetLevelButtonLabel(levelOneButton, 0);
            SetLevelButtonLabel(levelTwoButton, 1);
            SetLevelButtonLabel(levelThreeButton, 2);
            SetLevelButtonLabel(levelFourButton, 3);
            SetLevelButtonLabel(levelFiveButton, 4);
            SetLevelButtonLabel(levelSixButton, 5);
            SetLevelButtonLabel(levelSevenButton, 6);
        }

        static void SetLevelButtonLabel(Button button, int levelIndex)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<Text>();
            if (label == null)
                return;

            var manager = GameManager.Instance != null
                ? GameManager.Instance
                : Object.FindAnyObjectByType<GameManager>();
            var track = manager != null ? manager.GetTrackDefinition(levelIndex) : null;
            var trackName = track != null ? track.trackName.ToUpperInvariant() : $"TRACK {levelIndex + 1}";

            if (GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel)
            {
                label.text = $"L{levelIndex + 1}  {TimeTrialMedalStore.GetTrackSummary(levelIndex)}";
                return;
            }

            if (GameRaceModeSettings.IsScoreAttack)
            {
                label.text = $"L{levelIndex + 1}  {trackName}  •  {ScoreAttackRecordStore.GetTrackSummary(levelIndex)}";
                return;
            }

            if (GameRaceModeSettings.IsCareer)
            {
                label.text = CareerScoreStore.GetLevelButtonLabel(levelIndex, trackName);
                return;
            }

            if (GameRaceModeSettings.Rules.RequiresCareerUnlock && !CareerScoreStore.IsTrackUnlocked(levelIndex))
            {
                label.text = CareerScoreStore.GetLevelButtonLabel(levelIndex, trackName);
                return;
            }

            label.text = $"L{levelIndex + 1}  {trackName}";
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

        void OnControlsNavClicked()
        {
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(false);
            if (controlsPanel != null)
                controlsPanel.SetActive(true);
        }

        void OnControlsBackClicked()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(true);
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

        void OnAudioNavClicked()
        {
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(false);
            if (audioSettingsPanel != null)
                audioSettingsPanel.SetActive(true);
            RefreshAudioSettingsUi();
        }

        void OnAudioSettingsBackClicked()
        {
            if (audioSettingsPanel != null)
                audioSettingsPanel.SetActive(false);
            if (optionsHubPanel != null)
                optionsHubPanel.SetActive(true);
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

        void OnReverseSelected(bool enabled)
        {
            GameTrackOptions.SetReverseCircuit(enabled);
            RefreshSettingsUi();
            RefreshTrackPreview();
        }

        void OnNightSelected(bool enabled)
        {
            GameTrackOptions.SetNightVariant(enabled);
            RefreshSettingsUi();
            RefreshTrackPreview();
        }

        void OnWeatherSelected(TrackWeatherChoice choice)
        {
            GameTrackOptions.SetWeatherChoice(choice);
            RefreshSettingsUi();
            RefreshTrackPreview();
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

        void OnTeamSelected(RaceTeam team)
        {
            GameTeamRaceSettings.SetPlayerTeam(team);
            RefreshSettingsUi();
        }

        void CycleTimeTrialRivals()
        {
            var next = (TimeTrialSettings.RivalCount + 1) % 4;
            TimeTrialSettings.SetRivalCount(next);
            RefreshSettingsUi();
        }

        void OnGhostCollisionSelected(bool enabled)
        {
            TimeTrialSettings.SetGhostCollisionPenalty(enabled);
            RefreshSettingsUi();
        }

        void OnTimeTrialPoliceSelected(bool enabled)
        {
            TimeTrialSettings.SetPoliceEnabled(enabled);
            RefreshSettingsUi();
        }

        void OnTimeTrialRanksSelected(bool show)
        {
            TimeTrialSettings.SetShowTimeRanks(show);
            RefreshSettingsUi();
        }

        void WireAudioSliders()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(GameAudioSettings.MasterVolume);
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(GameAudioSettings.SfxVolume);
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(GameAudioSettings.MusicVolume);
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
        }

        void OnMasterVolumeChanged(float value)
        {
            GameAudioSettings.SetMasterVolume(value);
            RefreshAudioSettingsUi();
        }

        void OnSfxVolumeChanged(float value)
        {
            GameAudioSettings.SetSfxVolume(value);
            RefreshAudioSettingsUi();
        }

        void OnMusicVolumeChanged(float value)
        {
            GameAudioSettings.SetMusicVolume(value);
            RefreshAudioSettingsUi();
        }

        void RefreshAudioSettingsUi()
        {
            if (audioSettingsLabel != null)
                audioSettingsLabel.text = GameAudioSettings.GetSummaryLine();
        }

        void RefreshSettingsUi()
        {
            RefreshAudioSettingsUi();

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

            if (trackOptionsLabel != null)
                trackOptionsLabel.text = "Variants: " + GameTrackOptions.GetSummaryLine();

            StyleTierButton(reverseOffButton, !GameTrackOptions.ReverseCircuit, MediumAccent);
            StyleTierButton(reverseOnButton, GameTrackOptions.ReverseCircuit, HighAccent);
            StyleTierButton(nightOffButton, !GameTrackOptions.NightVariant, new Color(1f, 0.72f, 0.35f));
            StyleTierButton(nightOnButton, GameTrackOptions.NightVariant, new Color(0.85f, 0.55f, 1f));
            StyleTierButton(weatherForecastButton, GameTrackOptions.WeatherChoice == TrackWeatherChoice.Forecast,
                MediumAccent);
            StyleTierButton(weatherDryButton, GameTrackOptions.WeatherChoice == TrackWeatherChoice.ForceDry,
                new Color(0.65f, 1f, 0.55f));
            StyleTierButton(weatherRainButton, GameTrackOptions.WeatherChoice == TrackWeatherChoice.ForceRain,
                new Color(0.15f, 0.55f, 1f));
            StyleTierButton(weatherFogButton, GameTrackOptions.WeatherChoice == TrackWeatherChoice.ForceFog,
                new Color(0.72f, 0.78f, 0.88f));
            StyleTierButton(weatherSandButton, GameTrackOptions.WeatherChoice == TrackWeatherChoice.ForceSandstorm,
                new Color(0.95f, 0.62f, 0.25f));

            if (teamLabel != null)
                teamLabel.text = "Team: " + GameTeamRaceSettings.GetDisplayName(GameTeamRaceSettings.PlayerTeam);

            StyleTierButton(teamBlueButton, GameTeamRaceSettings.PlayerTeam == RaceTeam.Blue,
                GameTeamRaceSettings.GetTeamColor(RaceTeam.Blue));
            StyleTierButton(teamRedButton, GameTeamRaceSettings.PlayerTeam == RaceTeam.Red,
                GameTeamRaceSettings.GetTeamColor(RaceTeam.Red));

            if (ttSettingsLabel != null)
                ttSettingsLabel.text = TimeTrialSettings.GetSummaryLine();

            StyleTierButton(ghostCollisionOnButton, TimeTrialSettings.GhostCollisionPenalty,
                new Color(1f, 0.55f, 0.2f));
            StyleTierButton(ghostCollisionOffButton, !TimeTrialSettings.GhostCollisionPenalty,
                new Color(0.55f, 0.75f, 1f));
            StyleTierButton(ttPoliceOnButton, TimeTrialSettings.PoliceEnabled, new Color(0.15f, 0.55f, 1f));
            StyleTierButton(ttPoliceOffButton, !TimeTrialSettings.PoliceEnabled, new Color(0.55f, 0.75f, 1f));
            StyleTierButton(ttRanksOnButton, TimeTrialSettings.ShowTimeRanks, new Color(1f, 0.92f, 0.35f));
            StyleTierButton(ttRanksOffButton, !TimeTrialSettings.ShowTimeRanks, new Color(0.55f, 0.75f, 1f));

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

            if (steeringAssistSlider != null)
                steeringAssistSlider.SetValueWithoutNotify(GameAccessibilitySettings.SteeringAssist);

            StyleTierButton(autoAccelOffButton, !GameAccessibilitySettings.AutoAccelerate, LowAccent);
            StyleTierButton(autoAccelOnButton, GameAccessibilitySettings.AutoAccelerate, new Color(0.65f, 1f, 0.55f));
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
