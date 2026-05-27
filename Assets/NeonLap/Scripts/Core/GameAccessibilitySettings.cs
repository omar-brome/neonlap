using UnityEngine;

namespace NeonLap.Core
{
    public static class GameAccessibilitySettings
    {
        const string AssistKey = "NeonLap.Accessibility.SteeringAssist";
        const string AutoAccelKey = "NeonLap.Accessibility.AutoAccelerate";

        /// <summary>
        /// 0 = off, 1 = maximum assist (reduces high-speed steering).
        /// </summary>
        public static float SteeringAssist { get; private set; } = 0.35f;

        public static bool AutoAccelerate { get; private set; } = false;

        public static void Load()
        {
            SteeringAssist = Mathf.Clamp01(PlayerPrefs.GetFloat(AssistKey, 0.35f));
            AutoAccelerate = PlayerPrefs.GetInt(AutoAccelKey, 0) == 1;
        }

        public static void SetSteeringAssist(float value)
        {
            SteeringAssist = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(AssistKey, SteeringAssist);
            PlayerPrefs.Save();
        }

        public static void SetAutoAccelerate(bool enabled)
        {
            AutoAccelerate = enabled;
            PlayerPrefs.SetInt(AutoAccelKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

