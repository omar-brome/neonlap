using System.Collections.Generic;

namespace NeonLap.Services.Leaderboard
{
    public interface ILeaderboardBackend
    {
        string BackendId { get; }

        void Submit(LeaderboardEntry entry);

        IReadOnlyList<LeaderboardEntry> GetTop(string boardId, int maxEntries);
    }
}
