using UnityEngine;

namespace NeonLap.Vehicle
{
    public static class VehicleCustomizationStore
    {
        const string PaintKey = "NeonLap.Customize.Paint";
        const string DecalKey = "NeonLap.Customize.Decal";
        const string RimKey = "NeonLap.Customize.Rim";

        public static int PaintPresetIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PaintKey, 0), 0, VehicleCustomizationCatalog.PaintPresetCount - 1);
            set
            {
                PlayerPrefs.SetInt(PaintKey, Mathf.Clamp(value, 0, VehicleCustomizationCatalog.PaintPresetCount - 1));
                PlayerPrefs.Save();
            }
        }

        public static int DecalIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(DecalKey, 0), 0, VehicleCustomizationCatalog.DecalCount - 1);
            set
            {
                PlayerPrefs.SetInt(DecalKey, Mathf.Clamp(value, 0, VehicleCustomizationCatalog.DecalCount - 1));
                PlayerPrefs.Save();
            }
        }

        public static int RimIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(RimKey, 0), 0, VehicleCustomizationCatalog.RimCount - 1);
            set
            {
                PlayerPrefs.SetInt(RimKey, Mathf.Clamp(value, 0, VehicleCustomizationCatalog.RimCount - 1));
                PlayerPrefs.Save();
            }
        }

        public static void CyclePaint() => PaintPresetIndex = (PaintPresetIndex + 1) % VehicleCustomizationCatalog.PaintPresetCount;

        public static void CycleDecal() => DecalIndex = (DecalIndex + 1) % VehicleCustomizationCatalog.DecalCount;

        public static void CycleRim() => RimIndex = (RimIndex + 1) % VehicleCustomizationCatalog.RimCount;

        public static void GetResolvedColors(HoverBuildDefinition build, out Color body, out Color accent)
        {
            if (build == null)
            {
                body = new Color(0.1f, 0.35f, 0.45f);
                accent = new Color(0f, 3.5f, 4f);
                return;
            }

            var preset = VehicleCustomizationCatalog.GetPaintPreset(PaintPresetIndex);
            if (preset.UsesBuildDefault)
            {
                body = build.bodyColor;
                accent = build.accentColor;
                return;
            }

            body = preset.BodyColor;
            accent = preset.AccentColor;
        }

        public static HoverCarVisualBuilder.BuildArgs CreateBuildArgs(
            Material bodyTemplate,
            Material accentTemplate,
            HoverBuildDefinition build,
            bool isPlayer = false)
        {
            GetResolvedColors(build, out var body, out var accent);
            return new HoverCarVisualBuilder.BuildArgs(
                bodyTemplate,
                accentTemplate,
                body,
                accent,
                isPlayer,
                DecalIndex,
                RimIndex);
        }

        public static HoverCarVisualBuilder.BuildArgs CreateBuildArgs(
            Material bodyTemplate,
            Material accentTemplate,
            Color body,
            Color accent,
            bool isPlayer = false)
        {
            return new HoverCarVisualBuilder.BuildArgs(
                bodyTemplate,
                accentTemplate,
                body,
                accent,
                isPlayer,
                DecalIndex,
                RimIndex);
        }
    }

    public static class VehicleCustomizationCatalog
    {
        public static int PaintPresetCount => PaintPresets.Length;
        public static int DecalCount => Decals.Length;
        public static int RimCount => Rims.Length;

        public static readonly PaintPreset[] PaintPresets =
        {
            new("BUILD DEFAULT", true, default, default),
            new("NEON CYAN", false, new Color(0.08f, 0.32f, 0.42f), new Color(0f, 3.2f, 4f)),
            new("MAGENTA RUSH", false, new Color(0.32f, 0.06f, 0.38f), new Color(3.2f, 0.35f, 4f)),
            new("SOLAR FLARE", false, new Color(0.42f, 0.18f, 0.04f), new Color(4f, 1.4f, 0.15f)),
            new("TOXIC SLIP", false, new Color(0.06f, 0.34f, 0.14f), new Color(0.5f, 4f, 0.8f)),
            new("VOID BLACK", false, new Color(0.05f, 0.05f, 0.08f), new Color(1.8f, 0.4f, 4f)),
            new("ARCTIC WHITE", false, new Color(0.72f, 0.78f, 0.85f), new Color(0.4f, 2.8f, 4f)),
            new("CRIMSON EDGE", false, new Color(0.38f, 0.06f, 0.08f), new Color(4f, 0.25f, 0.35f)),
        };

        public static readonly DecalStyle[] Decals =
        {
            new("NONE", 0),
            new("CENTER STRIPE", 1),
            new("DUAL STRIPES", 2),
            new("CHEVRON", 3),
            new("NEON TAG", 4),
        };

        public static readonly RimStyle[] Rims =
        {
            new("SPORT", 0),
            new("BLADE", 1),
            new("SOLID", 2),
            new("GLOW RING", 3),
        };

        public static PaintPreset GetPaintPreset(int index)
        {
            if (PaintPresets.Length == 0)
                return default;

            return PaintPresets[Mathf.Abs(index) % PaintPresets.Length];
        }

        public static DecalStyle GetDecal(int index)
        {
            if (Decals.Length == 0)
                return default;

            return Decals[Mathf.Abs(index) % Decals.Length];
        }

        public static RimStyle GetRim(int index)
        {
            if (Rims.Length == 0)
                return default;

            return Rims[Mathf.Abs(index) % Rims.Length];
        }
    }

    public readonly struct PaintPreset
    {
        public readonly string DisplayName;
        public readonly bool UsesBuildDefault;
        public readonly Color BodyColor;
        public readonly Color AccentColor;

        public PaintPreset(string displayName, bool usesBuildDefault, Color bodyColor, Color accentColor)
        {
            DisplayName = displayName;
            UsesBuildDefault = usesBuildDefault;
            BodyColor = bodyColor;
            AccentColor = accentColor;
        }
    }

    public readonly struct DecalStyle
    {
        public readonly string DisplayName;
        public readonly int StyleId;

        public DecalStyle(string displayName, int styleId)
        {
            DisplayName = displayName;
            StyleId = styleId;
        }
    }

    public readonly struct RimStyle
    {
        public readonly string DisplayName;
        public readonly int StyleId;

        public RimStyle(string displayName, int styleId)
        {
            DisplayName = displayName;
            StyleId = styleId;
        }
    }
}
