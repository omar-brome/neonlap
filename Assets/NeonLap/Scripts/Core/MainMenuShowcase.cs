using System.Collections.Generic;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Core
{
    public class MainMenuShowcase : MonoBehaviour
    {
        static readonly Color[] BodyColors =
        {
            new(0.1f, 0.35f, 0.45f),
            new(0.45f, 0.08f, 0.08f),
            new(0.45f, 0.22f, 0.05f),
            new(0.08f, 0.15f, 0.42f),
            new(0.28f, 0.08f, 0.42f),
            new(0.08f, 0.38f, 0.12f),
        };

        static readonly Color[] AccentColors =
        {
            new(0f, 3.5f, 4f),
            new(4f, 0.3f, 0.3f),
            new(4f, 1.6f, 0.2f),
            new(0.5f, 1.2f, 4f),
            new(2.5f, 0.4f, 4f),
            new(0.4f, 4f, 0.8f),
        };

        Transform showcaseRoot;
        Material bodyTemplate;
        Material accentTemplate;
        Material trackSurfaceMat;
        Material trackEdgeMat;
        Material cityMat;

        public void Build()
        {
            if (showcaseRoot != null)
                Destroy(showcaseRoot.gameObject);

            CreateMaterials();
            showcaseRoot = new GameObject("MenuShowcase").transform;
            showcaseRoot.SetParent(transform, false);

            SetupCamera();

            var centerline = BuildCenterline(52f, 18f);
            BuildTrack(centerline);
            BuildCityBackdrop();
            BuildShowcaseLights(centerline);
            SpawnRacingCars(centerline);
            ApplyShowcaseEnvironment();
        }

        void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            bodyTemplate = new Material(lit);
            bodyTemplate.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f));
            bodyTemplate.SetFloat("_Metallic", 0.55f);
            bodyTemplate.SetFloat("_Smoothness", 0.72f);

            accentTemplate = new Material(lit);
            accentTemplate.SetColor("_BaseColor", new Color(0.05f, 0.2f, 0.25f));
            accentTemplate.EnableKeyword("_EMISSION");
            accentTemplate.SetColor("_EmissionColor", new Color(0f, 2.5f, 3f));
            accentTemplate.SetFloat("_Smoothness", 0.85f);

            trackSurfaceMat = new Material(lit);
            trackSurfaceMat.SetColor("_BaseColor", new Color(0.07f, 0.05f, 0.11f));
            trackSurfaceMat.SetFloat("_Smoothness", 0.35f);

            trackEdgeMat = new Material(lit);
            trackEdgeMat.SetColor("_BaseColor", new Color(0.05f, 0.18f, 0.22f));
            trackEdgeMat.EnableKeyword("_EMISSION");
            trackEdgeMat.SetColor("_EmissionColor", new Color(0.15f, 1.2f, 1.5f));
            trackEdgeMat.SetFloat("_Smoothness", 0.9f);

            cityMat = new Material(lit);
            cityMat.SetColor("_BaseColor", new Color(0.04f, 0.05f, 0.09f));
            cityMat.EnableKeyword("_EMISSION");
            cityMat.SetColor("_EmissionColor", new Color(0.08f, 0.25f, 0.45f));
        }

        void ApplyShowcaseEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.08f, 0.1f, 0.18f);
            RenderSettings.ambientEquatorColor = new Color(0.05f, 0.06f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.02f, 0.02f, 0.05f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.02f, 0.01f, 0.06f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.008f;
        }

        void SetupCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<UnityEngine.Camera>();
                cam.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(-6f, 11f, -34f);
            cam.transform.rotation = Quaternion.Euler(12f, 18f, 0f);

            if (cam.GetComponent<MainMenuCameraDrift>() == null)
                cam.gameObject.AddComponent<MainMenuCameraDrift>();
        }

        void SpawnRacingCars(List<Vector3> centerline)
        {
            var path = centerline.ToArray();
            var carRoot = new GameObject("ShowcaseCars").transform;
            carRoot.SetParent(showcaseRoot, false);

            for (var i = 0; i < BodyColors.Length; i++)
            {
                var car = new GameObject("ShowcaseCar_" + (i + 1));
                car.transform.SetParent(carRoot, false);
                car.transform.localScale = Vector3.one * 1.15f;

                HoverCarVisualBuilder.Build(car.transform,
                    new HoverCarVisualBuilder.BuildArgs(bodyTemplate, accentTemplate, BodyColors[i], AccentColors[i]));

                var racer = car.AddComponent<MainMenuCarRacer>();
                racer.Configure(path, i / (float)BodyColors.Length, 0.055f + i * 0.004f, 1.35f);

                AddCarTrail(car.transform, AccentColors[i]);
            }
        }

        void AddCarTrail(Transform car, Color accent)
        {
            var trailGo = new GameObject("SpeedTrail");
            trailGo.transform.SetParent(car, false);
            trailGo.transform.localPosition = new Vector3(0f, 0.25f, -1.35f);

            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.45f;
            trail.startWidth = 0.55f;
            trail.endWidth = 0.02f;
            trail.minVertexDistance = 0.08f;
            trail.material = accentTemplate;
            trail.startColor = new Color(accent.r, accent.g, accent.b, 0.85f);
            trail.endColor = new Color(accent.r, accent.g, accent.b, 0f);
        }

        void BuildTrack(List<Vector3> centerline)
        {
            const float trackWidth = 12f;
            var trackRoot = new GameObject("ShowcaseTrack").transform;
            trackRoot.SetParent(showcaseRoot, false);

            for (var i = 0; i < centerline.Count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Count];
                CreateTrackSegment(trackRoot, a, b, trackWidth, trackSurfaceMat);
                CreateEdgeStrip(trackRoot, a, b, trackWidth * 0.5f - 0.25f, trackEdgeMat);
                CreateEdgeStrip(trackRoot, a, b, -(trackWidth * 0.5f - 0.25f), trackEdgeMat);
            }

            BuildStartLine(trackRoot, centerline[0], centerline[1], trackWidth);
        }

        void BuildStartLine(Transform parent, Vector3 start, Vector3 next, float trackWidth)
        {
            var forward = (next - start).normalized;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "StartLine";
            stripe.transform.SetParent(parent, false);
            stripe.transform.position = start + Vector3.up * 0.12f;
            stripe.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            stripe.transform.localScale = new Vector3(trackWidth * 0.85f, 0.04f, 0.6f);
            Object.Destroy(stripe.GetComponent<Collider>());
            stripe.GetComponent<Renderer>().sharedMaterial = trackEdgeMat;
        }

        void BuildCityBackdrop()
        {
            var cityRoot = new GameObject("CityBackdrop").transform;
            cityRoot.SetParent(showcaseRoot, false);
            var random = new System.Random(1337);

            for (var i = 0; i < 22; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = 38f + (float)random.NextDouble() * 28f;
                var height = 10f + (float)random.NextDouble() * 28f;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius * 0.75f;

                var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "MenuBuilding_" + i;
                building.transform.SetParent(cityRoot, false);
                building.transform.position = new Vector3(x, height * 0.5f, z);
                building.transform.localScale = new Vector3(
                    3f + (float)random.NextDouble() * 5f,
                    height,
                    3f + (float)random.NextDouble() * 5f);
                Object.Destroy(building.GetComponent<Collider>());
                building.GetComponent<Renderer>().sharedMaterial = cityMat;
            }
        }

        void BuildShowcaseLights(List<Vector3> centerline)
        {
            var lightsRoot = new GameObject("ShowcaseLights").transform;
            lightsRoot.SetParent(showcaseRoot, false);

            for (var i = 0; i < centerline.Count; i += 4)
            {
                var point = centerline[i] + Vector3.up * 7f;
                var lightGo = new GameObject("TrackLight_" + i);
                lightGo.transform.SetParent(lightsRoot, false);
                lightGo.transform.position = point;

                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.55f, 0.9f, 1f);
                light.intensity = 2.4f;
                light.range = 28f;
            }

            var keyGo = new GameObject("KeyLight");
            keyGo.transform.SetParent(lightsRoot, false);
            keyGo.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.65f, 0.78f, 1f);
            key.intensity = 0.55f;
        }

        static void CreateTrackSegment(Transform parent, Vector3 a, Vector3 b, float width, Material material)
        {
            var direction = (b - a).normalized;
            var length = Vector3.Distance(a, b) + width * 0.2f;
            var mid = (a + b) * 0.5f;

            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "TrackSurface";
            segment.transform.SetParent(parent, false);
            segment.transform.position = mid + Vector3.up * 0.08f;
            segment.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            segment.transform.localScale = new Vector3(width, 0.16f, length);
            Object.Destroy(segment.GetComponent<Collider>());
            segment.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void CreateEdgeStrip(Transform parent, Vector3 a, Vector3 b, float lateralOffset, Material material)
        {
            var direction = (b - a).normalized;
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var length = Vector3.Distance(a, b) + 1.5f;
            var mid = (a + b) * 0.5f + right * lateralOffset;

            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "TrackEdge";
            strip.transform.SetParent(parent, false);
            strip.transform.position = mid + Vector3.up * 0.14f;
            strip.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            strip.transform.localScale = new Vector3(0.35f, 0.08f, length);
            Object.Destroy(strip.GetComponent<Collider>());
            strip.GetComponent<Renderer>().sharedMaterial = material;
        }

        static List<Vector3> BuildCenterline(float straightLength, float turnRadius)
        {
            var points = new List<Vector3>();
            var half = straightLength * 0.5f;

            AppendStraight(points, new Vector3(-half, 0f, turnRadius), new Vector3(half, 0f, turnRadius), 4);
            AppendArc(points, new Vector3(half, 0f, 0f), turnRadius, 90f, -90f, 12);
            AppendStraight(points, new Vector3(half, 0f, -turnRadius), new Vector3(-half, 0f, -turnRadius), 4);
            AppendArc(points, new Vector3(-half, 0f, 0f), turnRadius, -90f, -270f, 12);

            return points;
        }

        static void AppendStraight(List<Vector3> points, Vector3 from, Vector3 to, int subdivisions)
        {
            var startStep = points.Count > 0 ? 1 : 0;
            for (var i = startStep; i <= subdivisions; i++)
            {
                var t = i / (float)subdivisions;
                points.Add(Vector3.Lerp(from, to, t));
            }
        }

        static void AppendArc(List<Vector3> points, Vector3 center, float radius, float startDegrees,
            float endDegrees, int segments)
        {
            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                points.Add(new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    0f,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }
    }
}
