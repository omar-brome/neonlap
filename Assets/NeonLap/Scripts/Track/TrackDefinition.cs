using UnityEngine;

namespace NeonLap.Track
{
    public enum TrackLayout
    {
        Level1NeonCircuit = 0,
        Level2TurboSprint = 1,
        Level3MetroGauntlet = 2,
        Level4ZigZagThunder = 3,
        Level5SquareCircuit = 4,
        Level6RidgeRun = 5,
        Level7NeonCrossover = 6,

        // Legacy aliases for older assets and references.
        Oval = Level1NeonCircuit,
        TriOvalSpeedway = Level2TurboSprint,
        TechnicalRing = Level3MetroGauntlet,
        ZigZagRally = Level1NeonCircuit,
        ZigZagSprint = Level2TurboSprint,
        ZigZagMetro = Level3MetroGauntlet,
    }

    public enum TrackHazardDensity
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    [CreateAssetMenu(fileName = "TrackDefinition", menuName = "NeonLap/Track Definition")]
    public class TrackDefinition : ScriptableObject
    {
        public string trackName = "Neon Circuit";
        [TextArea(2, 4)] public string description = "Classic neon oval.";
        public string sceneName = "SampleScene";
        public TrackLayout layout = TrackLayout.Level1NeonCircuit;

        [Header("Visual theme")]
        public TrackTheme theme = TrackTheme.CityStreets;
        [Tooltip("When enabled, theme is used instead of the default theme for this layout.")]
        public bool themeOverridesLayout;

        public int lapCount = 1;
        public int checkpointCount = 10;
        public float straightLength = 60f;
        public float turnRadius = 25f;
        public float trackWidth = 26f;

        [Header("Browser")]
        public float lengthMeters = 2400f;
        public bool hasShortcuts;
        public TrackHazardDensity hazardDensity = TrackHazardDensity.Medium;

        [Header("Level tuning (optional)")]
        public TrackLevelModifiers levelModifiers;

        [Header("Hazard authoring")]
        [Tooltip("When enabled, only hazardWaypointIndices are used for static hazards.")]
        public bool useAuthoringHazardIndices;
        [Tooltip("AI waypoint / centerline point indices for static hazards.")]
        public int[] hazardWaypointIndices = System.Array.Empty<int>();

        public string GetLayoutLabel()
        {
            return layout switch
            {
                TrackLayout.Level1NeonCircuit or TrackLayout.Oval or TrackLayout.ZigZagRally => "Oval",
                TrackLayout.Level2TurboSprint or TrackLayout.TriOvalSpeedway or TrackLayout.ZigZagSprint =>
                    "Tri-Oval",
                TrackLayout.Level3MetroGauntlet or TrackLayout.TechnicalRing or TrackLayout.ZigZagMetro =>
                    "Technical Ring",
                TrackLayout.Level4ZigZagThunder => "Zigzag",
                TrackLayout.Level5SquareCircuit => "Square",
                TrackLayout.Level6RidgeRun => "Ridge / Elevation",
                TrackLayout.Level7NeonCrossover => "Crossover / Figure-8",
                _ => layout.ToString()
            };
        }

        public string GetHazardLabel()
        {
            return hazardDensity switch
            {
                TrackHazardDensity.Low => "Low",
                TrackHazardDensity.High => "High",
                _ => "Medium"
            };
        }

        public TrackTheme GetResolvedTheme() => TrackThemeProfile.ResolveTheme(this);

        public string GetThemeLabel() => TrackThemeProfile.Get(GetResolvedTheme()).DisplayName;

        public string GetBrowserSummary()
        {
            var shortcuts = hasShortcuts ? "Shortcuts: Yes" : "Shortcuts: No";
            return
                $"{GetLayoutLabel()}  •  {GetThemeLabel()}  •  {lengthMeters / 1000f:0.0} km  •  {shortcuts}  •  Hazards {GetHazardLabel()}";
        }
    }
}
