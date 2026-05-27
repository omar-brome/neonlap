using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Core
{
    public static class GarageCatalog
    {
        const string RegistryResourcePath = "NeonLap/GarageRegistry";

        public static GarageRegistry LoadRegistry()
        {
            var registry = Resources.Load<GarageRegistry>(RegistryResourcePath);
            if (registry != null && registry.Count > 0)
                return registry;

            return CreateRuntimeRegistry();
        }

        static GarageRegistry CreateRuntimeRegistry()
        {
            var registry = ScriptableObject.CreateInstance<GarageRegistry>();
            registry.builds = new[]
            {
                CreateRuntimeBuild("neon_pulse", "Neon Pulse", "Balanced all-rounder.",
                    LoadProfile("NeonLap/VehicleProfiles/DefaultVehicleProfile"), VehicleClass.Rookie, true, 0, 0, 0,
                    new Color(0.1f, 0.35f, 0.45f), new Color(0f, 3.5f, 4f)),
                CreateRuntimeBuild("drift_phantom", "Drift Phantom", "Loose slides, big entries.",
                    LoadProfile("NeonLap/VehicleProfiles/VehicleProfile_Drift"), VehicleClass.Rookie, false, 3, 0, 750,
                    new Color(0.28f, 0.08f, 0.42f), new Color(2.5f, 0.4f, 4f)),
                CreateRuntimeBuild("velocity_prime", "Velocity Prime", "Straight-line predator.",
                    LoadProfile("NeonLap/VehicleProfiles/VehicleProfile_Speed"), VehicleClass.Pro, false, 6, 0, 1500,
                    new Color(0.45f, 0.22f, 0.05f), new Color(4f, 1.6f, 0.2f)),
                CreateRuntimeBuild("razor_gt", "Razor GT", "High grip technical weapon.",
                    LoadProfile("NeonLap/VehicleProfiles/VehicleProfile_Razor"), VehicleClass.Pro, false, 9, 2000, 2800,
                    new Color(0.08f, 0.38f, 0.12f), new Color(0.4f, 4f, 0.8f)),
                CreateRuntimeBuild("void_runner", "Void Runner", "Maximum speed, wild drifts.",
                    LoadProfile("NeonLap/VehicleProfiles/VehicleProfile_Void"), VehicleClass.Elite, false, 12, 3500, 4500,
                    new Color(0.45f, 0.08f, 0.08f), new Color(4f, 0.3f, 0.3f)),
            };
            return registry;
        }

        static VehicleProfile LoadProfile(string resourcePath)
        {
            var profile = Resources.Load<VehicleProfile>(resourcePath);
            return profile != null ? profile : ScriptableObject.CreateInstance<VehicleProfile>();
        }

        static HoverBuildDefinition CreateRuntimeBuild(
            string id,
            string displayName,
            string tagline,
            VehicleProfile profile,
            VehicleClass vehicleClass,
            bool unlockedByDefault,
            int stars,
            int score,
            int credits,
            Color body,
            Color accent)
        {
            var build = ScriptableObject.CreateInstance<HoverBuildDefinition>();
            build.buildId = id;
            build.displayName = displayName;
            build.tagline = tagline;
            build.profile = profile;
            build.vehicleClass = vehicleClass;
            build.unlockedByDefault = unlockedByDefault;
            build.requiredCareerStars = stars;
            build.requiredScoreAttackBest = score;
            build.creditCost = credits;
            build.bodyColor = body;
            build.accentColor = accent;
            return build;
        }
    }
}
