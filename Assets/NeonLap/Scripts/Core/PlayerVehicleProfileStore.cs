using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Core
{
    public static class PlayerVehicleProfileStore
    {
        const string SelectedKindKey = "NeonLap.Vehicle.ProfileKind";
        const string LegacyGarageIndexKey = "NeonLap.Garage.SelectedIndex";

        const string BalancedProfilePath = "NeonLap/VehicleProfiles/DefaultVehicleProfile";
        const string DriftProfilePath = "NeonLap/VehicleProfiles/VehicleProfile_Drift";
        const string SpeedProfilePath = "NeonLap/VehicleProfiles/VehicleProfile_Speed";

        static VehicleProfile cachedBalanced;
        static VehicleProfile cachedDrift;
        static VehicleProfile cachedSpeed;
        static bool legacyMigrated;

        public static VehicleProfileKind SelectedKind
        {
            get
            {
                MigrateLegacySelectionIfNeeded();
                return (VehicleProfileKind)Mathf.Clamp(
                    PlayerPrefs.GetInt(SelectedKindKey, (int)VehicleProfileKind.Balanced),
                    0,
                    2);
            }
            set
            {
                PlayerPrefs.SetInt(SelectedKindKey, Mathf.Clamp((int)value, 0, 2));
                PlayerPrefs.Save();
            }
        }

        public static VehicleProfile GetSelectedProfile()
        {
            var build = PlayerGarageStore.GetSelectedBuild();
            if (build != null && build.profile != null)
                return build.profile;

            return GetProfile(SelectedKind);
        }

        public static void SyncFromGarageSelection(HoverBuildDefinition build)
        {
            if (build?.profile == null)
                return;

            SelectedKind = GetKindForProfile(build.profile);
        }

        public static VehicleProfile GetProfile(VehicleProfileKind kind)
        {
            return kind switch
            {
                VehicleProfileKind.Drift => LoadProfile(ref cachedDrift, DriftProfilePath),
                VehicleProfileKind.Speed => LoadProfile(ref cachedSpeed, SpeedProfilePath),
                _ => LoadProfile(ref cachedBalanced, BalancedProfilePath),
            };
        }

        public static VehicleProfileKind GetKindForProfile(VehicleProfile profile)
        {
            if (profile == null)
                return SelectedKind;

            var drift = GetProfile(VehicleProfileKind.Drift);
            var speed = GetProfile(VehicleProfileKind.Speed);
            if (profile == speed)
                return VehicleProfileKind.Speed;
            if (profile == drift)
                return VehicleProfileKind.Drift;
            return VehicleProfileKind.Balanced;
        }

        public static string GetDisplayName(VehicleProfileKind kind)
        {
            return kind switch
            {
                VehicleProfileKind.Drift => "Drift",
                VehicleProfileKind.Speed => "Speed",
                _ => "Balanced",
            };
        }

        public static string GetTagline(VehicleProfileKind kind)
        {
            return kind switch
            {
                VehicleProfileKind.Drift => "Loose slides, sharp rotation, big entries.",
                VehicleProfileKind.Speed => "Higher top speed, longer straights.",
                _ => "All-rounder grip and pace for every track.",
            };
        }

        public static Color GetBodyColor(VehicleProfileKind kind)
        {
            return kind switch
            {
                VehicleProfileKind.Drift => new Color(0.28f, 0.08f, 0.42f),
                VehicleProfileKind.Speed => new Color(0.45f, 0.22f, 0.05f),
                _ => new Color(0.1f, 0.35f, 0.45f),
            };
        }

        public static Color GetAccentColor(VehicleProfileKind kind)
        {
            return kind switch
            {
                VehicleProfileKind.Drift => new Color(2.5f, 0.4f, 4f),
                VehicleProfileKind.Speed => new Color(4f, 1.6f, 0.2f),
                _ => new Color(0f, 3.5f, 4f),
            };
        }

        public static DashboardSkin GetDashboardSkin(VehicleProfileKind kind)
        {
            return kind switch
            {
                VehicleProfileKind.Drift => new DashboardSkin(
                    new Color(0.35f, 1f, 0.65f),
                    new Color(0.85f, 0.45f, 1f),
                    new Color(0.06f, 0.04f, 0.1f, 0.9f)),
                VehicleProfileKind.Speed => new DashboardSkin(
                    new Color(1f, 0.72f, 0.25f),
                    new Color(1f, 0.45f, 0.2f),
                    new Color(0.1f, 0.05f, 0.03f, 0.9f)),
                _ => new DashboardSkin(
                    new Color(0.35f, 1f, 1f),
                    new Color(1f, 0.62f, 0.18f),
                    new Color(0.03f, 0.05f, 0.08f, 0.88f)),
            };
        }

        static VehicleProfile LoadProfile(ref VehicleProfile cache, string resourcePath)
        {
            if (cache != null)
                return cache;

            cache = Resources.Load<VehicleProfile>(resourcePath);
            if (cache == null)
            {
                cache = ScriptableObject.CreateInstance<VehicleProfile>();
                Debug.LogWarning($"PlayerVehicleProfileStore: Missing profile at Resources/{resourcePath}. Using runtime defaults.");
            }

            return cache;
        }

        static void MigrateLegacySelectionIfNeeded()
        {
            if (legacyMigrated || PlayerPrefs.HasKey(SelectedKindKey))
            {
                legacyMigrated = true;
                return;
            }

            legacyMigrated = true;
            if (!PlayerPrefs.HasKey(LegacyGarageIndexKey))
                return;

            var legacyIndex = Mathf.Clamp(PlayerPrefs.GetInt(LegacyGarageIndexKey, 0), 0, 2);
            PlayerPrefs.SetInt(SelectedKindKey, legacyIndex);
            PlayerPrefs.Save();
        }
    }

    public readonly struct DashboardSkin
    {
        public readonly Color SpeedNeedleColor;
        public readonly Color RpmNeedleColor;
        public readonly Color BezelColor;

        public DashboardSkin(Color speedNeedle, Color rpmNeedle, Color bezel)
        {
            SpeedNeedleColor = speedNeedle;
            RpmNeedleColor = rpmNeedle;
            BezelColor = bezel;
        }
    }
}
