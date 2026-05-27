using NeonLap.Core;
using NeonLap.Track;
using NeonLap.VFX;
using UnityEngine;

namespace NeonLap.Environment
{
    public static class TrackThemeApplicator
    {
        public static void Apply(TrackDefinition definition, QualityPreset qualityPreset, UnityEngine.Camera mainCamera = null)
        {
            var profile = TrackThemeProfile.ForDefinition(definition);
            Apply(profile, qualityPreset, mainCamera);
        }

        public static void Apply(TrackThemeProfile profile, QualityPreset qualityPreset,
            UnityEngine.Camera mainCamera = null)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.AmbientSky;
            RenderSettings.ambientEquatorColor = profile.AmbientEquator;
            RenderSettings.ambientGroundColor = profile.AmbientGround;
            RenderSettings.fogColor = profile.FogColor;

            var fogDensity = qualityPreset.FogDensity * profile.FogDensityScale;
            var lightIntensity = qualityPreset.LightIntensity * profile.LightIntensityScale;
            GameQualitySettings.ApplyFogAndLighting(fogDensity, lightIntensity);

            ApplyDirectionalLight(profile);

            var sky = SkyGraphicsSystem.Ensure(mainCamera);
            sky.ApplyTrackTheme(profile.Sky);

            if (mainCamera != null)
                mainCamera.backgroundColor = profile.Sky.CameraBackground;
        }

        static void ApplyDirectionalLight(TrackThemeProfile profile)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var light in lights)
            {
                if (light.type != LightType.Directional)
                    continue;

                light.color = profile.DirectionalLightColor;
                break;
            }
        }
    }
}
