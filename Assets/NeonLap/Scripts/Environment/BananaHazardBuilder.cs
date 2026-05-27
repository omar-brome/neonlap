using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Environment
{
    public class BananaHazardBuilder : MonoBehaviour
    {
        Transform hazardRoot;

        public void Build(IReadOnlyList<Transform> waypoints, TrackDefinition definition)
        {
            if (waypoints == null || waypoints.Count < 12)
                return;

            var levelIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var density = TrackLevelConfig.GetBananaDensity(levelIndex, GameQualitySettings.Preset.BananaDensity);
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

            if (levelIndex == 2)
                SpawnHairpinCluster(waypoints, trackWidth, density, startIndex: hazardIndices.Count);
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
            BananaHazardFactory.Spawn(position, rotation, hazardRoot, "BananaHazard_" + index,
                respawnAfterSlip: true, respawnDelay: 14f);
        }

        void SpawnHairpinCluster(IReadOnlyList<Transform> waypoints, float trackWidth, float density, int startIndex)
        {
            var count = waypoints.Count;
            if (count < 24 || hazardRoot == null)
                return;

            var hairpinIndex = FindTightestTurnIndex(waypoints);
            var clusterCount = Mathf.Clamp(Mathf.RoundToInt(6f * Mathf.Clamp01(density)), 3, 8);
            var lateralPattern = new[] { -0.22f, 0.22f, 0f, -0.1f, 0.1f };

            for (var i = 0; i < clusterCount; i++)
            {
                var index = (hairpinIndex + i * 2) % count;
                var lateral = trackWidth * lateralPattern[i % lateralPattern.Length];
                var position = GetTrackPoint(waypoints, index, lateral);
                var rotation = GetTrackRotation(waypoints, index);
                rotation *= Quaternion.Euler(0f, Random.Range(-18f, 18f), 0f);
                SpawnBanana(position, rotation, startIndex + i);
            }
        }

        static int FindTightestTurnIndex(IReadOnlyList<Transform> waypoints)
        {
            var count = waypoints.Count;
            var bestIndex = 0;
            var bestAngle = 0f;
            for (var i = 0; i < count; i++)
            {
                var prev = GetForward(waypoints, (i - 1 + count) % count);
                var next = GetForward(waypoints, i);
                var angle = Vector3.Angle(prev, next);
                if (angle <= bestAngle)
                    continue;
                bestAngle = angle;
                bestIndex = i;
            }

            return bestIndex;
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
