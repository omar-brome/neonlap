using NeonLap.Core;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public class GhostCollisionTrigger : MonoBehaviour
    {
        const float PenaltyMultiplier = 0.82f;
        const float PenaltyDuration = 0.65f;
        const float RetriggerCooldown = 1.25f;

        GhostRacer ghostRacer;
        float lastPenaltyTime = -10f;

        public void Configure(GhostRacer racer)
        {
            ghostRacer = racer;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!TimeTrialSettings.GhostCollisionPenalty)
                return;

            if (ghostRacer == null || !ghostRacer.IsVisible || !ghostRacer.HasGhost)
                return;

            if (Time.time - lastPenaltyTime < RetriggerCooldown)
                return;

            var vehicle = other.GetComponentInParent<VehicleController>();
            if (vehicle == null || vehicle.GetComponent<AIVehicleController>() != null)
                return;

            vehicle.ApplyGhostSpeedPenalty(PenaltyMultiplier, PenaltyDuration);
            lastPenaltyTime = Time.time;
        }
    }
}
