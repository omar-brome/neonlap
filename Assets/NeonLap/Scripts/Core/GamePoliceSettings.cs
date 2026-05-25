using UnityEngine;

namespace NeonLap.Core
{
    public static class GamePoliceSettings
    {
        const string PrefKey = "NeonLap.PoliceEnabled";

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

        public static string GetDisplayName(bool enabled)
        {
            return enabled ? "ON" : "OFF";
        }
    }
}
