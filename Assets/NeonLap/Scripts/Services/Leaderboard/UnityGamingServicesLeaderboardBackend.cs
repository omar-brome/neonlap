using System.Collections.Generic;

namespace NeonLap.Services.Leaderboard
{
    /// <summary>
    /// Placeholder for Unity Gaming Services Leaderboards. Install com.unity.services.leaderboards,
    /// authenticate with UGS, then implement Submit/GetTop against the cloud API.
    /// </summary>
    public sealed class UnityGamingServicesLeaderboardBackend : ILeaderboardBackend
    {
        readonly LocalJsonLeaderboardStore fallback = new();

        public string BackendId => "ugs_stub";

        public bool IsCloudReady => false;

        public void Submit(LeaderboardEntry entry)
        {
            // TODO: UGS Leaderboards API when online services are enabled.
            fallback.Submit(entry);
        }

        public IReadOnlyList<LeaderboardEntry> GetTop(string boardId, int maxEntries)
        {
            return fallback.GetTop(boardId, maxEntries);
        }
    }
}
