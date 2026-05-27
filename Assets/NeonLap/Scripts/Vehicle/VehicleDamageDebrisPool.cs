using NeonLap.Core;
using NeonLap.Environment;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class VehicleDamageDebrisPool : MonoBehaviour
    {
        const int ShardPrewarmCount = 48;
        const int MaxActiveShards = 96;

        static VehicleDamageDebrisPool instance;

        NeonLapObjectPool shardPool;
        Material sharedShardMaterial;
        int activeShardCount;

        public static VehicleDamageDebrisPool Instance => instance;

        public static void Ensure(Transform parent)
        {
            if (instance != null)
                return;

            var go = new GameObject("VehicleDamageDebrisPool");
            go.transform.SetParent(parent, false);
            go.AddComponent<VehicleDamageDebrisPool>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            shardPool = new NeonLapObjectPool(transform, "ImpactShard");
            shardPool.Prewarm(CreateShardInstance, ShardPrewarmCount);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;

            shardPool?.Clear();
        }

        public GameObject RentShard(Vector3 position, Quaternion rotation, Vector3 scale, Material appearanceMaterial,
            float lifetime)
        {
            if (activeShardCount >= MaxActiveShards)
                return null;

            var shard = shardPool.Rent();
            if (shard == null)
                shard = CreateShardInstance();

            ConfigureShard(shard, position, rotation, scale, appearanceMaterial, lifetime);
            activeShardCount++;
            return shard;
        }

        public void Release(GameObject shard)
        {
            if (shard == null)
                return;

            activeShardCount = Mathf.Max(0, activeShardCount - 1);
            ResetShard(shard);
            shardPool.Release(shard);
        }

        GameObject CreateShardInstance()
        {
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "ImpactShard";
            shard.layer = NeonLapLayers.Obstacle;
            shard.AddComponent<VehicleDebrisMarker>();

            var collider = shard.GetComponent<Collider>();
            ObstaclePhysics.ApplyDebrisMaterial(collider);

            var body = shard.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var debris = shard.AddComponent<VehicleDamageDebris>();
            debris.ConfigureForPool(this);
            return shard;
        }

        void ConfigureShard(GameObject shard, Vector3 position, Quaternion rotation, Vector3 scale,
            Material appearanceMaterial, float lifetime)
        {
            shard.transform.SetParent(null, true);
            shard.transform.SetPositionAndRotation(position, rotation);
            shard.transform.localScale = scale;
            shard.SetActive(true);

            var renderer = shard.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (appearanceMaterial != null)
                    renderer.sharedMaterial = appearanceMaterial;
                else if (sharedShardMaterial == null)
                {
                    sharedShardMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    sharedShardMaterial.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f));
                    sharedShardMaterial.SetFloat("_Metallic", 0.45f);
                    sharedShardMaterial.SetFloat("_Smoothness", 0.35f);
                }

                if (appearanceMaterial == null)
                    renderer.sharedMaterial = sharedShardMaterial;
            }

            var body = shard.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.mass = Random.Range(0.4f, 1.4f);
            }

            var collider = shard.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;

            var debris = shard.GetComponent<VehicleDamageDebris>();
            debris.ActivateFromPool(lifetime);
        }

        static void ResetShard(GameObject shard)
        {
            var renderer = shard.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null && renderer.material.HasProperty("_BaseColor"))
            {
                var color = renderer.material.GetColor("_BaseColor");
                color.a = 1f;
                renderer.material.SetColor("_BaseColor", color);
            }

            var body = shard.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
