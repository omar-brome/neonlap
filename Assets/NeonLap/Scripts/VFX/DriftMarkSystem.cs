using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.VFX
{
    public class DriftMarkSystem : MonoBehaviour
    {
        struct ActiveMark
        {
            public GameObject Object;
            public Renderer Renderer;
            public Material Material;
            public float SpawnTime;
            public float Lifetime;
            public float StartAlpha;
        }

        [SerializeField] int initialPoolSize = 220;
        [SerializeField] int maxMarks = 520;
        [SerializeField] float defaultLifetime = 22f;
        [SerializeField] float markSurfaceOffset = 0.012f;

        readonly Queue<ActiveMark> inactiveMarks = new();
        readonly List<ActiveMark> activeMarks = new();

        Transform marksRoot;
        Material markTemplate;
        static DriftMarkSystem instance;

        public static DriftMarkSystem Instance => instance;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            marksRoot = new GameObject("DriftMarks").transform;
            marksRoot.SetParent(transform, false);
            markTemplate = CreateMarkTemplate();
            WarmPool(initialPoolSize);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void Update()
        {
            var time = Time.time;

            for (var i = activeMarks.Count - 1; i >= 0; i--)
            {
                var mark = activeMarks[i];
                var age = time - mark.SpawnTime;
                if (age >= mark.Lifetime)
                {
                    RecycleMark(i);
                    continue;
                }

                var fade = 1f - age / mark.Lifetime;
                var alpha = mark.StartAlpha * fade * fade;
                SetMarkAlpha(mark, alpha);
            }
        }

        public void PlaceMark(Vector3 worldPosition, Vector3 forward, float intensity)
        {
            if (markTemplate == null)
                return;

            if (!TryProjectToTrack(worldPosition, forward, out var point, out var rotation))
                return;

            var mark = RentMark();
            mark.Object.transform.SetPositionAndRotation(point, rotation);
            mark.Object.transform.localScale = new Vector3(
                Random.Range(0.24f, 0.34f) * Mathf.Lerp(0.85f, 1.15f, intensity),
                0.012f,
                Random.Range(0.48f, 0.72f) * Mathf.Lerp(0.9f, 1.2f, intensity));
            mark.SpawnTime = Time.time;
            mark.Lifetime = defaultLifetime * Random.Range(0.85f, 1.1f);
            mark.StartAlpha = Mathf.Lerp(0.28f, 0.72f, intensity);
            SetMarkAlpha(mark, mark.StartAlpha);
            mark.Object.SetActive(true);
            activeMarks.Add(mark);
        }

        bool TryProjectToTrack(Vector3 worldPosition, Vector3 forward, out Vector3 point, out Quaternion rotation)
        {
            point = worldPosition;
            rotation = Quaternion.identity;

            if (!Physics.Raycast(worldPosition + Vector3.up * 2.5f, Vector3.down, out var hit, 6f,
                    NeonLapLayers.TrackMask, QueryTriggerInteraction.Ignore))
                return false;

            var flatForward = Vector3.ProjectOnPlane(forward, hit.normal);
            if (flatForward.sqrMagnitude < 0.01f)
                flatForward = Vector3.ProjectOnPlane(transform.forward, hit.normal);

            flatForward.Normalize();
            rotation = Quaternion.LookRotation(flatForward, hit.normal);
            point = hit.point + hit.normal * markSurfaceOffset;
            return true;
        }

        ActiveMark RentMark()
        {
            if (inactiveMarks.Count > 0)
                return inactiveMarks.Dequeue();

            if (activeMarks.Count >= maxMarks)
                RecycleOldestMark();

            return CreateMarkObject();
        }

        void RecycleOldestMark()
        {
            if (activeMarks.Count == 0)
                return;

            RecycleMark(0);
        }

        void RecycleMark(int index)
        {
            var mark = activeMarks[index];
            activeMarks.RemoveAt(index);
            mark.Object.SetActive(false);
            SetMarkAlpha(mark, 0.65f);
            inactiveMarks.Enqueue(mark);
        }

        void WarmPool(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var mark = CreateMarkObject();
                mark.Object.SetActive(false);
                inactiveMarks.Enqueue(mark);
            }
        }

        ActiveMark CreateMarkObject()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DriftMark";
            go.layer = NeonLapLayers.Track;
            go.transform.SetParent(marksRoot, false);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var material = new Material(markTemplate);
            go.GetComponent<Renderer>().sharedMaterial = material;

            return new ActiveMark
            {
                Object = go,
                Renderer = go.GetComponent<Renderer>(),
                Material = material,
            };
        }

        static void SetMarkAlpha(ActiveMark mark, float alpha)
        {
            if (mark.Material == null)
                return;

            var color = mark.Material.GetColor("_BaseColor");
            color.a = alpha;
            mark.Material.SetColor("_BaseColor", color);
        }

        static Material CreateMarkTemplate()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.055f, 0.65f));
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }
    }
}
