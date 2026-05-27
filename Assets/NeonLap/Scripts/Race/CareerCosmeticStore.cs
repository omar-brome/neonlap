using UnityEngine;

namespace NeonLap.Race
{
    public static class CareerCosmeticStore
    {
        const string TrailKey = "NeonLap.Cosmetic.Trail";
        const string HornKey = "NeonLap.Cosmetic.Horn";
        const string UnlockedPrefix = "NeonLap.Cosmetic.Unlocked.";

        public static int SelectedTrailIndex
        {
            get => PlayerPrefs.GetInt(TrailKey, 0);
            set
            {
                PlayerPrefs.SetInt(TrailKey, Mathf.Clamp(value, 0, TrailCatalog.Length - 1));
                PlayerPrefs.Save();
            }
        }

        public static bool HornUnlocked
        {
            get => PlayerPrefs.GetInt(UnlockedPrefix + "horn", 0) == 1;
            private set
            {
                PlayerPrefs.SetInt(UnlockedPrefix + "horn", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static int TrailCatalogLength => TrailCatalog.Length;

        public static TrailCosmetic GetTrail(int index)
        {
            if (TrailCatalog.Length == 0)
                return default;

            var i = Mathf.Abs(index) % TrailCatalog.Length;
            return TrailCatalog[i];
        }

        public static bool IsTrailUnlocked(int index)
        {
            if (index <= 0)
                return true;

            return PlayerPrefs.GetInt(UnlockedPrefix + "trail_" + index, 0) == 1;
        }

        public static bool TryUnlockTrail(int index)
        {
            var trail = GetTrail(index);
            if (IsTrailUnlocked(index))
                return true;

            if (!CareerCurrencyStore.TrySpend(trail.Cost))
                return false;

            PlayerPrefs.SetInt(UnlockedPrefix + "trail_" + index, 1);
            PlayerPrefs.Save();
            SelectedTrailIndex = index;
            return true;
        }

        public static bool TryUnlockHorn()
        {
            if (HornUnlocked)
                return true;

            const int hornCost = 1200;
            if (!CareerCurrencyStore.TrySpend(hornCost))
                return false;

            HornUnlocked = true;
            return true;
        }

        public static readonly TrailCosmetic[] TrailCatalog =
        {
            new("CYAN RUSH", new Color(0.2f, 1f, 1f), 0),
            new("MAGENTA PULSE", new Color(1f, 0.35f, 0.95f), 500),
            new("VIOLET DRIFT", new Color(0.45f, 0.55f, 1f), 650),
            new("SOLAR FLARE", new Color(1f, 0.55f, 0.15f), 750),
            new("TOXIC SLIP", new Color(0.35f, 1f, 0.55f), 850),
            new("CRIMSON EDGE", new Color(1f, 0.25f, 0.45f), 950),
        };
    }

    public readonly struct TrailCosmetic
    {
        public readonly string DisplayName;
        public readonly Color Color;
        public readonly int Cost;

        public TrailCosmetic(string displayName, Color color, int cost)
        {
            DisplayName = displayName;
            Color = color;
            Cost = cost;
        }
    }
}
