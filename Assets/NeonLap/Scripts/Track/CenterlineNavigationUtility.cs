using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public static class CenterlineNavigationUtility
    {
        public static float GetLateralDistance(Vector3 worldPosition, IReadOnlyList<Vector3> centerline,
            out Vector3 closestPoint)
        {
            closestPoint = worldPosition;
            if (centerline == null || centerline.Count < 2)
                return 0f;

            var position = worldPosition;
            position.y = 0f;
            var bestDistance = float.MaxValue;
            var bestPoint = position;

            for (var i = 0; i < centerline.Count; i++)
            {
                var start = centerline[i];
                var end = centerline[(i + 1) % centerline.Count];
                start.y = 0f;
                end.y = 0f;

                var closest = ClosestPointOnSegment(position, start, end);
                var distance = Vector3.Distance(position, closest);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestPoint = closest;
            }

            closestPoint = bestPoint;
            return bestDistance;
        }

        public static bool IsOffOptimalLine(Vector3 worldPosition, IReadOnlyList<Vector3> centerline, float halfTrackWidth,
            float toleranceFraction = 0.38f)
        {
            if (centerline == null || centerline.Count < 2 || halfTrackWidth <= 0.01f)
                return false;

            var lateral = GetLateralDistance(worldPosition, centerline, out _);
            return lateral > halfTrackWidth * Mathf.Clamp01(toleranceFraction);
        }

        static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.0001f)
                return a;

            var t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSq);
            return a + ab * t;
        }
    }
}
