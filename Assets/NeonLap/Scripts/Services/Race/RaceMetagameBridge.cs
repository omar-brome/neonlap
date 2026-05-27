using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Services.Race
{
    public struct RaceMetagameResult
    {
        public int CreditsEarned;
        public int XpEarned;
        public int TotalXpAfter;
        public int PlacementStars;
        public CareerRaceResult CareerResult;
        public string DailyChallengeDescription;
        public bool DailyCompleted;
        public int DailyBonusCredits;
        public int DailyBonusStars;
    }

    /// <summary>
    /// Applies race finish rewards that persist across sessions (credits, daily challenges, career PBs).
    /// </summary>
    public class RaceMetagameBridge : MonoBehaviour
    {
        RaceManager raceManager;
        RaceScoreSystem scoreSystem;
        bool subscribed;

        public static RaceMetagameResult Latest { get; private set; }

        public static RaceMetagameBridge Setup(RaceManager manager, GameObject playerCar)
        {
            var go = new GameObject("RaceMetagameBridge");
            go.transform.SetParent(manager.transform, false);
            var bridge = go.AddComponent<RaceMetagameBridge>();
            bridge.Configure(manager, playerCar);
            return bridge;
        }

        void Configure(RaceManager manager, GameObject playerCar)
        {
            raceManager = manager;
            scoreSystem = playerCar != null ? playerCar.GetComponent<RaceScoreSystem>() : null;
            Latest = default;
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
            Latest = default;

            if (!GameRaceModeSettings.IsCareer)
                return;

            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var shortcutMet = RaceShortcutTracker.Instance == null || RaceShortcutTracker.Instance.ShortcutRequirementMet;

            var daily = DailyChallengeService.EvaluateRace(trackIndex, raceManager.RaceTime, GamePoliceSettings.Enabled);
            if (daily.Completed && daily.BonusCredits > 0 && scoreSystem != null)
                scoreSystem.ApplyDailyChallengeBonus(daily.BonusCredits);

            var score = scoreSystem != null ? scoreSystem.Score : 0;
            var credits = CareerCurrencyStore.CreditsFromRaceScore(score);
            CareerCurrencyStore.Add(credits);

            if (daily.Completed && daily.BonusCredits > 0)
            {
                CareerCurrencyStore.Add(daily.BonusCredits);
                credits += daily.BonusCredits;
            }

            var careerResult = CareerScoreStore.RecordRace(
                trackIndex,
                score,
                placement,
                raceManager.BestLapTime,
                shortcutMet);
            var xpEarned = RaceFinishRewards.GetXpEarned(score, placement);
            var totalXp = CareerXpStore.Add(xpEarned);

            Latest = new RaceMetagameResult
            {
                CreditsEarned = credits,
                XpEarned = xpEarned,
                TotalXpAfter = totalXp,
                PlacementStars = RaceFinishRewards.GetPlacementStars(placement),
                CareerResult = careerResult,
                DailyChallengeDescription = daily.Description,
                DailyCompleted = daily.Completed,
                DailyBonusCredits = daily.BonusCredits,
                DailyBonusStars = daily.BonusStarsAwarded,
            };
        }
    }
}

