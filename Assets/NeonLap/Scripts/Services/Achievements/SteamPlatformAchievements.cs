using UnityEngine;

#if STEAMWORKS
using Steamworks;
#endif

namespace NeonLap.Services.Achievements
{
    /// <summary>
    /// Steamworks unlock hook. Define STEAMWORKS in Player Settings when the Steamworks.NET package is installed.
    /// </summary>
    public static class SteamPlatformAchievements
    {
        static bool initialized;

        public static bool TryInitialize()
        {
#if STEAMWORKS
            if (initialized)
                return true;

            if (!SteamAPI.Init())
            {
                Debug.LogWarning("SteamPlatformAchievements: SteamAPI.Init failed.");
                return false;
            }

            initialized = true;
            return true;
#else
            return false;
#endif
        }

        public static void Unlock(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
                return;

            var apiName = SteamAchievementMapping.GetSteamApiName(achievementId);

#if STEAMWORKS
            if (!TryInitialize())
                return;

            SteamUserStats.SetAchievement(apiName);
            SteamUserStats.StoreStats();
            Debug.Log($"Steam achievement unlocked: {apiName}");
#else
            Debug.Log($"Steam achievement (stub): {apiName}");
#endif
        }

        public static void Shutdown()
        {
#if STEAMWORKS
            if (!initialized)
                return;

            SteamAPI.Shutdown();
            initialized = false;
#endif
        }
    }
}
