using UnityEngine;

namespace NeonLap.Race
{
    public static class CareerCupStore
    {
        public const int StarsPerCup = 9;

        public static bool IsCupUnlocked(CareerCupTier cup)
        {
            return cup switch
            {
                CareerCupTier.Rookie => true,
                CareerCupTier.Pro => CareerScoreStore.IsTrackUnlocked(CareerCupCatalog.ProCupFirstTrack),
                CareerCupTier.Elite => CareerScoreStore.IsTrackUnlocked(CareerCupCatalog.EliteCupFirstTrack),
                _ => false,
            };
        }

        public static int GetCupStars(CareerCupTier cup)
        {
            var total = 0;
            var first = CareerCupCatalog.GetFirstTrackIndex(cup);
            var last = CareerCupCatalog.GetLastTrackIndex(cup);
            for (var i = first; i <= last; i++)
                total += CareerScoreStore.GetForwardStars(i);

            return total;
        }

        public static string GetCupProgressLine(CareerCupTier cup)
        {
            var stars = GetCupStars(cup);
            var name = CareerCupCatalog.GetDisplayName(cup);
            if (!IsCupUnlocked(cup))
                return $"{name}: LOCKED";

            return $"{name}: {stars}/{StarsPerCup} ★";
        }

        public static string GetAllCupsProgressLine() =>
            $"{GetCupProgressLine(CareerCupTier.Rookie)}  •  {GetCupProgressLine(CareerCupTier.Pro)}  •  {GetCupProgressLine(CareerCupTier.Elite)}";

        public static string GetCupUnlockHint(CareerCupTier cup)
        {
            if (IsCupUnlocked(cup))
                return string.Empty;

            return cup switch
            {
                CareerCupTier.Pro =>
                    $"Clear Level {CareerCupCatalog.ProCupFirstTrack} (★ on L{CareerCupCatalog.ProCupFirstTrack}) to open Pro Cup",
                CareerCupTier.Elite =>
                    $"Clear Level {CareerCupCatalog.EliteCupFirstTrack} (★ on L{CareerCupCatalog.EliteCupFirstTrack}) to open Elite Cup",
                _ => string.Empty,
            };
        }
    }
}
