using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Environment
{
    public static class ObstaclePhysics
    {
        const float TrackSurfaceY = 0.15f;

        public static float TrackSurfaceHeight => TrackSurfaceY;

        public static void ConfigureVehicle(Rigidbody rb)
        {
            if (rb == null)
                return;

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.maxDepenetrationVelocity = 2.5f;
        }

        public static void ConfigureStaticObstacle(GameObject go)
        {
            if (go == null)
                return;

            go.layer = NeonLapLayers.Obstacle;
            if (!go.CompareTag("Barrier"))
                go.tag = "Barrier";

            var collider = go.GetComponent<Collider>();
            if (collider == null)
                collider = go.AddComponent<BoxCollider>();

            collider.isTrigger = false;
            collider.material = GetObstacleMaterial();

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public static void ConfigureTrackBarrier(GameObject go)
        {
            if (go == null)
                return;

            go.layer = NeonLapLayers.Obstacle;
            go.tag = "Barrier";

            var collider = go.GetComponent<Collider>();
            if (collider == null)
                collider = go.AddComponent<BoxCollider>();

            collider.isTrigger = false;
            collider.material = GetObstacleMaterial();

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public static void ConfigureMovingObstacle(Rigidbody rb)
        {
            if (rb == null)
                return;

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public static Vector3 SnapToTrackSurface(Vector3 worldPosition, float halfHeight)
        {
            worldPosition.y = TrackSurfaceY + halfHeight;
            return worldPosition;
        }

        public static void ApplyColliderMaterial(Collider collider)
        {
            if (collider != null)
                collider.material = GetObstacleMaterial();
        }

        public static void ApplyVehicleColliderMaterial(Collider collider)
        {
            if (collider != null)
                collider.material = GetVehicleMaterial();
        }

        public static void ApplyDebrisMaterial(Collider collider)
        {
            if (collider != null)
                collider.material = GetDebrisMaterial();
        }

        public static PhysicsMaterial GetDebrisMaterial() => GetDebrisMaterialInternal();

        static PhysicsMaterial obstacleMaterial;
        static PhysicsMaterial vehicleMaterial;
        static PhysicsMaterial debrisMaterial;

        static PhysicsMaterial GetObstacleMaterial()
        {
            if (obstacleMaterial != null)
                return obstacleMaterial;

            obstacleMaterial = new PhysicsMaterial("NeonLapObstacle")
            {
                dynamicFriction = 0.85f,
                staticFriction = 0.85f,
                bounciness = 0.04f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            return obstacleMaterial;
        }

        static PhysicsMaterial GetVehicleMaterial()
        {
            if (vehicleMaterial != null)
                return vehicleMaterial;

            vehicleMaterial = new PhysicsMaterial("NeonLapVehicle")
            {
                dynamicFriction = 0.22f,
                staticFriction = 0.26f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            return vehicleMaterial;
        }

        static PhysicsMaterial GetDebrisMaterialInternal()
        {
            if (debrisMaterial != null)
                return debrisMaterial;

            debrisMaterial = new PhysicsMaterial("NeonLapDebris")
            {
                dynamicFriction = 0.45f,
                staticFriction = 0.5f,
                bounciness = 0.18f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            return debrisMaterial;
        }
    }
}
