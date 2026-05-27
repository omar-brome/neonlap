using NeonLap.Environment;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    /// <summary>
    /// When the player leads, assigns one nearby rival to lane-block for a short window.
    /// </summary>
    public class RivalBlockerCoordinator : MonoBehaviour
    {
        const float BlockDurationSeconds = 2f;
        const float RetryIntervalSeconds = 7f;
        const float MinProgressBehind = 0.012f;
        const float MaxProgressBehind = 0.09f;

        RaceManager raceManager;
        float nextBlockAllowedTime;

        public static RivalBlockerCoordinator Ensure(RaceManager manager)
        {
            if (manager == null)
                return null;

            var existing = manager.GetComponent<RivalBlockerCoordinator>();
            if (existing != null)
            {
                existing.Configure(manager);
                return existing;
            }

            var coordinator = manager.gameObject.AddComponent<RivalBlockerCoordinator>();
            coordinator.Configure(manager);
            return coordinator;
        }

        public void Configure(RaceManager manager)
        {
            raceManager = manager;
        }

        void Update()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (Time.time < nextBlockAllowedTime)
                return;

            if (raceManager.GetPlayerPosition() != 1)
                return;

            if (!TryAssignBlocker(out var blocker, out var label))
                return;

            blocker.ActivatePlayerBlock(BlockDurationSeconds);
            nextBlockAllowedTime = Time.time + RetryIntervalSeconds;
            StadiumIncidentHub.Report($"{label} BLOCKING");
        }

        bool TryAssignBlocker(out AIVehicleController blocker, out string broadcastName)
        {
            blocker = null;
            broadcastName = "RIVAL";

            var player = raceManager.PlayerRacer;
            if (player == null)
                return false;

            var playerProgress = raceManager.GetRaceProgress(player);
            var bestDelta = float.MaxValue;

            foreach (var racer in raceManager.Racers)
            {
                if (racer == null || racer == player || racer.IsFinished || racer.IsEliminated)
                    continue;

                var ai = racer.GetComponent<AIVehicleController>();
                if (ai == null || ai.IsBlockingPlayer)
                    continue;

                var delta = playerProgress - raceManager.GetRaceProgress(racer);
                if (delta < MinProgressBehind || delta > MaxProgressBehind)
                    continue;

                if (delta >= bestDelta)
                    continue;

                bestDelta = delta;
                blocker = ai;
                var identity = racer.GetComponent<RivalIdentity>();
                broadcastName = identity != null && !string.IsNullOrWhiteSpace(identity.ShortName)
                    ? identity.ShortName
                    : $"R{raceManager.GetRacerPlacement(racer)}";
            }

            return blocker != null;
        }
    }
}
