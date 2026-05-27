using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackCatalog
    {
        const string RegistryResourcePath = "NeonLap/TrackRegistry";

        public static TrackRegistry LoadRegistry()
        {
            var registry = Resources.Load<TrackRegistry>(RegistryResourcePath);
            if (registry != null && registry.Count > 0)
                return registry;

            return CreateRuntimeRegistry();
        }

        static TrackRegistry CreateRuntimeRegistry()
        {
            var registry = ScriptableObject.CreateInstance<TrackRegistry>();
            registry.tracks = new[]
            {
                CreateRuntimeTrack("Neon Circuit", TrackLayout.Level1NeonCircuit, 2400f, false,
                    TrackHazardDensity.Low, 88f, 22f, 26f, 10),
                CreateRuntimeTrack("Turbo Sprint", TrackLayout.Level2TurboSprint, 2800f, true,
                    TrackHazardDensity.Medium, 102f, 20f, 27f, 12),
                CreateRuntimeTrack("Metro Gauntlet", TrackLayout.Level3MetroGauntlet, 2650f, true,
                    TrackHazardDensity.High, 96f, 21f, 27f, 12),
                CreateRuntimeTrack("Zigzag Thunder", TrackLayout.Level4ZigZagThunder, 3100f, false,
                    TrackHazardDensity.Medium, 110f, 18f, 26f, 12),
                CreateRuntimeTrack("Square Circuit", TrackLayout.Level5SquareCircuit, 2950f, true,
                    TrackHazardDensity.Medium, 100f, 18f, 27f, 12),
                CreateRuntimeTrack("Ridge Run", TrackLayout.Level6RidgeRun, 2700f, false,
                    TrackHazardDensity.High, 92f, 22f, 27f, 12),
                CreateRuntimeTrack("Neon Crossover", TrackLayout.Level7NeonCrossover, 2850f, true,
                    TrackHazardDensity.Medium, 98f, 20f, 27f, 12),
            };
            return registry;
        }

        static TrackDefinition CreateRuntimeTrack(
            string trackName,
            TrackLayout layout,
            float lengthMeters,
            bool hasShortcuts,
            TrackHazardDensity hazardDensity,
            float straightLength,
            float turnRadius,
            float trackWidth,
            int checkpoints)
        {
            var track = ScriptableObject.CreateInstance<TrackDefinition>();
            track.trackName = trackName;
            track.layout = layout;
            track.sceneName = "SampleScene";
            track.lapCount = 1;
            track.lengthMeters = lengthMeters;
            track.hasShortcuts = hasShortcuts;
            track.hazardDensity = hazardDensity;
            track.straightLength = straightLength;
            track.turnRadius = turnRadius;
            track.trackWidth = trackWidth;
            track.checkpointCount = checkpoints;
            track.description = track.GetBrowserSummary();
            return track;
        }
    }
}
