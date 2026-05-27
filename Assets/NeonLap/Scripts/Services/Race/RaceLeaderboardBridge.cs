using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Services.Leaderboard;
using UnityEngine;

namespace NeonLap.Services.Race
{
    public class RaceLeaderboardBridge : MonoBehaviour
    {
        RaceManager raceManager;
        RaceScoreSystem scoreSystem;
        bool subscribed;

        public static RaceLeaderboardBridge Setup(RaceManager manager, GameObject playerCar)
        {
            var go = new GameObject("RaceLeaderboardBridge");
            go.transform.SetParent(manager.transform, false);
            var bridge = go.AddComponent<RaceLeaderboardBridge>();
            bridge.Configure(manager, playerCar);
            return bridge;
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
            if (subscribed)
                return;

            RaceEventHub.RaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;

            RaceEventHub.RaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void HandleRaceFinished(RaceFinishEvent finish)
        {
            var trackIndex = finish.TrackIndex;
            var modeName = GameRaceModeSettings.GetShortName(finish.Mode);

            switch (finish.Mode)
            {
                case RaceMode.TimeTrial:
                    if (finish.RaceTime > 0.05f)
                        LeaderboardService.SubmitTime(modeName, trackIndex, finish.RaceTime);
                    if (finish.BestLapTime > 0.05f)
                        LeaderboardService.SubmitTime(modeName + "_lap", trackIndex, finish.BestLapTime);
                    break;

                case RaceMode.ScoreAttack:
                    if (finish.Score > 0)
                        LeaderboardService.SubmitScore(modeName, trackIndex, finish.Score);
                    break;

                case RaceMode.Chase:
                    if (finish.Won && finish.RaceTime > 0.05f)
                        LeaderboardService.SubmitTime(modeName, trackIndex, finish.RaceTime);
                    break;
            }
        }
    }
}
