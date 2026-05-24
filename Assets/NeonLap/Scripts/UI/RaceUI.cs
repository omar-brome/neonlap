using NeonLap.Race;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class RaceUI : MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        [SerializeField] Text lapText;
        [SerializeField] Text lapTimerText;
        [SerializeField] Text raceTimerText;
        [SerializeField] Text bestLapText;
        [SerializeField] Text positionText;
        [SerializeField] Text countdownText;
        [SerializeField] Text countdownSubtitleText;
        [SerializeField] GameObject countdownPanel;
        [SerializeField] GameObject finishPanel;
        [SerializeField] Text finishTitleText;
        [SerializeField] Text finishDetailText;

        bool subscribed;

        public void Configure(
            RaceManager manager,
            Text lap,
            Text lapTimer,
            Text raceTimer,
            Text bestLap,
            Text position,
            Text countdown,
            Text countdownSubtitle,
            GameObject countdownPanelObject,
            GameObject finishPanelObject,
            Text finishTitle,
            Text finishDetail)
        {
            Unsubscribe();
            raceManager = manager;
            lapText = lap;
            lapTimerText = lapTimer;
            raceTimerText = raceTimer;
            bestLapText = bestLap;
            positionText = position;
            countdownText = countdown;
            countdownSubtitleText = countdownSubtitle;
            countdownPanel = countdownPanelObject;
            finishPanel = finishPanelObject;
            finishTitleText = finishTitle;
            finishDetailText = finishDetail;
            Subscribe();
            SyncCountdownFromManager();
        }

        void Awake()
        {
            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();
        }

        void Start()
        {
            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();
            Subscribe();
            SyncCountdownFromManager();
        }

        void OnEnable()
        {
            Subscribe();
            SyncCountdownFromManager();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnCountdownTick += HandleCountdownTick;
            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnCountdownTick -= HandleCountdownTick;
            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void SyncCountdownFromManager()
        {
            if (raceManager == null)
                return;

            if (raceManager.State == RaceState.Countdown)
                ShowCountdown(raceManager.CountdownValue);
        }

        void Update()
        {
            if (raceManager == null)
                return;

            if (lapText != null)
                lapText.text = $"Lap {Mathf.Min(raceManager.CurrentLap, raceManager.TotalLaps)}/{raceManager.TotalLaps}";

            if (lapTimerText != null && raceManager.State == RaceState.Racing)
                lapTimerText.text = FormatTime(raceManager.LapTime);

            if (raceTimerText != null && (raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished))
                raceTimerText.text = FormatTime(raceManager.RaceTime);

            if (bestLapText != null)
            {
                var best = raceManager.BestLapTime;
                bestLapText.text = best > 0f ? $"Best {FormatTime(best)}" : "Best --:--.--";
            }

            if (positionText != null && (raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished))
            {
                var position = raceManager.GetPlayerPosition();
                var total = Mathf.Max(raceManager.TotalRacers, 1);
                positionText.text = $"Position {GetPlacementLabel(position)} / {total}";
                positionText.color = position == 1
                    ? new Color(0.45f, 1f, 1f)
                    : Color.white;
            }
            else if (positionText != null)
            {
                positionText.text = string.Empty;
            }
        }

        void HandleCountdownTick(int value)
        {
            ShowCountdown(value);
        }

        void ShowCountdown(int value)
        {
            if (finishPanel != null)
                finishPanel.SetActive(false);

            if (countdownPanel != null)
                countdownPanel.SetActive(true);

            if (countdownSubtitleText != null)
                countdownSubtitleText.text = value > 0 ? "GET READY" : string.Empty;

            if (countdownText == null)
                return;

            countdownText.text = value > 0 ? value.ToString() : "GO!";
            countdownText.color = value > 0 ? new Color(0.45f, 1f, 1f) : new Color(1f, 0.95f, 0.35f);
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
            {
                if (finishPanel != null)
                    finishPanel.SetActive(false);
                SyncCountdownFromManager();
                return;
            }

            if (countdownPanel != null && state == RaceState.Racing)
                countdownPanel.SetActive(false);

            if (finishPanel != null && state == RaceState.Countdown)
                finishPanel.SetActive(false);
        }

        void HandleLapCompleted(int lap)
        {
            if (lapTimerText != null)
                lapTimerText.text = FormatTime(raceManager.LastLapTime);
        }

        void HandleRaceFinished(int placement)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            if (finishPanel != null)
                finishPanel.SetActive(true);

            if (finishTitleText != null)
                finishTitleText.text = placement == 1 ? "YOU WON!" : "RACE FINISHED";

            if (finishDetailText != null)
            {
                if (placement == 1)
                    finishDetailText.text = $"Time {FormatTime(raceManager.RaceTime)}";
                else
                    finishDetailText.text = $"{GetPlacementLabel(placement)} Place  •  {FormatTime(raceManager.RaceTime)}";
            }
        }

        static string GetPlacementLabel(int placement)
        {
            var suffix = (placement % 100) switch
            {
                11 or 12 or 13 => "th",
                _ when placement % 10 == 1 => "st",
                _ when placement % 10 == 2 => "nd",
                _ when placement % 10 == 3 => "rd",
                _ => "th"
            };

            return $"{placement}{suffix}";
        }

        static string FormatTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = seconds % 60f;
            return $"{minutes:00}:{secs:00.00}";
        }
    }
}
