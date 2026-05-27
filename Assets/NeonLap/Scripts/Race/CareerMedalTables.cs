namespace NeonLap.Race
{
    public readonly struct CareerMedalThresholds
    {
        public readonly int BronzeScore;
        public readonly int BronzeMaxPlacement;
        public readonly int SilverScore;
        public readonly int SilverMaxPlacement;
        public readonly int GoldScore;
        public readonly int GoldMaxPlacement;
        public readonly float GoldLapTimeMax;

        public CareerMedalThresholds(
            int bronzeScore,
            int bronzeMaxPlacement,
            int silverScore,
            int silverMaxPlacement,
            int goldScore,
            int goldMaxPlacement,
            float goldLapTimeMax)
        {
            BronzeScore = bronzeScore;
            BronzeMaxPlacement = bronzeMaxPlacement;
            SilverScore = silverScore;
            SilverMaxPlacement = silverMaxPlacement;
            GoldScore = goldScore;
            GoldMaxPlacement = goldMaxPlacement;
            GoldLapTimeMax = goldLapTimeMax;
        }
    }

    public static class CareerMedalTables
    {
        public static CareerMedalThresholds Get(int trackIndex)
        {
            trackIndex = UnityEngine.Mathf.Clamp(trackIndex, 0, 5);
            var tier = trackIndex;

            var bronzeScore = 750 + tier * 120;
            var silverScore = 1400 + tier * 220;
            var goldScore = 2100 + tier * 320;
            var goldLapMax = 48f - tier * 1.5f;

            return new CareerMedalThresholds(
                bronzeScore: bronzeScore,
                bronzeMaxPlacement: 3,
                silverScore: silverScore,
                silverMaxPlacement: 2,
                goldScore: goldScore,
                goldMaxPlacement: 1,
                goldLapTimeMax: goldLapMax);
        }

        public static string GetMedalHint(int trackIndex, RaceMedal targetMedal)
        {
            var table = Get(trackIndex);
            return targetMedal switch
            {
                RaceMedal.Gold =>
                    $"P1 + {table.GoldScore:N0} pts" + (table.GoldLapTimeMax > 0f
                        ? $" + lap ≤ {table.GoldLapTimeMax:0}s"
                        : string.Empty),
                RaceMedal.Silver => $"P{table.SilverMaxPlacement} + {table.SilverScore:N0} pts",
                RaceMedal.Bronze => $"P{table.BronzeMaxPlacement} + {table.BronzeScore:N0} pts",
                _ => "Finish on podium with a solid score"
            };
        }
    }
}
