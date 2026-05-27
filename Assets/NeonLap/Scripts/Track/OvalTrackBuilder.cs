using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Track
{
    public class OvalTrackBuilder : MonoBehaviour
    {
        [Header("Oval Dimensions")]
        [SerializeField] float straightLength = 60f;
        [SerializeField] float turnRadius = 25f;
        [SerializeField] float trackWidth = 26f;
        [SerializeField] int segmentsPerTurn = 16;
        [SerializeField] int straightSubdivisions = 6;

        [Header("Materials")]
        [SerializeField] Material trackSurfaceMaterial;
        [SerializeField] Material trackEdgeMaterial;
        [SerializeField] Material barrierMaterial;

        [Header("Checkpoints")]
        [SerializeField] bool createCheckpoints = true;
        [SerializeField] int checkpointCount = 10;

        Transform trackRoot;
        Transform aiWaypointRoot;
        TrackLayout layout = TrackLayout.Oval;
        int levelIndex;
        readonly List<Transform> checkpointTransforms = new();
        readonly List<Transform> aiWaypointTransforms = new();
        readonly List<Vector3> centerlinePoints = new();
        readonly List<TrackShortcutDefinition> shortcutDefinitions = new();
        readonly List<List<Vector3>> shortcutPaths = new();

        public IReadOnlyList<Transform> CheckpointTransforms => checkpointTransforms;
        public IReadOnlyList<Transform> AiWaypointTransforms => aiWaypointTransforms;
        public IReadOnlyList<Vector3> CenterlinePoints => centerlinePoints;
        public IReadOnlyList<TrackShortcutDefinition> ShortcutDefinitions => shortcutDefinitions;
        public IReadOnlyList<IReadOnlyList<Vector3>> ShortcutPaths => shortcutPaths;
        public Vector3 StartPosition { get; private set; }
        public Quaternion StartRotation { get; private set; }
        public float TrackWidth => trackWidth;
        public Vector2 EnvironmentHalfExtents { get; private set; } = new(80f, 50f);

        public void Configure(TrackDefinition definition, Material surface, Material edge, Material barrier)
        {
            if (definition != null)
            {
                layout = TrackLayoutUtility.Normalize(definition.layout);
                levelIndex = TrackLayoutUtility.LevelIndexForLayout(layout);
                straightLength = definition.straightLength;
                turnRadius = definition.turnRadius;
                trackWidth = definition.trackWidth;
                checkpointCount = definition.checkpointCount;

                switch (levelIndex)
                {
                    case 1:
                        straightSubdivisions = 8;
                        segmentsPerTurn = 18;
                        break;
                    case 2:
                        straightSubdivisions = 8;
                        segmentsPerTurn = 16;
                        break;
                    case 3:
                        straightSubdivisions = 9;
                        segmentsPerTurn = 12;
                        break;
                    case 4:
                        straightSubdivisions = 8;
                        segmentsPerTurn = 16;
                        break;
                    case 5:
                        straightSubdivisions = 10;
                        segmentsPerTurn = 14;
                        break;
                    case 6:
                        straightSubdivisions = 9;
                        segmentsPerTurn = 18;
                        break;
                    default:
                        straightSubdivisions = 6;
                        segmentsPerTurn = 14;
                        break;
                }
            }

            trackSurfaceMaterial = surface;
            trackEdgeMaterial = edge;
            barrierMaterial = barrier ?? edge;
        }

        public void BuildTrack()
        {
            if (trackRoot != null)
                Destroy(trackRoot.gameObject);
            if (aiWaypointRoot != null)
                Destroy(aiWaypointRoot.gameObject);

            checkpointTransforms.Clear();
            aiWaypointTransforms.Clear();
            shortcutPaths.Clear();

            trackRoot = new GameObject("TrackGeometry").transform;
            trackRoot.SetParent(transform, false);

            aiWaypointRoot = new GameObject("AIWaypoints").transform;
            aiWaypointRoot.SetParent(transform, false);

            var path = BuildCenterlinePath();
            var centerline = path.Centerline;
            if (GameTrackOptions.ReverseCircuit)
            {
                TrackGeometryPaths.ReverseCenterlineInPlace(centerline);
                TrackGeometryPaths.ReverseShortcutsInPlace(path.Shortcuts);
            }

            centerlinePoints.Clear();
            centerlinePoints.AddRange(centerline);
            shortcutDefinitions.Clear();
            shortcutPaths.Clear();
            foreach (var shortcut in path.Shortcuts)
            {
                shortcutDefinitions.Add(shortcut);
                shortcutPaths.Add(shortcut.Path);
            }

            EnvironmentHalfExtents = TrackCenterlineBuilder.ComputeEnvironmentHalfExtents(centerline, trackWidth);
            TrackRoadMarkingBuilder.ApplyAsphaltLook(trackSurfaceMaterial, trackEdgeMaterial);
            BuildSurface(centerline);
            TrackRoadMarkingBuilder.Build(trackRoot, centerline, trackWidth, layout, GameTrackOptions.ReverseCircuit);
            if (GameRaceModeSettings.IsStuntFreestyle)
                StuntTrackGeometryBuilder.Build(trackRoot, centerline, trackWidth, trackEdgeMaterial);
            BuildEdges(centerline);
            BuildBarriers(centerline);
            BuildAiWaypoints(centerline);
            TrackSpecialZoneBuilder.Build(trackRoot, layout, centerline, trackWidth, trackSurfaceMaterial);

            if (createCheckpoints)
            {
                BuildCheckpoints(centerline);
                AlignFinishCheckpoint(centerline);
                AlignShortcutMergePoints(centerline);
            }

            if (shortcutDefinitions.Count > 0)
                TrackShortcutBuilder.Build(trackRoot, shortcutDefinitions, trackWidth, trackSurfaceMaterial, trackEdgeMaterial);

            if (centerline.Count > 1)
            {
                var start = centerline[0];
                var next = centerline[1];
                var forward = next - start;
                var hoverHeight = TrackLayoutUtility.HasElevation(layout) ? 1.8f : 1.6f;
                StartPosition = new Vector3(start.x, start.y + 0.1f + hoverHeight, start.z);
                StartRotation = TrackLayoutUtility.HasElevation(layout)
                    ? TrackGeometryUtility.SafeLookRotationAlongPath(forward)
                    : TrackGeometryUtility.SafeLookRotation(forward, Vector3.up);
            }

            Physics.SyncTransforms();
        }

        TrackPathResult BuildCenterlinePath()
        {
            var path = new TrackPathResult();
            TrackCenterlineBuilder.BuildPathInto(path, levelIndex, straightLength, turnRadius, segmentsPerTurn,
                straightSubdivisions);
            return path;
        }

        void BuildSurface(List<Vector3> centerline)
        {
            var followElevation = TrackLayoutUtility.HasElevation(layout);
            TrackSurfaceBuilder.BuildContinuousSurface(trackRoot, centerline, trackWidth, trackSurfaceMaterial,
                followElevation);
        }

        void BuildEdges(List<Vector3> centerline)
        {
            var count = centerline.Count;
            var edgeOffset = trackWidth * 0.5f - 0.3f;
            var useElevation = TrackLayoutUtility.HasElevation(layout);

            for (var i = 0; i < count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % count];
                if (!TryGetSegmentFrame(a, b, useElevation, out var direction, out var right, out var mid, out var length))
                    continue;

                var rotation = useElevation
                    ? TrackGeometryUtility.SafeLookRotationAlongPath(direction)
                    : TrackGeometryUtility.SafeLookRotation(direction, Vector3.up);
                mid.y = (a.y + b.y) * 0.5f + 0.1f;

                CreateSegment("EdgeL_" + i, mid + right * edgeOffset, rotation,
                    new Vector3(0.4f, 0.3f, length), trackEdgeMaterial, NeonLapLayers.Track, "Track");
                CreateSegment("EdgeR_" + i, mid - right * edgeOffset, rotation,
                    new Vector3(0.4f, 0.3f, length), trackEdgeMaterial, NeonLapLayers.Track, "Track");
            }
        }

        void BuildBarriers(List<Vector3> centerline)
        {
            var count = centerline.Count;
            var barrierOffset = trackWidth * 0.5f + 5f;
            var useElevation = TrackLayoutUtility.HasElevation(layout);

            for (var i = 0; i < count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % count];
                if (!TryGetSegmentFrame(a, b, useElevation, out var direction, out var right, out var mid, out var length))
                    continue;

                var rotation = useElevation
                    ? TrackGeometryUtility.SafeLookRotationAlongPath(direction)
                    : TrackGeometryUtility.SafeLookRotation(direction, Vector3.up);
                mid.y = (a.y + b.y) * 0.5f + 0.5f;

                CreateSegment("BarrierL_" + i, mid + right * barrierOffset, rotation,
                    new Vector3(0.8f, 1.2f, length), barrierMaterial, NeonLapLayers.Obstacle, "Barrier", addCollider: true);
                CreateSegment("BarrierR_" + i, mid - right * barrierOffset, rotation,
                    new Vector3(0.8f, 1.2f, length), barrierMaterial, NeonLapLayers.Obstacle, "Barrier", addCollider: true);
            }
        }

        static bool TryGetSegmentFrame(Vector3 a, Vector3 b, bool useElevation, out Vector3 direction, out Vector3 right,
            out Vector3 mid, out float length)
        {
            direction = Vector3.zero;
            right = Vector3.right;
            mid = (a + b) * 0.5f;
            length = 0f;

            if (useElevation)
            {
                if (!TrackGeometryUtility.TryGetSegmentDirection(a, b, out direction))
                    return false;

                right = TrackGeometryUtility.GetLateralOffset(direction, 1f);
                length = Vector3.Distance(a, b) + 9f;
                return true;
            }

            if (!TrackGeometryUtility.TryGetPlanarDirection(a, b, out direction))
                return false;

            right = Vector3.Cross(Vector3.up, direction).normalized;
            length = Vector3.Distance(a, b) + 9f;
            return true;
        }

        void BuildAiWaypoints(List<Vector3> centerline)
        {
            var useElevation = TrackLayoutUtility.HasElevation(layout);
            for (var i = 0; i < centerline.Count; i++)
            {
                var current = centerline[i];
                var next = centerline[(i + 1) % centerline.Count];
                var forward = next - current;
                var rotation = useElevation
                    ? TrackGeometryUtility.SafeLookRotationAlongPath(forward)
                    : TrackGeometryUtility.SafeLookRotation(forward, Vector3.up);

                var wp = new GameObject("AIWaypoint_" + i);
                wp.transform.SetParent(aiWaypointRoot, false);
                wp.transform.SetPositionAndRotation(new Vector3(current.x, current.y + 0.5f, current.z), rotation);
                aiWaypointTransforms.Add(wp.transform);
            }
        }

        void BuildCheckpoints(List<Vector3> centerline)
        {
            if (centerline == null || centerline.Count < 2)
                return;

            var checkpointRoot = new GameObject("Checkpoints").transform;
            checkpointRoot.SetParent(transform, false);
            var loopForward = GetLoopForward(centerline);

            for (var i = 0; i < checkpointCount; i++)
            {
                var t = i / (float)checkpointCount;
                var index = Mathf.FloorToInt(t * centerline.Count);
                index = Mathf.Clamp(index, 0, centerline.Count - 1);
                var nextIndex = (index + 1) % centerline.Count;
                var a = centerline[index];
                var b = centerline[nextIndex];
                var localT = t * centerline.Count - index;
                var pos = Vector3.Lerp(a, b, localT);
                var forward = TrackGeometryUtility.TryGetPlanarDirection(a, b, out var segmentForward)
                    ? segmentForward
                    : loopForward;
                var rot = TrackGeometryUtility.SafeLookRotation(forward, Vector3.up);

                var cpGo = new GameObject("Checkpoint_" + i);
                cpGo.transform.SetParent(checkpointRoot, false);
                cpGo.transform.SetPositionAndRotation(pos + Vector3.up * 1f, rot);

                var trigger = cpGo.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                var isFinish = i == 0;
                trigger.size = isFinish
                    ? new Vector3(trackWidth + 14f, 8f, 28f)
                    : new Vector3(trackWidth + 8f, 6f, 16f);

                var checkpoint = cpGo.AddComponent<TrackCheckpoint>();
                checkpoint.Configure(i, isFinish);

                checkpointTransforms.Add(cpGo.transform);
            }
        }

        static Vector3 GetLoopForward(IReadOnlyList<Vector3> centerline)
        {
            if (centerline.Count < 2)
                return Vector3.forward;

            for (var i = 0; i < centerline.Count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Count];
                if (TrackGeometryUtility.TryGetPlanarDirection(a, b, out var forward))
                    return forward;
            }

            return Vector3.forward;
        }

        void AlignFinishCheckpoint(List<Vector3> centerline)
        {
            if (centerline.Count < 2 || checkpointTransforms.Count == 0)
                return;

            var finish = checkpointTransforms[0];
            var start = centerline[0];
            var next = centerline[1];
            var forward = next - start;
            finish.SetPositionAndRotation(start + Vector3.up * 1f,
                TrackGeometryUtility.SafeLookRotation(forward, Vector3.up));
        }

        void AlignShortcutMergePoints(List<Vector3> centerline)
        {
            if (shortcutDefinitions.Count == 0 || checkpointTransforms.Count == 0)
                return;

            foreach (var shortcut in shortcutDefinitions)
            {
                if (shortcut == null || shortcut.Path.Count < 2)
                    continue;

                var mergeIndex = Mathf.Clamp(shortcut.MergeCheckpointIndex, 0, checkpointTransforms.Count - 1);
                var mergePoint = checkpointTransforms[mergeIndex].position;
                mergePoint.y = 0f;
                shortcut.Path[^1] = mergePoint;

                if (shortcut.Path.Count >= 2)
                {
                    var entry = shortcut.Path[0];
                    entry.y = 0f;
                    shortcut.Path[0] = entry;
                }
            }
        }

        void CreateTrackSegment(string name, Vector3 a, Vector3 b, float width, float height, Material material,
            int layer, string tag, bool addCollider)
        {
            if (!TrackGeometryUtility.TryGetPlanarDirection(a, b, out var direction))
                return;

            var length = Vector3.Distance(a, b) + width * 0.35f;
            var mid = (a + b) * 0.5f;
            CreateSegment(name, mid, TrackGeometryUtility.SafeLookRotation(direction, Vector3.up),
                new Vector3(width, height, length),
                material, layer, tag, addCollider);
        }

        void CreateSegment(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material,
            int layer, string tag, bool addCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(trackRoot, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.layer = layer;
            go.tag = tag;

            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;

            if (addCollider && tag == "Barrier")
                ObstaclePhysics.ConfigureTrackBarrier(go);

            if (!addCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
            }
        }
    }
}
