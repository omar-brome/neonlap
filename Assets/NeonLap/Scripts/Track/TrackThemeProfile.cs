using UnityEngine;

namespace NeonLap.Track
{
    public readonly struct TrackThemeSky
    {
        public readonly Color Zenith;
        public readonly Color Mid;
        public readonly Color Horizon;
        public readonly Color SunGlow;
        public readonly Color CameraBackground;

        public TrackThemeSky(Color zenith, Color mid, Color horizon, Color sunGlow, Color cameraBackground)
        {
            Zenith = zenith;
            Mid = mid;
            Horizon = horizon;
            SunGlow = sunGlow;
            CameraBackground = cameraBackground;
        }
    }

    public readonly struct TrackThemeProfile
    {
        public readonly TrackTheme Theme;
        public readonly string DisplayName;
        public readonly TrackThemeSky Sky;
        public readonly Color AmbientSky;
        public readonly Color AmbientEquator;
        public readonly Color AmbientGround;
        public readonly Color FogColor;
        public readonly float FogDensityScale;
        public readonly float LightIntensityScale;
        public readonly Color DirectionalLightColor;
        public readonly Color GroundColor;
        public readonly Color StadiumColor;
        public readonly Color BuildingColor;
        public readonly Color BuildingAccentColor;
        public readonly float BuildingAccentEmission;
        public readonly Color FoliageColor;
        public readonly Color FoliageEmission;
        public readonly Color TrunkColor;
        public readonly Color ScreenGlowColor;
        public readonly Color LampHeadColor;
        public readonly Color AsphaltColor;
        public readonly Color CurbColor;
        public readonly Color EdgeBaseColor;
        public readonly Color EdgeEmissionColor;
        public readonly float BuildingDensity;
        public readonly float TreeDensity;
        public readonly float ContainerDensity;
        public readonly float RockDensity;
        public readonly bool UsePalms;
        public readonly bool UseContainers;
        public readonly bool PreferNightLighting;

        public TrackThemeProfile(
            TrackTheme theme,
            string displayName,
            TrackThemeSky sky,
            Color ambientSky,
            Color ambientEquator,
            Color ambientGround,
            Color fogColor,
            float fogDensityScale,
            float lightIntensityScale,
            Color directionalLightColor,
            Color groundColor,
            Color stadiumColor,
            Color buildingColor,
            Color buildingAccentColor,
            float buildingAccentEmission,
            Color foliageColor,
            Color foliageEmission,
            Color trunkColor,
            Color screenGlowColor,
            Color lampHeadColor,
            Color asphaltColor,
            Color curbColor,
            Color edgeBaseColor,
            Color edgeEmissionColor,
            float buildingDensity,
            float treeDensity,
            float containerDensity,
            float rockDensity,
            bool usePalms,
            bool useContainers,
            bool preferNightLighting)
        {
            Theme = theme;
            DisplayName = displayName;
            Sky = sky;
            AmbientSky = ambientSky;
            AmbientEquator = ambientEquator;
            AmbientGround = ambientGround;
            FogColor = fogColor;
            FogDensityScale = fogDensityScale;
            LightIntensityScale = lightIntensityScale;
            DirectionalLightColor = directionalLightColor;
            GroundColor = groundColor;
            StadiumColor = stadiumColor;
            BuildingColor = buildingColor;
            BuildingAccentColor = buildingAccentColor;
            BuildingAccentEmission = buildingAccentEmission;
            FoliageColor = foliageColor;
            FoliageEmission = foliageEmission;
            TrunkColor = trunkColor;
            ScreenGlowColor = screenGlowColor;
            LampHeadColor = lampHeadColor;
            AsphaltColor = asphaltColor;
            CurbColor = curbColor;
            EdgeBaseColor = edgeBaseColor;
            EdgeEmissionColor = edgeEmissionColor;
            BuildingDensity = buildingDensity;
            TreeDensity = treeDensity;
            ContainerDensity = containerDensity;
            RockDensity = rockDensity;
            UsePalms = usePalms;
            UseContainers = useContainers;
            PreferNightLighting = preferNightLighting;
        }

        public static TrackTheme ResolveTheme(TrackDefinition definition)
        {
            if (definition == null)
                return TrackTheme.CityStreets;

            if (definition.themeOverridesLayout)
                return definition.theme;

            return ThemeForLayout(definition.layout);
        }

        public static TrackTheme ThemeForLayout(TrackLayout layout)
        {
            return TrackLayoutUtility.LevelIndexForLayout(layout) switch
            {
                1 => TrackTheme.CityStreets,
                2 => TrackTheme.DockyardNight,
                3 => TrackTheme.DesertCanyon,
                4 => TrackTheme.MountainPass,
                5 => TrackTheme.MountainPass,
                6 => TrackTheme.BeachBoardwalk,
                _ => TrackTheme.CityStreets,
            };
        }

        public static TrackThemeProfile Get(TrackTheme theme)
        {
            return theme switch
            {
                TrackTheme.MountainPass => MountainPass,
                TrackTheme.DesertCanyon => DesertCanyon,
                TrackTheme.DockyardNight => DockyardNight,
                TrackTheme.BeachBoardwalk => BeachBoardwalk,
                _ => CityStreets,
            };
        }

        public static TrackThemeProfile ForDefinition(TrackDefinition definition)
        {
            return Get(ResolveTheme(definition));
        }

        static readonly TrackThemeProfile CityStreets = new(
            TrackTheme.CityStreets,
            "City Streets",
            new TrackThemeSky(
                new Color(0.05f, 0.06f, 0.2f),
                new Color(0.16f, 0.12f, 0.38f),
                new Color(0.52f, 0.18f, 0.52f),
                new Color(0.95f, 0.42f, 0.28f),
                new Color(0.08f, 0.05f, 0.14f)),
            new Color(0.12f, 0.14f, 0.22f),
            new Color(0.08f, 0.09f, 0.14f),
            new Color(0.03f, 0.03f, 0.06f),
            new Color(0.14f, 0.16f, 0.24f),
            1f,
            1f,
            new Color(1f, 0.9f, 0.78f),
            new Color(0.03f, 0.05f, 0.08f),
            new Color(0.08f, 0.1f, 0.16f),
            new Color(0.06f, 0.07f, 0.12f),
            new Color(0.2f, 0.8f, 1f),
            1.8f,
            new Color(0.05f, 0.22f, 0.14f),
            new Color(0.1f, 0.8f, 0.35f),
            new Color(0.12f, 0.08f, 0.06f),
            new Color(0.3f, 1f, 1f),
            new Color(0.6f, 0.95f, 1f),
            new Color(0.1f, 0.1f, 0.11f),
            new Color(0.52f, 0.5f, 0.48f),
            new Color(0.18f, 0.82f, 0.95f),
            new Color(0.25f, 1.1f, 1.35f),
            1f,
            0.55f,
            0f,
            0f,
            false,
            false,
            false);

        static readonly TrackThemeProfile MountainPass = new(
            TrackTheme.MountainPass,
            "Mountain Pass",
            new TrackThemeSky(
                new Color(0.12f, 0.16f, 0.28f),
                new Color(0.35f, 0.42f, 0.55f),
                new Color(0.62f, 0.68f, 0.78f),
                new Color(0.88f, 0.72f, 0.55f),
                new Color(0.1f, 0.12f, 0.16f)),
            new Color(0.22f, 0.26f, 0.32f),
            new Color(0.16f, 0.18f, 0.22f),
            new Color(0.08f, 0.09f, 0.11f),
            new Color(0.55f, 0.6f, 0.68f),
            1.35f,
            0.92f,
            new Color(0.92f, 0.88f, 0.82f),
            new Color(0.12f, 0.14f, 0.16f),
            new Color(0.1f, 0.12f, 0.15f),
            new Color(0.14f, 0.16f, 0.2f),
            new Color(0.35f, 0.55f, 0.75f),
            0.45f,
            new Color(0.08f, 0.2f, 0.12f),
            new Color(0.15f, 0.45f, 0.28f),
            new Color(0.18f, 0.14f, 0.1f),
            new Color(0.45f, 0.75f, 0.95f),
            new Color(0.82f, 0.9f, 1f),
            new Color(0.11f, 0.11f, 0.12f),
            new Color(0.48f, 0.5f, 0.52f),
            new Color(0.15f, 0.65f, 0.85f),
            new Color(0.2f, 0.75f, 0.95f),
            0.35f,
            1.15f,
            0f,
            0.85f,
            false,
            false,
            false);

        static readonly TrackThemeProfile DesertCanyon = new(
            TrackTheme.DesertCanyon,
            "Desert Canyon",
            new TrackThemeSky(
                new Color(0.18f, 0.12f, 0.28f),
                new Color(0.55f, 0.32f, 0.22f),
                new Color(0.92f, 0.55f, 0.28f),
                new Color(1f, 0.72f, 0.35f),
                new Color(0.22f, 0.12f, 0.08f)),
            new Color(0.42f, 0.32f, 0.24f),
            new Color(0.32f, 0.24f, 0.18f),
            new Color(0.18f, 0.12f, 0.08f),
            new Color(0.75f, 0.55f, 0.35f),
            0.75f,
            1.08f,
            new Color(1f, 0.88f, 0.65f),
            new Color(0.28f, 0.2f, 0.12f),
            new Color(0.2f, 0.14f, 0.1f),
            new Color(0.35f, 0.22f, 0.14f),
            new Color(1f, 0.55f, 0.2f),
            1.2f,
            new Color(0.22f, 0.38f, 0.18f),
            new Color(0.35f, 0.65f, 0.25f),
            new Color(0.28f, 0.18f, 0.1f),
            new Color(1f, 0.65f, 0.25f),
            new Color(1f, 0.85f, 0.55f),
            new Color(0.14f, 0.12f, 0.1f),
            new Color(0.58f, 0.48f, 0.38f),
            new Color(0.95f, 0.55f, 0.2f),
            new Color(1f, 0.55f, 0.2f),
            0.25f,
            0.2f,
            0f,
            1.1f,
            false,
            false,
            false);

        static readonly TrackThemeProfile DockyardNight = new(
            TrackTheme.DockyardNight,
            "Dockyard (Night)",
            new TrackThemeSky(
                new Color(0.02f, 0.03f, 0.08f),
                new Color(0.04f, 0.06f, 0.14f),
                new Color(0.08f, 0.1f, 0.2f),
                new Color(0.15f, 0.22f, 0.45f),
                new Color(0.02f, 0.03f, 0.06f)),
            new Color(0.04f, 0.05f, 0.1f),
            new Color(0.03f, 0.03f, 0.07f),
            new Color(0.01f, 0.01f, 0.03f),
            new Color(0.03f, 0.04f, 0.08f),
            1.25f,
            0.42f,
            new Color(0.55f, 0.65f, 0.95f),
            new Color(0.02f, 0.03f, 0.05f),
            new Color(0.06f, 0.08f, 0.12f),
            new Color(0.05f, 0.06f, 0.09f),
            new Color(0.12f, 0.18f, 0.22f),
            2.2f,
            new Color(0.04f, 0.08f, 0.06f),
            new Color(0.08f, 0.2f, 0.15f),
            new Color(0.1f, 0.08f, 0.06f),
            new Color(0.35f, 0.95f, 1f),
            new Color(0.75f, 0.92f, 1f),
            new Color(0.09f, 0.09f, 0.1f),
            new Color(0.4f, 0.42f, 0.45f),
            new Color(0.2f, 0.95f, 1f),
            new Color(0.35f, 1.4f, 1.6f),
            0.15f,
            0f,
            1.25f,
            0f,
            false,
            true,
            true);

        static readonly TrackThemeProfile BeachBoardwalk = new(
            TrackTheme.BeachBoardwalk,
            "Beach Boardwalk",
            new TrackThemeSky(
                new Color(0.12f, 0.35f, 0.62f),
                new Color(0.35f, 0.62f, 0.88f),
                new Color(0.85f, 0.72f, 0.55f),
                new Color(1f, 0.82f, 0.45f),
                new Color(0.35f, 0.55f, 0.72f)),
            new Color(0.45f, 0.58f, 0.72f),
            new Color(0.38f, 0.5f, 0.62f),
            new Color(0.28f, 0.38f, 0.45f),
            new Color(0.55f, 0.68f, 0.78f),
            0.55f,
            1.05f,
            new Color(1f, 0.92f, 0.78f),
            new Color(0.42f, 0.36f, 0.26f),
            new Color(0.22f, 0.2f, 0.18f),
            new Color(0.35f, 0.32f, 0.28f),
            new Color(0.2f, 0.85f, 0.95f),
            1.4f,
            new Color(0.12f, 0.42f, 0.22f),
            new Color(0.2f, 0.85f, 0.45f),
            new Color(0.35f, 0.22f, 0.12f),
            new Color(0.25f, 0.95f, 1f),
            new Color(0.95f, 0.98f, 1f),
            new Color(0.12f, 0.11f, 0.1f),
            new Color(0.62f, 0.58f, 0.5f),
            new Color(0.2f, 0.95f, 1.05f),
            new Color(0.2f, 0.95f, 1.05f),
            0.45f,
            0.85f,
            0f,
            0f,
            true,
            false,
            false);
    }
}
