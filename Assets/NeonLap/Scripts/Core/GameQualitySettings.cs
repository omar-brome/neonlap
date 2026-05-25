using UnityEngine;

namespace NeonLap.Core
{
    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    public struct QualityPreset
    {
        public int AiRivalCount;
        public float HazardDensity;
        public float PickupDensity;
        public float BananaDensity;
        public float CrowdDensity;
        public float EnvironmentDensity;
        public bool EnableRain;
        public float RainIntensity;
        public bool EnableHelicopter;
        public float FogDensity;
        public float LightIntensity;
    }

    public static class GameQualitySettings
    {
        const string PrefKey = "NeonLap.QualityLevel";

        public static QualityLevel Current { get; private set; } = QualityLevel.Medium;

        public static QualityPreset Preset => GetPreset(Current);

        public static void Load()
        {
            Current = (QualityLevel)PlayerPrefs.GetInt(PrefKey, (int)QualityLevel.Medium);
            ApplyUnityQualityLevel(Current);
        }

        public static void SetLevel(QualityLevel level)
        {
            Current = level;
            PlayerPrefs.SetInt(PrefKey, (int)level);
            PlayerPrefs.Save();
            ApplyUnityQualityLevel(level);
        }

        public static string GetDisplayName(QualityLevel level)
        {
            return level switch
            {
                QualityLevel.Low => "LOW",
                QualityLevel.Medium => "MEDIUM",
                QualityLevel.High => "HIGH",
                _ => level.ToString().ToUpperInvariant(),
            };
        }

        public static void ApplyFogAndLighting(float fogDensity, float lightIntensity)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.03f, 0.02f, 0.07f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;

            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var sceneLight in lights)
            {
                if (sceneLight.type != LightType.Directional)
                    continue;

                sceneLight.intensity = lightIntensity;
                sceneLight.color = new Color(0.72f, 0.82f, 1f);
                break;
            }
        }

        public static QualityPreset GetPreset(QualityLevel level)
        {
            return level switch
            {
                QualityLevel.Low => new QualityPreset
                {
                    AiRivalCount = 3,
                    HazardDensity = 0.3f,
                    PickupDensity = 0.35f,
                    BananaDensity = 0.35f,
                    CrowdDensity = 0.35f,
                    EnvironmentDensity = 0.4f,
                    EnableRain = false,
                    RainIntensity = 0f,
                    EnableHelicopter = false,
                    FogDensity = 0.0028f,
                    LightIntensity = 0.24f,
                },
                QualityLevel.High => new QualityPreset
                {
                    AiRivalCount = 9,
                    HazardDensity = 1f,
                    PickupDensity = 1f,
                    BananaDensity = 1f,
                    CrowdDensity = 1f,
                    EnvironmentDensity = 1f,
                    EnableRain = true,
                    RainIntensity = 1f,
                    EnableHelicopter = true,
                    FogDensity = 0.0052f,
                    LightIntensity = 0.32f,
                },
                _ => new QualityPreset
                {
                    AiRivalCount = 6,
                    HazardDensity = 0.65f,
                    PickupDensity = 0.65f,
                    BananaDensity = 0.65f,
                    CrowdDensity = 0.65f,
                    EnvironmentDensity = 0.7f,
                    EnableRain = true,
                    RainIntensity = 0.65f,
                    EnableHelicopter = true,
                    FogDensity = 0.004f,
                    LightIntensity = 0.28f,
                },
            };
        }

        static void ApplyUnityQualityLevel(QualityLevel level)
        {
            if (QualitySettings.names == null || QualitySettings.names.Length == 0)
                return;

            var index = level switch
            {
                QualityLevel.Low => 0,
                QualityLevel.High => QualitySettings.names.Length - 1,
                _ => Mathf.Clamp(1, 0, QualitySettings.names.Length - 1),
            };

            QualitySettings.SetQualityLevel(index, true);
        }
    }
}
