using UnityEngine;

namespace NeonLap.Vehicle
{
    public class VehicleDamageDebris : MonoBehaviour
    {
        [SerializeField] float lifetime = 10f;
        [SerializeField] float fadeDuration = 2.5f;
        [SerializeField] float colliderDisableDelay = 4f;

        Rigidbody rb;
        Collider debrisCollider;
        Renderer[] renderers;
        float spawnTime;
        bool colliderDisabled;
        VehicleDamageDebrisPool ownerPool;
        bool pooled;

        public void Configure(float debrisLifetime = 10f)
        {
            lifetime = debrisLifetime;
            spawnTime = Time.time;
            pooled = false;
        }

        public void ConfigureForPool(VehicleDamageDebrisPool pool)
        {
            ownerPool = pool;
            pooled = true;
        }

        public void ActivateFromPool(float debrisLifetime)
        {
            lifetime = debrisLifetime;
            spawnTime = Time.time;
            colliderDisabled = false;
            pooled = true;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            debrisCollider = GetComponent<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            spawnTime = Time.time;

            if (GetComponent<VehicleDebrisMarker>() == null)
                gameObject.AddComponent<VehicleDebrisMarker>();
        }

        void Update()
        {
            var age = Time.time - spawnTime;

            if (!colliderDisabled && age >= colliderDisableDelay && debrisCollider != null)
            {
                debrisCollider.enabled = false;
                colliderDisabled = true;
            }

            if (age < lifetime - fadeDuration)
                return;

            var fadeT = Mathf.InverseLerp(lifetime - fadeDuration, lifetime, age);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                foreach (var mat in renderer.materials)
                {
                    if (mat == null)
                        continue;

                    if (mat.HasProperty("_BaseColor"))
                    {
                        var color = mat.GetColor("_BaseColor");
                        color.a = Mathf.Lerp(1f, 0f, fadeT);
                        mat.SetColor("_BaseColor", color);
                    }
                }
            }

            if (age >= lifetime)
                Release();
        }

        void Release()
        {
            if (pooled && ownerPool != null)
                ownerPool.Release(gameObject);
            else
                Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (rb == null || collision.relativeVelocity.magnitude < 2f)
                return;

            rb.linearVelocity *= 0.82f;
        }
    }
}
