using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Environment
{
    public class BananaHazardBuilder : MonoBehaviour
    {
        Transform hazardRoot;
        Material peelMaterial;
        Material tipMaterial;

        public void Build(IReadOnlyList<Transform> waypoints, TrackDefinition definition)
        {
            if (waypoints == null || waypoints.Count < 12)
                return;

            var density = GameQualitySettings.Preset.BananaDensity;
            if (density <= 0.01f)
            {
                if (hazardRoot != null)
                    Destroy(hazardRoot.gameObject);
                return;
            }

            if (hazardRoot != null)
                Destroy(hazardRoot.gameObject);

            hazardRoot = new GameObject("BananaHazards").transform;
            hazardRoot.SetParent(transform, false);
            CreateMaterials();

            var trackWidth = definition != null ? definition.trackWidth : 26f;
            var startSkip = Mathf.Max(20, waypoints.Count / 5);
            var hazardIndices = PickBananaIndices(waypoints.Count, startSkip, density);

            for (var i = 0; i < hazardIndices.Count; i++)
            {
                var index = hazardIndices[i];
                var lateralPattern = new[] { -0.28f, 0f, 0.28f, -0.14f, 0.14f };
                var lateral = trackWidth * lateralPattern[i % lateralPattern.Length];
                var position = GetTrackPoint(waypoints, index, lateral);
                var rotation = GetTrackRotation(waypoints, index);
                rotation *= Quaternion.Euler(0f, Random.Range(-28f, 28f), 0f);
                SpawnBanana(position, rotation, i);
            }
        }

        void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            peelMaterial = new Material(lit);
            peelMaterial.SetColor("_BaseColor", new Color(0.98f, 0.86f, 0.08f));
            peelMaterial.SetFloat("_Smoothness", 0.62f);
            peelMaterial.SetFloat("_Metallic", 0.02f);

            tipMaterial = new Material(lit);
            tipMaterial.SetColor("_BaseColor", new Color(0.42f, 0.24f, 0.06f));
            tipMaterial.SetFloat("_Smoothness", 0.35f);
        }

        static List<int> PickBananaIndices(int waypointCount, int startSkip, float density)
        {
            var indices = new List<int>();
            var divisor = Mathf.Max(4, Mathf.RoundToInt(7f / Mathf.Max(density, 0.25f)));
            var step = Mathf.Max(8, waypointCount / divisor);
            for (var i = startSkip; i < waypointCount; i += step)
                indices.Add(i);

            return indices;
        }

        void SpawnBanana(Vector3 position, Quaternion rotation, int index)
        {
            var banana = new GameObject("BananaHazard_" + index);
            banana.transform.SetParent(hazardRoot, false);
            banana.transform.SetPositionAndRotation(
                ObstaclePhysics.SnapToTrackSurface(position, 0.55f),
                rotation);
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
        }

        void BuildBananaMesh(Transform parent)
        {
            var body = CreatePart(parent, "BananaBody", PrimitiveType.Capsule,
                new Vector3(0f, 0f, 0f), new Vector3(0.75f, 1.15f, 0.75f),
                Quaternion.Euler(18f, 0f, 92f), peelMaterial);
            body.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            CreatePart(parent, "BananaCurve", PrimitiveType.Capsule,
                new Vector3(0.15f, 0.12f, 0.55f), new Vector3(0.62f, 0.85f, 0.62f),
                Quaternion.Euler(34f, 18f, 96f), peelMaterial);

            CreatePart(parent, "BananaTipStem", PrimitiveType.Capsule,
                new Vector3(-0.05f, 0.08f, -0.95f), new Vector3(0.28f, 0.35f, 0.28f),
                Quaternion.Euler(8f, 0f, 90f), tipMaterial);

            CreatePart(parent, "BananaTipEnd", PrimitiveType.Sphere,
                new Vector3(0.22f, 0.18f, 1.05f), new Vector3(0.42f, 0.42f, 0.42f),
                Quaternion.identity, tipMaterial);

            CreatePart(parent, "BananaHighlight", PrimitiveType.Cube,
                new Vector3(-0.18f, 0.08f, 0.15f), new Vector3(0.08f, 0.55f, 1.4f),
                Quaternion.Euler(12f, 0f, 18f), peelMaterial);
        }

        static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition,
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

            return go;
        }

        static Vector3 GetTrackPoint(IReadOnlyList<Transform> waypoints, int index, float lateral)
        {
            var wp = waypoints[index];
            var forward = GetForward(waypoints, index);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var point = wp.position + right * lateral;
            point.y = ObstaclePhysics.TrackSurfaceHeight;
            return point;
        }

        static Quaternion GetTrackRotation(IReadOnlyList<Transform> waypoints, int index)
        {
            return Quaternion.LookRotation(GetForward(waypoints, index), Vector3.up);
        }

        static Vector3 GetForward(IReadOnlyList<Transform> waypoints, int index)
        {
            var current = waypoints[index].position;
            var next = waypoints[(index + 1) % waypoints.Count].position;
            var forward = next - current;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.01f ? forward.normalized : waypoints[index].forward;
        }
    }
}
