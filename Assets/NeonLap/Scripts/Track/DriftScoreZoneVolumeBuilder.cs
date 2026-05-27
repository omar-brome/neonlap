using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Track
{
    public static class DriftScoreZoneVolumeBuilder
    {
        public static void Build(Transform trackRoot, IReadOnlyList<Vector3> centerline, float trackWidth,
            IReadOnlyList<TrackGameplayZone> zones)
        {
            if (trackRoot == null || centerline == null || zones == null || zones.Count == 0)
                return;

            var root = new GameObject("DriftScoreVolumes").transform;
            root.SetParent(trackRoot, false);

            for (var z = 0; z < zones.Count; z++)
            {
                var zone = zones[z];
                if (zone.Type != TrackZoneType.DriftMultiplier)
                    continue;

                var startIndex = FindIndexForDistance(centerline, zone.StartDistance);
                var endIndex = FindIndexForDistance(centerline, zone.EndDistance);
                BuildVolumeStrip(root, centerline, startIndex, endIndex, trackWidth, zone.Strength, z);
            }
        }

        static void BuildVolumeStrip(Transform parent, IReadOnlyList<Vector3> centerline, int startIndex, int endIndex,
            float trackWidth, float multiplier, int zoneIndex)
        {
            var count = centerline.Count;
            var index = startIndex;
            var safety = 0;
            while (safety++ < count + 2)
            {
                var next = (index + 1) % count;
                var a = centerline[index];
                var b = centerline[next];
                CreateVolumeSegment(parent, a, b, trackWidth, multiplier, zoneIndex, index);
                if (index == endIndex)
                    break;
                index = next;
            }
        }

        static void CreateVolumeSegment(Transform parent, Vector3 a, Vector3 b, float trackWidth, float multiplier,
            int zoneIndex, int segmentIndex)
        {
            var delta = b - a;
            delta.y = 0f;
            var length = delta.magnitude;
            if (length < 0.5f)
                return;

            var forward = delta / length;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var mid = (a + b) * 0.5f + Vector3.up * 1.2f;

            var go = new GameObject($"DriftZone_{zoneIndex}_{segmentIndex}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(forward, Vector3.up));
            go.layer = NeonLapLayers.Track;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(trackWidth * 1.05f, 4.5f, length + 0.6f);

            var trigger = go.AddComponent<DriftScoreZoneTrigger>();
            trigger.Configure(multiplier);
        }

        static int FindIndexForDistance(IReadOnlyList<Vector3> centerline, float targetDistance)
        {
            var accumulated = 0f;
            for (var i = 0; i < centerline.Count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Count];
                var segmentLength = Vector3.Distance(a, b);
                if (accumulated + segmentLength >= targetDistance)
                    return i;
                accumulated += segmentLength;
            }

            return 0;
        }
    }
}
