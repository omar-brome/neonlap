#if UNITY_EDITOR
using System.Collections.Generic;
using NeonLap.Track;
using UnityEditor;
using UnityEngine;

namespace NeonLap.Editor
{
    public static class TrackContentMenu
    {
        const string ScriptableTracksFolder = "Assets/NeonLap/ScriptableObjects/Tracks";
        const string RegistryPath = "Assets/NeonLap/Resources/NeonLap/TrackRegistry.asset";

        struct TrackSeed
        {
            public string fileName;
            public string trackName;
            public TrackLayout layout;
            public TrackTheme theme;
            public float lengthMeters;
            public bool hasShortcuts;
            public TrackHazardDensity hazardDensity;
            public float straightLength;
            public float turnRadius;
            public float trackWidth;
            public int checkpointCount;
            public string description;
        }

        static readonly TrackSeed[] Seeds =
        {
            new()
            {
                fileName = "Track_Level01_NeonCircuit",
                trackName = "Neon Circuit",
                layout = TrackLayout.Level1NeonCircuit,
                theme = TrackTheme.CityStreets,
                lengthMeters = 2400f,
                hasShortcuts = false,
                hazardDensity = TrackHazardDensity.Low,
                straightLength = 88f,
                turnRadius = 22f,
                trackWidth = 26f,
                checkpointCount = 10,
                description = "Classic neon oval. Wide lines and forgiving turns — great for learning the circuit.",
            },
            new()
            {
                fileName = "Track_Level02_TurboSprint",
                trackName = "Turbo Sprint",
                layout = TrackLayout.Level2TurboSprint,
                theme = TrackTheme.CityStreets,
                lengthMeters = 2800f,
                hasShortcuts = true,
                hazardDensity = TrackHazardDensity.Medium,
                straightLength = 102f,
                turnRadius = 20f,
                trackWidth = 27f,
                checkpointCount = 12,
                description = "Tri-oval stadium with a chicane on the back straight.",
            },
            new()
            {
                fileName = "Track_Level03_MetroGauntlet",
                trackName = "Metro Gauntlet",
                layout = TrackLayout.Level3MetroGauntlet,
                theme = TrackTheme.DockyardNight,
                lengthMeters = 2650f,
                hasShortcuts = true,
                hazardDensity = TrackHazardDensity.High,
                straightLength = 96f,
                turnRadius = 21f,
                trackWidth = 27f,
                checkpointCount = 12,
                description = "Technical ring with esses, hairpin, and a metro tunnel sector.",
            },
            new()
            {
                fileName = "Track_Level04_ZigzagThunder",
                trackName = "Zigzag Thunder",
                layout = TrackLayout.Level4ZigZagThunder,
                theme = TrackTheme.DesertCanyon,
                lengthMeters = 3100f,
                hasShortcuts = false,
                hazardDensity = TrackHazardDensity.Medium,
                straightLength = 110f,
                turnRadius = 18f,
                trackWidth = 26f,
                checkpointCount = 12,
                description = "Lightning zigzag circuit built from long straights and sharp transitions.",
            },
            new()
            {
                fileName = "Track_Level05_SquareCircuit",
                trackName = "Square Circuit",
                layout = TrackLayout.Level5SquareCircuit,
                theme = TrackTheme.MountainPass,
                lengthMeters = 2950f,
                hasShortcuts = true,
                hazardDensity = TrackHazardDensity.Medium,
                straightLength = 100f,
                turnRadius = 18f,
                trackWidth = 27f,
                checkpointCount = 12,
                description = "Equal-sided square loop with tight 90° corners.",
            },
            new()
            {
                fileName = "Track_Level06_RidgeRun",
                trackName = "Ridge Run",
                layout = TrackLayout.Level6RidgeRun,
                theme = TrackTheme.MountainPass,
                lengthMeters = 2700f,
                hasShortcuts = false,
                hazardDensity = TrackHazardDensity.High,
                straightLength = 92f,
                turnRadius = 22f,
                trackWidth = 27f,
                checkpointCount = 12,
                description = "Elevated square with climbs, crests, and dips.",
            },
            new()
            {
                fileName = "Track_Level07_NeonCrossover",
                trackName = "Neon Crossover",
                layout = TrackLayout.Level7NeonCrossover,
                theme = TrackTheme.BeachBoardwalk,
                lengthMeters = 2850f,
                hasShortcuts = true,
                hazardDensity = TrackHazardDensity.Medium,
                straightLength = 98f,
                turnRadius = 20f,
                trackWidth = 27f,
                checkpointCount = 12,
                description = "Twin-loop crossover circuit with a center-cut shortcut.",
            },
        };

        [MenuItem("NeonLap/Content/Create Or Update Track Definition Assets")]
        public static void CreateOrUpdateTrackAssets()
        {
            EnsureFolder("Assets/NeonLap/ScriptableObjects");
            EnsureFolder(ScriptableTracksFolder);

            var tracks = new List<TrackDefinition>();
            foreach (var seed in Seeds)
            {
                var path = $"{ScriptableTracksFolder}/{seed.fileName}.asset";
                var track = AssetDatabase.LoadAssetAtPath<TrackDefinition>(path);
                if (track == null)
                {
                    track = ScriptableObject.CreateInstance<TrackDefinition>();
                    AssetDatabase.CreateAsset(track, path);
                }

                ApplySeed(track, seed);
                EditorUtility.SetDirty(track);
                tracks.Add(track);
            }

            AssetDatabase.SaveAssets();
            SyncRegistry(tracks);
            Debug.Log($"Created/updated {tracks.Count} track definitions under {ScriptableTracksFolder}.");
        }

        [MenuItem("NeonLap/Content/Sync Track Registry From ScriptableObjects")]
        public static void SyncRegistryFromScriptableObjects()
        {
            var guids = AssetDatabase.FindAssets("t:TrackDefinition", new[] { ScriptableTracksFolder });
            var tracks = new List<TrackDefinition>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var track = AssetDatabase.LoadAssetAtPath<TrackDefinition>(path);
                if (track != null)
                    tracks.Add(track);
            }

            tracks.Sort((a, b) => ((int)a.layout).CompareTo((int)b.layout));
            SyncRegistry(tracks);
        }

        static void ApplySeed(TrackDefinition track, TrackSeed seed)
        {
            track.trackName = seed.trackName;
            track.layout = seed.layout;
            track.theme = seed.theme;
            track.themeOverridesLayout = true;
            track.sceneName = "SampleScene";
            track.lapCount = 1;
            track.lengthMeters = seed.lengthMeters;
            track.hasShortcuts = seed.hasShortcuts;
            track.hazardDensity = seed.hazardDensity;
            track.straightLength = seed.straightLength;
            track.turnRadius = seed.turnRadius;
            track.trackWidth = seed.trackWidth;
            track.checkpointCount = seed.checkpointCount;
            track.description = seed.description;
        }

        static void SyncRegistry(List<TrackDefinition> tracks)
        {
            var registry = AssetDatabase.LoadAssetAtPath<TrackRegistry>(RegistryPath);
            if (registry == null)
            {
                EnsureFolder("Assets/NeonLap/Resources/NeonLap");
                registry = ScriptableObject.CreateInstance<TrackRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            registry.tracks = tracks.ToArray();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log($"Track registry synced with {registry.tracks.Length} tracks.", registry);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
