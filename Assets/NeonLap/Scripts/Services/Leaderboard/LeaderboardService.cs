using System.Collections.Generic;

namespace NeonLap.Services.Leaderboard
{
    public static class LeaderboardService
    {
        static ILeaderboardBackend backend;
        static bool useCloudBackend;

        public static ILeaderboardBackend Backend => backend ??= new LocalJsonLeaderboardStore();

        public static void Initialize(bool preferUnityGamingServices = false)
        {
            useCloudBackend = preferUnityGamingServices;
            backend = preferUnityGamingServices
                ? new UnityGamingServicesLeaderboardBackend()
                : new LocalJsonLeaderboardStore();
        }

        public static void SetBackend(ILeaderboardBackend customBackend)
        {
            backend = customBackend;
        }

        public static string GetBoardId(string mode, int trackIndex, bool lowerIsBetter)
        {
            var metric = lowerIsBetter ? "time" : "score";
            return $"{mode}_track{trackIndex}_{metric}";
        }

        public static void SubmitTime(string mode, int trackIndex, float timeSeconds, string playerName = "Player")
        {
            Backend.Submit(new LeaderboardEntry
            {
                BoardId = GetBoardId(mode, trackIndex, lowerIsBetter: true),
                Mode = mode,
                TrackIndex = trackIndex,
                PlayerName = playerName,
                PrimaryValue = timeSeconds,
                SecondaryValue = 0,
            });
        }

        public static void SubmitScore(string mode, int trackIndex, int score, string playerName = "Player")
        {
            Backend.Submit(new LeaderboardEntry
            {
                BoardId = GetBoardId(mode, trackIndex, lowerIsBetter: false),
                Mode = mode,
                TrackIndex = trackIndex,
                PlayerName = playerName,
                PrimaryValue = -score,
                SecondaryValue = score,
            });
        }

        public static IReadOnlyList<LeaderboardEntry> GetTopTimes(string mode, int trackIndex, int maxEntries = 10)
        {
            return Backend.GetTop(GetBoardId(mode, trackIndex, lowerIsBetter: true), maxEntries);
        }

        public static IReadOnlyList<LeaderboardEntry> GetTopScores(string mode, int trackIndex, int maxEntries = 10)
        {
            var entries = Backend.GetTop(GetBoardId(mode, trackIndex, lowerIsBetter: false), maxEntries);
            return entries;
        }
    }
}
