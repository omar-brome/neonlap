using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleHoverPodSystem : MonoBehaviour
    {
        static readonly string[] PodIds = { "PodFL", "PodFR", "PodRL", "PodRR" };

        [SerializeField] float playerTireDetachChance = 0.07f;
        [SerializeField] float aiTireDetachChance = 0.88f;
        [SerializeField] float aiMinimumChance = 0.62f;
        [SerializeField] float minImpactSpeed = 6.5f;
        [SerializeField] float heavyImpactSpeed = 14f;
        [SerializeField] float detachCooldown = 0.55f;
        [SerializeField] float raceStartGracePeriod = 4f;
        [SerializeField] float speedPenaltyPerPod = 0.17f;
        [SerializeField] float slowdownImpulse = 2.8f;

        readonly Dictionary<string, List<Transform>> podParts = new();
        readonly HashSet<string> attachedPods = new();
        readonly List<GameObject> spawnedDebris = new();

        Rigidbody rb;
        RaceManager raceManager;
        bool isPlayer;
        float lastDetachTime;

        public float SpeedMultiplier
        {
            get
            {
                var missing = PodIds.Length - attachedPods.Count;
                return Mathf.Clamp(1f - missing * speedPenaltyPerPod, 0.25f, 1f);
            }
        }

        public int AttachedPodCount => attachedPods.Count;

        public void Configure(bool playerVehicle)
        {
            isPlayer = playerVehicle;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            raceManager = FindAnyObjectByType<RaceManager>();
            CachePods();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!ShouldProcessCollision(collision.collider))
                return;

            if (raceManager != null)
            {
                if (raceManager.State != RaceState.Racing)
                    return;

                if (raceManager.RaceTime < raceStartGracePeriod)
                    return;
            }

            if (collision.contactCount == 0 || attachedPods.Count == 0)
                return;

            if (Time.time - lastDetachTime < detachCooldown)
                return;

            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minImpactSpeed)
                return;

            if (Random.value > GetDetachChance(impactSpeed))
                return;

            var contact = collision.GetContact(0);
            if (!TryDetachNearestPod(contact.point, contact.normal, impactSpeed))
                return;

            lastDetachTime = Time.time;
        }

        float GetDetachChance(float impactSpeed)
        {
            var baseChance = isPlayer ? playerTireDetachChance : aiTireDetachChance;
            var speedFactor = Mathf.InverseLerp(minImpactSpeed, heavyImpactSpeed, impactSpeed);
            var chance = baseChance * Mathf.Lerp(0.5f, 1f, speedFactor);

            if (!isPlayer && impactSpeed >= minImpactSpeed)
                chance = Mathf.Max(chance, aiMinimumChance);

            return Mathf.Clamp01(chance);
        }

        bool TryDetachNearestPod(Vector3 impactPoint, Vector3 impactNormal, float impactSpeed)
        {
            string nearestPod = null;
            var nearestDistance = float.MaxValue;

            foreach (var podId in attachedPods)
            {
                if (!podParts.TryGetValue(podId, out var parts) || parts.Count == 0)
                    continue;

                var podCenter = GetPodCenter(parts);
                var distance = Vector3.SqrMagnitude(podCenter - impactPoint);
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestPod = podId;
            }

            if (nearestPod == null)
                return false;

            DetachPod(nearestPod, impactPoint, impactNormal, impactSpeed);
            ApplySlowdown(impactNormal, impactSpeed);
            return true;
        }

        static Vector3 GetPodCenter(List<Transform> parts)
        {
            var sum = Vector3.zero;
            var count = 0;
            foreach (var part in parts)
            {
                if (part == null)
                    continue;

                sum += part.position;
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        void DetachPod(string podId, Vector3 impactPoint, Vector3 impactNormal, float impactSpeed)
        {
            if (!attachedPods.Remove(podId))
                return;

            if (!podParts.TryGetValue(podId, out var parts))
                return;

            foreach (var part in parts)
            {
                if (part == null)
                    continue;

                LaunchPodPart(part, impactPoint, impactNormal, impactSpeed);
            }

            podParts[podId].Clear();
        }

        void LaunchPodPart(Transform partTransform, Vector3 impactPoint, Vector3 impactNormal, float impactSpeed)
        {
            var partObject = partTransform.gameObject;
            partObject.transform.SetParent(null, true);

            var detachable = partObject.GetComponent<DetachableVehiclePart>();
            if (detachable != null)
                detachable.MarkDetached();

            Collider collider;
            if (!partObject.TryGetComponent<Collider>(out collider))
            {
                var box = partObject.AddComponent<BoxCollider>();
                box.size = Vector3.one;
                collider = box;
            }

            collider.isTrigger = false;
            collider.material = ObstaclePhysics.GetDebrisMaterial();

            var debrisBody = partObject.GetComponent<Rigidbody>();
            if (debrisBody == null)
                debrisBody = partObject.AddComponent<Rigidbody>();

            debrisBody.mass = detachable != null ? detachable.Mass : 6f;
            debrisBody.linearDamping = 0.12f;
            debrisBody.angularDamping = 0.2f;
            debrisBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            debrisBody.interpolation = RigidbodyInterpolation.Interpolate;

            partObject.layer = NeonLapLayers.Obstacle;
            if (partObject.GetComponent<VehicleDebrisMarker>() == null)
                partObject.AddComponent<VehicleDebrisMarker>();

            var debris = partObject.GetComponent<VehicleDamageDebris>();
            if (debris == null)
                debris = partObject.AddComponent<VehicleDamageDebris>();
            debris.Configure(12f);

            spawnedDebris.Add(partObject);

            var ejectDirection = (partTransform.position - impactPoint).normalized;
            if (ejectDirection.sqrMagnitude < 0.01f)
                ejectDirection = impactNormal;

            var force = Mathf.Lerp(4.5f, 12f, Mathf.InverseLerp(minImpactSpeed, heavyImpactSpeed * 1.3f, impactSpeed));
            debrisBody.AddForce((ejectDirection + impactNormal * 0.75f + Vector3.up * 0.35f).normalized * force,
                ForceMode.Impulse);
            debrisBody.AddTorque(Random.insideUnitSphere * force * 0.85f, ForceMode.Impulse);
            debrisBody.AddForce(rb.linearVelocity * 0.25f, ForceMode.VelocityChange);
        }

        void ApplySlowdown(Vector3 impactNormal, float impactSpeed)
        {
            if (rb == null || rb.isKinematic)
                return;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            var slowdown = Mathf.Lerp(0.72f, 0.55f, Mathf.InverseLerp(minImpactSpeed, heavyImpactSpeed, impactSpeed));
            slowdown *= SpeedMultiplier;
            rb.linearVelocity *= slowdown;

            if (forwardSpeed > 1f)
            {
                var brakeStrength = slowdownImpulse * Mathf.Lerp(0.65f, 1f,
                    Mathf.InverseLerp(minImpactSpeed, heavyImpactSpeed, impactSpeed));
                rb.AddForce(-transform.forward * brakeStrength, ForceMode.VelocityChange);
            }

            rb.AddForce(impactNormal * 0.8f, ForceMode.VelocityChange);
        }

        bool ShouldProcessCollision(Collider other)
        {
            if (other == null)
                return false;

            if (CollisionHazardUtility.IsDebris(other))
                return false;

            if (CollisionHazardUtility.IsHazard(other, this))
                return true;

            return other.gameObject.layer == NeonLapLayers.Vehicle
                   && other.attachedRigidbody != null
                   && other.attachedRigidbody != rb;
        }

        void CachePods()
        {
            podParts.Clear();
            attachedPods.Clear();

            foreach (var podId in PodIds)
            {
                podParts[podId] = new List<Transform>();
                attachedPods.Add(podId);
            }

            var visual = transform.Find("Visual");
            if (visual == null)
                return;

            foreach (var part in visual.GetComponentsInChildren<Transform>(true))
            {
                foreach (var podId in PodIds)
                {
                    if (!part.name.StartsWith(podId + "_") || part.name.EndsWith("_Arm"))
                        continue;

                    podParts[podId].Add(part);
                }
            }
        }

        public void Refresh()
        {
            for (var i = spawnedDebris.Count - 1; i >= 0; i--)
            {
                if (spawnedDebris[i] != null)
                    Destroy(spawnedDebris[i]);
            }

            spawnedDebris.Clear();
            lastDetachTime = 0f;
            CachePods();
        }
    }
}
