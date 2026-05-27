namespace NeonLap.Track
{
    public static class TrackLayoutUtility
    {
        public const int LevelCount = 7;

        public static TrackLayout LayoutForLevelIndex(int levelIndex)
        {
            return levelIndex switch
            {
                1 => TrackLayout.Level2TurboSprint,
                2 => TrackLayout.Level3MetroGauntlet,
                3 => TrackLayout.Level4ZigZagThunder,
                4 => TrackLayout.Level5SquareCircuit,
                5 => TrackLayout.Level6RidgeRun,
                6 => TrackLayout.Level7NeonCrossover,
                _ => TrackLayout.Level1NeonCircuit,
            };
        }

        public static int LevelIndexForLayout(TrackLayout layout)
        {
            return (int)Normalize(layout);
        }

        public static TrackLayout Normalize(TrackLayout layout)
        {
            return (int)layout switch
            {
                1 => TrackLayout.Level2TurboSprint,
                2 => TrackLayout.Level3MetroGauntlet,
                3 => TrackLayout.Level4ZigZagThunder,
                4 => TrackLayout.Level5SquareCircuit,
                5 => TrackLayout.Level6RidgeRun,
                6 => TrackLayout.Level7NeonCrossover,
                _ => TrackLayout.Level1NeonCircuit,
            };
        }

        public static bool HasElevation(TrackLayout layout) => LevelIndexForLayout(layout) == 5;

        public static bool IsComplexLayout(TrackLayout layout) => LevelIndexForLayout(layout) >= 2;

        public static bool IsZigZagLayout(TrackLayout layout) => IsComplexLayout(layout);
    }
}
