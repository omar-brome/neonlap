using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackGeometryUtility
    {
        public const float MinSegmentLength = 0.75f;

        public static bool TryGetPlanarDirection(Vector3 from, Vector3 to, out Vector3 direction)
        {
            direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < MinSegmentLength * MinSegmentLength)
            {
                direction = Vector3.zero;
                return false;
            }

            direction.Normalize();
            return true;
        }

        public static bool TryGetSegmentDirection(Vector3 from, Vector3 to, out Vector3 direction)
        {
            direction = to - from;
            if (direction.sqrMagnitude < MinSegmentLength * MinSegmentLength)
            {
                direction = Vector3.zero;
                return false;
            }

            direction.Normalize();
            return true;
        }

        public static Quaternion SafeLookRotation(Vector3 forward, Vector3 up)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(forward.normalized, up);
        }

        public static Quaternion SafeLookRotationAlongPath(Vector3 forward)
        {
            if (forward.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward.normalized, up)) > 0.92f)
                up = Vector3.forward;

            return Quaternion.LookRotation(forward.normalized, up);
        }

        public static Vector3 GetLateralOffset(Vector3 forward, float distance)
        {
            var planar = forward;
            planar.y = 0f;
            if (planar.sqrMagnitude < 0.0001f)
                return Vector3.right * distance;

            planar.Normalize();
            return Vector3.Cross(Vector3.up, planar).normalized * distance;
        }

        public static void SanitizeCenterline(System.Collections.Generic.List<Vector3> points, float minDistance)
        {
            if (points == null || points.Count < 2)
                return;

            minDistance = Mathf.Max(0.25f, minDistance);
            var sanitized = new System.Collections.Generic.List<Vector3>(points.Count);
            sanitized.Add(points[0]);

            for (var i = 1; i < points.Count; i++)
            {
                var candidate = points[i];
                if (Vector3.Distance(sanitized[^1], candidate) >= minDistance)
                    sanitized.Add(candidate);
            }

            if (sanitized.Count > 1 && Vector3.Distance(sanitized[^1], sanitized[0]) < minDistance)
                sanitized[^1] = sanitized[0];

            points.Clear();
            points.AddRange(sanitized);
        }
    }
}
