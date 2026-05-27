using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using NeonLap.VFX;
using NeonLap.Track;
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
        float progressRubberBandStrength;
        float progressAheadSlowdownScale;
        float progressBehindCatchUpScale;
        float steerResponseDivisor = 55f;
        float cornerSpeedMin = 0.28f;
        float cornerAccelMin = 0.25f;
        bool seeksNitroPickups;
        float centeringWeightMult = 1f;
        float passingSteerBias;
        float lastUpcomingTurnAngle;
        float playerBlockEndTime;
        float zoneHoverForceMultiplier = 1f;
        float zoneTrackHalfWidthMultiplier = 1f;
        RaceManager raceManager;
        RacerProgress racerProgress;
        VehicleNitroBoost nitroBoost;
        bool hasElevationTrack;

        public bool IsBraking { get; private set; }
        public float SteerInput { get; private set; }
        public float EstimatedUpcomingTurnAngle => lastUpcomingTurnAngle;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundProbe = GetComponent<VehicleGroundProbe>();
            hoverPodSystem = GetComponent<VehicleHoverPodSystem>();
            slipEffect = GetComponent<VehicleSlipEffect>();
            racerProgress = GetComponent<RacerProgress>();
            nitroBoost = GetComponent<VehicleNitroBoost>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        }

        void Start()
        {
            var track = GameManager.Instance != null ? GameManager.Instance.GetCurrentTrackDefinition() : null;
            hasElevationTrack = track != null && TrackLayoutUtility.HasElevation(track.layout);
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

        public void ActivatePlayerBlock(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return;

            playerBlockEndTime = Mathf.Max(playerBlockEndTime, Time.time + durationSeconds);
        }

        public bool IsBlockingPlayer => playerTarget != null && Time.time < playerBlockEndTime;

        public void ConfigureTrack(float halfWidth)
        {
            trackHalfWidth = Mathf.Max(halfWidth, 4f);
        }

        public void SetZoneHoverForceMultiplier(float multiplier)
        {
            zoneHoverForceMultiplier = Mathf.Clamp(multiplier, 0.2f, 1.5f);
        }

        public void SetZoneTrackHalfWidthMultiplier(float multiplier)
        {
            zoneTrackHalfWidthMultiplier = Mathf.Clamp(multiplier, 0.35f, 1f);
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
            progressRubberBandStrength = preset.ProgressRubberBandStrength;
            progressAheadSlowdownScale = preset.ProgressAheadSlowdownScale;
            progressBehindCatchUpScale = preset.ProgressBehindCatchUpScale;
            lookAheadMin = preset.LookAheadMin;
            lookAheadMax = preset.LookAheadMax;
            steerResponseDivisor = Mathf.Max(preset.SteerResponseDivisor, 20f);
            cornerSpeedMin = Mathf.Clamp(preset.CornerSpeedMin, 0.1f, 0.6f);
            cornerAccelMin = Mathf.Clamp(preset.CornerAccelMin, 0.1f, 0.6f);
            seeksNitroPickups = preset.AiSeeksNitroPickups;
        }

        public void ApplyPersonality(AIPersonalityProfile profile)
        {
            lookAheadMin *= profile.LookAheadMultiplier;
            lookAheadMax *= profile.LookAheadMultiplier;
            cornerSpeedMin = Mathf.Clamp(cornerSpeedMin * profile.CornerSpeedMultiplier, 0.1f, 0.65f);
            cornerAccelMin = Mathf.Clamp(cornerAccelMin * profile.CornerAccelMultiplier, 0.1f, 0.65f);
            steerResponseDivisor = Mathf.Max(steerResponseDivisor / Mathf.Max(profile.SteerAggression, 0.5f), 20f);
            centeringWeightMult = profile.CenteringWeight;
            passingSteerBias = profile.PassingSteerBias;
        }

        public bool IsOffTrack()
        {
            return GetLateralDistanceFromPath() > GetEffectiveTrackHalfWidth() * 0.85f;
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
            var distanceMultiplier = ComputeDistanceRubberBand();
            var progressMultiplier = ComputeProgressRubberBand();
            targetSpeedMultiplier = distanceMultiplier * progressMultiplier;
        }

        float ComputeDistanceRubberBand()
        {
            if (playerTarget == null)
                return 1f;

            var dist = Vector3.Distance(transform.position, playerTarget.position);
            if (dist > 45f)
                return 1f + rubberBandStrength * rubberBandCatchUpScale;
            if (dist < 18f)
                return 1f - rubberBandStrength * rubberBandSlowdownScale;
            return 1f;
        }

        float ComputeProgressRubberBand()
        {
            if (progressRubberBandStrength <= 0.001f)
                return 1f;

            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();

            if (raceManager == null || racerProgress == null)
                return 1f;

            var playerRacer = raceManager.PlayerRacer;
            if (playerRacer == null)
                return 1f;

            var aiProgress = raceManager.GetRaceProgress(racerProgress);
            var playerProgress = raceManager.GetRaceProgress(playerRacer);
            var delta = aiProgress - playerProgress;
            const float progressSpan = 0.12f;

            if (delta > 0.02f)
            {
                var aheadAmount = Mathf.Clamp01(delta / progressSpan);
                return 1f - progressRubberBandStrength * progressAheadSlowdownScale * aheadAmount;
            }

            if (delta < -0.02f)
            {
                var behindAmount = Mathf.Clamp01(-delta / progressSpan);
                var catchUpScale = progressBehindCatchUpScale;
                var ghost = CatchUpGhostController.Instance;
                if (ghost != null && ghost.IsActive)
                    catchUpScale *= ghost.RubberBandDampScaleWhenActive;

                return 1f + progressRubberBandStrength * catchUpScale * behindAmount;
            }

            return 1f;
        }

        void DriveTowardWaypoint()
        {
            AdvanceWaypointIfReached();

            var lookAhead = GetLookAheadPoint(out var upcomingTurnAngle);
            lastUpcomingTurnAngle = upcomingTurnAngle;
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
            var nitroMultiplier = nitroBoost != null ? nitroBoost.ActiveSpeedMultiplier : 1f;
            var maxSpeed = profile.maxSpeed * targetSpeedMultiplier * rivalSpeedMultiplier * aiSpeedScale *
                           cornerSpeedFactor * podMultiplier * nitroMultiplier * GetWeatherTopSpeedMultiplier();
            if (hasElevationTrack && rb != null && rb.linearVelocity.y > 0.65f)
                maxSpeed *= 0.88f; // slower on climbs

            var steer = ComputeSteer(toTarget);
            steer += ComputeTrackCenteringSteer();
            steer += ComputePassingSteer();
            steer += ComputeBlockingSteer();
            steer += ComputeNitroSeekSteer();
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

        float GetEffectiveTrackHalfWidth()
        {
            var width = trackHalfWidth * zoneTrackHalfWidthMultiplier;
            return IsBlockingPlayer ? width * 1.45f : width;
        }

        float ComputeNitroSeekSteer()
        {
            if (!seeksNitroPickups || lastUpcomingTurnAngle > 24f)
                return 0f;

            if (!NitroPickupRegistry.TryGetNearestAvailable(transform.position, 40f, out var pickupPosition))
                return 0f;

            var toPickup = pickupPosition - transform.position;
            toPickup.y = 0f;
            if (toPickup.sqrMagnitude < 9f || toPickup.sqrMagnitude > 1600f)
                return 0f;

            var forward = transform.forward;
            forward.y = 0f;
            return Mathf.Clamp(Vector3.SignedAngle(forward, toPickup.normalized, Vector3.up) / steerResponseDivisor,
                -0.42f, 0.42f);
        }

        float ComputeBlockingSteer()
        {
            if (!IsBlockingPlayer || playerTarget == null)
                return 0f;

            var toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 625f || toPlayer.sqrMagnitude < 25f)
                return 0f;

            var lateral = Vector3.Dot(transform.right, toPlayer.normalized);
            return Mathf.Clamp(lateral * 3.2f, -0.9f, 0.9f);
        }

        float ComputeTrackCenteringSteer()
        {
            var halfWidth = GetEffectiveTrackHalfWidth();
            var lateralDistance = GetLateralDistanceFromPath();
            if (lateralDistance < halfWidth * 0.3f)
                return 0f;

            var (closestPoint, _) = GetClosestPathSegment();
            var toCenter = closestPoint - transform.position;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude < 0.01f)
                return 0f;

            var weight = Mathf.InverseLerp(halfWidth * 0.3f, halfWidth * 0.8f, lateralDistance);
            var centerSteer = Vector3.SignedAngle(transform.forward, toCenter.normalized, Vector3.up) / steerResponseDivisor;
            return Mathf.Clamp(centerSteer, -1f, 1f) * weight * centeringWeightMult;
        }

        float ComputePassingSteer()
        {
            if (Mathf.Abs(passingSteerBias) < 0.001f || playerTarget == null)
                return 0f;

            var toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 576f || toPlayer.sqrMagnitude < 36f)
                return 0f;

            var lateral = Vector3.Dot(transform.right, toPlayer.normalized);
            return Mathf.Clamp(lateral * passingSteerBias * 2.5f, -0.35f, 0.35f);
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
            var hoverForce = profile.hoverForce * zoneHoverForceMultiplier;
            var force = Vector3.up * (error * hoverForce - rb.linearVelocity.y * profile.hoverDamping);
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
            var nitroAccel = nitroBoost != null ? nitroBoost.ActiveAccelerationMultiplier : 1f;

            if (speed < maxSpeed - 1.5f)
            {
                IsBraking = false;
                rb.AddForce(forward * (profile.acceleration * accelScale * podMultiplier * nitroAccel),
                    ForceMode.Acceleration);
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
            var gripScale = (slipEffect != null ? slipEffect.GripMultiplier : 1f) * GetWeatherGripMultiplier();
            rb.AddForce(-lateral * profile.grip * gripScale, ForceMode.Acceleration);
        }

        void AlignToGround(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var targetRotation = Quaternion.FromToRotation(transform.up, probe.GroundNormal) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 8f * Time.fixedDeltaTime));
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
