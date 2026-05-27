using NeonLap.Input;
using NeonLap.VFX;
using NeonLap.Track;
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
        VehicleNitroBoost nitroBoost;
        VehicleHoverPodSystem hoverPodSystem;
        VehicleSlipEffect slipEffect;
        VehicleBarrelRoll barrelRoll;
        VehicleFuelSystem fuelSystem;
        float currentGripMultiplier = 1f;
        float zoneHoverForceMultiplier = 1f;
        float ghostSpeedPenaltyMultiplier = 1f;
        float ghostSpeedPenaltyUntil;
        bool hasElevationTrack;

        public VehicleProfile Profile => profile;
        public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public float LateralSpeed { get; private set; }
        public float SteerInput { get; private set; }
        public bool IsDrifting { get; private set; }
        public bool IsBraking { get; private set; }
        public bool HandbrakeHeld => inputProvider != null && inputProvider.DriftHeld;

        public void SetZoneHoverForceMultiplier(float multiplier)
        {
            zoneHoverForceMultiplier = Mathf.Clamp(multiplier, 0.2f, 1.5f);
        }

        public void ApplyGhostSpeedPenalty(float speedMultiplier, float durationSeconds)
        {
            ghostSpeedPenaltyMultiplier = Mathf.Clamp(speedMultiplier, 0.5f, 1f);
            ghostSpeedPenaltyUntil = Time.time + Mathf.Max(durationSeconds, 0.1f);
        }

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
            nitroBoost = GetComponent<VehicleNitroBoost>();
            hoverPodSystem = GetComponent<VehicleHoverPodSystem>();
            slipEffect = GetComponent<VehicleSlipEffect>();
            barrelRoll = GetComponent<VehicleBarrelRoll>();
            fuelSystem = GetComponent<VehicleFuelSystem>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0f, -0.45f, 0.08f);
            ResolveInputProvider();
        }

        void Start()
        {
            ResolveInputProvider();

            if (profile == null)
                Debug.LogError("VehicleController: VehicleProfile is not assigned.", this);

            if (inputProvider == null && GetComponent<AIVehicleController>() == null)
                Debug.LogError("VehicleController: IVehicleInputProvider is not assigned.", this);

            var track = NeonLap.Core.GameManager.Instance != null ? NeonLap.Core.GameManager.Instance.GetCurrentTrackDefinition() : null;
            hasElevationTrack = track != null && TrackLayoutUtility.HasElevation(track.layout);
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

            if (barrelRoll != null && barrelRoll.IsRolling)
                return;

            var probe = groundProbe.Probe();
            SteerInput = inputProvider.Steer;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            LateralSpeed = GetLateralSpeed();
            IsDrifting = EvaluateDrifting(forwardSpeed, LateralSpeed);
            IsBraking = inputProvider.Brake > 0.01f && forwardSpeed > 0.5f;

            ApplyHover(probe);
            ApplyForwardForce();
            ApplySteering(forwardSpeed);
            ApplyDriftAssist(forwardSpeed, probe);
            ApplyGrip(probe);
            ApplyDownforce();
            ClampSpeed();
            AlignToGround(probe);
        }

        float GetLateralSpeed()
        {
            var forward = transform.forward;
            var lateral = rb.linearVelocity - forward * Vector3.Dot(rb.linearVelocity, forward);
            lateral.y = 0f;
            return lateral.magnitude;
        }

        bool EvaluateDrifting(float forwardSpeed, float lateralSpeed)
        {
            if (inputProvider.DriftHeld && forwardSpeed > profile.driftMinSpeed * 0.35f)
                return true;

            if (forwardSpeed < profile.driftMinSpeed)
                return false;

            if (lateralSpeed >= profile.driftSlipThreshold && Mathf.Abs(inputProvider.Steer) > 0.25f)
                return true;

            return inputProvider.DriftHeld && lateralSpeed > profile.driftSlipThreshold * 0.55f;
        }

        void ApplyHover(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var error = profile.hoverHeight - probe.Distance;
            var hoverForce = profile.hoverForce * zoneHoverForceMultiplier;
            var force = Vector3.up * (error * hoverForce - rb.linearVelocity.y * profile.hoverDamping);
            rb.AddForce(force, ForceMode.Acceleration);
        }

        void ApplyForwardForce()
        {
            var forward = transform.forward;
            var speed = Vector3.Dot(rb.linearVelocity, forward);

            var accel = inputProvider.Accelerate;
            if (NeonLap.Core.GameAccessibilitySettings.AutoAccelerate && accel < 0.01f && inputProvider.Brake < 0.01f)
                accel = 1f;

            if (accel > 0.01f && (fuelSystem == null || !fuelSystem.IsEmpty))
            {
                var accelMultiplier = nitroBoost != null ? nitroBoost.ActiveAccelerationMultiplier : 1f;
                accelMultiplier *= hoverPodSystem != null ? hoverPodSystem.SpeedMultiplier : 1f;
                rb.AddForce(forward * (profile.acceleration * accel * accelMultiplier),
                    ForceMode.Acceleration);
            }
            else if (inputProvider.Brake > 0.01f)
            {
                if (speed > 1f)
                {
                    var brake = profile.brakeForce;
                    if (hasElevationTrack && rb != null && rb.linearVelocity.y < -0.65f)
                        brake *= 1.25f; // heavier braking in dips
                    rb.AddForce(-forward * (brake * inputProvider.Brake), ForceMode.Acceleration);
                }
                else
                    rb.AddForce(-forward * (profile.reverseForce * inputProvider.Brake), ForceMode.Acceleration);
            }
        }

        void ApplySteering(float forwardSpeed)
        {
            var speedRatio = Mathf.Clamp01(CurrentSpeed / profile.maxSpeed);
            var turnSpeed = Mathf.Lerp(profile.turnSpeedLow, profile.turnSpeedHigh, speedRatio);

            var assist = NeonLap.Core.GameAccessibilitySettings.SteeringAssist;
            if (assist > 0.001f)
            {
                var highSpeed = Mathf.InverseLerp(0.45f, 1f, speedRatio);
                var clamp = Mathf.Lerp(1f, 0.55f, assist * highSpeed);
                turnSpeed *= clamp;
            }

            if (IsDrifting && forwardSpeed > profile.driftMinSpeed * 0.5f)
            {
                var driftRatio = Mathf.InverseLerp(profile.driftMinSpeed, profile.maxSpeed * 0.85f, CurrentSpeed);
                turnSpeed *= Mathf.Lerp(1f, profile.driftSteerMultiplier, driftRatio);
            }

            var yaw = inputProvider.Steer * turnSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }

        void ApplyDriftAssist(float forwardSpeed, GroundProbeResult probe)
        {
            if (!probe.IsGrounded || forwardSpeed < profile.driftMinSpeed * 0.5f)
                return;

            if (!inputProvider.DriftHeld && !(IsDrifting && Mathf.Abs(inputProvider.Steer) > 0.45f))
                return;

            var forward = transform.forward;
            var right = transform.right;

            if (inputProvider.DriftHeld && Mathf.Abs(inputProvider.Steer) > 0.08f)
            {
                var pushStrength = profile.driftLateralPush *
                                 Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(profile.driftMinSpeed, profile.maxSpeed, CurrentSpeed));
                rb.AddForce(right * (inputProvider.Steer * pushStrength), ForceMode.Acceleration);
            }

            if (inputProvider.DriftHeld && inputProvider.Accelerate > 0.05f)
            {
                rb.AddForce(forward * (profile.driftForwardBoost * inputProvider.Accelerate), ForceMode.Acceleration);
            }
        }

        void ApplyGrip(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            var targetGrip = CalculateTargetGrip(forwardSpeed);
            var recoveryRate = inputProvider.DriftHeld ? profile.driftRecovery * 2.2f : profile.driftRecovery;
            currentGripMultiplier = Mathf.MoveTowards(currentGripMultiplier, targetGrip, recoveryRate * Time.fixedDeltaTime);

            var forward = transform.forward;
            var lateralVelocity = rb.linearVelocity - forward * Vector3.Dot(rb.linearVelocity, forward);
            lateralVelocity.y = 0f;
            var gripScale = currentGripMultiplier * GetWeatherGripMultiplier();
            if (slipEffect != null)
                gripScale *= slipEffect.GripMultiplier;

            var gripForce = -lateralVelocity * profile.grip * gripScale;
            rb.AddForce(gripForce, ForceMode.Acceleration);
        }

        float CalculateTargetGrip(float forwardSpeed)
        {
            if (inputProvider.DriftHeld && forwardSpeed > profile.driftMinSpeed * 0.35f)
                return profile.handbrakeGripMultiplier;

            if (forwardSpeed < profile.driftMinSpeed)
                return 1f;

            var steerAbs = Mathf.Abs(inputProvider.Steer);
            if (steerAbs > 0.55f)
            {
                var powerSlide = Mathf.InverseLerp(0.55f, 1f, steerAbs) *
                                 Mathf.InverseLerp(profile.driftMinSpeed, profile.maxSpeed * 0.75f, CurrentSpeed);
                return Mathf.Lerp(1f, profile.powerSlideGrip, powerSlide);
            }

            if (LateralSpeed >= profile.driftSlipThreshold)
                return Mathf.Min(profile.driftGripMultiplier, profile.powerSlideGrip);

            return 1f;
        }

        void ApplyDownforce()
        {
            var downforce = profile.downforce;
            if (IsDrifting)
                downforce *= 0.55f;

            if (hasElevationTrack)
            {
                var vy = rb != null ? rb.linearVelocity.y : 0f;
                if (vy > 0.65f)
                    downforce *= 0.35f; // lighter over crests / mid-air
                else if (vy < -0.65f)
                    downforce *= 1.35f; // extra bite on descents
            }

            rb.AddForce(-transform.up * downforce, ForceMode.Acceleration);
        }

        void ClampSpeed()
        {
            if (Time.time >= ghostSpeedPenaltyUntil && ghostSpeedPenaltyMultiplier < 0.999f)
                ghostSpeedPenaltyMultiplier = 1f;

            var maxSpeed = profile.maxSpeed * GetWeatherTopSpeedMultiplier() * ghostSpeedPenaltyMultiplier;
            if (nitroBoost != null)
                maxSpeed *= nitroBoost.ActiveSpeedMultiplier;
            if (hoverPodSystem != null)
                maxSpeed *= hoverPodSystem.SpeedMultiplier;

            if (rb.linearVelocity.magnitude <= maxSpeed)
                return;

            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        void AlignToGround(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var alignSpeed = IsDrifting ? 5f : 8f;
            var targetUp = probe.GroundNormal;
            var targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, alignSpeed * Time.fixedDeltaTime));
        }

        static float GetWeatherGripMultiplier()
        {
            var weather = DynamicWeatherSystem.Instance;
            return weather != null ? weather.GripMultiplier : 1f;
        }

        static float GetWeatherTopSpeedMultiplier()
        {
            var weather = DynamicWeatherSystem.Instance;
            return weather != null ? weather.TopSpeedMultiplier : 1f;
        }
    }
}
