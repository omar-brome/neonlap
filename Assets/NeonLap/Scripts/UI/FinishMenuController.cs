using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Services.Platform;
using NeonLap.Track;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class FinishMenuController : MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        [SerializeField] GameObject finishPanel;
        [SerializeField] Button mainMenuButton;
        [SerializeField] Button restartButton;
        [SerializeField] Text restartLabel;
        [SerializeField] Button nextLevelButton;
        [SerializeField] Text nextLevelLabel;
        [SerializeField] Button itchExportButton;

        bool subscribed;

        public void Configure(
            RaceManager manager,
            GameObject panel,
            Button mainMenu,
            Button restart,
            Button nextLevel,
            Text nextLevelText,
            Text restartText = null)
        {
            Unsubscribe();
            raceManager = manager;
            finishPanel = panel;
            mainMenuButton = mainMenu;
            restartButton = restart;
            restartLabel = restartText ?? restart?.GetComponentInChildren<Text>();
            nextLevelButton = nextLevel;
            nextLevelLabel = nextLevelText;
            WireButtons();
            Subscribe();
            HideNextLevelButton();
        }

        void OnEnable()
        {
            WireButtons();
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void WireButtons()
        {
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(GoToMainMenu);
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartRace);
                restartButton.onClick.AddListener(RestartRace);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(GoToNextLevel);
                nextLevelButton.onClick.AddListener(GoToNextLevel);
            }
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        public void ConfigureItchExportButton(Button exportButton)
        {
            itchExportButton = exportButton;
            if (itchExportButton == null)
                return;

            itchExportButton.onClick.RemoveListener(CopyItchLeaderboardLine);
            itchExportButton.onClick.AddListener(CopyItchLeaderboardLine);
            itchExportButton.gameObject.SetActive(GameRaceModeSettings.IsTimeTrial);
        }

        public void ConfigureTimeTrialFinish(bool canAdvanceNext)
        {
            if (restartLabel != null)
                restartLabel.text = "RETRY";

            if (itchExportButton != null)
                itchExportButton.gameObject.SetActive(GameRaceModeSettings.IsTimeTrial);

            if (nextLevelButton == null)
                return;

            nextLevelButton.gameObject.SetActive(canAdvanceNext);
            if (!canAdvanceNext || nextLevelLabel == null)
                return;

            var manager = Core.GameManager.Instance;
            var nextTrack = manager != null ? manager.GetTrackDefinition(manager.CurrentLevelIndex + 1) : null;
            nextLevelLabel.text = nextTrack != null
                ? $"NEXT: {nextTrack.trackName.ToUpper()}"
                : "NEXT TRACK";
        }

        void HandleRaceFinished(int placement)
        {
            if (!Core.GameRaceModeSettings.IsCareer)
            {
                if (!Core.GameRaceModeSettings.IsTimeTrial && !Core.GameRaceModeSettings.IsGhostDuel)
                {
                    if (nextLevelButton != null)
                        nextLevelButton.gameObject.SetActive(false);
                }

                return;
            }

            var manager = Core.GameManager.Instance;
            var canAdvance = placement == 1 && manager != null && manager.HasNextLevel;

            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(canAdvance);

            if (nextLevelLabel == null)
                return;

            if (canAdvance)
            {
                var nextTrack = manager.GetTrackDefinition(manager.CurrentLevelIndex + 1);
                nextLevelLabel.text = nextTrack != null
                    ? $"NEXT: {nextTrack.trackName.ToUpper()}"
                    : "NEXT LEVEL";
            }
        }

        void HideNextLevelButton()
        {
            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(false);
        }

        void GoToMainMenu()
        {
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.LoadMainMenu();
        }

        void RestartRace()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.RestartCurrentLevel();
                return;
            }

            if (raceManager != null)
                raceManager.RestartRace();
        }

        void GoToNextLevel()
        {
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.LoadNextLevel();
        }

        void CopyItchLeaderboardLine()
        {
            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            ItchIoHonorLeaderboardExporter.TryCopyTrackLineToClipboard(trackIndex);
        }
    }
}
