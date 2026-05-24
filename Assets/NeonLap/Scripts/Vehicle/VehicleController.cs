using NeonLap.Input;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VehicleGroundProbe))]
    public class VehicleController : MonoBehaviour
    {
        [SerializeField] VehicleProfile profile;
        [SerializeField] MonoBehaviour inputProviderBehaviour;

        Rigidbody rb;
        VehicleGroundProbe groundProbe;
        IVehicleInputProvider inputProvider;
        float currentGripMultiplier = 1f;

        public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public float SteerInput { get; private set; }
        public bool IsDrifting { get; private set; }

        public void Configure(VehicleProfile vehicleProfile, IVehicleInputProvider provider)
        {
            profile = vehicleProfile;
            inputProvider = provider;
            inputProviderBehaviour = provider as MonoBehaviour;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundProbe = GetComponent<VehicleGroundProbe>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
            ResolveInputProvider();
        }

        void Start()
        {
            ResolveInputProvider();

            if (profile == null)
                Debug.LogError("VehicleController: VehicleProfile is not assigned.", this);
            if (inputProvider == null)
                Debug.LogError("VehicleController: IVehicleInputProvider is not assigned.", this);
        }

        void ResolveInputProvider()
        {
            if (inputProvider != null)
                return;

            inputProvider = inputProviderBehaviour as IVehicleInputProvider;
            if (inputProvider == null && inputProviderBehaviour != null)
                inputProvider = inputProviderBehaviour.GetComponent<IVehicleInputProvider>();
            if (inputProvider == null)
                inputProvider = GetComponent<IVehicleInputProvider>();
        }

        void FixedUpdate()
        {
            if (profile == null || inputProvider == null || rb.isKinematic)
                return;

            var probe = groundProbe.Probe();
            SteerInput = inputProvider.Steer;
            IsDrifting = inputProvider.DriftHeld;

            ApplyHover(probe);
            ApplyForwardForce();
            ApplySteering();
            ApplyGrip(probe);
            ApplyDownforce();
            ClampSpeed();
            AlignToGround(probe);
        }

        void ApplyHover(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var error = profile.hoverHeight - probe.Distance;
            var force = Vector3.up * (error * profile.hoverForce - rb.linearVelocity.y * profile.hoverDamping);
            rb.AddForce(force, ForceMode.Acceleration);
        }

        void ApplyForwardForce()
        {
            var forward = transform.forward;
            var speed = Vector3.Dot(rb.linearVelocity, forward);

            if (inputProvider.Accelerate > 0.01f)
            {
                rb.AddForce(forward * (profile.acceleration * inputProvider.Accelerate), ForceMode.Acceleration);
            }
            else if (inputProvider.Brake > 0.01f)
            {
                if (speed > 1f)
                    rb.AddForce(-forward * (profile.brakeForce * inputProvider.Brake), ForceMode.Acceleration);
                else
                    rb.AddForce(-forward * (profile.reverseForce * inputProvider.Brake), ForceMode.Acceleration);
            }
        }

        void ApplySteering()
        {
            var speedRatio = Mathf.Clamp01(CurrentSpeed / profile.maxSpeed);
            var turnSpeed = Mathf.Lerp(profile.turnSpeedLow, profile.turnSpeedHigh, speedRatio);
            var yaw = inputProvider.Steer * turnSpeed * Time.fixedDeltaTime;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            rb.MoveRotation(rb.rotation * rotation);
        }

        void ApplyGrip(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var targetGrip = inputProvider.DriftHeld ? profile.driftGripMultiplier : 1f;
            currentGripMultiplier = Mathf.MoveTowards(currentGripMultiplier, targetGrip,
                profile.driftRecovery * Time.fixedDeltaTime);

            var forward = transform.forward;
            var lateralVelocity = rb.linearVelocity - forward * Vector3.Dot(rb.linearVelocity, forward);
            var gripForce = -lateralVelocity * profile.grip * currentGripMultiplier;
            rb.AddForce(gripForce, ForceMode.Acceleration);
        }

        void ApplyDownforce()
        {
            rb.AddForce(-transform.up * profile.downforce, ForceMode.Acceleration);
        }

        void ClampSpeed()
        {
            if (rb.linearVelocity.magnitude <= profile.maxSpeed)
                return;

            rb.linearVelocity = rb.linearVelocity.normalized * profile.maxSpeed;
        }

        void AlignToGround(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var targetUp = probe.GroundNormal;
            var targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 8f * Time.fixedDeltaTime));
        }
    }
}
