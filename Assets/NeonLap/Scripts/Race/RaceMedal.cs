namespace NeonLap.Race
{
    public enum RaceMedal
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
    }

    public static class RaceMedalUtility
    {
        public static RaceMedal Evaluate(
            int score,
            int placement,
            float bestLapTime,
            bool shortcutRequirementMet = true)
        {
            return Evaluate(0, score, placement, bestLapTime, shortcutRequirementMet);
        }

        public static RaceMedal Evaluate(
            int trackIndex,
            int score,
            int placement,
            float bestLapTime,
            bool shortcutRequirementMet = true)
        {
            if (!shortcutRequirementMet)
                return RaceMedal.None;

            var table = CareerMedalTables.Get(trackIndex);
            var lapBonus = bestLapTime > 0.05f && bestLapTime <= table.GoldLapTimeMax;

            if (placement <= table.GoldMaxPlacement && score >= table.GoldScore)
                return lapBonus ? RaceMedal.Gold : RaceMedal.Silver;

            if (placement <= table.SilverMaxPlacement && score >= table.SilverScore)
                return RaceMedal.Silver;

            if (placement <= table.BronzeMaxPlacement && score >= table.BronzeScore)
                return RaceMedal.Bronze;

            if (placement == 1 && score >= table.BronzeScore - 200)
                return RaceMedal.Bronze;

            return RaceMedal.None;
        }

        public static int StarsFromMedal(RaceMedal medal)
        {
            return medal switch
            {
                RaceMedal.Gold => 3,
                RaceMedal.Silver => 2,
                RaceMedal.Bronze => 1,
                _ => 0
            };
        }

        public static string GetMedalLabel(RaceMedal medal)
        {
            return medal switch
            {
                RaceMedal.Gold => "GOLD",
                RaceMedal.Silver => "SILVER",
                RaceMedal.Bronze => "BRONZE",
                _ => "—"
            };
        }

        public static char GetMedalLetter(RaceMedal medal)
        {
            return medal switch
            {
                RaceMedal.Gold => 'G',
                RaceMedal.Silver => 'S',
                RaceMedal.Bronze => 'B',
                _ => '-'
            };
        }

        public static string GetMedalSubtitle(RaceMedal medal)
        {
            return medal switch
            {
                RaceMedal.Gold => "Podium legend",
                RaceMedal.Silver => "Solid run",
                RaceMedal.Bronze => "On the board",
                _ => "No medal earned"
            };
        }

        public static string FormatStars(int stars)
        {
            stars = UnityEngine.Mathf.Clamp(stars, 0, 3);
            return stars switch
            {
                3 => "★★★",
                2 => "★★☆",
                1 => "★☆☆",
                _ => "☆☆☆"
            };
        }

        public static string FormatCompactProgress(int stars, RaceMedal medal)
        {
            return $"{FormatStars(stars)}  {GetMedalLetter(medal)}";
        }
    }
}
