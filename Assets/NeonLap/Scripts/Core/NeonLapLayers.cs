namespace NeonLap.Core
{
    public static class NeonLapLayers
    {
        public const int Track = 6;
        public const int Vehicle = 7;
        public const int Obstacle = 8;

        public static int TrackMask => 1 << Track;
        public static int ObstacleMask => 1 << Obstacle;
        public static int VehicleMask => 1 << Vehicle;
    }
}
