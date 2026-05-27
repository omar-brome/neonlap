using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Environment
{
    public static class BananaHazardFactory
    {
        static Material peelMaterial;
        static Material tipMaterial;

        public static GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null,
            string objectName = "DroppedBanana", bool respawnAfterSlip = false, float respawnDelay = 12f)
        {
            var pool = BananaHazardPool.Instance;
            if (pool != null)
            {
                var rented = pool.Rent(position, rotation, parent, objectName, respawnAfterSlip, respawnDelay);
                if (rented != null)
                    return rented;
            }

            var banana = BuildPooledInstance(parent);
            ActivateFromPool(banana, position, rotation, parent, objectName, respawnAfterSlip, respawnDelay);
            return banana;
        }

        public static GameObject BuildPooledInstance(Transform poolRoot)
        {
            EnsureMaterials();

            var banana = new GameObject("BananaPooled");
            if (poolRoot != null)
                banana.transform.SetParent(poolRoot, false);

            banana.layer = NeonLapLayers.Track;

            var trigger = banana.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.8f, 1.4f, 4.2f);
            trigger.center = new Vector3(0f, 0.45f, 0f);

            banana.AddComponent<BananaSlipHazard>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(banana.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            BuildBananaMesh(visual.transform);

            return banana;
        }

        public static void ActivateFromPool(GameObject banana, Vector3 position, Quaternion rotation, Transform parent,
            string objectName, bool respawnAfterSlip, float respawnDelay)
        {
            if (banana == null)
                return;

            banana.name = objectName;
            if (parent != null)
                banana.transform.SetParent(parent, false);

            banana.transform.SetPositionAndRotation(
                ObstaclePhysics.SnapToTrackSurface(position, 0.55f),
                rotation);
            banana.SetActive(true);

            var trigger = banana.GetComponent<Collider>();
            if (trigger != null)
                trigger.enabled = true;

            var slip = banana.GetComponent<BananaSlipHazard>();
            if (slip != null)
                slip.ResetForSpawn(respawnAfterSlip, respawnDelay);
        }

        public static void DeactivateToPool(GameObject banana)
        {
            if (banana == null)
                return;

            var slip = banana.GetComponent<BananaSlipHazard>();
            slip?.CancelRespawn();

            banana.SetActive(false);
        }

        static void EnsureMaterials()
        {
            if (peelMaterial != null)
                return;

            var lit = Shader.Find("Universal Render Pipeline/Lit");

            peelMaterial = new Material(lit);
            peelMaterial.SetColor("_BaseColor", new Color(0.98f, 0.86f, 0.08f));
            peelMaterial.SetFloat("_Smoothness", 0.62f);
            peelMaterial.SetFloat("_Metallic", 0.02f);

            tipMaterial = new Material(lit);
            tipMaterial.SetColor("_BaseColor", new Color(0.42f, 0.24f, 0.06f));
            tipMaterial.SetFloat("_Smoothness", 0.35f);
        }

        static void BuildBananaMesh(Transform parent)
        {
            CreatePart(parent, "BananaBody", PrimitiveType.Capsule,
                new Vector3(0f, 0.05f, 0f), new Vector3(0.75f, 1.15f, 0.75f),
                Quaternion.Euler(18f, 0f, 92f), peelMaterial);

            CreatePart(parent, "BananaCurve", PrimitiveType.Capsule,
                new Vector3(0.15f, 0.12f, 0.55f), new Vector3(0.62f, 0.85f, 0.62f),
                Quaternion.Euler(34f, 18f, 96f), peelMaterial);

            CreatePart(parent, "BananaTipStem", PrimitiveType.Capsule,
                new Vector3(-0.05f, 0.08f, -0.95f), new Vector3(0.28f, 0.35f, 0.28f),
                Quaternion.Euler(8f, 0f, 90f), tipMaterial);

            CreatePart(parent, "BananaTipEnd", PrimitiveType.Sphere,
                new Vector3(0.22f, 0.18f, 1.05f), new Vector3(0.42f, 0.42f, 0.42f),
                Quaternion.identity, tipMaterial);
        }

        static void CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition,
            Vector3 localScale, Quaternion localRotation, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = localRotation;
            Object.Destroy(go.GetComponent<Collider>());

            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
