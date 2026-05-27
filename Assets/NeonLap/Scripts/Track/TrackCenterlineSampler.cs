using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public struct TrackCenterlineSample
    {
        public int SegmentIndex;
        public float SegmentT;
        public float DistanceAlong;
        public Vector3 ClosestPoint;
        public Vector3 Forward;
        public Vector3 Right;
    }

    public static class TrackCenterlineSampler
    {
        public static float ComputeLoopLength(IReadOnlyList<Vector3> centerline)
        {
            if (centerline == null || centerline.Count < 2)
                return 0f;

            var length = 0f;
            for (var i = 0; i < centerline.Count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Count];
                length += Vector3.Distance(a, b);
            }

            return length;
        }

        public static TrackCenterlineSample SampleClosest(IReadOnlyList<Vector3> centerline, Vector3 worldPosition)
        {
            var result = new TrackCenterlineSample
            {
                Forward = Vector3.forward,
                Right = Vector3.right,
            };

            if (centerline == null || centerline.Count < 2)
                return result;

            var bestDistSq = float.MaxValue;
            var accumulated = 0f;

            for (var i = 0; i < centerline.Count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Count];
                var segment = b - a;
                var segmentLength = segment.magnitude;
                if (segmentLength < 0.01f)
                    continue;

                var segmentDir = segment / segmentLength;
                var ap = worldPosition - a;
                ap.y = 0f;
                var t = Mathf.Clamp01(Vector3.Dot(ap, segmentDir) / segmentLength);
                var closest = a + segmentDir * (t * segmentLength);
                var delta = worldPosition - closest;
                delta.y = 0f;
                var distSq = delta.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    result.SegmentIndex = i;
                    result.SegmentT = t;
                    result.ClosestPoint = closest;
                    result.DistanceAlong = accumulated + t * segmentLength;
                    result.Forward = segmentDir;
                    result.Right = Vector3.Cross(Vector3.up, segmentDir).normalized;
                }

                accumulated += segmentLength;
            }

            return result;
        }

        public static void GetDistanceRange(IReadOnlyList<Vector3> centerline, int startIndex, int endIndex,
            out float startDistance, out float endDistance)
        {
            startDistance = 0f;
            endDistance = 0f;
            if (centerline == null || centerline.Count < 2)
                return;

            var loopLength = ComputeLoopLength(centerline);
            startIndex = Mathf.Clamp(startIndex, 0, centerline.Count - 1);
            endIndex = Mathf.Clamp(endIndex, 0, centerline.Count - 1);

            var accumulated = 0f;
            for (var i = 0; i < centerline.Count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Count];
                var segmentLength = Vector3.Distance(a, b);

                if (i == startIndex)
                    startDistance = accumulated;
                if (i == endIndex)
                {
                    endDistance = accumulated + segmentLength;
                    break;
                }

                accumulated += segmentLength;
            }

            if (endDistance < startDistance)
                endDistance += loopLength;
        }

        public static int IndexFromFraction(IReadOnlyList<Vector3> centerline, float fraction)
        {
            if (centerline == null || centerline.Count == 0)
                return 0;

            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(fraction, 1f) * centerline.Count), 0,
                centerline.Count - 1);
        }

        public static bool IsDistanceInside(float distanceAlong, float startDistance, float endDistance, float loopLength)
        {
            if (loopLength <= 0.01f)
                return false;

            distanceAlong = Mathf.Repeat(distanceAlong, loopLength);
            startDistance = Mathf.Repeat(startDistance, loopLength);
            endDistance = Mathf.Repeat(endDistance, loopLength);

            if (startDistance <= endDistance)
                return distanceAlong >= startDistance && distanceAlong <= endDistance;

            return distanceAlong >= startDistance || distanceAlong <= endDistance;
        }
    }
}
