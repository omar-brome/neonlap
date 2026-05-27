namespace NeonLap.Race
{
    public enum CareerCupTier
    {
        Rookie = 0,
        Pro = 1,
        Elite = 2,
    }

    public static class CareerCupCatalog
    {
        public const int RookieCupFirstTrack = 0;
        public const int RookieCupLastTrack = 2;
        public const int ProCupFirstTrack = 3;
        public const int ProCupLastTrack = 4;
        public const int EliteCupFirstTrack = 5;
        public const int EliteCupLastTrack = 6;

        public static CareerCupTier GetCupForTrack(int trackIndex)
        {
            if (trackIndex <= RookieCupLastTrack)
                return CareerCupTier.Rookie;

            if (trackIndex <= ProCupLastTrack)
                return CareerCupTier.Pro;

            return CareerCupTier.Elite;
        }

        public static int GetFirstTrackIndex(CareerCupTier cup) =>
            cup switch
            {
                CareerCupTier.Pro => ProCupFirstTrack,
                CareerCupTier.Elite => EliteCupFirstTrack,
                _ => RookieCupFirstTrack,
            };

        public static int GetLastTrackIndex(CareerCupTier cup) =>
            cup switch
            {
                CareerCupTier.Pro => ProCupLastTrack,
                CareerCupTier.Elite => EliteCupLastTrack,
                _ => RookieCupLastTrack,
            };

        public static string GetDisplayName(CareerCupTier cup) =>
            cup switch
            {
                CareerCupTier.Pro => "Pro Cup",
                CareerCupTier.Elite => "Elite Cup",
                _ => "Rookie Cup",
            };

        public static string GetShortTag(CareerCupTier cup) =>
            cup switch
            {
                CareerCupTier.Pro => "PRO",
                CareerCupTier.Elite => "ELITE",
                _ => "ROOKIE",
            };
    }
}
