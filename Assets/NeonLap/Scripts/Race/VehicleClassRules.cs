using NeonLap.Vehicle;

namespace NeonLap.Race
{
    public static class VehicleClassRules
    {
        public static CareerCupTier GetRequiredCup(VehicleClass vehicleClass) =>
            vehicleClass switch
            {
                VehicleClass.Pro => CareerCupTier.Pro,
                VehicleClass.Elite => CareerCupTier.Elite,
                _ => CareerCupTier.Rookie,
            };

        public static VehicleClass GetMaxClassForTrack(int trackIndex) =>
            GetMaxClassForCup(CareerCupCatalog.GetCupForTrack(trackIndex));

        public static VehicleClass GetMaxClassForCup(CareerCupTier cup) =>
            cup switch
            {
                CareerCupTier.Pro => VehicleClass.Pro,
                CareerCupTier.Elite => VehicleClass.Elite,
                _ => VehicleClass.Rookie,
            };

        public static bool IsCupRequirementMet(HoverBuildDefinition build)
        {
            if (build == null)
                return false;

            return CareerCupStore.IsCupUnlocked(GetRequiredCup(build.vehicleClass));
        }

        public static bool IsBuildAllowedForTrack(HoverBuildDefinition build, int trackIndex)
        {
            if (build == null)
                return false;

            return (int)build.vehicleClass <= (int)GetMaxClassForTrack(trackIndex);
        }

        public static string GetGarageCupHint(HoverBuildDefinition build)
        {
            if (build == null || IsCupRequirementMet(build))
                return string.Empty;

            var cup = GetRequiredCup(build.vehicleClass);
            return CareerCupStore.GetCupUnlockHint(cup);
        }

        public static string GetClassBadge(HoverBuildDefinition build)
        {
            if (build == null)
                return string.Empty;

            return $"{VehicleClassLabels.GetDisplayName(build.vehicleClass)} ({VehicleClassLabels.GetShortLabel(build.vehicleClass)})";
        }

        public static string GetTrackClassLimitLine(int trackIndex)
        {
            var cup = CareerCupCatalog.GetCupForTrack(trackIndex);
            var max = GetMaxClassForCup(cup);
            return $"Vehicle class: {VehicleClassLabels.GetDisplayName(max)} ({VehicleClassLabels.GetShortLabel(max)}) max  •  {CareerCupCatalog.GetDisplayName(cup)}";
        }
    }
}
