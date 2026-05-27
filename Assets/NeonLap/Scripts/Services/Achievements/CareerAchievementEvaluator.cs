using NeonLap.Core;
using NeonLap.Race;

namespace NeonLap.Services.Achievements
{
    public static class CareerAchievementEvaluator
    {
        public static void SyncAll()
        {
            SyncCareerMedals();
            SyncStarTotals();
            SyncGarage();
        }

        static void SyncCareerMedals()
        {
            var levelCount = GameManager.Instance != null ? GameManager.Instance.TotalLevels : 6;
            var allGold = true;
            var allSilver = true;

            for (var i = 0; i < levelCount; i++)
            {
                var medal = CareerScoreStore.GetBestMedal(i);
                if (medal < RaceMedal.Gold)
                    allGold = false;
                if (medal < RaceMedal.Silver)
                    allSilver = false;
            }

            if (allGold && levelCount > 0)
                AchievementTracker.TryUnlock(AchievementIds.AllCareerGold);

            if (allSilver && levelCount > 0)
                AchievementTracker.TryUnlock(AchievementIds.AllCareerSilver);
        }

        static void SyncStarTotals()
        {
            if (CareerScoreStore.GetTotalStars() >= CareerProgressionGate.MaxStars)
                AchievementTracker.TryUnlock(AchievementIds.MaxCareerStars);

            if (CareerProgressionGate.IsEndlessUnlocked())
                AchievementTracker.TryUnlock(AchievementIds.EndlessUnlocked);
        }

        static void SyncGarage()
        {
            if (VehicleUnderglowUnlockStore.GetUnlockedCount() >= VehicleUnderglowUnlockStore.CatalogLength)
                AchievementTracker.TryUnlock(AchievementIds.AllUnderglowUnlocked);
        }
    }
}
