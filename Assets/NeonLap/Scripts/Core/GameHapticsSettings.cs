using UnityEngine;

namespace NeonLap.Core
{
    public static class GameHapticsSettings
    {
        const string PrefKey = "NeonLap.HapticsEnabled";

        public static bool Enabled { get; private set; } = true;

        public static void Load()
        {
            Enabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            PlayerPrefs.SetInt(PrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
