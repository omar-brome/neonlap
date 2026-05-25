using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleDamageSystem : MonoBehaviour
    {
        [SerializeField] float minImpactSpeed = 10f;
        [SerializeField] float heavyImpactSpeed = 16f;
        [SerializeField] float detachCooldown = 0.65f;
        [SerializeField] float raceStartGracePeriod = 4f;
        [SerializeField] int maxDetachedParts = 28;
        [SerializeField] float playerPartDetachMultiplier = 0.35f;
        [SerializeField] float aiPartDetachMultiplier = 1.35f;

        readonly List<DetachableVehiclePart> detachableParts = new();
        readonly List<GameObject> spawnedDebris = new();

        Rigidbody rb;
        RaceManager raceManager;
        VehicleAppearance appearance;
        RacerProgress racerProgress;
        Transform visualRoot;
        float lastDetachTime;
        int detachedCount;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            appearance = GetComponent<VehicleAppearance>();
            racerProgress = GetComponent<RacerProgress>();
            raceManager = FindAnyObjectByType<RaceManager>();
            CacheVisualParts();
        }

        void CacheVisualParts()
        {
            detachableParts.Clear();
            visualRoot = transform.Find("Visual");
            if (visualRoot == null)
                return;

            foreach (var part in visualRoot.GetComponentsInChildren<DetachableVehiclePart>(true))
            {
                if (part.IsAttached)
                    detachableParts.Add(part);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!ShouldApplyDamage(collision.collider))
                return;

            if (raceManager != null)
            {
                if (raceManager.State != RaceState.Racing)
                    return;

                if (raceManager.RaceTime < raceStartGracePeriod)
                    return;
            }

            if (collision.contactCount == 0)
                return;

            if (Time.time - lastDetachTime < detachCooldown)
                return;

            if (detachedCount >= maxDetachedParts)
                return;

            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minImpactSpeed)
                return;

            var contact = collision.GetContact(0);
            ApplyDamage(contact.point, contact.normal, impactSpeed);
            lastDetachTime = Time.time;
        }

        void ApplyDamage(Vector3 impactPoint, Vector3 impactNormal, float impactSpeed)
        {
            var isPlayer = racerProgress != null && racerProgress.IsPlayer;
            var partMultiplier = isPlayer ? playerPartDetachMultiplier : aiPartDetachMultiplier;
            var partCount = impactSpeed >= heavyImpactSpeed
                ? Mathf.RoundToInt(3f * partMultiplier)
                : impactSpeed >= minImpactSpeed * 1.8f
                    ? Mathf.RoundToInt(2f * partMultiplier)
                    : Mathf.RoundToInt(1f * partMultiplier);
            partCount = Mathf.Clamp(partCount, isPlayer ? 0 : 1, 4);

            if (isPlayer && partCount > 0 && Random.value > playerPartDetachMultiplier)
                partCount = 0;

            if (partCount <= 0)
            {
                if (impactSpeed >= minImpactSpeed * 2f)
                    SpawnImpactShards(impactPoint, impactNormal, impactSpeed, isPlayer ? 1 : 2);
                return;
            }

            var detachedThisHit = 0;

            var candidates = new List<DetachableVehiclePart>();
            foreach (var part in detachableParts)
            {
                if (part == null || !part.IsAttached || !part.gameObject.activeInHierarchy)
                    continue;

                var requiredSpeed = minImpactSpeed * part.BreakThreshold;
                if (impactSpeed < requiredSpeed)
                    continue;

                candidates.Add(part);
            }

            candidates.Sort((a, b) =>
            {
                var distA = Vector3.SqrMagnitude(a.transform.position - impactPoint);
                var distB = Vector3.SqrMagnitude(b.transform.position - impactPoint);
                return distA.CompareTo(distB);
            });

            for (var i = 0; i < candidates.Count && detachedThisHit < partCount; i++)
            {
                if (detachedCount >= maxDetachedParts)
                    break;

                DetachPart(candidates[i], impactPoint, impactNormal, impactSpeed);
                detachedThisHit++;
            }

            if (impactSpeed >= heavyImpactSpeed * 0.85f)
                SpawnImpactShards(impactPoint, impactNormal, impactSpeed, detachedThisHit == 0 ? 4 : 2);
            else if (impactSpeed >= minImpactSpeed * 2f)
                SpawnImpactShards(impactPoint, impactNormal, impactSpeed, 2);
        }

        void DetachPart(DetachableVehiclePart part, Vector3 impactPoint, Vector3 impactNormal, float impactSpeed)
        {
            if (part == null || !part.IsAttached)
                return;

            part.MarkDetached();
            detachedCount++;

            var partMass = part.Mass;
            var partTransform = part.transform;
            var partObject = partTransform.gameObject;
            partObject.transform.SetParent(null, true);

            Object.Destroy(part);

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

            debrisBody.mass = partMass;
            debrisBody.linearDamping = 0.15f;
            debrisBody.angularDamping = 0.25f;
            debrisBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            debrisBody.interpolation = RigidbodyInterpolation.Interpolate;

            partObject.layer = NeonLapLayers.Obstacle;
            if (partObject.GetComponent<VehicleDebrisMarker>() == null)
                partObject.AddComponent<VehicleDebrisMarker>();

            var debris = partObject.GetComponent<VehicleDamageDebris>();
            if (debris == null)
                debris = partObject.AddComponent<VehicleDamageDebris>();
            debris.Configure();

            spawnedDebris.Add(partObject);

            var ejectDirection = (partTransform.position - impactPoint).normalized;
            if (ejectDirection.sqrMagnitude < 0.01f)
                ejectDirection = impactNormal;

            var force = Mathf.Lerp(2.8f, 9f, Mathf.InverseLerp(minImpactSpeed, heavyImpactSpeed * 1.4f, impactSpeed));
            debrisBody.AddForce((ejectDirection + impactNormal * 0.65f) * force, ForceMode.Impulse);
            debrisBody.AddForce(Vector3.up * force * 0.22f, ForceMode.Impulse);
            debrisBody.AddTorque(Random.insideUnitSphere * force * 0.55f, ForceMode.Impulse);
            debrisBody.AddForce(rb.linearVelocity * 0.35f, ForceMode.VelocityChange);
        }

        void SpawnImpactShards(Vector3 impactPoint, Vector3 impactNormal, float impactSpeed, int shardCount)
        {
            if (appearance == null || !appearance.HasBuildArgs)
                return;

            var args = appearance.GetBuildArgs();
            var shardMat = new Material(args.BodyTemplate);
            shardMat.SetColor("_BaseColor", args.BodyColor * 0.85f);
            shardMat.SetFloat("_Metallic", 0.45f);
            shardMat.SetFloat("_Smoothness", 0.35f);

            for (var i = 0; i < shardCount; i++)
            {
                if (detachedCount >= maxDetachedParts)
                    break;

                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "ImpactShard";
                shard.transform.position = impactPoint + Random.insideUnitSphere * 0.18f;
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
                shard.GetComponent<Renderer>().material = shardMat;
                shard.layer = NeonLapLayers.Obstacle;
                shard.AddComponent<VehicleDebrisMarker>();

                var shardBody = shard.AddComponent<Rigidbody>();
                shardBody.mass = Random.Range(0.4f, 1.4f);
                shardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                ObstaclePhysics.ApplyDebrisMaterial(shard.GetComponent<Collider>());

                var debris = shard.AddComponent<VehicleDamageDebris>();
                debris.Configure(8f);

                spawnedDebris.Add(shard);
                detachedCount++;

                var burstDir = (Random.onUnitSphere + impactNormal).normalized;
                var force = Random.Range(2f, 5f) * (impactSpeed / heavyImpactSpeed);
                shardBody.AddForce(burstDir * force, ForceMode.Impulse);
                shardBody.AddTorque(Random.insideUnitSphere * force, ForceMode.Impulse);
            }
        }

        bool ShouldApplyDamage(Collider other)
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

        public void RestoreVisuals()
        {
            for (var i = spawnedDebris.Count - 1; i >= 0; i--)
            {
                if (spawnedDebris[i] != null)
                    Destroy(spawnedDebris[i]);
            }

            spawnedDebris.Clear();
            detachedCount = 0;
            lastDetachTime = 0f;

            if (visualRoot != null)
                Destroy(visualRoot.gameObject);

            if (appearance != null && appearance.HasBuildArgs)
                visualRoot = HoverCarVisualBuilder.Build(transform, appearance.GetBuildArgs());

            GetComponent<VehicleTaillightController>()?.Refresh();
            GetComponent<VehicleTurnSignalController>()?.Refresh();
            GetComponent<VehicleHoverPodSystem>()?.Refresh();
            CacheVisualParts();
        }
    }
}
