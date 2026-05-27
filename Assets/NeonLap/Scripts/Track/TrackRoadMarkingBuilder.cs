using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackRoadMarkingBuilder
    {
        const float MarkSurfaceY = 0.102f;
        const float MarkThickness = 0.018f;

        public static void Build(Transform trackRoot, IReadOnlyList<Vector3> centerline, float trackWidth,
            TrackLayout layout = TrackLayout.Oval, bool reverseCircuit = false)
        {
            if (trackRoot == null || centerline == null || centerline.Count < 2)
                return;

            var markingsRoot = new GameObject("RoadMarkings").transform;
            markingsRoot.SetParent(trackRoot, false);

            var whiteMat = CreateMarkingMaterial(new Color(0.96f, 0.96f, 0.94f));
            var yellowMat = CreateMarkingMaterial(new Color(0.98f, 0.8f, 0.08f));
            var blackMat = CreateMarkingMaterial(new Color(0.06f, 0.06f, 0.07f));
            var arrowMat = CreateMarkingMaterial(new Color(0.35f, 1f, 1f));

            var count = centerline.Count;
            for (var i = 0; i < count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % count];

                BuildDashedLine(markingsRoot, a, b, 0f, yellowMat, 3.2f, 3.4f, 0.26f);
                BuildSolidLine(markingsRoot, a, b, trackWidth * 0.47f, whiteMat, 0.2f);
                BuildSolidLine(markingsRoot, a, b, -trackWidth * 0.47f, whiteMat, 0.2f);

                if (trackWidth >= 14f)
                {
                    BuildDashedLine(markingsRoot, a, b, trackWidth * 0.22f, whiteMat, 4f, 4.2f, 0.15f);
                    BuildDashedLine(markingsRoot, a, b, -trackWidth * 0.22f, whiteMat, 4f, 4.2f, 0.15f);
                }

                if (IsZigZagLayout(layout) && i % 2 == 0)
                    BuildDirectionArrow(markingsRoot, reverseCircuit ? b : a, reverseCircuit ? a : b, arrowMat);
            }

            BuildStartFinishLine(markingsRoot, centerline[0], centerline[1], trackWidth, whiteMat, blackMat);
            BuildStartGridLines(markingsRoot, centerline[0], centerline[1], trackWidth, whiteMat);
        }

        static void BuildDashedLine(Transform parent, Vector3 a, Vector3 b, float lateralOffset, Material material,
            float dashLength, float gapLength, float lineWidth)
        {
            var direction = (b - a);
            direction.y = 0f;
            var segmentLength = direction.magnitude;
            if (segmentLength < 0.01f)
                return;

            direction /= segmentLength;
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var cursor = 0f;

            while (cursor < segmentLength)
            {
                var dashEnd = Mathf.Min(cursor + dashLength, segmentLength);
                var start = a + direction * cursor + right * lateralOffset + Vector3.up * MarkSurfaceY;
                var end = a + direction * dashEnd + right * lateralOffset + Vector3.up * MarkSurfaceY;
                CreateLineStrip(parent, start, end, lineWidth, material);
                cursor += dashLength + gapLength;
            }
        }

        static void BuildSolidLine(Transform parent, Vector3 a, Vector3 b, float lateralOffset, Material material,
            float lineWidth)
        {
            var direction = (b - a);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;

            direction.Normalize();
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var length = Vector3.Distance(a, b) + lineWidth * 0.5f;
            var mid = (a + b) * 0.5f + right * lateralOffset + Vector3.up * MarkSurfaceY;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);

            CreateMarkCube(parent, "EdgeLine", mid, rotation, new Vector3(lineWidth, MarkThickness, length), material);
        }

        static void BuildDirectionArrow(Transform parent, Vector3 a, Vector3 b, Material material)
        {
            var direction = (b - a);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;

            direction.Normalize();
            var mid = (a + b) * 0.5f + Vector3.up * (MarkSurfaceY + 0.01f);
            var rotation = Quaternion.LookRotation(direction, Vector3.up);

            CreateMarkCube(parent, "DirectionArrowStem", mid - direction * 0.55f, rotation,
                new Vector3(0.55f, MarkThickness * 1.2f, 1.35f), material);
            CreateMarkCube(parent, "DirectionArrowHead", mid + direction * 0.55f, rotation,
                new Vector3(1.35f, MarkThickness * 1.2f, 0.55f), material);
        }

        static void BuildStartFinishLine(Transform parent, Vector3 start, Vector3 next, float trackWidth,
            Material whiteMat, Material blackMat)
        {
            var forward = (next - start);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return;

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var origin = start + Vector3.up * MarkSurfaceY;

            const int columns = 12;
            const int rows = 2;
            var cellWidth = trackWidth / columns;
            var cellDepth = 0.65f;

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var lateral = -trackWidth * 0.5f + (col + 0.5f) * cellWidth;
                    var along = (row - 0.5f) * cellDepth;
                    var position = origin + right * lateral + forward * along;
                    var rotation = Quaternion.LookRotation(forward, Vector3.up);
                    var material = (row + col) % 2 == 0 ? whiteMat : blackMat;
                    CreateMarkCube(parent, "StartFinish_" + row + "_" + col, position, rotation,
                        new Vector3(cellWidth * 0.92f, MarkThickness, cellDepth * 0.92f), material);
                }
            }
        }

        static void BuildStartGridLines(Transform parent, Vector3 start, Vector3 next, float trackWidth, Material whiteMat)
        {
            var forward = (next - start);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return;

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var lineOrigin = start + forward * 2.8f + Vector3.up * MarkSurfaceY;

            CreateMarkCube(parent, "StartLine", lineOrigin, Quaternion.LookRotation(forward, Vector3.up),
                new Vector3(trackWidth * 0.88f, MarkThickness, 0.28f), whiteMat);

            const int gridColumns = 5;
            var spacing = trackWidth * 0.72f / (gridColumns - 1);
            for (var i = 0; i < gridColumns; i++)
            {
                var lateral = -trackWidth * 0.36f + spacing * i;
                var position = lineOrigin + right * lateral + forward * 1.1f;
                CreateMarkCube(parent, "GridLine_" + i, position, Quaternion.LookRotation(forward, Vector3.up),
                    new Vector3(0.16f, MarkThickness, 2.2f), whiteMat);
            }
        }

        static void CreateLineStrip(Transform parent, Vector3 start, Vector3 end, float width, Material material)
        {
            var delta = end - start;
            delta.y = 0f;
            var length = delta.magnitude;
            if (length < 0.01f)
                return;

            var direction = delta / length;
            var mid = (start + end) * 0.5f;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            CreateMarkCube(parent, "Dash", mid, rotation, new Vector3(width, MarkThickness, length), material);
        }

        static void CreateMarkCube(Transform parent, string name, Vector3 position, Quaternion rotation,
            Vector3 scale, Material material)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = name;
            mark.transform.SetParent(parent, false);
            mark.transform.SetPositionAndRotation(position, rotation);
            mark.transform.localScale = scale;
            mark.layer = NeonLapLayers.Track;
            mark.tag = "Track";

            var collider = mark.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            if (material != null)
                mark.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Material CreateMarkingMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.35f);
            material.SetFloat("_Metallic", 0f);
            return material;
        }

        public static void BuildShortcutMarkings(Transform shortcutRoot, IReadOnlyList<Vector3> path, float trackWidth)
        {
            if (shortcutRoot == null || path == null || path.Count < 2)
                return;

            var shortcutMat = CreateMarkingMaterial(new Color(0.2f, 1f, 0.95f));
            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                BuildDashedLine(shortcutRoot, a, b, 0f, shortcutMat, 2.4f, 2.6f, 0.34f);
                BuildSolidLine(shortcutRoot, a, b, trackWidth * 0.42f, shortcutMat, 0.18f);
                BuildSolidLine(shortcutRoot, a, b, -trackWidth * 0.42f, shortcutMat, 0.18f);
            }
        }

        static bool IsZigZagLayout(TrackLayout layout) => TrackLayoutUtility.IsComplexLayout(layout);

        public static void ApplyAsphaltLook(Material surface, Material curb)
        {
            ApplyAsphaltLook(surface, curb, new Color(0.1f, 0.1f, 0.11f), new Color(0.52f, 0.5f, 0.48f));
        }

        public static void ApplyAsphaltLook(Material surface, Material curb, Color asphaltColor, Color curbColor)
        {
            if (surface != null)
            {
                surface.SetColor("_BaseColor", asphaltColor);
                surface.SetFloat("_Smoothness", 0.16f);
                surface.SetFloat("_Metallic", 0.02f);
            }

            if (curb != null)
            {
                curb.SetColor("_BaseColor", curbColor);
                curb.SetFloat("_Smoothness", 0.42f);
                curb.SetFloat("_Metallic", 0f);
            }
        }

        public static void ApplyNeonEdgeLook(Material curb)
        {
            ApplyNeonEdgeLook(curb, new Color(0.25f, 1.1f, 1.35f), new Color(0.18f, 0.82f, 0.95f));
        }

        public static void ApplyNeonEdgeLook(Material curb, Color emissionColor, Color baseColor)
        {
            if (curb == null)
                return;

            curb.EnableKeyword("_EMISSION");
            curb.SetColor("_EmissionColor", emissionColor);
            curb.SetColor("_BaseColor", baseColor);
            curb.SetFloat("_Smoothness", 0.72f);
        }
    }
}
