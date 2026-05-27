using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Core
{
    public static class PlayerGarageStore
    {
        const string SelectedIndexKey = "NeonLap.Garage.SelectedIndex";
        const string PurchasedPrefix = "NeonLap.Garage.Purchased.";
        const string LegacyProfileKindKey = "NeonLap.Vehicle.ProfileKind";

        static GarageRegistry cachedRegistry;
        static bool legacySelectionMigrated;

        public static GarageRegistry Registry
        {
            get
            {
                if (cachedRegistry == null)
                    cachedRegistry = GarageCatalog.LoadRegistry();
                return cachedRegistry;
            }
        }

        public static int SelectedIndex
        {
            get
            {
                MigrateLegacySelectionIfNeeded();
                return PlayerPrefs.GetInt(SelectedIndexKey, 0);
            }
            set
            {
                var count = Registry != null ? Registry.Count : 1;
                var clamped = Mathf.Clamp(value, 0, Mathf.Max(count - 1, 0));
                PlayerPrefs.SetInt(SelectedIndexKey, clamped);
                PlayerPrefs.Save();
                var registry = Registry;
                PlayerVehicleProfileStore.SyncFromGarageSelection(
                    registry != null ? registry.GetBuild(clamped) : null);
            }
        }

        public static HoverBuildDefinition GetSelectedBuild()
        {
            var registry = Registry;
            var build = registry != null ? registry.GetBuild(SelectedIndex) : null;
            PlayerVehicleProfileStore.SyncFromGarageSelection(build);
            return build;
        }

        public static bool IsUnlocked(HoverBuildDefinition build)
        {
            if (build == null)
                return false;

            if (!VehicleClassRules.IsCupRequirementMet(build))
                return false;

            if (build.unlockedByDefault)
                return true;

            if (IsCreditPurchased(build))
                return true;

            if (requiredCareerStarsMet(build))
                return true;

            if (build.requiredScoreAttackBest > 0
                && ScoreAttackRecordStore.GetGlobalBestScore() >= build.requiredScoreAttackBest)
                return true;

            return false;
        }

        public static bool IsCreditPurchased(HoverBuildDefinition build)
        {
            if (build == null || string.IsNullOrEmpty(build.buildId))
                return false;

            return PlayerPrefs.GetInt(PurchasedPrefix + build.buildId, 0) == 1;
        }

        public static bool CanPurchaseWithCredits(HoverBuildDefinition build)
        {
            return build != null
                   && build.creditCost > 0
                   && VehicleClassRules.IsCupRequirementMet(build)
                   && !IsUnlocked(build);
        }

        public static string GetUnlockStatus(HoverBuildDefinition build)
        {
            if (build == null)
                return "Unknown";

            if (!VehicleClassRules.IsCupRequirementMet(build))
                return VehicleClassRules.GetGarageCupHint(build);

            return IsUnlocked(build) ? "Unlocked" : build.GetUnlockHint();
        }

        public static bool TryPurchaseUnlock(HoverBuildDefinition build)
        {
            if (build == null)
                return false;

            if (IsUnlocked(build))
                return true;

            if (build.creditCost <= 0 || !CareerCurrencyStore.TrySpend(build.creditCost))
                return false;

            PlayerPrefs.SetInt(PurchasedPrefix + build.buildId, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static bool IsUnlocked(int buildIndex)
        {
            var registry = Registry;
            return registry != null && IsUnlocked(registry.GetBuild(buildIndex));
        }

        public static int GetUnlockedCount()
        {
            var registry = Registry;
            if (registry == null)
                return 0;

            var count = 0;
            for (var i = 0; i < registry.Count; i++)
            {
                if (IsUnlocked(i))
                    count++;
            }

            return count;
        }

        static bool requiredCareerStarsMet(HoverBuildDefinition build)
        {
            return build.requiredCareerStars > 0
                   && CareerScoreStore.GetTotalStars() >= build.requiredCareerStars;
        }

        public static string GetBuildButtonLabel(int index)
        {
            var registry = Registry;
            var build = registry != null ? registry.GetBuild(index) : null;
            if (build == null)
                return $"BUILD {index + 1}";

            var badge = VehicleClassLabels.GetShortLabel(build.vehicleClass);
            return IsUnlocked(build)
                ? $"{build.displayName.ToUpperInvariant()}  [{badge}]"
                : $"{build.displayName.ToUpperInvariant()}  [{badge}]  •  LOCKED";
        }

        public static void EnsureLegalBuildForTrack(int trackIndex)
        {
            var registry = Registry;
            if (registry == null || registry.Count == 0)
                return;

            var current = GetSelectedBuild();
            if (current != null && VehicleClassRules.IsBuildAllowedForTrack(current, trackIndex))
                return;

            var bestIndex = -1;
            var bestRank = -1;
            for (var i = 0; i < registry.Count; i++)
            {
                var build = registry.GetBuild(i);
                if (!IsUnlocked(build) || !VehicleClassRules.IsBuildAllowedForTrack(build, trackIndex))
                    continue;

                var rank = (int)build.vehicleClass;
                if (rank <= bestRank)
                    continue;

                bestRank = rank;
                bestIndex = i;
            }

            if (bestIndex >= 0)
                SelectedIndex = bestIndex;
        }

        public static string GetEquipButtonLabel(HoverBuildDefinition build, int previewIndex)
        {
            if (build == null)
                return "EQUIP";

            if (!IsUnlocked(build))
            {
                if (CanPurchaseWithCredits(build))
                    return $"UNLOCK ({build.creditCost:N0} CR)";

                return "LOCKED";
            }

            return previewIndex == SelectedIndex ? "EQUIPPED" : "EQUIP BUILD";
        }

        static void MigrateLegacySelectionIfNeeded()
        {
            if (legacySelectionMigrated || PlayerPrefs.HasKey(SelectedIndexKey))
            {
                legacySelectionMigrated = true;
                return;
            }

            legacySelectionMigrated = true;
            if (!PlayerPrefs.HasKey(LegacyProfileKindKey))
                return;

            var kind = Mathf.Clamp(PlayerPrefs.GetInt(LegacyProfileKindKey, 0), 0, 2);
            var index = kind switch
            {
                1 => 1,
                2 => 2,
                _ => 0,
            };
            PlayerPrefs.SetInt(SelectedIndexKey, index);
            PlayerPrefs.Save();
        }
    }
}
