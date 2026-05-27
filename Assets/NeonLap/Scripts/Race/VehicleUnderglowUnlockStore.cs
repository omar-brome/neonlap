using UnityEngine;

namespace NeonLap.Race
{
    public static class VehicleUnderglowUnlockStore
    {
        const string SelectedIndexKey = "NeonLap.Vehicle.UnderglowIndex";

        public static int SelectedIndex
        {
            get
            {
                var index = PlayerPrefs.GetInt(SelectedIndexKey, 0);
                if (!IsUnlocked(index))
                    index = GetFirstUnlockedIndex();
                return index;
            }
            set
            {
                var clamped = Mathf.Clamp(value, 0, Catalog.Length - 1);
                if (!IsUnlocked(clamped))
                    clamped = GetFirstUnlockedIndex();
                PlayerPrefs.SetInt(SelectedIndexKey, clamped);
                PlayerPrefs.Save();
            }
        }

        public static int CatalogLength => Catalog.Length;

        public static UnderglowColorOption GetOption(int index)
        {
            if (Catalog.Length == 0)
                return default;

            var i = Mathf.Abs(index) % Catalog.Length;
            return Catalog[i];
        }

        public static Color GetSelectedColor() => GetOption(SelectedIndex).Color;

        public static bool IsUnlocked(int index)
        {
            var option = GetOption(index);
            return CareerScoreStore.GetTotalStars() >= option.RequiredStars;
        }

        public static int GetUnlockedCount()
        {
            var count = 0;
            for (var i = 0; i < Catalog.Length; i++)
            {
                if (IsUnlocked(i))
                    count++;
            }

            return count;
        }

        public static void CycleToNextUnlocked()
        {
            if (Catalog.Length == 0)
                return;

            var start = SelectedIndex;
            for (var step = 1; step <= Catalog.Length; step++)
            {
                var next = (start + step) % Catalog.Length;
                if (!IsUnlocked(next))
                    continue;

                SelectedIndex = next;
                return;
            }
        }

        public static int GetFirstUnlockedIndex()
        {
            for (var i = 0; i < Catalog.Length; i++)
            {
                if (IsUnlocked(i))
                    return i;
            }

            return 0;
        }

        public static readonly UnderglowColorOption[] Catalog =
        {
            new("NEON CYAN", new Color(0.2f, 1f, 1f), 0),
            new("MAGENTA PULSE", new Color(1f, 0.35f, 0.95f), 3),
            new("VIOLET DRIFT", new Color(0.45f, 0.55f, 1f), 6),
            new("SOLAR FLARE", new Color(1f, 0.55f, 0.15f), 9),
            new("TOXIC SLIP", new Color(0.35f, 1f, 0.55f), 12),
            new("CRIMSON EDGE", new Color(1f, 0.25f, 0.45f), 15),
        };
    }

    public readonly struct UnderglowColorOption
    {
        public readonly string DisplayName;
        public readonly Color Color;
        public readonly int RequiredStars;

        public UnderglowColorOption(string displayName, Color color, int requiredStars)
        {
            DisplayName = displayName;
            Color = color;
            RequiredStars = requiredStars;
        }

        public string GetUnlockHint()
        {
            return RequiredStars <= 0
                ? "Unlocked"
                : $"{RequiredStars} career ★ to unlock";
        }
    }
}
