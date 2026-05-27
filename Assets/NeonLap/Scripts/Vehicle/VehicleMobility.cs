using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public static class VehicleMobility
    {
        const float MinAttachedPartRatio = 0.35f;

        public static bool IsRacerMobile(RacerProgress racer)
        {
            if (racer == null || racer.IsEliminated || racer.IsFinished)
                return false;

            var root = racer.transform;
            if (root == null)
                return false;

            var health = root.GetComponent<VehicleHealthSystem>();
            if (health != null && health.enabled && health.IsTotalled)
                return false;

            var ai = root.GetComponent<AIVehicleController>();
            if (ai != null && !ai.enabled)
                return false;

            var player = root.GetComponent<VehicleController>();
            if (player != null && !player.enabled)
                return false;

            var damage = root.GetComponent<VehicleDamageSystem>();
            if (damage != null && damage.AttachedPartRatio < MinAttachedPartRatio)
                return false;

            var rb = root.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic)
            {
                if (health != null && health.IsTotalled)
                    return false;
            }

            return true;
        }
    }
}
