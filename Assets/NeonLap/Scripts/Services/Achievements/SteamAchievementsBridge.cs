using UnityEngine;

namespace NeonLap.Services.Achievements
{
    public class SteamAchievementsBridge : MonoBehaviour
    {
        bool subscribed;

        public static SteamAchievementsBridge Ensure()
        {
            var existing = FindAnyObjectByType<SteamAchievementsBridge>();
            if (existing != null)
                return existing;

            var tracker = AchievementTracker.EnsureInstance();
            return tracker.gameObject.AddComponent<SteamAchievementsBridge>();
        }

        void OnEnable()
        {
            Subscribe();
            SteamPlatformAchievements.TryInitialize();
            CareerAchievementEvaluator.SyncAll();
        }

        void OnDisable() => Unsubscribe();

        void OnDestroy() => SteamPlatformAchievements.Shutdown();

        void Subscribe()
        {
            if (subscribed)
                return;

            AchievementTracker.AchievementUnlocked += HandleAchievementUnlocked;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;

            AchievementTracker.AchievementUnlocked -= HandleAchievementUnlocked;
            subscribed = false;
        }

        void HandleAchievementUnlocked(string achievementId)
        {
            SteamPlatformAchievements.Unlock(achievementId);
            CareerAchievementEvaluator.SyncAll();
        }
    }
}
