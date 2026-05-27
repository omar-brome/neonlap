using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackLevelConfig
    {
        const string ResourcePath = "NeonLap/TrackLevelModifiers";

        static TrackLevelModifiers asset;

        public static TrackLevelModifierEntry Get(int levelIndex)
        {
            EnsureAsset();
            if (asset != null)
                return asset.GetEntry(levelIndex);

            return GetDefaultEntry(levelIndex);
        }

        public static float GetBananaDensity(int levelIndex, float qualityBananaDensity)
        {
            return qualityBananaDensity * Get(levelIndex).bananaDensityMultiplier;
        }

        public static float GetHazardDensity(int levelIndex, float qualityHazardDensity)
        {
            return qualityHazardDensity * Get(levelIndex).hazardDensityMultiplier;
        }

        public static float GetPickupDensity(int levelIndex, float qualityPickupDensity)
        {
            return qualityPickupDensity * Get(levelIndex).pickupDensityMultiplier;
        }

        public static float GetMovingHazardDensity(int levelIndex, float qualityHazardDensity)
        {
            return qualityHazardDensity * Get(levelIndex).movingHazardDensityMultiplier;
        }

        public static bool RequiresShortcutForMedal(int levelIndex)
        {
            return Get(levelIndex).shortcutsRequiredForMedal;
        }

        public static string GetModifierNote(int levelIndex)
        {
            var note = Get(levelIndex).modifierNote;
            return string.IsNullOrWhiteSpace(note) ? string.Empty : note;
        }

        public static TrackLevelModifierEntry GetDefaultEntry(int levelIndex)
        {
            return levelIndex switch
            {
                0 => new TrackLevelModifierEntry
                {
                    levelName = "Neon Circuit",
                    bananaDensityMultiplier = 0.6f,
                    hazardDensityMultiplier = 0.75f,
                    pickupDensityMultiplier = 1f,
                    movingHazardDensityMultiplier = 0.65f,
                    shortcutsRequiredForMedal = false,
                    modifierNote = "Tutorial track: fewer hazards. Try drifting + nitro on the long curve.",
                },
                1 => new TrackLevelModifierEntry
                {
                    levelName = "Turbo Sprint",
                    bananaDensityMultiplier = 0.9f,
                    hazardDensityMultiplier = 1f,
                    pickupDensityMultiplier = 1.15f,
                    movingHazardDensityMultiplier = 1f,
                    shortcutsRequiredForMedal = false,
                    modifierNote = "Nitro pickups cluster on straights.",
                },
                3 => new TrackLevelModifierEntry
                {
                    levelName = "Zigzag Thunder",
                    bananaDensityMultiplier = 1.7f,
                    hazardDensityMultiplier = 1f,
                    pickupDensityMultiplier = 1f,
                    movingHazardDensityMultiplier = 1f,
                    shortcutsRequiredForMedal = false,
                    modifierNote = "Drift zones: extra drift score multiplier strips.",
                },
                2 => new TrackLevelModifierEntry
                {
                    levelName = "Metro Gauntlet",
                    bananaDensityMultiplier = 1f,
                    hazardDensityMultiplier = 1f,
                    pickupDensityMultiplier = 1f,
                    movingHazardDensityMultiplier = 1f,
                    shortcutsRequiredForMedal = true,
                    modifierNote = "Shortcut required for medals. Bananas stack near the hairpin.",
                },
                4 => new TrackLevelModifierEntry
                {
                    levelName = "Square Circuit",
                    bananaDensityMultiplier = 1f,
                    hazardDensityMultiplier = 1f,
                    pickupDensityMultiplier = 1f,
                    movingHazardDensityMultiplier = 1.9f,
                    shortcutsRequiredForMedal = false,
                    modifierNote = "Patrol traffic on cross streets.",
                },
                5 => new TrackLevelModifierEntry
                {
                    levelName = "Ridge Run",
                    bananaDensityMultiplier = 1f,
                    hazardDensityMultiplier = 1.15f,
                    pickupDensityMultiplier = 1f,
                    movingHazardDensityMultiplier = 1.55f,
                    shortcutsRequiredForMedal = false,
                    modifierNote = "Elevation: lighter over crests, heavier downforce on descents. AI is slower on climbs.",
                },
                _ => new TrackLevelModifierEntry
                {
                    levelName = $"Level {levelIndex + 1}",
                    bananaDensityMultiplier = 1f,
                    hazardDensityMultiplier = 1f,
                    pickupDensityMultiplier = 1f,
                    movingHazardDensityMultiplier = 1f,
                    shortcutsRequiredForMedal = false,
                    modifierNote = string.Empty,
                },
            };
        }

        static void EnsureAsset()
        {
            if (asset != null)
                return;

            asset = Resources.Load<TrackLevelModifiers>(ResourcePath);
        }
    }
}
