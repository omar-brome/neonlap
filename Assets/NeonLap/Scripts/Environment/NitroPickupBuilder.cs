using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Environment
{
    public class NitroPickupBuilder : MonoBehaviour
    {
        Transform pickupRoot;
        Transform aiZoneRoot;
        Material padMaterial;
        Material ringMaterial;
        Material aiZoneMaterial;

        public void Build(IReadOnlyList<Transform> waypoints, TrackDefinition definition)
        {
            if (waypoints == null || waypoints.Count < 12)
                return;

            var levelIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var density = TrackLevelConfig.GetPickupDensity(levelIndex, GameQualitySettings.Preset.PickupDensity);
            if (density <= 0.01f)
            {
                if (pickupRoot != null)
                    Destroy(pickupRoot.gameObject);
                return;
            }

            if (pickupRoot != null)
                Destroy(pickupRoot.gameObject);

            pickupRoot = new GameObject("NitroPickups").transform;
            pickupRoot.SetParent(transform, false);
            CreateMaterials();

            var trackWidth = definition != null ? definition.trackWidth : 26f;
            var startSkip = Mathf.Max(16, waypoints.Count / 6);
            var pickupIndices = levelIndex == 1
                ? PickStraightClusterPickupIndices(waypoints, startSkip, density)
                : PickPickupIndices(waypoints.Count, startSkip, density);

            for (var i = 0; i < pickupIndices.Count; i++)
            {
                var index = pickupIndices[i];
                var lateral = (i % 2 == 0 ? -1f : 1f) * trackWidth * 0.12f;
                var position = GetTrackPoint(waypoints, index, lateral);
                var rotation = GetTrackRotation(waypoints, index);
                SpawnPickup(position, rotation, i);
            }
        }

        public void BuildAiNitroZones(IReadOnlyList<Transform> waypoints, TrackDefinition definition)
        {
            if (waypoints == null || waypoints.Count < 16)
                return;

            if (GameDifficultySettings.Current != DifficultyLevel.Hard)
            {
                if (aiZoneRoot != null)
                    Destroy(aiZoneRoot.gameObject);
                return;
            }

            if (aiZoneRoot != null)
                Destroy(aiZoneRoot.gameObject);

            aiZoneRoot = new GameObject("AiNitroZones").transform;
            aiZoneRoot.SetParent(transform, false);
            CreateMaterials();

            var trackWidth = definition != null ? definition.trackWidth : 26f;
            var count = waypoints.Count;
            var zoneIndices = new[]
            {
                Mathf.Max(20, count / 3),
                Mathf.Max(24, count * 2 / 3),
            };

            for (var i = 0; i < zoneIndices.Length; i++)
            {
                var index = zoneIndices[i] % count;
                var lateral = (i % 2 == 0 ? -1f : 1f) * trackWidth * 0.08f;
                var position = GetTrackPoint(waypoints, index, lateral);
                var rotation = GetTrackRotation(waypoints, index);
                SpawnAiNitroZone(position, rotation, i);
            }
        }

        void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            padMaterial = new Material(lit);
            padMaterial.SetColor("_BaseColor", new Color(0.08f, 0.35f, 0.42f, 0.85f));
            padMaterial.EnableKeyword("_EMISSION");
            padMaterial.SetColor("_EmissionColor", new Color(0.2f, 2.4f, 2.8f));
            padMaterial.SetFloat("_Smoothness", 0.85f);

            ringMaterial = new Material(lit);
            ringMaterial.SetColor("_BaseColor", new Color(0.95f, 0.35f, 1f, 0.9f));
            ringMaterial.EnableKeyword("_EMISSION");
            ringMaterial.SetColor("_EmissionColor", new Color(2.5f, 0.6f, 3f));
            ringMaterial.SetFloat("_Smoothness", 0.95f);

            aiZoneMaterial = new Material(lit);
            aiZoneMaterial.SetColor("_BaseColor", new Color(0.35f, 0.12f, 0.05f, 0.82f));
            aiZoneMaterial.EnableKeyword("_EMISSION");
            aiZoneMaterial.SetColor("_EmissionColor", new Color(2.8f, 0.7f, 0.15f));
            aiZoneMaterial.SetFloat("_Smoothness", 0.8f);
        }

        void SpawnAiNitroZone(Vector3 position, Quaternion rotation, int index)
        {
            var zone = new GameObject("AiNitroZone_" + index);
            zone.transform.SetParent(aiZoneRoot, false);
            zone.transform.SetPositionAndRotation(
                ObstaclePhysics.SnapToTrackSurface(position, 0.04f),
                rotation);
            zone.layer = NeonLapLayers.Track;

            var trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(5.2f, 1.4f, 8.5f);
            trigger.center = new Vector3(0f, 0.35f, 0f);

            zone.AddComponent<NitroZone>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(zone.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            CreateVisualPrimitive(visual.transform, PrimitiveType.Cylinder, "ZonePad",
                Vector3.zero, new Vector3(4.6f, 0.06f, 4.6f), aiZoneMaterial);
            CreateVisualPrimitive(visual.transform, PrimitiveType.Cube, "ZoneChevron",
                new Vector3(0f, 0.2f, 0f), new Vector3(0.25f, 0.25f, 1.2f), aiZoneMaterial);
        }

        static List<int> PickPickupIndices(int waypointCount, int startSkip, float density)
        {
            var indices = new List<int>();
            var divisor = Mathf.Max(4, Mathf.RoundToInt(9f / Mathf.Max(density, 0.25f)));
            var step = Mathf.Max(6, waypointCount / divisor);
            for (var i = startSkip; i < waypointCount; i += step)
                indices.Add(i);

            return indices;
        }

        static List<int> PickStraightClusterPickupIndices(IReadOnlyList<Transform> waypoints, int startSkip, float density)
        {
            var indices = new List<int>();
            var count = waypoints.Count;
            if (count < 16)
                return PickPickupIndices(count, startSkip, density);

            var straightCandidates = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var prev = GetForward(waypoints, (i - 1 + count) % count);
                var next = GetForward(waypoints, i);
                var angle = Vector3.Angle(prev, next);
                if (angle < 6.5f)
                    straightCandidates.Add(i);
            }

            if (straightCandidates.Count < 8)
                return PickPickupIndices(count, startSkip, density);

            var divisor = Mathf.Max(3, Mathf.RoundToInt(7f / Mathf.Max(density, 0.25f)));
            var step = Mathf.Max(3, straightCandidates.Count / divisor);
            for (var i = 0; i < straightCandidates.Count; i += step)
            {
                var index = straightCandidates[i];
                if (index < startSkip)
                    continue;
                indices.Add(index);
            }

            return indices.Count > 0 ? indices : PickPickupIndices(count, startSkip, density);
        }

        void SpawnPickup(Vector3 position, Quaternion rotation, int index)
        {
            var pickup = new GameObject("NitroPickup_" + index);
            pickup.transform.SetParent(pickupRoot, false);
            pickup.transform.SetPositionAndRotation(
                ObstaclePhysics.SnapToTrackSurface(position, 0.06f),
                rotation);
            pickup.layer = NeonLapLayers.Track;

            var trigger = pickup.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.6f, 1.2f, 3.6f);
            trigger.center = new Vector3(0f, 0.35f, 0f);

            pickup.AddComponent<NitroPickup>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(pickup.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.18f, 0f);

            CreateVisualPrimitive(visual.transform, PrimitiveType.Cylinder, "Pad",
                Vector3.zero, new Vector3(2.8f, 0.08f, 2.8f), padMaterial);
            CreateVisualPrimitive(visual.transform, PrimitiveType.Cylinder, "Ring",
                new Vector3(0f, 0.22f, 0f), new Vector3(1.35f, 0.14f, 1.35f), ringMaterial);
            CreateVisualPrimitive(visual.transform, PrimitiveType.Cube, "Chevron",
                new Vector3(0f, 0.34f, 0f), new Vector3(0.35f, 0.35f, 0.9f), ringMaterial);
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
