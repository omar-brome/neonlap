using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.VFX
{
    public class VehicleDriftMarkEmitter : MonoBehaviour
    {
        static readonly Vector3[] WheelLocalPositions =
        {
            new(-0.62f, 0.02f, -0.85f),
            new(0.62f, 0.02f, -0.85f),
            new(-0.62f, 0.02f, 0.85f),
            new(0.62f, 0.02f, 0.85f),
        };

        [SerializeField] float lateralSlipThreshold = 2.2f;
        [SerializeField] float minForwardSpeed = 4f;
        [SerializeField] float emitInterval = 0.055f;

        VehicleController vehicleController;
        VehicleSlipEffect slipEffect;
        Rigidbody rb;
        float nextEmitTime;
        int nextWheelIndex;

        void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            slipEffect = GetComponent<VehicleSlipEffect>();
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (rb == null || rb.isKinematic || !IsLeavingDriftMarks())
                return;

            if (Time.time < nextEmitTime)
                return;

            var system = DriftMarkSystem.Instance;
            if (system == null)
                return;

            nextEmitTime = Time.time + emitInterval;
            var intensity = GetDriftIntensity();

            for (var i = 0; i < 2; i++)
            {
                var wheel = WheelLocalPositions[nextWheelIndex];
                nextWheelIndex = (nextWheelIndex + 1) % WheelLocalPositions.Length;
                system.PlaceMark(transform.TransformPoint(wheel), transform.forward, intensity);
            }
        }

        bool IsLeavingDriftMarks()
        {
            if (vehicleController != null)
            {
                return vehicleController.IsDrifting
                       || vehicleController.LateralSpeed >= lateralSlipThreshold
                       || (slipEffect != null && slipEffect.IsSlipping);
            }

            if (rb == null)
                return false;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (Mathf.Abs(forwardSpeed) < minForwardSpeed)
                return false;

            var lateral = rb.linearVelocity - transform.forward * forwardSpeed;
            lateral.y = 0f;
            return lateral.magnitude >= lateralSlipThreshold;
        }

        float GetDriftIntensity()
        {
            if (vehicleController != null)
            {
                var lateral = vehicleController.LateralSpeed;
                return Mathf.Clamp01(lateral / (lateralSlipThreshold * 2.4f));
            }

            if (rb == null)
                return 0.5f;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            var lateralVelocity = rb.linearVelocity - transform.forward * forwardSpeed;
            lateralVelocity.y = 0f;
            return Mathf.Clamp01(lateralVelocity.magnitude / (lateralSlipThreshold * 2.4f));
        }
    }
}
