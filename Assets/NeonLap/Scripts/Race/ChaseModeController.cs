using NeonLap.Core;
using NeonLap.Services.Race;
using NeonLap.UI;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public class ChaseModeController : MonoBehaviour
    {
        [SerializeField] float survivalDuration = 120f;
        [SerializeField] int checkpointEscapeTarget = 8;
        [SerializeField] int escapeLapTarget = 3;

        RaceManager raceManager;
        RaceUI raceUi;
        RacePodiumSequence podiumSequence;
        PlayerHeatSystem heatSystem;
        PoliceChaseSystem policeChase;
        int checkpointsPassed;
        bool subscribed;
        bool finished;
        bool lastRunWon;
        bool wonByLapEscape;

        public int CheckpointEscapeTarget => checkpointEscapeTarget;

        public static ChaseModeController Setup(
            RaceManager manager,
            RaceUI ui,
            RacePodiumSequence podium,
            GameObject playerCar,
            PoliceChaseSystem police)
        {
            if (!GameRaceModeSettings.IsChase)
                return null;

            var go = new GameObject("ChaseMode");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<ChaseModeController>();
            controller.Configure(manager, ui, podium, playerCar, police);
            return controller;
        }

        void Configure(
            RaceManager manager,
            RaceUI ui,
            RacePodiumSequence podium,
            GameObject playerCar,
            PoliceChaseSystem police)
        {
            raceManager = manager;
            raceUi = ui;
            podiumSequence = podium;
            policeChase = police;

            if (podiumSequence != null)
                podiumSequence.enabled = false;

            manager.SetPlayerLapFinishEnabled(false);
            police?.SetOutrunMode(true);
            heatSystem = PlayerHeatSystem.Setup(playerCar, manager);
            if (heatSystem != null)
                heatSystem.HeatMaxed += HandleBusted;

            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            Unsubscribe();
            if (heatSystem != null)
                heatSystem.HeatMaxed -= HandleBusted;
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnCheckpointPassed += HandleCheckpointPassed;
            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnCheckpointPassed -= HandleCheckpointPassed;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void Update()
        {
            if (finished || raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (raceManager.RaceTime >= survivalDuration)
                CompleteSurvivalWin();
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Racing)
            {
                finished = false;
                lastRunWon = false;
                wonByLapEscape = false;
                checkpointsPassed = 0;
            }
        }

        void HandleLapCompleted(int lap)
        {
            if (finished || escapeLapTarget <= 0)
                return;

            if (lap >= escapeLapTarget)
                CompleteLapEscapeWin();
        }

        void HandleCheckpointPassed(RacerProgress racer, TrackCheckpoint checkpoint)
        {
            if (finished || racer == null || !racer.IsPlayer)
                return;

            checkpointsPassed++;
            if (checkpointsPassed >= checkpointEscapeTarget)
                CompleteEscapeWin();
        }

        void HandleBusted()
        {
            if (finished)
                return;

            finished = true;
            lastRunWon = false;
            raceManager.EndPlayerRace(1);
        }

        void CompleteSurvivalWin()
        {
            if (finished)
                return;

            finished = true;
            lastRunWon = true;
            PublishEscapeEvent(checkpointEscape: false);
            raceManager.EndPlayerRace(1);
        }

        void CompleteEscapeWin()
        {
            if (finished)
                return;

            finished = true;
            lastRunWon = true;
            PublishEscapeEvent(checkpointEscape: true);
            raceManager.EndPlayerRace(1);
        }

        void CompleteLapEscapeWin()
        {
            if (finished)
                return;

            finished = true;
            lastRunWon = true;
            wonByLapEscape = true;
            PublishEscapeEvent(checkpointEscape: false);
            raceManager.EndPlayerRace(1);
        }

        void PublishEscapeEvent(bool checkpointEscape)
        {
            RaceEventHub.PublishPoliceEscaped(new PoliceEscapeEvent
            {
                CheckpointsPassed = checkpointsPassed,
                SurvivalTime = raceManager != null ? raceManager.RaceTime : 0f,
                CheckpointEscape = checkpointEscape,
            });
        }

        void HandleRaceFinished(int placement)
        {
            if (raceUi == null || raceManager == null)
                return;

            var won = lastRunWon;
            var heatPct = heatSystem != null ? Mathf.RoundToInt(heatSystem.NormalizedHeat * 100f) : 0;
            var detail = won
                ? wonByLapEscape
                    ? $"Cleared lap {escapeLapTarget}  •  Heat {heatPct}%"
                    : checkpointsPassed >= checkpointEscapeTarget
                        ? $"Escaped via {checkpointsPassed} checkpoints  •  Heat {heatPct}%"
                        : $"Survived {Mathf.FloorToInt(survivalDuration)}s  •  Heat {heatPct}%"
                : $"Busted at {FormatTime(raceManager.RaceTime)}  •  Heat maxed";

            raceUi.ShowModeFinish(
                won ? "YOU ESCAPED!" : "BUSTED!",
                detail,
                won ? new Color(1f, 0.92f, 0.35f) : new Color(1f, 0.45f, 0.55f));
        }

        static string FormatTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = seconds % 60f;
            return $"{minutes:00}:{secs:00.00}";
        }
    }
}
