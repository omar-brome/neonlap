using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Core
{
    public class NeonLapObjectPool
    {
        readonly Stack<GameObject> inactive = new();
        readonly Transform root;
        readonly string poolName;

        public int InactiveCount => inactive.Count;

        public NeonLapObjectPool(Transform parent, string name)
        {
            poolName = name;
            var rootGo = new GameObject(name + "_Pool");
            rootGo.transform.SetParent(parent, false);
            root = rootGo.transform;
        }

        public GameObject Rent()
        {
            while (inactive.Count > 0)
            {
                var candidate = inactive.Pop();
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
                return;

            instance.SetActive(false);
            instance.transform.SetParent(root, false);
            inactive.Push(instance);
        }

        public void Prewarm(System.Func<GameObject> factory, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var instance = factory();
                if (instance == null)
                    continue;

                instance.name = poolName + "_" + i;
                Release(instance);
            }
        }

        public void Clear(System.Action<GameObject> onDestroy = null)
        {
            while (inactive.Count > 0)
            {
                var instance = inactive.Pop();
                if (instance == null)
                    continue;

                if (onDestroy != null)
                    onDestroy(instance);
                else
                    Object.Destroy(instance);
            }
        }
    }
}
