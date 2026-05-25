using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.VFX
{
    public class DriftTrailVFX : MonoBehaviour
    {
        [SerializeField] VehicleController vehicle;
        [SerializeField] TrailRenderer leftTrail;
        [SerializeField] TrailRenderer rightTrail;
        [SerializeField] float lateralSlipThreshold = 2.4f;

        void Awake()
        {
            if (vehicle == null)
                vehicle = GetComponentInParent<VehicleController>();
        }

        void Update()
        {
            if (vehicle == null)
                return;

            var drifting = vehicle.IsDrifting || vehicle.LateralSpeed >= lateralSlipThreshold ||
                           (vehicle.GetComponent<VehicleSlipEffect>()?.IsSlipping ?? false);
            SetTrail(leftTrail, drifting);
            SetTrail(rightTrail, drifting);
        }

        static void SetTrail(TrailRenderer trail, bool emit)
        {
            if (trail == null)
                return;

            trail.emitting = emit;
        }
    }
}
