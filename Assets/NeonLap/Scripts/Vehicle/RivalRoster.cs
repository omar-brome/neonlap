using UnityEngine;

namespace NeonLap.Vehicle
{
    /// <summary>
    /// Canonical rival roster: index matches career grid colors, minimap blips, and AI spawn order.
    /// </summary>
    public static class RivalRoster
    {
        public const int Count = 9;

        static readonly Color[] BlipColors =
        {
            new(1f, 0.45f, 0.45f),
            new(1f, 0.72f, 0.35f),
            new(0.95f, 0.92f, 0.35f),
            new(0.45f, 1f, 0.55f),
            new(0.55f, 0.72f, 1f),
            new(0.82f, 0.5f, 1f),
            new(1f, 0.55f, 0.85f),
            new(1f, 0.85f, 0.45f),
            new(0.82f, 0.82f, 0.88f),
        };

        static readonly RivalIdentityProfile[] Profiles =
        {
            Create(0, "NEON VEGA", "VEGA", new(0.45f, 0.08f, 0.08f), new(4f, 0.3f, 0.3f)),
            Create(1, "RIFT", "RIFT", new(0.45f, 0.22f, 0.05f), new(4f, 1.6f, 0.2f)),
            Create(2, "KAZE", "KAZE", new(0.4f, 0.38f, 0.06f), new(3.8f, 3.5f, 0.3f)),
            Create(3, "TORQUE", "TORQ", new(0.08f, 0.38f, 0.12f), new(0.4f, 4f, 0.8f)),
            Create(4, "CYRA", "CYRA", new(0.08f, 0.15f, 0.42f), new(0.5f, 1.2f, 4f)),
            Create(5, "BLITZ", "BLTZ", new(0.28f, 0.08f, 0.42f), new(2.5f, 0.4f, 4f)),
            Create(6, "NOVA-7", "NV-7", new(0.42f, 0.1f, 0.32f), new(4f, 0.5f, 2.8f)),
            Create(7, "HAVOC", "HVC", new(0.38f, 0.32f, 0.08f), new(3.5f, 2.8f, 0.4f)),
            Create(8, "STRAY", "STRY", new(0.35f, 0.35f, 0.38f), new(3f, 3f, 3.5f)),
        };

        public static RivalIdentityProfile GetProfile(int rivalIndex)
        {
            if (Profiles.Length == 0)
                return Create(0, "RIVAL", "R", Color.gray, Color.white);

            var index = Mathf.Abs(rivalIndex) % Profiles.Length;
            return Profiles[index];
        }

        public static Color GetBodyColor(int rivalIndex) => GetProfile(rivalIndex).BodyColor;

        public static Color GetAccentColor(int rivalIndex) => GetProfile(rivalIndex).AccentColor;

        public static Color GetBlipColor(int rivalIndex)
        {
            if (BlipColors.Length == 0)
                return Color.white;

            return BlipColors[Mathf.Abs(rivalIndex) % BlipColors.Length];
        }

        static RivalIdentityProfile Create(int index, string displayName, string shortName, Color body, Color accent)
        {
            var hud = Color.Lerp(body, accent, 0.45f);
            hud.a = 1f;
            if (index >= 0 && index < BlipColors.Length)
                hud = Color.Lerp(hud, BlipColors[index], 0.35f);

            return new RivalIdentityProfile
            {
                DisplayName = displayName,
                ShortName = shortName,
                BodyColor = body,
                AccentColor = accent,
                HudColor = hud,
            };
        }
    }
}
