using NeonLap.Race;
using NeonLap.Services.Achievements;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Services.Race
{
    public class RaceNegativeAchievementBridge : MonoBehaviour
    {
        RaceManager raceManager;
        VehicleSlipEffect slipEffect;
        PlayerHeatSystem heatSystem;
        int slipCount;
        bool subscribed;

        public static RaceNegativeAchievementBridge Setup(RaceManager manager, GameObject playerCar)
        {
            var go = new GameObject("RaceNegativeAchievementBridge");
            go.transform.SetParent(manager.transform, false);
            var bridge = go.AddComponent<RaceNegativeAchievementBridge>();
            bridge.Configure(manager, playerCar);
            return bridge;
        }

        void Configure(RaceManager manager, GameObject playerCar)
        {
            raceManager = manager;
            slipEffect = playerCar != null ? playerCar.GetComponent<VehicleSlipEffect>() : null;
            heatSystem = playerCar != null ? playerCar.GetComponent<PlayerHeatSystem>() : null;
            slipCount = 0;
            AchievementTracker.EnsureInstance();
            Subscribe();
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnLapCompleted += HandleLapCompleted;
            if (slipEffect != null)
                slipEffect.SlipApplied += HandleSlipApplied;
            if (heatSystem != null)
                heatSystem.HeatMaxed += HandleHeatMaxed;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnLapCompleted -= HandleLapCompleted;
            if (slipEffect != null)
                slipEffect.SlipApplied -= HandleSlipApplied;
            if (heatSystem != null)
                heatSystem.HeatMaxed -= HandleHeatMaxed;
            subscribed = false;
        }

        void HandleSlipApplied()
        {
            slipCount++;
            if (slipCount >= 3)
                AchievementTracker.TryUnlock(AchievementIds.SpunThreeTimes);
        }

        void HandleHeatMaxed()
        {
            AchievementTracker.TryUnlock(AchievementIds.PoliceBusted);
        }

        void HandleLapCompleted(int lap)
        {
            if (lap != 1 || raceManager == null)
                return;

            if (raceManager.GetPlayerPosition() == raceManager.TotalRacers)
                AchievementTracker.TryUnlock(AchievementIds.LastPlaceLap1);
        }
    }
}

