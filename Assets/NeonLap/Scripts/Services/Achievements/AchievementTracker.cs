using System;
using UnityEngine;

namespace NeonLap.Services.Achievements
{
    /// <summary>
    /// Central achievement unlock + platform hook surface. Subscribe to <see cref="AchievementUnlocked"/>
    /// from Steamworks / mobile SDK wrappers.
    /// </summary>
    public class AchievementTracker : MonoBehaviour
    {
        public static AchievementTracker Instance { get; private set; }

        public static event Action<string> AchievementUnlocked;

        public static AchievementTracker EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("AchievementTracker");
            DontDestroyOnLoad(go);
            return go.AddComponent<AchievementTracker>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Unlock(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) || AchievementStore.IsUnlocked(achievementId))
                return;

            AchievementStore.Unlock(achievementId);
            AchievementUnlocked?.Invoke(achievementId);
            Debug.Log($"Achievement unlocked: {achievementId}");
        }

        public static void TryUnlock(string achievementId)
        {
            EnsureInstance()?.Unlock(achievementId);
        }
    }
}
