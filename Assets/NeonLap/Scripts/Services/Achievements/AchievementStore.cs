using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Services.Achievements
{
    public static class AchievementStore
    {
        const string UnlockedPrefix = "NeonLap.Achievement.";
        const string WinCountKey = "NeonLap.Achievement.WinCount";
        const string StylePointsKey = "NeonLap.Achievement.StylePoints";

        public static bool IsUnlocked(string achievementId)
        {
            return PlayerPrefs.GetInt(UnlockedPrefix + achievementId, 0) == 1;
        }

        public static void Unlock(string achievementId)
        {
            if (IsUnlocked(achievementId))
                return;

            PlayerPrefs.SetInt(UnlockedPrefix + achievementId, 1);
            PlayerPrefs.Save();
        }

        public static int GetWinCount()
        {
            return PlayerPrefs.GetInt(WinCountKey, 0);
        }

        public static int AddWin()
        {
            var count = GetWinCount() + 1;
            PlayerPrefs.SetInt(WinCountKey, count);
            PlayerPrefs.Save();
            return count;
        }

        public static int AddStylePoints(int points)
        {
            var total = PlayerPrefs.GetInt(StylePointsKey, 0) + Mathf.Max(0, points);
            PlayerPrefs.SetInt(StylePointsKey, total);
            PlayerPrefs.Save();
            return total;
        }

        public static IReadOnlyList<string> GetUnlockedIds()
        {
            var list = new List<string>();
            foreach (var id in AllIds)
            {
                if (IsUnlocked(id))
                    list.Add(id);
            }

            return list;
        }

        public static readonly string[] AllIds =
        {
            AchievementIds.FirstWin,
            AchievementIds.TenWins,
            AchievementIds.CareerMedalGold,
            AchievementIds.AllCareerGold,
            AchievementIds.AllCareerSilver,
            AchievementIds.MaxCareerStars,
            AchievementIds.EndlessUnlocked,
            AchievementIds.AllUnderglowUnlocked,
            AchievementIds.FiveLapFinisher,
            AchievementIds.PersonalBestLap,
            AchievementIds.PoliceEscape,
            AchievementIds.ScoreAttack100k,
            AchievementIds.StyleMaster,
            AchievementIds.GarageCollector,
            AchievementIds.SpunThreeTimes,
            AchievementIds.PoliceBusted,
            AchievementIds.LastPlaceLap1,
        };
    }
}
