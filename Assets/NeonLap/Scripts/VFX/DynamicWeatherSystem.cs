using System.Collections.Generic;
using System.Text;
using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.VFX
{
    public class DynamicWeatherSystem : MonoBehaviour
    {
        [SerializeField] float transitionSeconds = 10f;
        [SerializeField] float lapTransitionSeconds = 6f;
        [SerializeField] float clearHoldMin = 55f;
        [SerializeField] float clearHoldMax = 95f;
        [SerializeField] float stormHoldMin = 75f;
        [SerializeField] float stormHoldMax = 130f;

        Transform cameraTransform;
        QualityPreset qualityPreset;
        RainEffect rainEffect;
        SandstormEffect sandstormEffect;
        SkyGraphicsSystem skySystem;
        Light directionalLight;
        UnityEngine.Camera mainCamera;
        RaceManager raceManager;

        float baseFogDensity;
        float baseLightIntensity;
        float baseFarClipPlane = 800f;
        float maxRainIntensity;
        bool rainEnabled;

        Color baselineFogColor;
        Color baselineAmbientSky;
        Color baselineAmbientEquator;
        Color baselineAmbientGround;
        Color baselineCameraBackground;

        WeatherVariant fromVariant = WeatherVariant.Clear;
        WeatherVariant targetVariant = WeatherVariant.Clear;
        float variantCrossfade = 1f;

        bool useLapSchedule;
        WeatherVariant[] lapVariants;
        int scheduledTotalLaps;

        float nextWeatherChangeTime;

        public static DynamicWeatherSystem Instance { get; private set; }

        public WeatherVariant ActiveVariant
        {
            get
            {
                var profile = GetBlendedProfile();
                return profile.Variant;
            }
        }

        public float RainIntensity
        {
            get
            {
                var profile = GetBlendedProfile();
                return rainEnabled ? profile.RainIntensity * maxRainIntensity : 0f;
            }
        }

        public float SunnyBlend => GetBlendedProfile().SunnySkyBlend;

        public float GripMultiplier => GetBlendedProfile().GripMultiplier;

        public float TopSpeedMultiplier => GetBlendedProfile().TopSpeedMultiplier;

        public float DriftScoreMultiplier => GetBlendedProfile().DriftScoreMultiplier;

        public bool IsLowVisibility => GetBlendedProfile().VisibilityScale < 0.82f;

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            UnbindRace();
            if (Instance == this)
                Instance = null;
        }

        public void Configure(Transform followCamera, QualityPreset preset)
        {
            cameraTransform = followCamera;
            qualityPreset = preset;
            rainEnabled = preset.EnableRain && preset.RainIntensity > 0.01f;
            maxRainIntensity = preset.RainIntensity;
            baseFogDensity = RenderSettings.fog ? RenderSettings.fogDensity : preset.FogDensity;
            baseLightIntensity = preset.LightIntensity;

            EnsureRainEffect();
            EnsureSandstormEffect();
            skySystem = SkyGraphicsSystem.Ensure(followCamera != null ? followCamera.GetComponent<UnityEngine.Camera>() : null);
            ResolveSceneReferences();
            CaptureThemeBaseline();

            ApplyForcedMenuVariant();
            if (!useLapSchedule)
                ScheduleNextAmbientChange(false);

            ApplyWeatherVisuals();
        }

        public void BindRace(RaceManager manager)
        {
            UnbindRace();
            raceManager = manager;
            if (raceManager == null)
                return;

            raceManager.OnStateChanged += HandleRaceStateChanged;
            raceManager.OnLapCompleted += HandleLapCompleted;
            BeginLapForecast(raceManager.TotalLaps);

            if (raceManager.State == RaceState.Countdown || raceManager.State == RaceState.Racing)
                ApplyScheduledLap(Mathf.Max(1, raceManager.CurrentLap));
        }

        void UnbindRace()
        {
            if (raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleRaceStateChanged;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager = null;
        }

        void HandleRaceStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
            {
                BeginLapForecast(raceManager != null ? raceManager.TotalLaps : scheduledTotalLaps);
                ApplyScheduledLap(1);
            }
        }

        void HandleLapCompleted(int completedLap)
        {
            if (!useLapSchedule || raceManager == null)
                return;

            ApplyScheduledLap(Mathf.Min(completedLap + 1, raceManager.TotalLaps));
        }

        void CaptureThemeBaseline()
        {
            baselineFogColor = RenderSettings.fogColor;
            baselineAmbientSky = RenderSettings.ambientSkyColor;
            baselineAmbientEquator = RenderSettings.ambientEquatorColor;
            baselineAmbientGround = RenderSettings.ambientGroundColor;
            baselineCameraBackground = mainCamera != null
                ? mainCamera.backgroundColor
                : new Color(0.08f, 0.05f, 0.14f);
        }

        void ApplyForcedMenuVariant()
        {
            var forced = GameTrackOptions.WeatherChoice switch
            {
                TrackWeatherChoice.ForceDry => WeatherVariant.Clear,
                TrackWeatherChoice.ForceRain => WeatherVariant.Rain,
                TrackWeatherChoice.ForceFog => WeatherVariant.Fog,
                TrackWeatherChoice.ForceSandstorm => WeatherVariant.Sandstorm,
                _ => WeatherVariant.Clear,
            };

            if (GameTrackOptions.WeatherChoice != TrackWeatherChoice.Forecast)
            {
                useLapSchedule = false;
                SetTargetVariant(forced, true);
            }
        }

        public void BeginLapForecast(int totalLaps)
        {
            scheduledTotalLaps = Mathf.Max(1, totalLaps);
            lapVariants = new WeatherVariant[scheduledTotalLaps];
            for (var i = 0; i < lapVariants.Length; i++)
                lapVariants[i] = WeatherVariant.Clear;

            useLapSchedule = scheduledTotalLaps >= 1 &&
                             GameTrackOptions.WeatherChoice == TrackWeatherChoice.Forecast;

            if (!useLapSchedule)
            {
                ApplyForcedMenuVariant();
                return;
            }

            if (scheduledTotalLaps < 2)
                return;

            var rainStart = Random.Range(2, scheduledTotalLaps + 1);
            if (scheduledTotalLaps >= 4 && Random.value > 0.45f)
            {
                var rainEnd = Mathf.Min(scheduledTotalLaps, rainStart + Random.Range(0, 2));
                for (var lap = rainStart; lap <= rainEnd; lap++)
                    lapVariants[lap - 1] = WeatherVariant.Rain;
            }
            else
            {
                lapVariants[rainStart - 1] = WeatherVariant.Rain;
            }

            if (scheduledTotalLaps >= 3 && Random.value > 0.55f)
            {
                var fogLap = Random.Range(1, scheduledTotalLaps + 1);
                if (lapVariants[fogLap - 1] == WeatherVariant.Clear)
                    lapVariants[fogLap - 1] = WeatherVariant.Fog;
            }

            if (scheduledTotalLaps >= 4 && Random.value > 0.65f)
            {
                var sandLap = Random.Range(1, scheduledTotalLaps + 1);
                if (lapVariants[sandLap - 1] == WeatherVariant.Clear)
                    lapVariants[sandLap - 1] = WeatherVariant.Sandstorm;
            }
        }

        public string GetCountdownForecastText()
        {
            if (GameTrackOptions.WeatherChoice != TrackWeatherChoice.Forecast)
            {
                var profile = WeatherVariantProfile.Get(MapMenuChoice(GameTrackOptions.WeatherChoice));
                return profile.Variant switch
                {
                    WeatherVariant.Rain => "Rain locked — wet track, reduced grip",
                    WeatherVariant.Fog => "Fog locked — limited visibility",
                    WeatherVariant.Sandstorm => "Sandstorm locked — low grip, low visibility",
                    _ => "Clear skies — dry grip and top speed boost",
                };
            }

            if (lapVariants == null || lapVariants.Length == 0)
                return "Forecast: clear skies";

            var lines = new List<string>();
            AppendVariantLaps(lines, WeatherVariant.Rain, "Rain");
            AppendVariantLaps(lines, WeatherVariant.Fog, "Fog");
            AppendVariantLaps(lines, WeatherVariant.Sandstorm, "Sand");

            if (lines.Count == 0)
                return "Dry race — sunny speed boost";

            if (lines.Count == 1)
                return lines[0];

            return string.Join(" · ", lines);
        }

        void AppendVariantLaps(List<string> lines, WeatherVariant variant, string label)
        {
            var laps = new List<int>();
            for (var i = 0; i < lapVariants.Length; i++)
            {
                if (lapVariants[i] == variant)
                    laps.Add(i + 1);
            }

            if (laps.Count == 0)
                return;

            if (laps.Count == 1)
            {
                lines.Add($"{label} L{laps[0]}");
                return;
            }

            if (IsContiguous(laps))
                lines.Add($"{label} L{laps[0]}-{laps[laps.Count - 1]}");
            else
                lines.Add($"{label} ×{laps.Count}");
        }

        static bool IsContiguous(List<int> values)
        {
            if (values.Count <= 1)
                return true;

            for (var i = 1; i < values.Count; i++)
            {
                if (values[i] != values[i - 1] + 1)
                    return false;
            }

            return true;
        }

        void ApplyScheduledLap(int lap)
        {
            if (!useLapSchedule || lapVariants == null || lapVariants.Length == 0)
                return;

            var index = Mathf.Clamp(lap - 1, 0, lapVariants.Length - 1);
            SetTargetVariant(lapVariants[index], false);
        }

        void Update()
        {
            if (!useLapSchedule && Time.time >= nextWeatherChangeTime)
                ScheduleNextAmbientChange(true);

            var duration = useLapSchedule ? lapTransitionSeconds : transitionSeconds;
            var step = duration <= 0.01f ? 1f : Time.deltaTime / duration;
            variantCrossfade = Mathf.MoveTowards(variantCrossfade, 1f, step);
            ApplyWeatherVisuals();
        }

        void ScheduleNextAmbientChange(bool toggleTarget)
        {
            if (useLapSchedule)
                return;

            if (toggleTarget)
            {
                var next = targetVariant == WeatherVariant.Clear
                    ? (Random.value > 0.55f ? WeatherVariant.Rain : WeatherVariant.Fog)
                    : WeatherVariant.Clear;
                SetTargetVariant(next, false);
            }

            var holdDuration = targetVariant == WeatherVariant.Clear
                ? Random.Range(clearHoldMin, clearHoldMax)
                : Random.Range(stormHoldMin, stormHoldMax);
            nextWeatherChangeTime = Time.time + holdDuration + transitionSeconds;
        }

        void SetTargetVariant(WeatherVariant variant, bool immediate)
        {
            if (immediate || variantCrossfade >= 0.999f)
            {
                fromVariant = variant;
                targetVariant = variant;
                variantCrossfade = 1f;
                return;
            }

            fromVariant = variantCrossfade < 0.5f ? fromVariant : targetVariant;
            targetVariant = variant;
            variantCrossfade = 0f;
        }

        WeatherVariantProfile GetBlendedProfile()
        {
            if (variantCrossfade >= 1f)
                return WeatherVariantProfile.Get(targetVariant);

            return WeatherVariantProfile.Lerp(
                WeatherVariantProfile.Get(fromVariant),
                WeatherVariantProfile.Get(targetVariant),
                variantCrossfade);
        }

        static WeatherVariant MapMenuChoice(TrackWeatherChoice choice)
        {
            return choice switch
            {
                TrackWeatherChoice.ForceRain => WeatherVariant.Rain,
                TrackWeatherChoice.ForceFog => WeatherVariant.Fog,
                TrackWeatherChoice.ForceSandstorm => WeatherVariant.Sandstorm,
                _ => WeatherVariant.Clear,
            };
        }

        void EnsureRainEffect()
        {
            rainEffect = FindAnyObjectByType<RainEffect>();
            if (!rainEnabled)
            {
                rainEffect?.SetWeatherIntensity(0f);
                return;
            }

            if (rainEffect == null && cameraTransform != null)
            {
                var rainGo = new GameObject("RainEffect");
                rainEffect = rainGo.AddComponent<RainEffect>();
            }

            rainEffect?.Configure(cameraTransform, maxRainIntensity);
        }

        void EnsureSandstormEffect()
        {
            sandstormEffect = FindAnyObjectByType<SandstormEffect>();
            if (sandstormEffect == null && cameraTransform != null)
            {
                var sandGo = new GameObject("SandstormEffect");
                sandstormEffect = sandGo.AddComponent<SandstormEffect>();
            }

            sandstormEffect?.Configure(cameraTransform, 1f);
        }

        void ResolveSceneReferences()
        {
            if (mainCamera == null && cameraTransform != null)
                mainCamera = cameraTransform.GetComponent<UnityEngine.Camera>();

            mainCamera ??= UnityEngine.Camera.main;

            if (mainCamera != null)
                baseFarClipPlane = mainCamera.farClipPlane;

            if (directionalLight == null)
            {
                foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                {
                    if (light.type != LightType.Directional)
                        continue;

                    directionalLight = light;
                    break;
                }
            }
        }

        void ApplyWeatherVisuals()
        {
            var profile = GetBlendedProfile();
            var fogDensity = baseFogDensity * profile.FogDensityMultiplier;
            var fogColor = Color.Lerp(baselineFogColor, profile.FogColorTint, profile.FogColorBlend);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = fogColor;
            RenderSettings.ambientSkyColor = Color.Lerp(baselineAmbientSky, profile.AmbientSkyTint,
                profile.AmbientTintWeight);
            RenderSettings.ambientEquatorColor = Color.Lerp(baselineAmbientEquator, profile.AmbientEquatorTint,
                profile.AmbientTintWeight);
            RenderSettings.ambientGroundColor = Color.Lerp(baselineAmbientGround, profile.AmbientGroundTint,
                profile.AmbientTintWeight);

            if (directionalLight != null)
            {
                directionalLight.intensity = baseLightIntensity * profile.LightIntensityMultiplier;
                directionalLight.color = profile.DirectionalLightColor;
            }

            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.Lerp(baselineCameraBackground, profile.CameraBackgroundTint,
                    profile.CameraBackgroundBlend);
                mainCamera.farClipPlane = Mathf.Max(120f, baseFarClipPlane * profile.VisibilityScale);
            }

            if (rainEffect != null && rainEnabled)
                rainEffect.SetWeatherIntensity(profile.RainIntensity * maxRainIntensity);

            sandstormEffect?.SetWeatherIntensity(profile.SandIntensity);
            skySystem?.ApplyWeatherBlend(profile.SunnySkyBlend);
            WetTrackSurfaceController.Instance?.SetWetness(profile.WetRoadBlend);
        }
    }
}
