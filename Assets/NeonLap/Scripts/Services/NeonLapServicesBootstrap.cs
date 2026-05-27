using NeonLap.Core;
using NeonLap.Services.Achievements;
using NeonLap.Services.Leaderboard;
using NeonLap.Services.Platform;
using UnityEngine;

namespace NeonLap.Services
{
    /// <summary>
    /// Initializes offline-first services on the persistent <see cref="Core.GameManager"/> object.
    /// </summary>
    public static class NeonLapServicesBootstrap
    {
        static bool initialized;

        public static void EnsureInitialized()
        {
            if (initialized)
                return;

            LeaderboardService.Initialize(preferUnityGamingServices: false);
            AchievementTracker.EnsureInstance();
            SteamAchievementsBridge.Ensure();
            GameTouchSettings.Load();

            if (Application.isMobilePlatform)
                NeonLapCloudSaveService.TryRestoreBackup(mergeIntoPlayerPrefs: true, out _);

            initialized = true;
        }
    }
}
