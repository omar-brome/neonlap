using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackCenterlineUtility
    {
        public static Vector3 Point(float x, float z)
        {
            return new Vector3(x, 0f, z);
        }

        public static void AppendStraight(List<Vector3> points, Vector3 from, Vector3 to, int subdivisions)
        {
            AppendStraight(points, from, to, subdivisions, from.y, to.y);
        }

        public static void AppendStraight(List<Vector3> points, Vector3 from, Vector3 to, int subdivisions,
            float startY, float endY)
        {
            subdivisions = Mathf.Max(1, subdivisions);
            from.y = startY;
            to.y = endY;

            if (points.Count == 0)
            {
                for (var i = 0; i <= subdivisions; i++)
                    points.Add(Vector3.Lerp(from, to, i / (float)subdivisions));
                return;
            }

            var start = points[^1];
            for (var i = 1; i <= subdivisions; i++)
            {
                var t = i / (float)subdivisions;
                var pos = Vector3.Lerp(start, to, t);
                pos.y = Mathf.Lerp(start.y, endY, t);
                points.Add(pos);
            }
        }

        public static void AppendArc(List<Vector3> points, Vector3 center, float radius, float startDegrees,
            float endDegrees, int segments)
        {
            segments = Mathf.Max(2, segments);
            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                points.Add(new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }

        public static void AppendLaunchRamp(List<Vector3> points, float length, float peakHeight, int subdivisions)
        {
            if (points.Count == 0)
                return;

            subdivisions = Mathf.Max(3, subdivisions);
            var start = points[^1];
            var forward = Vector3.forward;
            if (points.Count >= 2)
            {
                forward = start - points[^2];
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.01f)
                    forward.Normalize();
                else
                    forward = Vector3.forward;
            }

            var crest = start + forward * (length * 0.72f) + Vector3.up * peakHeight;
            var landing = crest + forward * (length * 0.28f) + Vector3.down * peakHeight;
            AppendStraight(points, start, crest, subdivisions / 2 + 2, start.y, crest.y);
            AppendStraight(points, points[^1], landing, subdivisions / 2 + 2, crest.y, 0f);
        }

        public static void AppendVerticalLoop(List<Vector3> points, float radius, int segments, Vector3 forward)
        {
            if (points.Count == 0)
                return;

            segments = Mathf.Max(16, segments);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var entry = points[^1];
            var center = entry + Vector3.up * radius;
            for (var i = 1; i <= segments; i++)
            {
                var angle = (-0.5f * Mathf.PI) + (Mathf.PI * 2f * i / segments);
                var offset = forward * (Mathf.Cos(angle) * radius) + Vector3.up * (Mathf.Sin(angle) * radius);
                points.Add(center + offset);
            }
        }

        public static void AppendHalfPipe(List<Vector3> points, float radius, float length, int segments,
            Vector3 bankBias)
        {
            if (points.Count == 0)
                return;

            segments = Mathf.Max(12, segments);
            var origin = points[^1];
            var forward = Vector3.forward;
            if (points.Count >= 2)
            {
                forward = origin - points[^2];
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.01f)
                    forward.Normalize();
            }

            var right = Vector3.Cross(Vector3.up, forward).normalized;
            if (bankBias.sqrMagnitude > 0.01f)
                right = Vector3.Lerp(right, bankBias.normalized, 0.35f).normalized;

            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, t);
                var height = radius * (1f + Mathf.Sin(angle));
                var along = forward * (t * length) + right * (Mathf.Sin(angle * 2f) * radius * 0.08f);
                points.Add(new Vector3(origin.x, origin.y + height, origin.z) + along);
            }
        }

        public static void AppendWaypointRoute(List<Vector3> points, IReadOnlyList<Vector3> waypoints,
            float filletRadius, int subdivisions)
        {
            if (waypoints == null || waypoints.Count < 2)
                return;

            subdivisions = Mathf.Max(2, subdivisions);
            filletRadius = Mathf.Max(2f, filletRadius);

            for (var i = 0; i < waypoints.Count - 1; i++)
            {
                var prev = i == 0 ? waypoints[i] : waypoints[i - 1];
                var corner = waypoints[i];
                var next = waypoints[i + 1];

                if (i > 0)
                    AppendCornerFillet(points, prev, corner, next, filletRadius, subdivisions);
                else
                    points.Add(corner);
            }
        }

        public static void CloseLoop(List<Vector3> points, Vector3 startPoint, int subdivisions)
        {
            if (points.Count < 2)
                return;

            var last = points[^1];
            var first = startPoint;
            if (Vector3.Distance(last, first) < 1f)
            {
                points[^1] = first;
                return;
            }

            var prev = points.Count >= 2 ? points[^2] : last;
            AppendCornerFillet(points, prev, last, first, Mathf.Max(3f, Vector3.Distance(last, first) * 0.15f),
                subdivisions);
            if (Vector3.Distance(points[^1], first) > 0.5f)
                AppendStraight(points, points[^1], first, subdivisions);
            points[^1] = first;
        }

        static void AppendCornerFillet(List<Vector3> points, Vector3 prev, Vector3 corner, Vector3 next,
            float filletRadius, int subdivisions)
        {
            var inDir = corner - prev;
            var outDir = next - corner;
            inDir.y = 0f;
            outDir.y = 0f;

            var inLen = inDir.magnitude;
            var outLen = outDir.magnitude;
            if (inLen < 0.01f || outLen < 0.01f)
            {
                AppendStraight(points, points.Count > 0 ? points[^1] : prev, corner, subdivisions);
                return;
            }

            inDir /= inLen;
            outDir /= outLen;

            var turn = Vector3.Cross(inDir, outDir).y;
            if (Mathf.Abs(turn) < 0.05f)
            {
                AppendStraight(points, points.Count > 0 ? points[^1] : prev, corner, subdivisions);
                return;
            }

            var tangent = Mathf.Min(filletRadius * 1.2f, inLen * 0.45f, outLen * 0.45f);
            var entry = corner - inDir * tangent;
            var exit = corner + outDir * tangent;

            AppendStraight(points, points.Count > 0 ? points[^1] : prev, entry, subdivisions);

            var turnSign = turn > 0f ? 1f : -1f;
            var inNormal = Vector3.Cross(Vector3.up, inDir).normalized * turnSign;
            var arcCenter = entry + inNormal * filletRadius;
            var startAngle = Mathf.Atan2(entry.z - arcCenter.z, entry.x - arcCenter.x) * Mathf.Rad2Deg;
            var endAngle = Mathf.Atan2(exit.z - arcCenter.z, exit.x - arcCenter.x) * Mathf.Rad2Deg;

            if (turn > 0f && endAngle < startAngle)
                endAngle += 360f;
            if (turn < 0f && endAngle > startAngle)
                endAngle -= 360f;

            AppendArc(points, arcCenter, filletRadius, startAngle, endAngle, subdivisions + 4);
        }
    }
}
