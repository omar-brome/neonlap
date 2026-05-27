using System;

namespace NeonLap.Services.Leaderboard
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string BoardId;
        public string PlayerName;
        public int TrackIndex;
        public string Mode;
        public float PrimaryValue;
        public int SecondaryValue;
        public long UnixTimeMs;
    }
}
