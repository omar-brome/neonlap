using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackSurfaceBuilder
    {
        const float SurfaceHeight = 0.2f;
        const float SurfaceTopOffset = 0.1f;

        public static void BuildContinuousSurface(Transform trackRoot, IReadOnlyList<Vector3> centerline, float trackWidth,
            Material surfaceMaterial, bool followElevation = false)
        {
            if (trackRoot == null || centerline == null || centerline.Count < 2 || surfaceMaterial == null)
                return;

            var count = centerline.Count;
            for (var i = 0; i < count; i++)
            {
                var prev = centerline[(i - 1 + count) % count];
                var current = centerline[i];
                var next = centerline[(i + 1) % count];

                var overlap = ComputeSegmentOverlap(prev, current, next, trackWidth, followElevation);
                CreateSegment(trackRoot, "TrackSurface_" + i, current, next, trackWidth, overlap, surfaceMaterial,
                    addCollider: true, followElevation);

                CreateCornerCap(trackRoot, "TrackCap_" + i, current, prev, next, trackWidth, surfaceMaterial,
                    followElevation);
            }
        }

        static float ComputeSegmentOverlap(Vector3 prev, Vector3 a, Vector3 b, float trackWidth, bool followElevation)
        {
            var dirIn = a - prev;
            var dirOut = b - a;
            if (!followElevation)
            {
                dirIn.y = 0f;
                dirOut.y = 0f;
            }

            if (dirIn.sqrMagnitude < 0.01f || dirOut.sqrMagnitude < 0.01f)
                return trackWidth * 0.55f;

            dirIn.Normalize();
            dirOut.Normalize();
            var turnAngle = Vector3.Angle(dirIn, dirOut);
            var sharpness = 1f - Mathf.Clamp01(turnAngle / 150f);
            return trackWidth * (0.42f + sharpness * 0.58f);
        }

        static void CreateCornerCap(Transform parent, string name, Vector3 corner, Vector3 prev, Vector3 next,
            float trackWidth, Material material, bool followElevation)
        {
            var dirIn = corner - prev;
            var dirOut = next - corner;
            if (!followElevation)
            {
                dirIn.y = 0f;
                dirOut.y = 0f;
            }

            if (dirIn.sqrMagnitude < 0.01f || dirOut.sqrMagnitude < 0.01f)
                return;

            dirIn.Normalize();
            dirOut.Normalize();
            var turnAngle = Vector3.Angle(dirIn, dirOut);
            if (turnAngle < 12f)
                return;

            var sharpness = 1f - Mathf.Clamp01(turnAngle / 150f);
            var capScale = trackWidth * (0.92f + sharpness * 0.28f);
            var surfaceTop = corner.y + SurfaceTopOffset;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(corner.x, surfaceTop - SurfaceHeight * 0.5f, corner.z);
            go.transform.rotation = TrackGeometryUtility.SafeLookRotationAlongPath(dirOut);
            go.transform.localScale = new Vector3(capScale, SurfaceHeight, capScale);
            go.layer = NeonLapLayers.Track;
            go.tag = "Track";
            go.GetComponent<Renderer>().sharedMaterial = material;

            var capCollider = go.GetComponent<Collider>();
            if (capCollider == null)
                capCollider = go.AddComponent<BoxCollider>();
            capCollider.isTrigger = false;
        }

        static void CreateSegment(Transform parent, string name, Vector3 a, Vector3 b, float width, float lengthOverlap,
            Material material, bool addCollider, bool followElevation)
        {
            var delta = b - a;
            if (!followElevation)
                delta.y = 0f;

            if (delta.sqrMagnitude < 0.01f)
                return;

            var direction = delta.normalized;
            var length = delta.magnitude + lengthOverlap;
            var mid = (a + b) * 0.5f;
            var surfaceTop = (a.y + b.y) * 0.5f + SurfaceTopOffset;
            mid.y = surfaceTop - SurfaceHeight * 0.5f;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(mid,
                followElevation
                    ? TrackGeometryUtility.SafeLookRotationAlongPath(direction)
                    : TrackGeometryUtility.SafeLookRotation(direction, Vector3.up));
            go.transform.localScale = new Vector3(width, SurfaceHeight, length);
            go.layer = NeonLapLayers.Track;
            go.tag = "Track";
            go.GetComponent<Renderer>().sharedMaterial = material;

            if (!addCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null)
                    Object.Destroy(col);
            }
        }
    }
}
