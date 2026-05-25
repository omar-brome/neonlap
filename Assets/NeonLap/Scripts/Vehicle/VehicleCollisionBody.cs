using NeonLap.Environment;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public static class VehicleCollisionBody
    {
        public static void Build(GameObject car)
        {
            if (car == null)
                return;

            foreach (var existing in car.GetComponents<BoxCollider>())
                Object.Destroy(existing);

            AddBox(car, new Vector3(0f, 0.22f, 0f), new Vector3(1.55f, 0.54f, 2.65f));
            AddBox(car, new Vector3(0f, 0.2f, 1.18f), new Vector3(1.12f, 0.38f, 0.72f));
            AddBox(car, new Vector3(0f, 0.24f, -1.12f), new Vector3(1.42f, 0.42f, 0.75f));
            AddBox(car, new Vector3(0f, 0.1f, 0f), new Vector3(1.72f, 0.2f, 2.78f));

            foreach (var collider in car.GetComponents<BoxCollider>())
                ObstaclePhysics.ApplyVehicleColliderMaterial(collider);
        }

        static void AddBox(GameObject car, Vector3 center, Vector3 size)
        {
            var collider = car.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }
    }
}
