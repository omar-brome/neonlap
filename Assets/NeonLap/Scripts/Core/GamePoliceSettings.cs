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

        /// <summary>
        /// Whether optional police chase can spawn for the active race mode.
        /// Time Trial / Ghost Duel use <see cref="TimeTrialSettings.PoliceEnabled"/> instead of the global toggle.
        /// </summary>
        public static bool IsActiveForCurrentRace()
        {
            if (GameRaceModeSettings.IsChase)
                return true;

            if (GameRaceModeSettings.IsPractice)
                return false;

            if (GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel)
            {
                TimeTrialSettings.Load();
                return TimeTrialSettings.PoliceEnabled;
            }

            return Enabled;
        }
    }
}
