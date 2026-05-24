using UnityEngine;

namespace NeonLap.Vehicle
{
    public class AIVehicleController : MonoBehaviour
    {
        [SerializeField] VehicleProfile profile;
        [SerializeField] Transform[] waypoints;
        [SerializeField] float waypointReachDistance = 12f;
        [SerializeField] float rubberBandStrength = 0.35f;
        [SerializeField] Transform playerTarget;

        Rigidbody rb;
        VehicleGroundProbe groundProbe;
        int waypointIndex;
        float targetSpeedMultiplier = 1f;
        float rivalSpeedMultiplier = 1f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundProbe = GetComponent<VehicleGroundProbe>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        }

        void FixedUpdate()
        {
            if (profile == null || waypoints == null || waypoints.Length == 0 || rb.isKinematic)
                return;

            UpdateRubberBand();
            DriveTowardWaypoint();
        }

        public void SetWaypoints(Transform[] points)
        {
            waypoints = points;
            waypointIndex = 0;
        }

        public void SetPlayerTarget(Transform player)
        {
            playerTarget = player;
        }

        public void SetRivalVariation(int waypointOffset, float speedMultiplier)
        {
            if (waypoints != null && waypoints.Length > 0)
                waypointIndex = waypointOffset % waypoints.Length;
            rivalSpeedMultiplier = speedMultiplier;
        }

        public void RecoverToTrack()
        {
            if (waypoints == null || waypoints.Length == 0 || rb == null)
                return;

            var bestIndex = 0;
            var bestDist = float.MaxValue;
            for (var i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                    continue;

                var dist = Vector3.Distance(transform.position, waypoints[i].position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            waypointIndex = bestIndex;
            var waypoint = waypoints[bestIndex];
            var next = waypoints[(bestIndex + 1) % waypoints.Length];
            var forward = (next.position - waypoint.position).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = transform.forward;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.MovePosition(waypoint.position + Vector3.up * 1.5f);
            rb.MoveRotation(Quaternion.LookRotation(forward, Vector3.up));
        }

        void UpdateRubberBand()
        {
            if (playerTarget == null)
            {
                targetSpeedMultiplier = 1f;
                return;
            }

            var dist = Vector3.Distance(transform.position, playerTarget.position);
            if (dist > 40f)
                targetSpeedMultiplier = 1f + rubberBandStrength;
            else if (dist < 15f)
                targetSpeedMultiplier = 1f - rubberBandStrength * 0.5f;
            else
                targetSpeedMultiplier = 1f;
        }

        void DriveTowardWaypoint()
        {
            var target = waypoints[waypointIndex];
            if (target == null)
                return;

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude < waypointReachDistance)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                target = waypoints[waypointIndex];
                toTarget = target.position - transform.position;
                toTarget.y = 0f;
            }

            if (toTarget.sqrMagnitude < 0.01f)
                return;

            var desiredDir = toTarget.normalized;
            var forward = transform.forward;
            var steer = Vector3.SignedAngle(forward, desiredDir, Vector3.up) / 60f;
            steer = Mathf.Clamp(steer, -1f, 1f);

            var probe = groundProbe.Probe();
            ApplyHover(probe);

            var airborne = !probe.IsGrounded;
            if (airborne)
            {
                ApplySteer(steer * 0.35f);
                return;
            }

            ApplyForward();
            ApplySteer(steer);
            ApplyGrip(probe);
        }

        void ApplyHover(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var error = profile.hoverHeight - probe.Distance;
            var force = Vector3.up * (error * profile.hoverForce - rb.linearVelocity.y * profile.hoverDamping);
            rb.AddForce(force, ForceMode.Acceleration);
        }

        void ApplyForward()
        {
            var maxSpeed = profile.maxSpeed * targetSpeedMultiplier * rivalSpeedMultiplier;
            rb.AddForce(transform.forward * profile.acceleration, ForceMode.Acceleration);

            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        void ApplySteer(float steer)
        {
            var speedRatio = Mathf.Clamp01(rb.linearVelocity.magnitude / profile.maxSpeed);
            var turnSpeed = Mathf.Lerp(profile.turnSpeedLow, profile.turnSpeedHigh, speedRatio);
            var yaw = steer * turnSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }

        void ApplyGrip(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var forward = transform.forward;
            var lateral = rb.linearVelocity - forward * Vector3.Dot(rb.linearVelocity, forward);
            rb.AddForce(-lateral * profile.grip, ForceMode.Acceleration);
        }
    }
}
