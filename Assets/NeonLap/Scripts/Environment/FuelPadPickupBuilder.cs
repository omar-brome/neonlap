using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Environment
{
    public class FuelPadPickupBuilder : MonoBehaviour
    {
        Transform pickupRoot;
        Material padMaterial;
        Material stripeMaterial;

        public void Build(IReadOnlyList<Transform> waypoints, TrackDefinition definition)
        {
            if (waypoints == null || waypoints.Count < 12)
                return;

            if (GameRaceModeSettings.Rules.InfiniteFuel)
            {
                if (pickupRoot != null)
                    Destroy(pickupRoot.gameObject);
                return;
            }

            var density = GameQualitySettings.Preset.PickupDensity * 0.55f;
            if (density <= 0.01f)
            {
                if (pickupRoot != null)
                    Destroy(pickupRoot.gameObject);
                return;
            }

            if (pickupRoot != null)
                Destroy(pickupRoot.gameObject);

            pickupRoot = new GameObject("FuelPads").transform;
            pickupRoot.SetParent(transform, false);
            CreateMaterials();

            var trackWidth = definition != null ? definition.trackWidth : 26f;
            var startSkip = Mathf.Max(24, waypoints.Count / 4);
            var padIndices = PickPadIndices(waypoints.Count, startSkip, density, GameLapSettings.CurrentLaps);

            for (var i = 0; i < padIndices.Count; i++)
            {
                var index = padIndices[i];
                var lateral = (i % 2 == 0 ? 1f : -1f) * trackWidth * 0.2f;
                var position = GetTrackPoint(waypoints, index, lateral);
                var rotation = GetTrackRotation(waypoints, index);
                SpawnPad(position, rotation, i);
            }
        }

        void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            padMaterial = new Material(lit);
            padMaterial.SetColor("_BaseColor", new Color(0.12f, 0.55f, 0.22f, 0.9f));
            padMaterial.EnableKeyword("_EMISSION");
            padMaterial.SetColor("_EmissionColor", new Color(0.35f, 2.2f, 0.65f));
            padMaterial.SetFloat("_Smoothness", 0.82f);

            stripeMaterial = new Material(lit);
            stripeMaterial.SetColor("_BaseColor", new Color(0.95f, 0.92f, 0.2f, 0.95f));
            stripeMaterial.EnableKeyword("_EMISSION");
            stripeMaterial.SetColor("_EmissionColor", new Color(2.2f, 2f, 0.35f));
            stripeMaterial.SetFloat("_Smoothness", 0.9f);
        }

        static List<int> PickPadIndices(int waypointCount, int startSkip, float density, int lapCount)
        {
            var indices = new List<int>();
            var lapBonus = Mathf.Clamp(lapCount - 1, 0, 4);
            var divisor = Mathf.Max(5, Mathf.RoundToInt((11f - lapBonus * 1.5f) / Mathf.Max(density, 0.25f)));
            var step = Mathf.Max(10, waypointCount / divisor);
            for (var i = startSkip; i < waypointCount; i += step)
                indices.Add(i);

            return indices;
        }

        void SpawnPad(Vector3 position, Quaternion rotation, int index)
        {
            var pad = new GameObject("FuelPad_" + index);
            pad.transform.SetParent(pickupRoot, false);
            pad.transform.SetPositionAndRotation(
                ObstaclePhysics.SnapToTrackSurface(position, 0.05f),
                rotation);
            pad.layer = NeonLapLayers.Track;

            var trigger = pad.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(4.2f, 1.1f, 4.2f);
            trigger.center = new Vector3(0f, 0.3f, 0f);

            pad.AddComponent<FuelPadPickup>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(pad.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.16f, 0f);

            CreateVisualPrimitive(visual.transform, PrimitiveType.Cylinder, "Pad",
                Vector3.zero, new Vector3(3.4f, 0.07f, 3.4f), padMaterial);
            CreateVisualPrimitive(visual.transform, PrimitiveType.Cube, "StripeA",
                new Vector3(-0.55f, 0.2f, 0f), new Vector3(0.22f, 0.22f, 2.6f), stripeMaterial);
            CreateVisualPrimitive(visual.transform, PrimitiveType.Cube, "StripeB",
                new Vector3(0.55f, 0.2f, 0f), new Vector3(0.22f, 0.22f, 2.6f), stripeMaterial);
        }

        static void CreateVisualPrimitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition,
            Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());

            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;
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
