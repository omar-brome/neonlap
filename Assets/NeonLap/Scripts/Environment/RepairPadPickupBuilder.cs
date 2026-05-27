using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Environment
{
    public class RepairPadPickupBuilder : MonoBehaviour
    {
        Transform pickupRoot;
        Material padMaterial;
        Material crossMaterial;

        public void Build(IReadOnlyList<Transform> waypoints, TrackDefinition definition)
        {
            if (pickupRoot != null)
                Destroy(pickupRoot.gameObject);

            if (!RaceModeDamageRules.GetDamageProfile().SpawnRepairPads)
                return;

            if (waypoints == null || waypoints.Count < 12)
                return;

            var density = GameQualitySettings.Preset.PickupDensity * 0.45f;
            if (density <= 0.01f)
                return;

            pickupRoot = new GameObject("RepairPads").transform;
            pickupRoot.SetParent(transform, false);
            CreateMaterials();

            var trackWidth = definition != null ? definition.trackWidth : 26f;
            var startSkip = Mathf.Max(18, waypoints.Count / 5);
            var indices = PickIndices(waypoints.Count, startSkip, density, GameLapSettings.CurrentLaps);

            for (var i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                var lateral = (i % 2 == 0 ? -1f : 1f) * trackWidth * 0.24f;
                SpawnPad(GetTrackPoint(waypoints, index, lateral), GetTrackRotation(waypoints, index), i);
            }
        }

        void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            padMaterial = new Material(lit);
            padMaterial.SetColor("_BaseColor", new Color(0.12f, 0.28f, 0.62f, 0.92f));
            padMaterial.EnableKeyword("_EMISSION");
            padMaterial.SetColor("_EmissionColor", new Color(0.35f, 1.4f, 2.2f));
            padMaterial.SetFloat("_Smoothness", 0.85f);

            crossMaterial = new Material(lit);
            crossMaterial.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f, 0.95f));
            crossMaterial.EnableKeyword("_EMISSION");
            crossMaterial.SetColor("_EmissionColor", new Color(1.8f, 2.2f, 2.8f));
            crossMaterial.SetFloat("_Smoothness", 0.92f);
        }

        static List<int> PickIndices(int waypointCount, int startSkip, float density, int lapCount)
        {
            var indices = new List<int>();
            var lapBonus = Mathf.Clamp(lapCount - 1, 0, 4);
            var divisor = Mathf.Max(6, Mathf.RoundToInt((14f - lapBonus * 2f) / Mathf.Max(density, 0.25f)));
            var step = Mathf.Max(14, waypointCount / divisor);
            for (var i = startSkip; i < waypointCount; i += step)
                indices.Add(i);

            return indices;
        }

        void SpawnPad(Vector3 position, Quaternion rotation, int index)
        {
            var pad = new GameObject("RepairPad_" + index);
            pad.transform.SetParent(pickupRoot, false);
            pad.transform.SetPositionAndRotation(
                ObstaclePhysics.SnapToTrackSurface(position, 0.05f),
                rotation);
            pad.layer = NeonLapLayers.Track;

            var trigger = pad.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(4.4f, 1.1f, 4.4f);
            trigger.center = new Vector3(0f, 0.3f, 0f);

            pad.AddComponent<RepairPadPickup>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(pad.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.16f, 0f);

            CreatePrimitive(visual.transform, PrimitiveType.Cylinder, "Pad",
                Vector3.zero, new Vector3(3.6f, 0.07f, 3.6f), padMaterial);
            CreatePrimitive(visual.transform, PrimitiveType.Cube, "CrossA",
                Vector3.zero, new Vector3(2.4f, 0.18f, 0.35f), crossMaterial);
            CreatePrimitive(visual.transform, PrimitiveType.Cube, "CrossB",
                Vector3.zero, new Vector3(0.35f, 0.18f, 2.4f), crossMaterial);
        }

        static void CreatePrimitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition,
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
