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
        readonly List<Transform> checkpointTransforms = new();
        readonly List<Transform> aiWaypointTransforms = new();
        readonly List<Vector3> centerlinePoints = new();

        public IReadOnlyList<Transform> CheckpointTransforms => checkpointTransforms;
        public IReadOnlyList<Transform> AiWaypointTransforms => aiWaypointTransforms;
        public IReadOnlyList<Vector3> CenterlinePoints => centerlinePoints;
        public Vector3 StartPosition { get; private set; }
        public Quaternion StartRotation { get; private set; }
        public float TrackWidth => trackWidth;
        public Vector2 EnvironmentHalfExtents { get; private set; } = new(80f, 50f);

        public void Configure(TrackDefinition definition, Material surface, Material edge, Material barrier)
        {
            if (definition != null)
            {
                layout = definition.layout;
                straightLength = definition.straightLength;
                turnRadius = definition.turnRadius;
                trackWidth = definition.trackWidth;
                checkpointCount = definition.checkpointCount;

                switch (layout)
                {
                    case TrackLayout.TriOvalSpeedway:
                        straightSubdivisions = 8;
                        segmentsPerTurn = 18;
                        break;
                    case TrackLayout.TechnicalRing:
                        straightSubdivisions = 10;
                        segmentsPerTurn = 20;
                        break;
                    default:
                        straightSubdivisions = 6;
                        segmentsPerTurn = 16;
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

            trackRoot = new GameObject("TrackGeometry").transform;
            trackRoot.SetParent(transform, false);

            aiWaypointRoot = new GameObject("AIWaypoints").transform;
            aiWaypointRoot.SetParent(transform, false);

            var centerline = BuildCenterline();
            centerlinePoints.Clear();
            centerlinePoints.AddRange(centerline);
            EnvironmentHalfExtents = TrackCenterlineBuilder.ComputeEnvironmentHalfExtents(centerline, trackWidth);
            TrackRoadMarkingBuilder.ApplyAsphaltLook(trackSurfaceMaterial, trackEdgeMaterial);
            BuildSurface(centerline);
            TrackRoadMarkingBuilder.Build(trackRoot, centerline, trackWidth, layout);
            BuildEdges(centerline);
            BuildBarriers(centerline);
            BuildAiWaypoints(centerline);

            if (createCheckpoints)
            {
                BuildCheckpoints(centerline);
                AlignFinishCheckpoint(centerline);
            }

            if (centerline.Count > 1)
            {
                var start = centerline[0];
                var next = centerline[1];
                var trackTop = 0.1f;
                StartPosition = start + Vector3.up * (trackTop + 1.5f);
                StartRotation = Quaternion.LookRotation((next - start).normalized, Vector3.up);
            }

            Physics.SyncTransforms();
        }

        List<Vector3> BuildCenterline()
        {
            return TrackCenterlineBuilder.Build(layout, straightLength, turnRadius, segmentsPerTurn,
                straightSubdivisions);
        }

        void BuildSurface(List<Vector3> centerline)
        {
            var count = centerline.Count;
            for (var i = 0; i < count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % count];
                CreateTrackSegment("TrackSurface_" + i, a, b, trackWidth, 0.2f, trackSurfaceMaterial,
                    NeonLapLayers.Track, "Track", addCollider: true);
            }
        }

        void BuildEdges(List<Vector3> centerline)
        {
            var count = centerline.Count;
            var edgeOffset = trackWidth * 0.5f - 0.3f;

            for (var i = 0; i < count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % count];
                var direction = (b - a).normalized;
                var right = Vector3.Cross(Vector3.up, direction).normalized;
                var mid = (a + b) * 0.5f;
                var length = Vector3.Distance(a, b) + trackWidth * 0.35f;
                var rotation = Quaternion.LookRotation(direction);

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

            for (var i = 0; i < count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % count];
                var direction = (b - a).normalized;
                var right = Vector3.Cross(Vector3.up, direction).normalized;
                var mid = (a + b) * 0.5f;
                var length = Vector3.Distance(a, b) + trackWidth * 0.35f;
                var rotation = Quaternion.LookRotation(direction);

                CreateSegment("BarrierL_" + i, mid + right * barrierOffset, rotation,
                    new Vector3(0.8f, 1.2f, length), barrierMaterial, NeonLapLayers.Obstacle, "Barrier", addCollider: true);
                CreateSegment("BarrierR_" + i, mid - right * barrierOffset, rotation,
                    new Vector3(0.8f, 1.2f, length), barrierMaterial, NeonLapLayers.Obstacle, "Barrier", addCollider: true);
            }
        }

        void BuildAiWaypoints(List<Vector3> centerline)
        {
            for (var i = 0; i < centerline.Count; i++)
            {
                var current = centerline[i];
                var next = centerline[(i + 1) % centerline.Count];
                var rotation = Quaternion.LookRotation((next - current).normalized, Vector3.up);

                var wp = new GameObject("AIWaypoint_" + i);
                wp.transform.SetParent(aiWaypointRoot, false);
                wp.transform.SetPositionAndRotation(current + Vector3.up * 0.5f, rotation);
                aiWaypointTransforms.Add(wp.transform);
            }
        }

        void BuildCheckpoints(List<Vector3> centerline)
        {
            var checkpointRoot = new GameObject("Checkpoints").transform;
            checkpointRoot.SetParent(transform, false);

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
                var rot = Quaternion.LookRotation((b - a).normalized, Vector3.up);

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

        void AlignFinishCheckpoint(List<Vector3> centerline)
        {
            if (centerline.Count < 2 || checkpointTransforms.Count == 0)
                return;

            var finish = checkpointTransforms[0];
            var start = centerline[0];
            var next = centerline[1];
            var forward = (next - start).normalized;
            finish.SetPositionAndRotation(start + Vector3.up * 1f, Quaternion.LookRotation(forward, Vector3.up));
        }

        void CreateTrackSegment(string name, Vector3 a, Vector3 b, float width, float height, Material material,
            int layer, string tag, bool addCollider)
        {
            var direction = (b - a).normalized;
            var length = Vector3.Distance(a, b) + width * 0.35f;
            var mid = (a + b) * 0.5f;
            CreateSegment(name, mid, Quaternion.LookRotation(direction), new Vector3(width, height, length),
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
