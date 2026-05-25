using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class AIVehicleController : MonoBehaviour
    {
        [SerializeField] VehicleProfile profile;
        [SerializeField] Transform[] waypoints;
        [SerializeField] float waypointReachDistance = 5f;
        [SerializeField] float lookAheadMin = 8f;
        [SerializeField] float lookAheadMax = 24f;
        [SerializeField] float rubberBandStrength = 0.18f;
        [SerializeField] float aiSpeedScale = 0.8f;
        [SerializeField] float trackHalfWidth = 7f;
        [SerializeField] Transform playerTarget;

        Rigidbody rb;
        VehicleGroundProbe groundProbe;
        VehicleHoverPodSystem hoverPodSystem;
        VehicleSlipEffect slipEffect;
        int waypointIndex;
        float targetSpeedMultiplier = 1f;
        float rivalSpeedMultiplier = 1f;
        float rubberBandCatchUpScale = 1f;
        float rubberBandSlowdownScale = 0.35f;
        float steerResponseDivisor = 55f;
        float cornerSpeedMin = 0.28f;
        float cornerAccelMin = 0.25f;
        RaceManager raceManager;

        public bool IsBraking { get; private set; }
        public float SteerInput { get; private set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundProbe = GetComponent<VehicleGroundProbe>();
            hoverPodSystem = GetComponent<VehicleHoverPodSystem>();
            slipEffect = GetComponent<VehicleSlipEffect>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        }

        void FixedUpdate()
        {
            if (profile == null || waypoints == null || waypoints.Length == 0 || rb.isKinematic)
            {
                SteerInput = 0f;
                return;
            }

            if (IsRaceDrivingLocked())
            {
                SteerInput = 0f;
                return;
            }

            UpdateRubberBand();
            DriveTowardWaypoint();
        }

        bool IsRaceDrivingLocked()
        {
            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();

            return raceManager != null && raceManager.State != RaceState.Racing;
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

        public void ConfigureTrack(float halfWidth)
        {
            trackHalfWidth = Mathf.Max(halfWidth, 4f);
        }

        public void SetRivalVariation(int waypointOffset, float speedMultiplier)
        {
            if (waypoints != null && waypoints.Length > 0)
                waypointIndex = waypointOffset % waypoints.Length;
            rivalSpeedMultiplier = speedMultiplier;
        }

        public void ApplyDifficulty(DifficultyPreset preset)
        {
            aiSpeedScale = preset.AiSpeedScale;
            rubberBandStrength = preset.RubberBandStrength;
            rubberBandCatchUpScale = preset.RubberBandCatchUpScale;
            rubberBandSlowdownScale = preset.RubberBandSlowdownScale;
            lookAheadMin = preset.LookAheadMin;
            lookAheadMax = preset.LookAheadMax;
            steerResponseDivisor = Mathf.Max(preset.SteerResponseDivisor, 20f);
            cornerSpeedMin = Mathf.Clamp(preset.CornerSpeedMin, 0.1f, 0.6f);
            cornerAccelMin = Mathf.Clamp(preset.CornerAccelMin, 0.1f, 0.6f);
        }

        public bool IsOffTrack()
        {
            return GetLateralDistanceFromPath() > trackHalfWidth * 0.85f;
        }

        public void RecoverToTrack()
        {
            if (waypoints == null || waypoints.Length == 0 || rb == null)
                return;

            var (closestPoint, segmentIndex) = GetClosestPathSegment();
            waypointIndex = segmentIndex;

            var next = waypoints[(segmentIndex + 1) % waypoints.Length];
            var current = waypoints[segmentIndex];
            var forward = (next.position - current.position).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = transform.forward;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.MovePosition(closestPoint + Vector3.up * 1.5f);
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
            if (dist > 45f)
                targetSpeedMultiplier = 1f + rubberBandStrength * rubberBandCatchUpScale;
            else if (dist < 18f)
                targetSpeedMultiplier = 1f - rubberBandStrength * rubberBandSlowdownScale;
            else
                targetSpeedMultiplier = 1f;
        }

        void DriveTowardWaypoint()
        {
            AdvanceWaypointIfReached();

            var lookAhead = GetLookAheadPoint(out var upcomingTurnAngle);
            var toTarget = lookAhead - transform.position;
            toTarget.y = 0f;

            var probe = groundProbe.Probe();
            ApplyHover(probe);
            ApplyDownforce();

            var headingError = toTarget.sqrMagnitude > 0.01f
                ? Vector3.Angle(transform.forward, toTarget.normalized)
                : 0f;
            var turnAngle = Mathf.Max(upcomingTurnAngle, headingError);
            var cornerSpeedFactor = ComputeCornerSpeedFactor(turnAngle);
            var podMultiplier = hoverPodSystem != null ? hoverPodSystem.SpeedMultiplier : 1f;
            var maxSpeed = profile.maxSpeed * targetSpeedMultiplier * rivalSpeedMultiplier * aiSpeedScale *
                           cornerSpeedFactor * podMultiplier;

            var steer = ComputeSteer(toTarget);
            steer += ComputeTrackCenteringSteer();
            steer = Mathf.Clamp(steer, -1f, 1f);

            if (!probe.IsGrounded)
            {
                ApplySteer(steer * 0.45f);
                return;
            }

            ApplyForward(maxSpeed, turnAngle);
            ApplySteer(steer);
            ApplyGrip(probe);
            AlignToGround(probe);
        }

        void AdvanceWaypointIfReached()
        {
            if (waypoints == null || waypoints.Length == 0)
                return;

            var target = waypoints[waypointIndex];
            if (target == null)
                return;

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > waypointReachDistance)
                return;

            var nextIndex = (waypointIndex + 1) % waypoints.Length;
            var next = waypoints[nextIndex];
            if (next == null)
                return;

            var forward = transform.forward;
            forward.y = 0f;
            var segmentDir = (next.position - target.position).normalized;
            if (Vector3.Dot(forward, segmentDir) > 0.2f)
                waypointIndex = nextIndex;
        }

        Vector3 GetLookAheadPoint(out float maxTurnAngle)
        {
            maxTurnAngle = 0f;
            if (waypoints == null || waypoints.Length == 0)
                return transform.position + transform.forward * lookAheadMin;

            var speedRatio = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(profile.maxSpeed, 1f));
            var remaining = Mathf.Lerp(lookAheadMin, lookAheadMax, speedRatio);
            var index = waypointIndex;
            var startPoint = transform.position;
            startPoint.y = 0f;
            var previousDir = transform.forward;
            previousDir.y = 0f;
            if (previousDir.sqrMagnitude < 0.01f)
                previousDir = Vector3.forward;

            for (var step = 0; step < waypoints.Length && remaining > 0.01f; step++)
            {
                var current = waypoints[index];
                var nextIndex = (index + 1) % waypoints.Length;
                var next = waypoints[nextIndex];
                if (current == null || next == null)
                    break;

                var segmentStart = step == 0 ? startPoint : Flatten(current.position);
                var segmentEnd = Flatten(next.position);
                var segment = segmentEnd - segmentStart;
                var segmentLength = segment.magnitude;
                if (segmentLength < 0.01f)
                {
                    index = nextIndex;
                    continue;
                }

                var segmentDir = segment / segmentLength;
                maxTurnAngle = Mathf.Max(maxTurnAngle, Vector3.Angle(previousDir, segmentDir));
                previousDir = segmentDir;

                if (remaining <= segmentLength)
                    return segmentStart + segmentDir * remaining + Vector3.up * 0.5f;

                remaining -= segmentLength;
                index = nextIndex;
            }

            var fallback = waypoints[index];
            return fallback != null ? fallback.position : transform.position + transform.forward * lookAheadMin;
        }

        float ComputeSteer(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude < 0.01f)
                return 0f;

            var desiredDir = toTarget.normalized;
            var forward = transform.forward;
            forward.y = 0f;
            return Mathf.Clamp(Vector3.SignedAngle(forward, desiredDir, Vector3.up) / steerResponseDivisor, -1f, 1f);
        }

        float ComputeTrackCenteringSteer()
        {
            var lateralDistance = GetLateralDistanceFromPath();
            if (lateralDistance < trackHalfWidth * 0.3f)
                return 0f;

            var (closestPoint, _) = GetClosestPathSegment();
            var toCenter = closestPoint - transform.position;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude < 0.01f)
                return 0f;

            var weight = Mathf.InverseLerp(trackHalfWidth * 0.3f, trackHalfWidth * 0.8f, lateralDistance);
            var centerSteer = Vector3.SignedAngle(transform.forward, toCenter.normalized, Vector3.up) / steerResponseDivisor;
            return Mathf.Clamp(centerSteer, -1f, 1f) * weight;
        }

        float GetLateralDistanceFromPath()
        {
            var (closestPoint, _) = GetClosestPathSegment();
            var delta = transform.position - closestPoint;
            delta.y = 0f;
            return delta.magnitude;
        }

        (Vector3 closestPoint, int segmentIndex) GetClosestPathSegment()
        {
            var position = transform.position;
            var bestDistance = float.MaxValue;
            var bestPoint = position;
            var bestIndex = waypointIndex;

            for (var i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                    continue;

                var start = waypoints[i].position;
                var end = waypoints[(i + 1) % waypoints.Length].position;
                var closest = ClosestPointOnSegment(position, start, end);
                var distance = Vector3.Distance(Flatten(position), Flatten(closest));
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestPoint = closest;
                bestIndex = i;
            }

            return (bestPoint, bestIndex);
        }

        static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            ab.y = 0f;
            var ap = point - a;
            ap.y = 0f;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.0001f)
                return a;

            var t = Mathf.Clamp01(Vector3.Dot(ap, ab) / lengthSq);
            return a + ab * t;
        }

        static Vector3 Flatten(Vector3 point)
        {
            point.y = 0f;
            return point;
        }

        float ComputeCornerSpeedFactor(float turnAngle)
        {
            return Mathf.Lerp(1f, cornerSpeedMin, Mathf.Clamp01(turnAngle / 65f));
        }

        void ApplyHover(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var error = profile.hoverHeight - probe.Distance;
            var force = Vector3.up * (error * profile.hoverForce - rb.linearVelocity.y * profile.hoverDamping);
            rb.AddForce(force, ForceMode.Acceleration);
        }

        void ApplyDownforce()
        {
            rb.AddForce(-transform.up * profile.downforce, ForceMode.Acceleration);
        }

        void ApplyForward(float maxSpeed, float turnAngle)
        {
            var forward = transform.forward;
            var speed = Vector3.Dot(rb.linearVelocity, forward);
            var accelScale = Mathf.Lerp(1f, cornerAccelMin, Mathf.Clamp01(turnAngle / 70f));
            var podMultiplier = hoverPodSystem != null ? hoverPodSystem.SpeedMultiplier : 1f;

            if (speed < maxSpeed - 1.5f)
            {
                IsBraking = false;
                rb.AddForce(forward * (profile.acceleration * accelScale * podMultiplier), ForceMode.Acceleration);
            }
            else if (speed > maxSpeed)
            {
                IsBraking = speed > 1.5f;
                rb.AddForce(-forward * (profile.brakeForce * Mathf.Clamp01((speed - maxSpeed) / 10f)),
                    ForceMode.Acceleration);
            }
            else
            {
                IsBraking = false;
            }

            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        void ApplySteer(float steer)
        {
            SteerInput = steer;
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
            var gripScale = slipEffect != null ? slipEffect.GripMultiplier : 1f;
            rb.AddForce(-lateral * profile.grip * gripScale, ForceMode.Acceleration);
        }

        void AlignToGround(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var targetRotation = Quaternion.FromToRotation(transform.up, probe.GroundNormal) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 8f * Time.fixedDeltaTime));
        }
    }
}
