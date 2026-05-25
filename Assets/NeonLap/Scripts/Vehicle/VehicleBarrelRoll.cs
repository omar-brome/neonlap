using NeonLap.Input;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VehicleGroundProbe))]
    public class VehicleBarrelRoll : MonoBehaviour
    {
        const float MetersPerSecondToMph = 2.23694f;

        [SerializeField] VehicleProfile profile;
        [SerializeField] float minSpeedMph = 20f;
        [SerializeField] float rollDuration = 0.72f;
        [SerializeField] float cooldown = 1.4f;

        Rigidbody rb;
        VehicleGroundProbe groundProbe;
        PlayerInputReader inputReader;

        Vector3 rollAxis;
        Quaternion startRotation;
        Vector3 preservedVelocity;
        RigidbodyConstraints savedConstraints;
        float rollStartTime;
        float lastRollTime = -999f;

        public bool IsRolling { get; private set; }

        public void Configure(VehicleProfile vehicleProfile)
        {
            profile = vehicleProfile;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundProbe = GetComponent<VehicleGroundProbe>();
            inputReader = GetComponent<PlayerInputReader>();
        }

        void FixedUpdate()
        {
            if (rb.isKinematic)
                return;

            if (IsRolling)
            {
                UpdateRoll();
                return;
            }

            if (inputReader == null || !inputReader.BarrelRollPressed)
                return;

            TryStartRoll();
        }

        void TryStartRoll()
        {
            if (Time.time - lastRollTime < cooldown || profile == null)
                return;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (forwardSpeed < minSpeedMph / MetersPerSecondToMph)
                return;

            var probe = groundProbe.Probe();
            if (!probe.IsGrounded)
                return;

            rollAxis = transform.forward;
            startRotation = transform.rotation;
            preservedVelocity = rb.linearVelocity;
            savedConstraints = rb.constraints;

            rb.constraints = RigidbodyConstraints.None;
            rb.angularVelocity = Vector3.zero;

            rollStartTime = Time.time;
            lastRollTime = Time.time;
            IsRolling = true;
        }

        void UpdateRoll()
        {
            var t = Mathf.Clamp01((Time.time - rollStartTime) / rollDuration);
            var eased = Mathf.SmoothStep(0f, 1f, t);
            var angle = eased * 360f;

            rb.MoveRotation(Quaternion.AngleAxis(angle, rollAxis) * startRotation);

            var horizontalSpeed = Vector3.Dot(preservedVelocity, rollAxis);
            var velocity = rollAxis * horizontalSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            var probe = groundProbe.Probe();
            ApplyHover(probe);
            rb.AddForce(-transform.up * profile.downforce * 0.35f, ForceMode.Acceleration);

            if (t >= 1f)
                EndRoll(probe);
        }

        void EndRoll(GroundProbeResult probe)
        {
            IsRolling = false;
            rb.constraints = savedConstraints;
            rb.angularVelocity = Vector3.zero;

            if (probe.IsGrounded)
            {
                var forward = Vector3.ProjectOnPlane(rollAxis, probe.GroundNormal);
                if (forward.sqrMagnitude > 0.01f)
                    rb.MoveRotation(Quaternion.LookRotation(forward.normalized, probe.GroundNormal));
            }
            else
            {
                rb.MoveRotation(Quaternion.LookRotation(rollAxis, Vector3.up));
            }

            var horizontalSpeed = Vector3.Dot(preservedVelocity, rollAxis);
            rb.linearVelocity = rollAxis * horizontalSpeed + Vector3.up * rb.linearVelocity.y;
        }

        void ApplyHover(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var error = profile.hoverHeight - probe.Distance;
            var force = Vector3.up * (error * profile.hoverForce - rb.linearVelocity.y * profile.hoverDamping);
            rb.AddForce(force, ForceMode.Acceleration);
        }
    }
}
