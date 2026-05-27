namespace NeonLap.Services.Achievements
{
    /// <summary>
    /// Maps internal achievement IDs to Steam API names (set in Steamworks partner site).
    /// </summary>
    public static class SteamAchievementMapping
    {
        public static string GetSteamApiName(string achievementId)
        {
            return achievementId switch
            {
                AchievementIds.FirstWin => "ACH_FIRST_WIN",
                AchievementIds.TenWins => "ACH_TEN_WINS",
                AchievementIds.CareerMedalGold => "ACH_CAREER_GOLD",
                AchievementIds.AllCareerGold => "ACH_ALL_GOLD",
                AchievementIds.AllCareerSilver => "ACH_ALL_SILVER",
                AchievementIds.MaxCareerStars => "ACH_MAX_STARS",
                AchievementIds.EndlessUnlocked => "ACH_ENDLESS",
                AchievementIds.AllUnderglowUnlocked => "ACH_ALL_UNDERGLOW",
                AchievementIds.FiveLapFinisher => "ACH_FIVE_LAPS",
                AchievementIds.PersonalBestLap => "ACH_PB_LAP",
                AchievementIds.PoliceEscape => "ACH_POLICE_ESCAPE",
                AchievementIds.ScoreAttack100k => "ACH_SCORE_100K",
                AchievementIds.StyleMaster => "ACH_STYLE_MASTER",
                AchievementIds.GarageCollector => "ACH_GARAGE_COLLECTOR",
                AchievementIds.SpunThreeTimes => "ACH_SPIN_THRICE",
                AchievementIds.PoliceBusted => "ACH_POLICE_BUSTED",
                AchievementIds.LastPlaceLap1 => "ACH_LAST_LAP1",
                _ => achievementId.ToUpperInvariant().Replace('.', '_'),
            };
        }
    }
}
