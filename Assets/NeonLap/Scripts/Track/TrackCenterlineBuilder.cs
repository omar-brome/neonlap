using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackCenterlineBuilder
    {
        public static List<Vector3> Build(
            TrackLayout layout,
            float straightLength,
            float turnRadius,
            int segmentsPerTurn,
            int straightSubmotion)
        {
            return layout switch
            {
                TrackLayout.TriOvalSpeedway => BuildTriOvalSpeedway(straightLength, turnRadius, segmentsPerTurn,
                    straightSubmotion),
                TrackLayout.TechnicalRing => BuildTechnicalRing(straightLength, turnRadius, segmentsPerTurn,
                    straightSubmotion),
                _ => BuildOval(straightLength, turnRadius, segmentsPerTurn, straightSubmotion),
            };
        }

        public static Vector2 ComputeEnvironmentHalfExtents(IReadOnlyList<Vector3> centerline, float trackWidth)
        {
            if (centerline == null || centerline.Count == 0)
                return new Vector2(80f, 50f);

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            foreach (var point in centerline)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            var padding = trackWidth + 48f;
            var halfX = Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX)) + padding;
            var halfZ = Mathf.Max(Mathf.Abs(minZ), Mathf.Abs(maxZ)) + padding;
            return new Vector2(halfX, halfZ);
        }

        static List<Vector3> BuildOval(float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubmotion)
        {
            var points = new List<Vector3>();
            var half = straightLength * 0.5f;

            AppendStraight(points, new Vector3(-half, 0f, turnRadius), new Vector3(half, 0f, turnRadius),
                straightSubmotion);
            AppendArc(points, new Vector3(half, 0f, 0f), turnRadius, 90f, -90f, segmentsPerTurn);
            AppendStraight(points, new Vector3(half, 0f, -turnRadius), new Vector3(-half, 0f, -turnRadius),
                straightSubmotion);
            AppendArc(points, new Vector3(-half, 0f, 0f), turnRadius, -90f, -270f, segmentsPerTurn);

            return points;
        }

        static List<Vector3> BuildTriOvalSpeedway(float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubmotion)
        {
            var points = new List<Vector3>();
            var half = straightLength * 0.5f;
            var northZ = turnRadius * 1.18f;
            var southZ = -turnRadius * 0.92f;
            var sweeperRadius = turnRadius * 1.08f;

            AppendStraight(points, new Vector3(-half, 0f, northZ), new Vector3(half, 0f, northZ),
                straightSubmotion + 2);
            AppendArc(points, new Vector3(half + turnRadius * 0.08f, 0f, northZ - turnRadius * 0.82f), sweeperRadius,
                88f, -48f, segmentsPerTurn + 2);
            AppendStraight(points, points[^1], new Vector3(half * 0.72f, 0f, southZ), straightSubmotion / 2 + 2);

            AppendStraight(points, points[^1], new Vector3(half * 0.28f, 0f, southZ), 3);
            AppendStraight(points, points[^1], new Vector3(half * 0.08f, 0f, southZ - turnRadius * 0.42f), 3);
            AppendStraight(points, points[^1], new Vector3(-half * 0.18f, 0f, southZ - turnRadius * 0.28f), 3);
            AppendStraight(points, points[^1], new Vector3(-half * 0.42f, 0f, southZ), 3);
            AppendStraight(points, points[^1], new Vector3(-half, 0f, southZ), straightSubmotion / 2 + 2);

            AppendArc(points, new Vector3(-half - turnRadius * 0.08f, 0f, southZ + turnRadius * 0.78f),
                sweeperRadius, -132f, -248f, segmentsPerTurn + 2);
            AppendStraight(points, points[^1], new Vector3(-half, 0f, northZ), 3);

            return points;
        }

        static List<Vector3> BuildTechnicalRing(float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubmotion)
        {
            var points = new List<Vector3>();
            var half = straightLength * 0.5f;
            var topZ = turnRadius * 1.15f;
            var bottomZ = -turnRadius * 1.15f;
            var cornerRadius = turnRadius * 1.12f;
            var cornerX = turnRadius * 0.92f;
            var midZ = (topZ + bottomZ) * 0.5f;

            AppendStraight(points, new Vector3(-half, 0f, bottomZ), new Vector3(half, 0f, bottomZ),
                straightSubmotion + 2);
            AppendArc(points, new Vector3(half + cornerX, 0f, midZ), cornerRadius, -68f, 68f,
                segmentsPerTurn + 4);

            AppendStraight(points, points[^1], new Vector3(half * 0.42f, 0f, topZ), straightSubmotion / 2 + 2);
            AppendStraight(points, points[^1], new Vector3(half * 0.08f, 0f, topZ + turnRadius * 0.18f), 3);
            AppendStraight(points, points[^1], new Vector3(-half * 0.08f, 0f, topZ - turnRadius * 0.1f), 3);
            AppendStraight(points, points[^1], new Vector3(-half, 0f, topZ), straightSubmotion / 2 + 2);

            AppendArc(points, new Vector3(-half - cornerX, 0f, midZ), cornerRadius, 112f, 248f,
                segmentsPerTurn + 4);
            AppendStraight(points, points[^1], new Vector3(-half, 0f, bottomZ), straightSubmotion + 2);

            return points;
        }

        static void AppendStraight(List<Vector3> points, Vector3 from, Vector3 to, int subdivisions)
        {
            subdivisions = Mathf.Max(1, subdivisions);
            if (points.Count == 0)
            {
                for (var i = 0; i <= subdivisions; i++)
                    points.Add(Vector3.Lerp(from, to, i / (float)subdivisions));
                return;
            }

            var start = points[^1];
            for (var i = 1; i <= subdivisions; i++)
                points.Add(Vector3.Lerp(start, to, i / (float)subdivisions));
        }

        static void AppendArc(List<Vector3> points, Vector3 center, float radius, float startDegrees,
            float endDegrees, int segments)
        {
            segments = Mathf.Max(2, segments);
            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                points.Add(new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    0f,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }
    }
}
