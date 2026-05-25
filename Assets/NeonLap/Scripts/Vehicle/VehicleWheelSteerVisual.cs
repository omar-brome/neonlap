using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleWheelSteerVisual : MonoBehaviour
    {
        struct WheelVisual
        {
            public Transform SteerPivot;
            public Transform SpinPivot;
            public bool IsLeft;
            public bool Steerable;
            public float SpinAngle;
        }

        [SerializeField] float maxSteerAngle = 30f;
        [SerializeField] float steerSmoothTime = 0.08f;
        [SerializeField] float wheelRadius = 0.18f;
        [SerializeField] float ackermannInnerScale = 1.1f;
        [SerializeField] float ackermannOuterScale = 0.9f;
        [SerializeField] float reverseSpinMultiplier = -1f;

        readonly WheelVisual[] wheels = new WheelVisual[4];

        Rigidbody rb;
        VehicleController playerController;
        AIVehicleController aiController;
        float smoothedLeftSteer;
        float smoothedRightSteer;
        float leftSteerVelocity;
        float rightSteerVelocity;
        bool initialized;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            playerController = GetComponent<VehicleController>();
            aiController = GetComponent<AIVehicleController>();
        }

        void Start()
        {
            TryInitialize();
        }

        void LateUpdate()
        {
            if (!initialized && !TryInitialize())
                return;

            var steerInput = GetSteerInput();
            var speedRatio = GetSpeedSteerRatio();
            var targetLeft = ComputeSteerAngle(steerInput, true, speedRatio);
            var targetRight = ComputeSteerAngle(steerInput, false, speedRatio);

            smoothedLeftSteer = Mathf.SmoothDamp(smoothedLeftSteer, targetLeft, ref leftSteerVelocity, steerSmoothTime);
            smoothedRightSteer = Mathf.SmoothDamp(smoothedRightSteer, targetRight, ref rightSteerVelocity,
                steerSmoothTime);

            var spinDelta = ComputeSpinDelta();

            for (var i = 0; i < wheels.Length; i++)
            {
                ref var wheel = ref wheels[i];
                if (wheel.SpinPivot == null)
                    continue;

                wheel.SpinAngle += spinDelta;
                wheel.SpinPivot.localRotation = Quaternion.Euler(wheel.SpinAngle, 0f, 0f);

                if (!wheel.Steerable || wheel.SteerPivot == null)
                    continue;

                var steerAngle = wheel.IsLeft ? smoothedLeftSteer : smoothedRightSteer;
                wheel.SteerPivot.localRotation = Quaternion.Euler(0f, steerAngle, 0f);
            }
        }

        bool TryInitialize()
        {
            if (initialized)
                return true;

            var visual = transform.Find("Visual");
            if (visual == null)
                return false;

            wheels[0] = BuildWheel(visual, "PodFL", true, true);
            wheels[1] = BuildWheel(visual, "PodFR", false, true);
            wheels[2] = BuildWheel(visual, "PodRL", true, false);
            wheels[3] = BuildWheel(visual, "PodRR", false, false);

            initialized = wheels[0].SpinPivot != null || wheels[2].SpinPivot != null;
            return initialized;
        }

        static WheelVisual BuildWheel(Transform visualRoot, string podId, bool isLeft, bool steerable)
        {
            var wheel = new WheelVisual
            {
                IsLeft = isLeft,
                Steerable = steerable,
            };

            if (steerable)
            {
                wheel.SteerPivot = visualRoot.Find(podId + "SteerPivot");
                wheel.SpinPivot = wheel.SteerPivot != null
                    ? wheel.SteerPivot.Find(podId + "SpinPivot")
                    : null;
            }
            else
            {
                wheel.SpinPivot = visualRoot.Find(podId + "SpinPivot");
            }

            return wheel;
        }

        float GetSteerInput()
        {
            if (playerController != null)
                return playerController.SteerInput;

            if (aiController != null)
                return aiController.SteerInput;

            return 0f;
        }

        float GetSpeedSteerRatio()
        {
            if (rb == null)
                return 0f;

            var forwardSpeed = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.forward));
            return Mathf.Clamp01(forwardSpeed / 12f);
        }

        float ComputeSteerAngle(float steerInput, bool isLeftWheel, float speedRatio)
        {
            if (Mathf.Abs(steerInput) < 0.001f)
                return 0f;

            var speedFalloff = Mathf.Lerp(0.35f, 1f, speedRatio);
            var turningRight = steerInput > 0f;
            var isInnerWheel = turningRight ? !isLeftWheel : isLeftWheel;
            var ackermann = isInnerWheel ? ackermannInnerScale : ackermannOuterScale;

            return steerInput * maxSteerAngle * ackermann * speedFalloff;
        }

        float ComputeSpinDelta()
        {
            if (rb == null || wheelRadius <= 0.01f)
                return 0f;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            var direction = forwardSpeed >= 0f ? 1f : reverseSpinMultiplier;
            var angularDegrees = forwardSpeed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime * direction;
            return angularDegrees;
        }
    }
}
