using UnityEngine;

namespace NeonLap.Race
{
    public static class CareerProgressionGate
    {
        public const int MaxStars = 18;
        const string EndlessUnlockedKey = "NeonLap.Career.EndlessUnlocked";
        const string EndlessCosmeticKey = "NeonLap.Career.EndlessCosmetic";

        public static bool IsEndlessUnlocked()
        {
            if (PlayerPrefs.GetInt(EndlessUnlockedKey, 0) == 1)
                return true;

            if (CareerScoreStore.GetTotalStars() >= MaxStars)
            {
                PlayerPrefs.SetInt(EndlessUnlockedKey, 1);
                PlayerPrefs.SetInt(EndlessCosmeticKey, 1);
                PlayerPrefs.Save();
                return true;
            }

            return false;
        }

        public static bool HasEndlessCosmetic() => PlayerPrefs.GetInt(EndlessCosmeticKey, 0) == 1;

        public static string GetStarProgressLine()
        {
            var total = CareerScoreStore.GetTotalStars();
            var line = $"{total}/{MaxStars} ★  •  {CareerCupStore.GetAllCupsProgressLine()}";
            if (IsEndlessUnlocked())
                return line + "  •  ENDLESS UNLOCKED";
            return line + $"  •  {MaxStars - total} ★ TO ENDLESS";
        }

        public static string GetUnlockRequirementForTrack(int trackIndex)
        {
            if (trackIndex <= 0)
                return string.Empty;

            if (CareerScoreStore.IsTrackUnlocked(trackIndex))
                return string.Empty;

            return $"Beat Level {trackIndex} with ★ to unlock";
        }
    }
}
