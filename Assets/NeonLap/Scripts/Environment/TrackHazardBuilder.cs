using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Rendering;
using NeonLap.Track;
using NeonLap.UI;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    public class TrackHazardBuilder : MonoBehaviour
    {
        static readonly Color[] PatrolBodyColors =
        {
            new(0.55f, 0.55f, 0.12f),
            new(0.12f, 0.42f, 0.55f),
            new(0.48f, 0.12f, 0.48f),
        };

        static readonly Color[] PatrolAccentColors =
        {
            new(3.5f, 3f, 0.3f),
            new(0.4f, 3.5f, 4f),
            new(3.5f, 0.5f, 3.5f),
        };

        Transform hazardRoot;
        Material barrelMat;
        Material crateMat;
        Material coneMat;
        Material debrisMat;
        Material foliageMat;
        Material trunkMat;
        Material bodyTemplate;
        Material accentTemplate;

        public void Build(IReadOnlyList<Transform> waypoints, TrackDefinition definition, Material carBody,
            Material carAccent)
        {
            if (waypoints == null || waypoints.Count < 8)
                return;

            var levelIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var density = TrackLevelConfig.GetHazardDensity(levelIndex, GameQualitySettings.Preset.HazardDensity);
            var movingDensity =
                TrackLevelConfig.GetMovingHazardDensity(levelIndex, GameQualitySettings.Preset.HazardDensity);
            if (density <= 0.01f)
                return;

            if (hazardRoot != null)
                Destroy(hazardRoot.gameObject);

            MinimapHazardRegistry.Clear();
            hazardRoot = new GameObject("TrackHazards").transform;
            hazardRoot.SetParent(transform, false);

            if (GameQualitySettings.UseGpuInstancing)
                InstancedPropRenderer.Ensure(hazardRoot);

            bodyTemplate = carBody;
            accentTemplate = carAccent;
            CreateMaterials();

            var trackWidth = definition != null ? definition.trackWidth : 26f;
            var startSkip = Mathf.Max(28, waypoints.Count / 2);
            var hazardIndices = ResolveStaticHazardIndices(waypoints.Count, startSkip, density, definition);

            SpawnStaticHazards(waypoints, hazardIndices, trackWidth);
            SpawnMovingHazards(waypoints, startSkip, trackWidth, movingDensity);
        }

        void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material Make(Color color, Color emission, float emissionIntensity = 0f)
            {
                var mat = new Material(lit);
                mat.enableInstancing = true;
                mat.SetColor("_BaseColor", color);
                if (emissionIntensity > 0f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emission * emissionIntensity);
                }

                return mat;
            }

            barrelMat = Make(new Color(0.85f, 0.45f, 0.08f), new Color(1f, 0.55f, 0.1f), 0.35f);
            crateMat = Make(new Color(0.35f, 0.22f, 0.12f), Color.black);
            coneMat = Make(new Color(1f, 0.35f, 0.08f), new Color(1f, 0.4f, 0.05f), 0.5f);
            debrisMat = Make(new Color(0.15f, 0.35f, 0.42f), new Color(0.2f, 0.9f, 1f), 1.2f);
            foliageMat = Make(new Color(0.05f, 0.22f, 0.14f), new Color(0.1f, 0.8f, 0.35f), 0.6f);
            trunkMat = Make(new Color(0.12f, 0.08f, 0.06f), Color.black);
        }

        static List<int> ResolveStaticHazardIndices(int waypointCount, int startSkip, float density,
            TrackDefinition definition)
        {
            if (definition != null && definition.useAuthoringHazardIndices
                && definition.hazardWaypointIndices != null && definition.hazardWaypointIndices.Length > 0)
            {
                var authored = new List<int>();
                foreach (var index in definition.hazardWaypointIndices)
                {
                    if (index < startSkip || index >= waypointCount - 2)
                        continue;

                    if (!authored.Contains(index))
                        authored.Add(index);
                }

                if (authored.Count > 0)
                    return authored;
            }

            return PickHazardIndices(waypointCount, startSkip, density);
        }

        static List<int> PickHazardIndices(int waypointCount, int startSkip, float density)
        {
            var indices = new List<int>();
            var divisor = Mathf.Max(6, Mathf.RoundToInt(14f / Mathf.Max(density, 0.2f)));
            var step = Mathf.Max(4, waypointCount / divisor);
            for (var i = startSkip; i < waypointCount - 2; i += step)
                indices.Add(i);

            return indices;
        }

        void SpawnStaticHazards(IReadOnlyList<Transform> waypoints, List<int> indices, float trackWidth)
        {
            var random = new System.Random(424242);
            for (var i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                var side = random.NextDouble() < 0.5 ? -1f : 1f;
                var lateral = side * (trackWidth * 0.32f + (float)random.NextDouble() * trackWidth * 0.08f);
                var pos = GetTrackPoint(waypoints, index, lateral);
                var rot = GetTrackRotation(waypoints, index);

                switch (i % 5)
                {
                    case 0:
                        SpawnBarrel(pos, rot);
                        MinimapHazardRegistry.Register(MinimapHazardKind.Barrel, pos, index);
                        break;
                    case 1:
                        SpawnCrateStack(pos, rot);
                        MinimapHazardRegistry.Register(MinimapHazardKind.Crate, pos, index);
                        break;
                    case 2:
                        SpawnConeCluster(pos, rot, random);
                        MinimapHazardRegistry.Register(MinimapHazardKind.Cone, pos, index);
                        break;
                    case 3:
                        SpawnTrackTree(pos, rot, random);
                        break;
                    default:
                        SpawnDebrisChunk(pos, rot, random);
                        MinimapHazardRegistry.Register(MinimapHazardKind.Debris, pos, index);
                        break;
                }
            }
        }

        void SpawnMovingHazards(IReadOnlyList<Transform> waypoints, int startSkip, float trackWidth, float density)
        {
            var count = waypoints.Count;
            var patrolCount = density >= 0.85f ? 3 : density >= 0.5f ? 2 : 1;
            var patrolSlots = new[]
            {
                startSkip + count / 2,
                startSkip + count * 2 / 3,
                startSkip + count * 5 / 6,
            };

            for (var i = 0; i < patrolCount; i++)
            {
                var index = patrolSlots[i] % count;
                var forward = GetForward(waypoints, index);
                var center = GetTrackPoint(waypoints, index, 0f);
                var patrolSpan = Mathf.Min(22f, Vector3.Distance(waypoints[index].position,
                    waypoints[(index + 6) % count].position) * 0.45f);
                var start = ObstaclePhysics.SnapToTrackSurface(center - forward * patrolSpan, 0.75f);
                var end = ObstaclePhysics.SnapToTrackSurface(center + forward * patrolSpan, 0.75f);
                SpawnPatrolCar(start, end, 10f + i * 1.5f, i);
            }

            if (density >= 0.45f)
            {
                var slideCount = density >= 0.85f ? 2 : 1;
                var slideIndices = new[] { startSkip + count * 3 / 5, startSkip + count * 4 / 5 };
                for (var i = 0; i < slideCount; i++)
                {
                    var index = slideIndices[i] % count;
                    var center = ObstaclePhysics.SnapToTrackSurface(GetTrackPoint(waypoints, index, 0f), 0.45f);
                    var right = GetRight(waypoints, index);
                    SpawnSlidingBarrier(center, right, trackWidth * 0.38f, 2.2f + i * 0.4f, i * 1.7f);
                }
            }

            if (density >= 0.45f)
            {
                var barrelCount = density >= 0.85f ? 3 : density >= 0.65f ? 2 : 1;
                var barrelIndices = new[]
                {
                    (startSkip + count * 3 / 5) % count,
                    (startSkip + count * 4 / 5) % count,
                    (startSkip + count * 7 / 8) % count,
                };

                for (var i = 0; i < barrelCount; i++)
                {
                    var barrelIndex = barrelIndices[i] % count;
                    var lateral = (i % 2 == 0 ? 1f : -1f) * trackWidth * 0.14f;
                    var barrelCenter = ObstaclePhysics.SnapToTrackSurface(
                        GetTrackPoint(waypoints, barrelIndex, lateral), 0.9f);
                    var barrelAxis = GetForward(waypoints, barrelIndex);
                    SpawnRollingBarrel(barrelCenter, barrelAxis, 6.5f + i * 1.2f, 1.6f + i * 0.25f, i * 0.9f);
                }
            }
        }

        void SpawnBarrel(Vector3 position, Quaternion rotation)
        {
            var snapped = ObstaclePhysics.SnapToTrackSurface(position, 0.9f);
            var scale = new Vector3(0.75f, 0.9f, 0.75f);
            if (TrySpawnInstancedHazard("HazardBarrel", snapped, scale, rotation, barrelMat, HazardKind.Cylinder))
                return;

            CreateHazardPrimitive(PrimitiveType.Cylinder, "HazardBarrel", snapped, scale, rotation, barrelMat);
        }

        void SpawnCrateStack(Vector3 position, Quaternion rotation)
        {
            var crateA = ObstaclePhysics.SnapToTrackSurface(position, 0.45f);
            var scaleA = new Vector3(1.1f, 0.9f, 1.1f);
            if (!TrySpawnInstancedHazard("CrateA", crateA, scaleA, rotation, crateMat, HazardKind.Cube))
                CreateHazardPrimitive(PrimitiveType.Cube, "CrateA", crateA, scaleA, rotation, crateMat);

            var crateB = ObstaclePhysics.SnapToTrackSurface(position + rotation * Vector3.right * 0.15f, 0.95f);
            var scaleB = new Vector3(0.85f, 0.75f, 0.85f);
            if (!TrySpawnInstancedHazard("CrateB", crateB, scaleB, rotation, crateMat, HazardKind.Cube))
                CreateHazardPrimitive(PrimitiveType.Cube, "CrateB", crateB, scaleB, rotation, crateMat);
        }

        void SpawnConeCluster(Vector3 position, Quaternion rotation, System.Random random)
        {
            for (var i = 0; i < 3; i++)
            {
                var offset = rotation * new Vector3((i - 1) * 1.1f, 0f, (float)random.NextDouble() * 0.6f);
                var snapped = ObstaclePhysics.SnapToTrackSurface(position + offset, 0.55f);
                var scale = new Vector3(0.55f, 0.55f, 0.55f);
                if (TrySpawnInstancedHazard("Cone_" + i, snapped, scale, rotation, coneMat, HazardKind.Cylinder))
                    continue;

                CreateHazardPrimitive(PrimitiveType.Cylinder, "Cone_" + i, snapped, scale, rotation, coneMat);
            }
        }

        void SpawnTrackTree(Vector3 position, Quaternion rotation, System.Random random)
        {
            var scale = 0.85f + (float)random.NextDouble() * 0.25f;
            CreateHazardPrimitive(PrimitiveType.Cylinder, "Trunk",
                ObstaclePhysics.SnapToTrackSurface(position, 1.1f * scale),
                new Vector3(0.4f * scale, 1.1f * scale, 0.4f * scale), rotation, trunkMat);
            CreateHazardPrimitive(PrimitiveType.Sphere, "Canopy",
                ObstaclePhysics.SnapToTrackSurface(position, 2.6f * scale),
                new Vector3(2f * scale, 2.2f * scale, 2f * scale), rotation, foliageMat);
        }

        void SpawnDebrisChunk(Vector3 position, Quaternion rotation, System.Random random)
        {
            var chunkRotation = rotation * Quaternion.Euler(0f, (float)random.NextDouble() * 25f, 8f);
            var snapped = ObstaclePhysics.SnapToTrackSurface(position, 0.35f);
            var scale = new Vector3(1.4f, 0.7f, 1.1f);
            if (TrySpawnInstancedHazard("NeonDebris", snapped, scale, chunkRotation, debrisMat, HazardKind.Cube))
                return;

            CreateHazardPrimitive(PrimitiveType.Cube, "NeonDebris", snapped, scale, chunkRotation, debrisMat);
        }

        enum HazardKind
        {
            Cube,
            Cylinder,
        }

        bool TrySpawnInstancedHazard(string name, Vector3 position, Vector3 scale, Quaternion rotation,
            Material material, HazardKind kind)
        {
            var instancer = InstancedPropRenderer.Instance;
            if (instancer == null || !GameQualitySettings.UseGpuInstancing)
                return false;

            var proxy = new GameObject(name);
            proxy.transform.SetParent(hazardRoot, false);
            proxy.transform.SetPositionAndRotation(position, rotation);
            proxy.transform.localScale = Vector3.one;

            var collider = proxy.AddComponent<BoxCollider>();
            collider.size = scale;
            collider.center = Vector3.up * (scale.y * 0.5f);
            ObstaclePhysics.ConfigureStaticObstacle(proxy);

            switch (kind)
            {
                case HazardKind.Cylinder:
                    instancer.AddCylinder(position, rotation, scale, material);
                    break;
                default:
                    instancer.AddCube(position, rotation, scale, material);
                    break;
            }

            return true;
        }

        void SpawnSquareCrossPatrols(IReadOnlyList<Transform> waypoints, float trackWidth)
        {
            var midpoints = FindStraightMidpointIndices(waypoints);
            var crossSpan = trackWidth * 0.58f;
            var speed = 14f;
            for (var i = 0; i < midpoints.Count && i < 4; i++)
            {
                var index = midpoints[i];
                var center = ObstaclePhysics.SnapToTrackSurface(GetTrackPoint(waypoints, index, 0f), 0.75f);
                var right = GetRight(waypoints, index);
                var start = ObstaclePhysics.SnapToTrackSurface(center - right * crossSpan, 0.75f);
                var end = ObstaclePhysics.SnapToTrackSurface(center + right * crossSpan, 0.75f);
                SpawnPatrolCar(start, end, speed + i * 1.4f, 20 + i);
                MinimapHazardRegistry.Register(MinimapHazardKind.Barrel, center, index);
            }
        }

        static List<int> FindStraightMidpointIndices(IReadOnlyList<Transform> waypoints, int minStraightPoints = 7,
            float maxTurnDegrees = 11f)
        {
            var results = new List<int>();
            if (waypoints == null || waypoints.Count < minStraightPoints + 2)
                return results;

            var cosThreshold = Mathf.Cos(maxTurnDegrees * Mathf.Deg2Rad);
            var count = waypoints.Count;
            var index = 0;
            while (index < count)
            {
                var runStart = index;
                var runForward = GetForward(waypoints, index);
                var runEnd = index + 1;
                while (runEnd < count)
                {
                    var forward = GetForward(waypoints, runEnd);
                    if (Vector3.Dot(runForward, forward) < cosThreshold)
                        break;

                    runEnd++;
                }

                var runLength = runEnd - runStart;
                if (runLength >= minStraightPoints)
                {
                    results.Add(runStart + runLength / 2);
                    index = runEnd;
                }
                else
                {
                    index++;
                }
            }

            return results;
        }

        void SpawnPatrolCar(Vector3 start, Vector3 end, float speed, int colorIndex)
        {
            var car = new GameObject("PatrolNpc_" + colorIndex);
            car.transform.SetParent(hazardRoot, false);
            car.layer = NeonLapLayers.Obstacle;
            car.tag = "Barrier";

            var bodyColor = PatrolBodyColors[colorIndex % PatrolBodyColors.Length];
            var accentColor = PatrolAccentColors[colorIndex % PatrolAccentColors.Length];
            if (bodyTemplate != null && accentTemplate != null)
            {
                HoverCarVisualBuilder.Build(car.transform,
                    new HoverCarVisualBuilder.BuildArgs(bodyTemplate, accentTemplate, bodyColor, accentColor));
            }

            var rb = car.AddComponent<Rigidbody>();
            ObstaclePhysics.ConfigureMovingObstacle(rb);

            var collider = car.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.7f, 0.75f, 2.9f);
            collider.center = new Vector3(0f, 0.35f, 0f);
            ObstaclePhysics.ApplyColliderMaterial(collider);

            car.transform.position = start;
            car.transform.rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);

            var patrol = car.AddComponent<PatrolNpcVehicle>();
            patrol.Configure(start, end, speed);
        }

        void SpawnSlidingBarrier(Vector3 center, Vector3 slideAxis, float slideDistance, float speed, float phase)
        {
            var barrier = CreateHazardPrimitive(PrimitiveType.Cube, "SlidingBarrier", center,
                new Vector3(2.4f, 0.9f, 0.8f), Quaternion.LookRotation(slideAxis, Vector3.up), debrisMat);
            var rb = barrier.GetComponent<Rigidbody>();
            ObstaclePhysics.ConfigureMovingObstacle(rb);
            barrier.AddComponent<MovingTrackObstacle>()
                .Configure(center, slideAxis, slideDistance, speed, phase);
        }

        void SpawnRollingBarrel(Vector3 center, Vector3 moveAxis, float distance, float speed, float phase)
        {
            var barrel = CreateHazardPrimitive(PrimitiveType.Cylinder, "RollingBarrel", center,
                new Vector3(0.8f, 0.9f, 0.8f), Quaternion.identity, barrelMat);
            var rb = barrel.GetComponent<Rigidbody>();
            ObstaclePhysics.ConfigureMovingObstacle(rb);
            barrel.AddComponent<RollingBarrelImpact>();
            barrel.AddComponent<MovingTrackObstacle>()
                .Configure(center, moveAxis, distance, speed, phase, enableRollVisual: true);
        }

        static Vector3 GetTrackPoint(IReadOnlyList<Transform> waypoints, int index, float lateral)
        {
            var wp = waypoints[index];
            var forward = GetForward(waypoints, index);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var point = wp.position + right * lateral;
            point.y = ObstaclePhysics.TrackSurfaceHeight;
            return point;
        }

        static Quaternion GetTrackRotation(IReadOnlyList<Transform> waypoints, int index)
        {
            return Quaternion.LookRotation(GetForward(waypoints, index), Vector3.up);
        }

        static Vector3 GetForward(IReadOnlyList<Transform> waypoints, int index)
        {
            var current = waypoints[index].position;
            var next = waypoints[(index + 1) % waypoints.Count].position;
            var forward = next - current;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.01f ? forward.normalized : waypoints[index].forward;
        }

        static Vector3 GetRight(IReadOnlyList<Transform> waypoints, int index)
        {
            return Vector3.Cross(Vector3.up, GetForward(waypoints, index)).normalized;
        }

        GameObject CreateHazardPrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale,
            Quaternion rotation, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(hazardRoot, false);
            go.transform.localScale = scale;
            go.transform.rotation = rotation;
            go.transform.position = position;

            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;

            ObstaclePhysics.ConfigureStaticObstacle(go);
            return go;
        }
    }
}
