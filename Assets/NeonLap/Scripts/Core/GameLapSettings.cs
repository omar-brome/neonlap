using UnityEngine;

namespace NeonLap.Core
{
    public static class GameLapSettings
    {
        const string PrefKey = "NeonLap.LapCount";

        public static readonly int[] AvailableLaps = { 1, 2, 3, 5 };

        public static int CurrentLaps { get; private set; } = 1;

        public static void Load()
        {
            CurrentLaps = ClampToAvailable(PlayerPrefs.GetInt(PrefKey, 1));
        }

        public static void SetLaps(int laps)
        {
            CurrentLaps = ClampToAvailable(laps);
            PlayerPrefs.SetInt(PrefKey, CurrentLaps);
            PlayerPrefs.Save();
        }

        public static string GetDisplayName(int laps)
        {
            return laps == 1 ? "1 LAP" : $"{laps} LAPS";
        }

        static int ClampToAvailable(int laps)
        {
            foreach (var option in AvailableLaps)
            {
                if (option == laps)
                    return laps;
            }

            return 1;
        }
    }
}
