using NeonLap.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.VFX
{
    public class DynamicWeatherSystem : MonoBehaviour
    {
        static readonly Color RainyFogColor = new(0.03f, 0.02f, 0.07f);
        static readonly Color SunnyFogColor = new(0.92f, 0.78f, 0.42f);
        static readonly Color RainyAmbientSky = new(0.08f, 0.1f, 0.16f);
        static readonly Color SunnyAmbientSky = new(1f, 0.94f, 0.68f);
        static readonly Color RainyAmbientEquator = new(0.06f, 0.07f, 0.12f);
        static readonly Color SunnyAmbientEquator = new(1f, 0.88f, 0.52f);
        static readonly Color RainyAmbientGround = new(0.03f, 0.03f, 0.06f);
        static readonly Color SunnyAmbientGround = new(0.42f, 0.34f, 0.18f);
        static readonly Color RainyCameraBackground = new(0.08f, 0.05f, 0.14f);
        static readonly Color SunnyCameraBackground = new(0.98f, 0.82f, 0.38f);
        static readonly Color RainyLightColor = new(0.72f, 0.82f, 1f);
        static readonly Color SunnyLightColor = new(1f, 0.96f, 0.72f);

        [SerializeField] float transitionSeconds = 10f;
        [SerializeField] float rainyHoldMin = 75f;
        [SerializeField] float rainyHoldMax = 130f;
        [SerializeField] float sunnyHoldMin = 55f;
        [SerializeField] float sunnyHoldMax = 95f;

        Transform cameraTransform;
        QualityPreset qualityPreset;
        RainEffect rainEffect;
        SkyGraphicsSystem skySystem;
        Light directionalLight;
        UnityEngine.Camera mainCamera;

        float baseFogDensity;
        float baseLightIntensity;
        float maxRainIntensity;
        bool rainEnabled;

        float currentSunnyBlend;
        float targetSunnyBlend;
        float nextWeatherChangeTime;
        float sunnyFogDensity;
        float sunnyLightIntensity;

        public void Configure(Transform followCamera, QualityPreset preset)
        {
            cameraTransform = followCamera;
            qualityPreset = preset;
            rainEnabled = preset.EnableRain && preset.RainIntensity > 0.01f;
            maxRainIntensity = preset.RainIntensity;
            baseFogDensity = preset.FogDensity;
            baseLightIntensity = preset.LightIntensity;

            sunnyFogDensity = Mathf.Max(baseFogDensity * 0.28f, 0.0011f);
            sunnyLightIntensity = Mathf.Max(baseLightIntensity * 1.85f, 0.48f);

            EnsureRainEffect();
            skySystem = SkyGraphicsSystem.Ensure(followCamera != null ? followCamera.GetComponent<UnityEngine.Camera>() : null);
            ResolveSceneReferences();
            ScheduleNextWeatherChange(false);
            ApplyWeatherVisuals(currentSunnyBlend);
        }

        void Update()
        {
            if (Time.time >= nextWeatherChangeTime)
                ScheduleNextWeatherChange(true);

            var step = transitionSeconds <= 0.01f ? 1f : Time.deltaTime / transitionSeconds;
            currentSunnyBlend = Mathf.MoveTowards(currentSunnyBlend, targetSunnyBlend, step);
            ApplyWeatherVisuals(currentSunnyBlend);
        }

        void ScheduleNextWeatherChange(bool toggleTarget)
        {
            if (toggleTarget)
                targetSunnyBlend = targetSunnyBlend < 0.5f ? 1f : 0f;

            var holdDuration = targetSunnyBlend > 0.5f
                ? Random.Range(sunnyHoldMin, sunnyHoldMax)
                : Random.Range(rainyHoldMin, rainyHoldMax);
            nextWeatherChangeTime = Time.time + holdDuration + transitionSeconds;
        }

        void EnsureRainEffect()
        {
            rainEffect = FindAnyObjectByType<RainEffect>();
            if (!rainEnabled)
            {
                if (rainEffect != null)
                    rainEffect.SetWeatherIntensity(0f);
                return;
            }

            if (rainEffect == null && cameraTransform != null)
            {
                var rainGo = new GameObject("RainEffect");
                rainEffect = rainGo.AddComponent<RainEffect>();
            }

            if (rainEffect != null)
                rainEffect.Configure(cameraTransform, maxRainIntensity);
        }

        void ResolveSceneReferences()
        {
            if (mainCamera == null && cameraTransform != null)
                mainCamera = cameraTransform.GetComponent<UnityEngine.Camera>();

            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;

            if (directionalLight == null)
            {
                foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLight = light;
                        break;
                    }
                }
            }
        }

        void ApplyWeatherVisuals(float sunnyBlend)
        {
            sunnyBlend = Mathf.Clamp01(sunnyBlend);
            var rainyBlend = 1f - sunnyBlend;
            var rainyFog = baseFogDensity * (rainEnabled ? 1.35f : 1f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = Mathf.Lerp(rainyFog, sunnyFogDensity, sunnyBlend);
            RenderSettings.fogColor = Color.Lerp(RainyFogColor, SunnyFogColor, sunnyBlend);
            RenderSettings.ambientSkyColor = Color.Lerp(RainyAmbientSky, SunnyAmbientSky, sunnyBlend);
            RenderSettings.ambientEquatorColor = Color.Lerp(RainyAmbientEquator, SunnyAmbientEquator, sunnyBlend);
            RenderSettings.ambientGroundColor = Color.Lerp(RainyAmbientGround, SunnyAmbientGround, sunnyBlend);

            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(baseLightIntensity, sunnyLightIntensity, sunnyBlend);
                directionalLight.color = Color.Lerp(RainyLightColor, SunnyLightColor, sunnyBlend);
            }

            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.Lerp(RainyCameraBackground, SunnyCameraBackground, sunnyBlend);
            }

            if (rainEffect != null && rainEnabled)
                rainEffect.SetWeatherIntensity(rainyBlend * maxRainIntensity);

            skySystem?.ApplyWeatherBlend(sunnyBlend);
        }
    }
}
