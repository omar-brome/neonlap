using UnityEngine;

namespace NeonLap.Core
{
    public static class GameTouchSettings
    {
        const string ForceUiKey = "NeonLap.Touch.ForceUi";
        const string AutoAccelerateKey = "NeonLap.Touch.AutoAccelerate";

        public static bool ForceTouchUi
        {
            get => PlayerPrefs.GetInt(ForceUiKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ForceUiKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool AutoAccelerate
        {
            get => PlayerPrefs.GetInt(AutoAccelerateKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(AutoAccelerateKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void Load()
        {
            _ = ForceTouchUi;
            _ = AutoAccelerate;
        }

        public static string GetSummaryLine()
        {
            return $"Touch UI: {(ForceTouchUi ? "FORCED" : "AUTO")}  •  Auto gas: {(AutoAccelerate ? "ON" : "OFF")}";
        }
    }
}
