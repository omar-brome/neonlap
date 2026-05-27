using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.Environment
{
    public static class HazardVisibilityMarker
    {
        const string MarkerChildName = "HazardMarker";

        static Material markerMaterial;
        static Mesh triangleMesh;

        public static void Attach(GameObject obstacle)
        {
            if (obstacle == null || obstacle.transform.Find(MarkerChildName) != null)
                return;

            if (obstacle.GetComponentInChildren<PoliceChaseVehicle>() != null)
                return;

            if (ShouldSkipMarker(obstacle))
                return;

            var collider = obstacle.GetComponent<Collider>();
            if (collider == null)
                return;

            EnsureAssets();

            var markerGo = new GameObject(MarkerChildName);
            markerGo.transform.SetParent(obstacle.transform, false);
            markerGo.transform.localPosition = GetLocalTopPoint(collider) + Vector3.up * 0.08f;
            markerGo.transform.localRotation = Quaternion.identity;
            markerGo.transform.localScale = Vector3.one * GetMarkerScale(collider);

            var filter = markerGo.AddComponent<MeshFilter>();
            filter.sharedMesh = triangleMesh;

            var renderer = markerGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = markerMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static bool ShouldSkipMarker(GameObject obstacle)
        {
            if (obstacle.CompareTag("Barrier"))
                return true;

            if (obstacle.name.StartsWith("Barrier"))
                return true;

            var collider = obstacle.GetComponent<Collider>();
            if (collider == null)
                return false;

            var size = collider.bounds.size;
            var footprint = Mathf.Max(size.x, size.z);
            return footprint > 6f || size.y > 4f;
        }

        static float GetMarkerScale(Collider collider)
        {
            var size = collider.bounds.size;
            var footprint = Mathf.Max(size.x, size.z);
            return Mathf.Clamp(footprint * 0.42f, 0.75f, 2.4f);
        }

        static Vector3 GetLocalTopPoint(Collider collider)
        {
            if (collider is BoxCollider box)
                return box.center + Vector3.up * box.size.y * 0.5f;

            if (collider is CapsuleCollider capsule)
            {
                var halfHeight = capsule.direction switch
                {
                    0 => capsule.radius,
                    1 => capsule.height * 0.5f,
                    _ => capsule.radius
                };
                return capsule.center + Vector3.up * halfHeight;
            }

            if (collider is SphereCollider sphere)
                return sphere.center + Vector3.up * sphere.radius;

            var topWorld = new Vector3(collider.bounds.center.x, collider.bounds.max.y, collider.bounds.center.z);
            return collider.transform.InverseTransformPoint(topWorld);
        }

        static void EnsureAssets()
        {
            if (triangleMesh == null)
                triangleMesh = BuildTriangleMesh();

            if (markerMaterial != null)
                return;

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            markerMaterial = new Material(lit)
            {
                name = "HazardMarkerOrange"
            };
            markerMaterial.SetColor("_BaseColor", new Color(1f, 0.42f, 0.04f));
            markerMaterial.EnableKeyword("_EMISSION");
            markerMaterial.SetColor("_EmissionColor", new Color(1f, 0.48f, 0.06f) * 2.2f);
        }

        static Mesh BuildTriangleMesh()
        {
            const float halfWidth = 0.5f;
            const float height = 1.15f;
            const float depth = 0.35f;

            var mesh = new Mesh { name = "HazardMarkerTriangle" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -depth),
                new Vector3(halfWidth, 0f, -depth),
                new Vector3(0f, 0f, depth),
                new Vector3(0f, height, 0f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1,
                0, 3, 2,
                2, 3, 1,
                1, 3, 0
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
