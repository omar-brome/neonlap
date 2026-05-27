using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public static class StuntProgressionGate
    {
        const string UnlockedKey = "NeonLap.StuntPark.Unlocked";
        public const int RequiredStars = 9;

        public static bool IsUnlocked()
        {
            if (PlayerPrefs.GetInt(UnlockedKey, 0) == 1)
                return true;

            if (CareerScoreStore.GetTotalStars() >= RequiredStars)
            {
                PlayerPrefs.SetInt(UnlockedKey, 1);
                PlayerPrefs.Save();
                return true;
            }

            return false;
        }

        public static string GetUnlockHint()
        {
            if (IsUnlocked())
                return "STUNT PARK — ramps, loops, half-pipes. No lap count, pure freestyle.";

            var remaining = Mathf.Max(0, RequiredStars - CareerScoreStore.GetTotalStars());
            return $"STUNT PARK locked — earn {remaining} more career ★ ({RequiredStars} total) to unlock.";
        }
    }
}
