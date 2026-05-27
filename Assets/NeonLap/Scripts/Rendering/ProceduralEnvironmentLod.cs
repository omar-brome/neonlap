using UnityEngine;

namespace NeonLap.Rendering
{
    public static class ProceduralEnvironmentLod
    {
        public static void AddBoxLod(GameObject highDetailRoot, Vector3 worldSize, Material lod1Material,
            float lod1RelativeSize = 0.55f)
        {
            if (highDetailRoot == null || lod1Material == null)
                return;

            var lod0Renderers = highDetailRoot.GetComponentsInChildren<Renderer>();

            var lod1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lod1.name = highDetailRoot.name + "_Lod1";
            lod1.transform.SetParent(highDetailRoot.transform, false);
            lod1.transform.localPosition = Vector3.zero;
            lod1.transform.localRotation = Quaternion.identity;
            lod1.transform.localScale = Vector3.one * Mathf.Clamp(lod1RelativeSize, 0.35f, 0.85f);
            Object.Destroy(lod1.GetComponent<Collider>());
            lod1.GetComponent<Renderer>().sharedMaterial = lod1Material;

            var group = highDetailRoot.GetComponent<LODGroup>();
            if (group == null)
                group = highDetailRoot.AddComponent<LODGroup>();

            var maxExtent = Mathf.Max(worldSize.x, worldSize.y, worldSize.z);
            group.SetLODs(new[]
            {
                new LOD(0.55f, lod0Renderers),
                new LOD(0.12f, new[] { lod1.GetComponent<Renderer>() }),
            });
            group.size = maxExtent;
            group.RecalculateBounds();
        }

        public static void AddTreeLod(GameObject treeRoot, float scale, Material trunkMaterial, Material foliageMaterial)
        {
            if (treeRoot == null)
                return;

            var lod0Renderers = treeRoot.GetComponentsInChildren<Renderer>();

            var lod1 = new GameObject(treeRoot.name + "_Lod1");
            lod1.transform.SetParent(treeRoot.transform, false);
            lod1.transform.localPosition = new Vector3(0f, 1.4f * scale, 0f);
            lod1.transform.localRotation = Quaternion.identity;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(lod1.transform, false);
            trunk.transform.localPosition = Vector3.zero;
            trunk.transform.localScale = new Vector3(0.25f * scale, 1.4f * scale, 0.25f * scale);
            Object.Destroy(trunk.GetComponent<Collider>());
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.SetParent(lod1.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 1.6f * scale, 0f);
            canopy.transform.localScale = Vector3.one * 1.6f * scale;
            Object.Destroy(canopy.GetComponent<Collider>());
            canopy.GetComponent<Renderer>().sharedMaterial = foliageMaterial;

            var group = treeRoot.GetComponent<LODGroup>();
            if (group == null)
                group = treeRoot.AddComponent<LODGroup>();

            group.SetLODs(new[]
            {
                new LOD(0.5f, lod0Renderers),
                new LOD(0.1f, lod1.GetComponentsInChildren<Renderer>()),
            });
            group.size = 4f * scale;
            group.RecalculateBounds();
        }
    }
}
