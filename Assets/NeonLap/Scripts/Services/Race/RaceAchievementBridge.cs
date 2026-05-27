using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Services.Achievements;
using UnityEngine;

namespace NeonLap.Services.Race
{
    public class RaceAchievementBridge : MonoBehaviour
    {
        RaceScoreSystem scoreSystem;
        bool subscribed;

        public static RaceAchievementBridge Setup(RaceManager manager, GameObject playerCar)
        {
            var go = new GameObject("RaceAchievementBridge");
            go.transform.SetParent(manager.transform, false);
            var bridge = go.AddComponent<RaceAchievementBridge>();
            bridge.Configure(playerCar);
            return bridge;
        }

        void Configure(GameObject playerCar)
        {
            scoreSystem = playerCar != null ? playerCar.GetComponent<RaceScoreSystem>() : null;
            AchievementTracker.EnsureInstance();
            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed)
                return;

            RaceEventHub.RaceFinished += HandleRaceFinishedEvent;
            RaceEventHub.LapPersonalBest += HandleLapPersonalBest;
            RaceEventHub.PoliceEscaped += HandlePoliceEscaped;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;

            RaceEventHub.RaceFinished -= HandleRaceFinishedEvent;
            RaceEventHub.LapPersonalBest -= HandleLapPersonalBest;
            RaceEventHub.PoliceEscaped -= HandlePoliceEscaped;
            subscribed = false;
        }

        void HandleRaceFinishedEvent(RaceFinishEvent finish) =>
            ProcessRaceFinish(finish.Placement, finish.RaceTime, finish.BestLapTime);

        void ProcessRaceFinish(int placement, float raceTime, float bestLap)
        {
            if (placement != 1)
                return;

            var wins = AchievementStore.AddWin();
            if (wins == 1)
                AchievementTracker.TryUnlock(AchievementIds.FirstWin);
            if (wins >= 10)
                AchievementTracker.TryUnlock(AchievementIds.TenWins);

            if (GameRaceModeSettings.IsCareer && GameManager.Instance != null)
            {
                var trackIndex = GameManager.Instance.CurrentLevelIndex;
                var shortcutMet = RaceShortcutTracker.Instance == null || RaceShortcutTracker.Instance.ShortcutRequirementMet;
                var medal = RaceMedalUtility.Evaluate(
                    trackIndex,
                    scoreSystem != null ? scoreSystem.Score : 0,
                    placement,
                    bestLap,
                    shortcutMet);
                if (medal == RaceMedal.Gold)
                    AchievementTracker.TryUnlock(AchievementIds.CareerMedalGold);
            }

            if (GameLapSettings.CurrentLaps >= 5 && raceTime > 1f)
                AchievementTracker.TryUnlock(AchievementIds.FiveLapFinisher);

            if (GameRaceModeSettings.IsScoreAttack && scoreSystem != null && scoreSystem.Score >= 100000)
                AchievementTracker.TryUnlock(AchievementIds.ScoreAttack100k);

            if (scoreSystem != null)
            {
                var styleTotal = AchievementStore.AddStylePoints(GetStylePointsFromBreakdown());
                if (styleTotal >= 2500)
                    AchievementTracker.TryUnlock(AchievementIds.StyleMaster);
            }

            if (PlayerGarageStore.GetUnlockedCount() >= 5)
                AchievementTracker.TryUnlock(AchievementIds.GarageCollector);

            CareerAchievementEvaluator.SyncAll();
        }

        int GetStylePointsFromBreakdown()
        {
            if (scoreSystem == null)
                return 0;

            foreach (var line in scoreSystem.GetBreakdownLines())
            {
                if (line.Label == "Barrel Roll")
                    return line.Amount;
            }

            return 0;
        }

        void HandleLapPersonalBest(LapPersonalBestEvent lapPb)
        {
            if (lapPb.LapTime > 0.05f)
                AchievementTracker.TryUnlock(AchievementIds.PersonalBestLap);
        }

        void HandlePoliceEscaped(PoliceEscapeEvent escape)
        {
            AchievementTracker.TryUnlock(AchievementIds.PoliceEscape);
        }
    }
}
