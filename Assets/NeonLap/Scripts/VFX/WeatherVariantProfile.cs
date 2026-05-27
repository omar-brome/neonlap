using UnityEngine;

namespace NeonLap.VFX
{
    public readonly struct WeatherVariantProfile
    {
        public readonly WeatherVariant Variant;
        public readonly string DisplayName;
        public readonly float GripMultiplier;
        public readonly float TopSpeedMultiplier;
        public readonly float DriftScoreMultiplier;
        public readonly float FogDensityMultiplier;
        public readonly Color FogColorTint;
        public readonly float FogColorBlend;
        public readonly float SunnySkyBlend;
        public readonly float RainIntensity;
        public readonly float WetRoadBlend;
        public readonly float SandIntensity;
        public readonly float VisibilityScale;
        public readonly float LightIntensityMultiplier;
        public readonly Color DirectionalLightColor;
        public readonly Color AmbientSkyTint;
        public readonly Color AmbientEquatorTint;
        public readonly Color AmbientGroundTint;
        public readonly float AmbientTintWeight;
        public readonly Color CameraBackgroundTint;
        public readonly float CameraBackgroundBlend;

        public WeatherVariantProfile(
            WeatherVariant variant,
            string displayName,
            float gripMultiplier,
            float topSpeedMultiplier,
            float driftScoreMultiplier,
            float fogDensityMultiplier,
            Color fogColorTint,
            float fogColorBlend,
            float sunnySkyBlend,
            float rainIntensity,
            float wetRoadBlend,
            float sandIntensity,
            float visibilityScale,
            float lightIntensityMultiplier,
            Color directionalLightColor,
            Color ambientSkyTint,
            Color ambientEquatorTint,
            Color ambientGroundTint,
            float ambientTintWeight,
            Color cameraBackgroundTint,
            float cameraBackgroundBlend)
        {
            Variant = variant;
            DisplayName = displayName;
            GripMultiplier = gripMultiplier;
            TopSpeedMultiplier = topSpeedMultiplier;
            DriftScoreMultiplier = driftScoreMultiplier;
            FogDensityMultiplier = fogDensityMultiplier;
            FogColorTint = fogColorTint;
            FogColorBlend = fogColorBlend;
            SunnySkyBlend = sunnySkyBlend;
            RainIntensity = rainIntensity;
            WetRoadBlend = wetRoadBlend;
            SandIntensity = sandIntensity;
            VisibilityScale = visibilityScale;
            LightIntensityMultiplier = lightIntensityMultiplier;
            DirectionalLightColor = directionalLightColor;
            AmbientSkyTint = ambientSkyTint;
            AmbientEquatorTint = ambientEquatorTint;
            AmbientGroundTint = ambientGroundTint;
            AmbientTintWeight = ambientTintWeight;
            CameraBackgroundTint = cameraBackgroundTint;
            CameraBackgroundBlend = cameraBackgroundBlend;
        }

        public static WeatherVariantProfile Get(WeatherVariant variant)
        {
            return variant switch
            {
                WeatherVariant.Rain => Rain,
                WeatherVariant.Fog => Fog,
                WeatherVariant.Sandstorm => Sandstorm,
                _ => Clear,
            };
        }

        public static WeatherVariantProfile Lerp(WeatherVariantProfile a, WeatherVariantProfile b, float t)
        {
            t = Mathf.Clamp01(t);
            return new WeatherVariantProfile(
                t < 0.5f ? a.Variant : b.Variant,
                t < 0.5f ? a.DisplayName : b.DisplayName,
                Mathf.Lerp(a.GripMultiplier, b.GripMultiplier, t),
                Mathf.Lerp(a.TopSpeedMultiplier, b.TopSpeedMultiplier, t),
                Mathf.Lerp(a.DriftScoreMultiplier, b.DriftScoreMultiplier, t),
                Mathf.Lerp(a.FogDensityMultiplier, b.FogDensityMultiplier, t),
                Color.Lerp(a.FogColorTint, b.FogColorTint, t),
                Mathf.Lerp(a.FogColorBlend, b.FogColorBlend, t),
                Mathf.Lerp(a.SunnySkyBlend, b.SunnySkyBlend, t),
                Mathf.Lerp(a.RainIntensity, b.RainIntensity, t),
                Mathf.Lerp(a.WetRoadBlend, b.WetRoadBlend, t),
                Mathf.Lerp(a.SandIntensity, b.SandIntensity, t),
                Mathf.Lerp(a.VisibilityScale, b.VisibilityScale, t),
                Mathf.Lerp(a.LightIntensityMultiplier, b.LightIntensityMultiplier, t),
                Color.Lerp(a.DirectionalLightColor, b.DirectionalLightColor, t),
                Color.Lerp(a.AmbientSkyTint, b.AmbientSkyTint, t),
                Color.Lerp(a.AmbientEquatorTint, b.AmbientEquatorTint, t),
                Color.Lerp(a.AmbientGroundTint, b.AmbientGroundTint, t),
                Mathf.Lerp(a.AmbientTintWeight, b.AmbientTintWeight, t),
                Color.Lerp(a.CameraBackgroundTint, b.CameraBackgroundTint, t),
                Mathf.Lerp(a.CameraBackgroundBlend, b.CameraBackgroundBlend, t));
        }

        static readonly WeatherVariantProfile Clear = new(
            WeatherVariant.Clear,
            "Clear",
            1f,
            1.12f,
            0.72f,
            0.55f,
            new Color(0.92f, 0.78f, 0.42f),
            0.15f,
            1f,
            0f,
            0f,
            0f,
            1f,
            1.85f,
            new Color(1f, 0.96f, 0.72f),
            new Color(1f, 0.94f, 0.68f),
            new Color(1f, 0.88f, 0.52f),
            new Color(0.42f, 0.34f, 0.18f),
            0.35f,
            new Color(0.98f, 0.82f, 0.38f),
            0.25f);

        static readonly WeatherVariantProfile Rain = new(
            WeatherVariant.Rain,
            "Rain",
            0.62f,
            1f,
            1.55f,
            1.45f,
            new Color(0.03f, 0.02f, 0.07f),
            0.75f,
            0f,
            1f,
            1f,
            0f,
            0.78f,
            0.88f,
            new Color(0.72f, 0.82f, 1f),
            new Color(0.08f, 0.1f, 0.16f),
            new Color(0.06f, 0.07f, 0.12f),
            new Color(0.03f, 0.03f, 0.06f),
            0.55f,
            new Color(0.08f, 0.05f, 0.14f),
            0.65f);

        static readonly WeatherVariantProfile Fog = new(
            WeatherVariant.Fog,
            "Fog",
            0.88f,
            0.94f,
            1.05f,
            3.1f,
            new Color(0.62f, 0.66f, 0.72f),
            0.82f,
            0.55f,
            0f,
            0.18f,
            0f,
            0.58f,
            0.72f,
            new Color(0.82f, 0.86f, 0.92f),
            new Color(0.55f, 0.58f, 0.64f),
            new Color(0.45f, 0.48f, 0.54f),
            new Color(0.28f, 0.3f, 0.34f),
            0.65f,
            new Color(0.52f, 0.56f, 0.62f),
            0.72f);

        static readonly WeatherVariantProfile Sandstorm = new(
            WeatherVariant.Sandstorm,
            "Sandstorm",
            0.52f,
            0.88f,
            1.35f,
            2.35f,
            new Color(0.62f, 0.38f, 0.16f),
            0.88f,
            0.35f,
            0f,
            0.08f,
            1f,
            0.48f,
            0.68f,
            new Color(1f, 0.78f, 0.45f),
            new Color(0.72f, 0.42f, 0.2f),
            new Color(0.58f, 0.32f, 0.14f),
            new Color(0.32f, 0.18f, 0.08f),
            0.72f,
            new Color(0.55f, 0.32f, 0.12f),
            0.78f);
    }
}
