using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public static class CollisionHazardUtility
    {
        public static int HazardProbeMask =>
            NeonLapLayers.ObstacleMask | NeonLapLayers.TrackMask | NeonLapLayers.VehicleMask;

        public static bool IsDebris(Collider collider)
        {
            return collider != null && collider.GetComponentInParent<VehicleDebrisMarker>() != null;
        }

        public static bool IsHazard(Collider collider, Component self)
        {
            if (collider == null || self == null)
                return false;

            if (collider.isTrigger)
                return false;

            if (IsDebris(collider))
                return false;

            var selfTransform = self.transform;
            if (collider.transform == selfTransform || collider.transform.IsChildOf(selfTransform))
                return false;

            var selfBody = self.GetComponentInParent<Rigidbody>();
            var otherBody = collider.attachedRigidbody;
            if (selfBody != null && otherBody == selfBody)
                return false;

            if (collider.gameObject.layer == NeonLapLayers.Obstacle)
                return true;

            if (collider.CompareTag("Barrier"))
                return true;

            if (collider.gameObject.layer == NeonLapLayers.Vehicle && otherBody != null && otherBody.gameObject != selfBody?.gameObject)
                return true;

            return false;
        }

        public static bool IsProximityThreat(Collider collider, Component self, float distance, Rigidbody selfBody)
        {
            if (!IsHazard(collider, self))
                return false;

            if (collider.gameObject.layer != NeonLapLayers.Vehicle)
                return true;

            if (distance > 5f)
                return false;

            var otherBody = collider.attachedRigidbody;
            if (selfBody == null || otherBody == null)
                return distance < 2.8f;

            var relativeSpeed = (selfBody.linearVelocity - otherBody.linearVelocity).magnitude;
            return relativeSpeed > 12f || distance < 2.4f;
        }
    }
}
