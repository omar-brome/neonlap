using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VehicleGroundProbe))]
    public class PoliceChaseVehicle : MonoBehaviour
    {
        [SerializeField] VehicleProfile profile;
        [SerializeField] float chaseSpeedScale = 1.05f;
        [SerializeField] float maxChaseRange = 140f;
        [SerializeField] float retargetInterval = 1.4f;
        [SerializeField] float ramBoostDistance = 14f;
        [SerializeField] float ramSlowdownForce = 5.5f;
        [SerializeField] float ramMinImpactSpeed = 4f;
        [SerializeField] float ramCooldown = 0.7f;

        Rigidbody rb;
        VehicleGroundProbe groundProbe;
        RaceManager raceManager;
        Transform chaseTarget;
        float nextRetargetTime;
        float lastRamTime;

        public void Configure(VehicleProfile vehicleProfile, RaceManager manager)
        {
            profile = vehicleProfile;
            raceManager = manager;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundProbe = GetComponent<VehicleGroundProbe>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0f, -0.42f, 0f);
        }

        void FixedUpdate()
        {
            if (profile == null || rb.isKinematic)
                return;

            if (IsDrivingLocked())
                return;

            if (Time.time >= nextRetargetTime)
            {
                chaseTarget = FindChaseTarget();
                nextRetargetTime = Time.time + retargetInterval;
            }

            if (chaseTarget == null)
                return;

            var probe = groundProbe.Probe();
            ApplyHover(probe);
            ApplyDownforce();
            DriveTowardTarget(probe);
            ApplyGrip(probe);
        }

        bool IsDrivingLocked()
        {
            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();

            return raceManager != null && raceManager.State != RaceState.Racing;
        }

        Transform FindChaseTarget()
        {
            if (raceManager == null)
                return null;

            Transform nearest = null;
            var nearestDistance = maxChaseRange;
            Transform randomCandidate = null;
            var candidateCount = 0;

            foreach (var racer in raceManager.Racers)
            {
                if (racer == null || racer.IsEliminated || racer.IsFinished)
                    continue;

                if (racer.GetComponent<PoliceChaseVehicle>() != null)
                    continue;

                candidateCount++;
                if (Random.value < 0.35f)
                    randomCandidate = racer.transform;

                var offset = racer.transform.position - transform.position;
                offset.y = 0f;
                var distance = offset.magnitude;
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = racer.transform;
            }

            if (candidateCount == 0)
                return null;

            return Random.value < 0.72f ? nearest : randomCandidate ?? nearest;
        }

        void DriveTowardTarget(GroundProbeResult probe)
        {
            var toTarget = chaseTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.5f)
                return;

            var distance = toTarget.magnitude;
            var desiredForward = toTarget.normalized;
            var steer = Vector3.SignedAngle(transform.forward, desiredForward, Vector3.up);
            var steerRatio = Mathf.Clamp(steer / 45f, -1f, 1f);
            var turnSpeed = Mathf.Lerp(95f, 55f, Mathf.Clamp01(rb.linearVelocity.magnitude / profile.maxSpeed));
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steerRatio * turnSpeed * Time.fixedDeltaTime, 0f));

            var forward = transform.forward;
            var speed = Vector3.Dot(rb.linearVelocity, forward);
            var maxSpeed = profile.maxSpeed * chaseSpeedScale;
            if (distance > 24f)
                maxSpeed *= 1.08f;

            if (speed < maxSpeed)
            {
                var accel = profile.acceleration * 1.15f;
                if (distance < ramBoostDistance)
                    accel *= 1.35f;

                rb.AddForce(forward * accel, ForceMode.Acceleration);
            }

            if (distance < ramBoostDistance && probe.IsGrounded)
            {
                var homing = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
                rb.AddForce(homing * 10f, ForceMode.Acceleration);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Time.time - lastRamTime < ramCooldown)
                return;

            if (collision.contactCount == 0)
                return;

            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < ramMinImpactSpeed)
                return;

            var targetBody = collision.rigidbody;
            if (targetBody == null || targetBody == rb)
                return;

            if (targetBody.GetComponentInParent<PoliceChaseVehicle>() != null)
                return;

            if (!IsValidRamTarget(targetBody))
                return;

            ApplyRamSlowdown(targetBody, collision.GetContact(0).normal, impactSpeed);
            lastRamTime = Time.time;
        }

        static bool IsValidRamTarget(Rigidbody targetBody)
        {
            var root = targetBody.transform.root;
            if (root.GetComponent<RacerProgress>() != null)
                return true;

            return root.CompareTag("Player");
        }

        void ApplyRamSlowdown(Rigidbody targetBody, Vector3 contactNormal, float impactSpeed)
        {
            var forward = targetBody.transform.forward;
            var velocity = targetBody.linearVelocity;
            var forwardSpeed = Vector3.Dot(velocity, forward);
            if (forwardSpeed > 0.5f)
            {
                var reduction = Mathf.Min(forwardSpeed * 0.42f, ramSlowdownForce);
                targetBody.linearVelocity = velocity - forward * reduction;
            }

            var push = contactNormal.normalized;
            if (push.sqrMagnitude < 0.01f)
                push = (targetBody.position - transform.position).normalized;

            var pushStrength = Mathf.Lerp(1.8f, ramSlowdownForce, Mathf.Clamp01(impactSpeed / 16f));
            targetBody.AddForce(push * pushStrength, ForceMode.VelocityChange);
            targetBody.AddForce(Vector3.up * pushStrength * 0.08f, ForceMode.VelocityChange);
        }

        void ApplyHover(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var error = profile.hoverHeight - probe.Distance;
            var force = Vector3.up * (error * profile.hoverForce - rb.linearVelocity.y * profile.hoverDamping);
            rb.AddForce(force, ForceMode.Acceleration);
        }

        void ApplyGrip(GroundProbeResult probe)
        {
            if (!probe.IsGrounded)
                return;

            var forward = transform.forward;
            var lateralVelocity = rb.linearVelocity - forward * Vector3.Dot(rb.linearVelocity, forward);
            lateralVelocity.y = 0f;
            rb.AddForce(-lateralVelocity * profile.grip * 0.85f, ForceMode.Acceleration);
        }

        void ApplyDownforce()
        {
            rb.AddForce(-transform.up * profile.downforce, ForceMode.Acceleration);
        }
    }
}
