using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Services.Race
{
    /// <summary>
    /// Publishes <see cref="RaceEventHub"/> events from <see cref="RaceManager"/> state.
    /// </summary>
    public class RaceSessionReporter : MonoBehaviour
    {
        RaceManager raceManager;
        RaceScoreSystem scoreSystem;
        bool subscribed;

        public static RaceSessionReporter Setup(RaceManager manager, GameObject playerCar)
        {
            var go = new GameObject("RaceSessionReporter");
            go.transform.SetParent(manager.transform, false);
            var reporter = go.AddComponent<RaceSessionReporter>();
            reporter.Configure(manager, playerCar);
            return reporter;
        }

        void Configure(RaceManager manager, GameObject playerCar)
        {
            raceManager = manager;
            scoreSystem = playerCar != null ? playerCar.GetComponent<RaceScoreSystem>() : null;
            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

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

        void HandleRaceFinished(int placement)
        {
            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            RaceEventHub.PublishRaceFinished(new RaceFinishEvent
            {
                Placement = placement,
                TrackIndex = trackIndex,
                Mode = GameRaceModeSettings.Current,
                RaceTime = raceManager.RaceTime,
                BestLapTime = raceManager.BestLapTime,
                Score = scoreSystem != null ? scoreSystem.Score : 0,
                Won = placement == 1,
            });
        }

        public void PublishLapPersonalBest(float lapTime)
        {
            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            RaceEventHub.PublishLapPersonalBest(new LapPersonalBestEvent
            {
                TrackIndex = trackIndex,
                LapTime = lapTime,
                Mode = GameRaceModeSettings.Current,
            });
        }

        public void PublishPoliceEscape(int checkpoints, float survivalTime, bool checkpointEscape)
        {
            RaceEventHub.PublishPoliceEscaped(new PoliceEscapeEvent
            {
                CheckpointsPassed = checkpoints,
                SurvivalTime = survivalTime,
                CheckpointEscape = checkpointEscape,
            });
        }
    }
}
