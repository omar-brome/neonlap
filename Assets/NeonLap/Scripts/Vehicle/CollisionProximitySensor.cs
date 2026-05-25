using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class CollisionProximitySensor : MonoBehaviour
    {
        [SerializeField] float warningDistance = 6f;
        [SerializeField] float criticalDistance = 2.4f;
        [SerializeField] float forwardCastDistance = 7f;

        readonly Collider[] overlapHits = new Collider[32];
        readonly RaycastHit[] castHits = new RaycastHit[16];

        Rigidbody rb;
        RaceManager raceManager;
        Vector3 probeCenter;
        Vector3 probeHalfExtents = new(0.88f, 0.38f, 1.55f);

        public float WarningLevel { get; private set; }
        public float ClosestDistance { get; private set; } = float.MaxValue;
        public Vector3 NearestHazardPoint { get; private set; }
        public bool IsWarningActive => WarningLevel > 0.02f;

        public void Configure(RaceManager manager)
        {
            raceManager = manager;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            probeCenter = new Vector3(0f, 0.22f, 0f);
        }

        void FixedUpdate()
        {
            if (raceManager != null && raceManager.State != RaceState.Racing)
            {
                ClearWarning();
                return;
            }

            if (raceManager != null && raceManager.RaceTime < 4f)
            {
                ClearWarning();
                return;
            }

            Scan();
        }

        void Scan()
        {
            var worldCenter = transform.TransformPoint(probeCenter);
            var closest = float.MaxValue;
            var nearestPoint = worldCenter;
            var mask = CollisionHazardUtility.HazardProbeMask;

            var overlapCount = Physics.OverlapBoxNonAlloc(
                worldCenter,
                probeHalfExtents,
                overlapHits,
                transform.rotation,
                mask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < overlapCount; i++)
                TryUpdateClosest(overlapHits[i], worldCenter, ref closest, ref nearestPoint);

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
                forward.Normalize();
            else
                forward = transform.forward;

            var speed = rb.linearVelocity.magnitude;
            var sweepDistance = Mathf.Clamp(forwardCastDistance + speed * 0.12f, forwardCastDistance, warningDistance + 2f);
            var castHalfExtents = new Vector3(probeHalfExtents.x * 0.92f, probeHalfExtents.y * 0.85f, 0.35f);
            var castOrigin = worldCenter + forward * 0.4f;

            var castCount = Physics.BoxCastNonAlloc(
                castOrigin,
                castHalfExtents,
                forward,
                castHits,
                transform.rotation,
                sweepDistance,
                mask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < castCount; i++)
            {
                var hitCollider = castHits[i].collider;
                if (!CollisionHazardUtility.IsHazard(hitCollider, this))
                    continue;

                var distance = castHits[i].distance;
                if (!CollisionHazardUtility.IsProximityThreat(hitCollider, this, distance, rb))
                    continue;

                if (distance >= closest)
                    continue;

                closest = distance;
                nearestPoint = castHits[i].point;
            }

            ScanSideCast(worldCenter, transform.right, mask, ref closest, ref nearestPoint);
            ScanSideCast(worldCenter, -transform.right, mask, ref closest, ref nearestPoint);

            ClosestDistance = closest;
            NearestHazardPoint = nearestPoint;

            if (closest >= warningDistance)
            {
                WarningLevel = 0f;
                return;
            }

            if (closest <= criticalDistance)
            {
                WarningLevel = 1f;
                return;
            }

            WarningLevel = 1f - (closest - criticalDistance) / (warningDistance - criticalDistance);
        }

        void ScanSideCast(Vector3 worldCenter, Vector3 direction, int mask, ref float closest, ref Vector3 nearestPoint)
        {
            var castCount = Physics.BoxCastNonAlloc(
                worldCenter,
                new Vector3(0.25f, probeHalfExtents.y, probeHalfExtents.z * 0.75f),
                direction,
                castHits,
                transform.rotation,
                warningDistance * 0.75f,
                mask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < castCount; i++)
            {
                var hitCollider = castHits[i].collider;
                if (!CollisionHazardUtility.IsHazard(hitCollider, this))
                    continue;

                var distance = castHits[i].distance;
                if (!CollisionHazardUtility.IsProximityThreat(hitCollider, this, distance, rb))
                    continue;

                if (distance >= closest)
                    continue;

                closest = distance;
                nearestPoint = castHits[i].point;
            }
        }

        void TryUpdateClosest(Collider collider, Vector3 worldCenter, ref float closest, ref Vector3 nearestPoint)
        {
            if (!CollisionHazardUtility.IsHazard(collider, this))
                return;

            var point = collider.ClosestPoint(worldCenter);
            var distance = Vector3.Distance(worldCenter, point);
            if (!CollisionHazardUtility.IsProximityThreat(collider, this, distance, rb))
                return;

            if (distance >= closest)
                return;

            closest = distance;
            nearestPoint = point;
        }

        void ClearWarning()
        {
            WarningLevel = 0f;
            ClosestDistance = float.MaxValue;
        }
    }
}
