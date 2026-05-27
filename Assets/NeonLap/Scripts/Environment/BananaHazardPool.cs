using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Environment
{
    public class BananaHazardPool : MonoBehaviour
    {
        const int PrewarmCount = 24;
        const int MaxActive = 64;

        static BananaHazardPool instance;

        NeonLapObjectPool pool;
        int activeCount;

        public static BananaHazardPool Instance => instance;

        public static void Ensure(Transform parent)
        {
            if (instance != null)
                return;

            var go = new GameObject("BananaHazardPool");
            go.transform.SetParent(parent, false);
            go.AddComponent<BananaHazardPool>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            pool = new NeonLapObjectPool(transform, "Banana");
            pool.Prewarm(() => BananaHazardFactory.BuildPooledInstance(transform), PrewarmCount);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;

            pool?.Clear(go => Destroy(go));
        }

        public GameObject Rent(Vector3 position, Quaternion rotation, Transform parent, string objectName,
            bool respawnAfterSlip, float respawnDelay)
        {
            if (activeCount >= MaxActive)
                return null;

            var banana = pool.Rent();
            if (banana == null)
                banana = BananaHazardFactory.BuildPooledInstance(transform);

            BananaHazardFactory.ActivateFromPool(banana, position, rotation, parent, objectName, respawnAfterSlip,
                respawnDelay);
            activeCount++;
            return banana;
        }

        public void Release(GameObject banana)
        {
            if (banana == null)
                return;

            activeCount = Mathf.Max(0, activeCount - 1);
            BananaHazardFactory.DeactivateToPool(banana);
            pool.Release(banana);
        }
    }
}
