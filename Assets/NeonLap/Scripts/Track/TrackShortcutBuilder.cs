using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Environment;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackShortcutBuilder
    {
        const float ShortcutSurfaceY = 0.2f;

        public static void Build(Transform trackRoot, IReadOnlyList<TrackShortcutDefinition> shortcuts, float trackWidth,
            Material surfaceMaterial, Material edgeMaterial)
        {
            if (trackRoot == null || shortcuts == null || shortcuts.Count == 0 || surfaceMaterial == null)
                return;

            var root = new GameObject("TrackShortcuts").transform;
            root.SetParent(trackRoot, false);

            for (var i = 0; i < shortcuts.Count; i++)
            {
                var definition = shortcuts[i];
                if (definition == null || definition.Path.Count < 2)
                    continue;

                BuildShortcutPath(root, definition, trackWidth, surfaceMaterial, edgeMaterial, i);
            }
        }

        static void BuildShortcutPath(Transform parent, TrackShortcutDefinition definition, float trackWidth,
            Material surfaceMaterial, Material edgeMaterial, int index)
        {
            var path = definition.Path;
            var pathRoot = new GameObject("Shortcut_" + index).transform;
            pathRoot.SetParent(parent, false);

            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                if (Vector3.Distance(a, b) < 0.5f)
                    continue;

                var shortcutWidth = trackWidth * 0.72f;
                CreateShortcutSegment(pathRoot, "ShortcutSurface_" + i, a, b, shortcutWidth, surfaceMaterial);
                CreateShortcutEdges(pathRoot, "ShortcutEdge_" + i, a, b, shortcutWidth, edgeMaterial);
            }

            TrackRoadMarkingBuilder.BuildShortcutMarkings(pathRoot, path, trackWidth * 0.72f);
            BuildEntryGate(pathRoot, path, definition);
            BuildMergeGate(pathRoot, path, definition);
        }

        static void BuildEntryGate(Transform pathRoot, IReadOnlyList<Vector3> path, TrackShortcutDefinition definition)
        {
            if (path.Count < 2)
                return;

            var entry = path[0];
            var next = path[1];
            CreateGate(pathRoot, "ShortcutEntry", entry, next, definition, typeof(ShortcutEntryGate));
        }

        static void BuildMergeGate(Transform pathRoot, IReadOnlyList<Vector3> path, TrackShortcutDefinition definition)
        {
            if (path.Count < 2)
                return;

            var merge = path[^1];
            var previous = path[^2];
            CreateGate(pathRoot, "ShortcutMerge", merge, previous, definition, typeof(ShortcutMergeGate));
        }

        static void CreateGate(Transform pathRoot, string name, Vector3 position, Vector3 directionPoint,
            TrackShortcutDefinition definition, System.Type gateType)
        {
            var forward = directionPoint - position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            var gateGo = new GameObject(name);
            gateGo.transform.SetParent(pathRoot, false);
            gateGo.transform.SetPositionAndRotation(position + Vector3.up * ShortcutSurfaceY,
                Quaternion.LookRotation(forward));
            gateGo.layer = NeonLapLayers.Track;

            var trigger = gateGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(10f, 4f, 10f);

            var gate = gateGo.AddComponent(gateType) as MonoBehaviour;
            if (gate is ShortcutEntryGate entry)
                entry.Configure(definition);
            else if (gate is ShortcutMergeGate merge)
                merge.Configure(definition);
        }

        static void CreateShortcutSegment(Transform parent, string name, Vector3 a, Vector3 b, float width,
            Material material)
        {
            var direction = (b - a);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;

            direction.Normalize();
            var length = Vector3.Distance(a, b) + width * 0.35f;
            var mid = (a + b) * 0.5f;
            mid.y = ShortcutSurfaceY;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(direction));
            go.transform.localScale = new Vector3(width, 0.2f, length);
            go.layer = NeonLapLayers.Track;
            go.tag = "Track";
            go.GetComponent<Renderer>().sharedMaterial = material;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }

        static void CreateShortcutEdges(Transform parent, string name, Vector3 a, Vector3 b, float width,
            Material material)
        {
            var direction = (b - a);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;

            direction.Normalize();
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var offset = width * 0.5f - 0.25f;
            var length = Vector3.Distance(a, b) + width * 0.35f;
            var mid = (a + b) * 0.5f;
            mid.y = ShortcutSurfaceY;
            var rotation = Quaternion.LookRotation(direction);

            CreateEdge(parent, name + "_L", mid + right * offset, rotation, length, material);
            CreateEdge(parent, name + "_R", mid - right * offset, rotation, length, material);
        }

        static void CreateEdge(Transform parent, string name, Vector3 position, Quaternion rotation, float length,
            Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = new Vector3(0.35f, 0.28f, length);
            go.layer = NeonLapLayers.Track;
            go.tag = "Track";
            go.GetComponent<Renderer>().sharedMaterial = material;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }
    }
}
