using UnityEngine;

namespace NeonLap.Track
{
    public enum TrackZoneType
    {
        Overdrive = 0,
        GravityWell = 1,
        WindGust = 2,
        MetroTunnel = 3,
        DriftMultiplier = 4,
        AirCrest = 5,
    }

    public struct TrackGameplayZone
    {
        public TrackZoneType Type;
        public float StartDistance;
        public float EndDistance;
        public float Strength;
        public float AiTrackHalfWidthScale;
        public Vector3 WindDirection;
    }
}
