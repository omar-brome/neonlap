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
        Transform playerShowcaseCar;
        Material bodyTemplate;
        Material accentTemplate;
        Material trackSurfaceMat;
        Material trackEdgeMat;
        Material cityMat;
        Material archMat;
        Vector3[] showcasePath;

        public void Build()
        {
            if (showcaseRoot != null)
                Destroy(showcaseRoot.gameObject);

            CreateMaterials();
            showcaseRoot = new GameObject("MenuShowcase").transform;
            showcaseRoot.SetParent(transform, false);

            showcasePath = BuildDramaticCenterline();
            BuildGroundPlane();
            BuildTrack(showcasePath);
            BuildCityBackdrop();
            BuildNeonArch(showcasePath);
            BuildShowcaseLights(showcasePath);
            SpawnShowcaseCars(showcasePath);
            ApplyShowcaseEnvironment();

            var pulse = showcaseRoot.gameObject.AddComponent<MainMenuShowcasePulse>();
            pulse.Configure(trackEdgeMat);

            SetupOrbitCamera();
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

            archMat = new Material(lit);
            archMat.SetColor("_BaseColor", new Color(0.04f, 0.08f, 0.12f));
            archMat.EnableKeyword("_EMISSION");
            archMat.SetColor("_EmissionColor", new Color(0.2f, 2.8f, 3.5f));
            archMat.SetFloat("_Smoothness", 0.92f);
        }

        void ApplyShowcaseEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.06f, 0.08f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.04f, 0.05f, 0.11f);
            RenderSettings.ambientGroundColor = new Color(0.015f, 0.015f, 0.04f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.015f, 0.008f, 0.05f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0065f;
        }

        void SetupOrbitCamera()
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
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 450f;

            var drift = cam.GetComponent<MainMenuCameraDrift>();
            if (drift != null)
                Destroy(drift);

            var orbit = cam.GetComponent<MainMenuOrbitCamera>();
            if (orbit == null)
                orbit = cam.gameObject.AddComponent<MainMenuOrbitCamera>();

            if (playerShowcaseCar != null)
                orbit.Configure(playerShowcaseCar);
        }

        void SpawnShowcaseCars(Vector3[] path)
        {
            var carRoot = new GameObject("ShowcaseCars").transform;
            carRoot.SetParent(showcaseRoot, false);

            SpawnHeroCar(carRoot, path);
            SpawnBackgroundRacers(carRoot, path);
        }

        void SpawnHeroCar(Transform carRoot, Vector3[] path)
        {
            var heroProgress = 0.08f;
            var heroPosition = SamplePath(path, heroProgress);
            var heroLook = SamplePath(path, heroProgress + 0.02f);
            var heroForward = heroLook - heroPosition;
            heroForward.y = 0f;
            if (heroForward.sqrMagnitude < 0.0001f)
                heroForward = Vector3.forward;
            var heroRotation = Quaternion.LookRotation(heroForward.normalized, Vector3.up);

            var car = new GameObject("HeroCar");
            car.transform.SetParent(carRoot, false);
            car.transform.localScale = Vector3.one * 1.22f;

            var build = PlayerGarageStore.GetSelectedBuild();
            Color body;
            Color accent;
            HoverCarVisualBuilder.BuildArgs buildArgs;
            if (build != null)
            {
                buildArgs = VehicleCustomizationStore.CreateBuildArgs(bodyTemplate, accentTemplate, build, true);
                VehicleCustomizationStore.GetResolvedColors(build, out body, out accent);
            }
            else
            {
                body = PlayerVehicleProfileStore.GetBodyColor(PlayerVehicleProfileStore.SelectedKind);
                accent = PlayerVehicleProfileStore.GetAccentColor(PlayerVehicleProfileStore.SelectedKind);
                buildArgs = VehicleCustomizationStore.CreateBuildArgs(bodyTemplate, accentTemplate, body, accent, true);
            }

            HoverCarVisualBuilder.Build(car.transform, buildArgs);
            AddCarTrail(car.transform, accent);

            var idle = car.AddComponent<MainMenuHeroCarIdle>();
            idle.SnapToPose(heroPosition + Vector3.up * 1.38f, heroRotation);
            playerShowcaseCar = car.transform;
        }

        void SpawnBackgroundRacers(Transform carRoot, Vector3[] path)
        {
            const int backgroundCount = 4;
            for (var i = 0; i < backgroundCount; i++)
            {
                var paletteIndex = (i + 1) % BodyColors.Length;
                var car = new GameObject("BackgroundCar_" + (i + 1));
                car.transform.SetParent(carRoot, false);
                car.transform.localScale = Vector3.one * 1.05f;

                var body = BodyColors[paletteIndex];
                var accent = AccentColors[paletteIndex];
                var buildArgs = new HoverCarVisualBuilder.BuildArgs(bodyTemplate, accentTemplate, body, accent);
                HoverCarVisualBuilder.Build(car.transform, buildArgs);

                var racer = car.AddComponent<MainMenuCarRacer>();
                var start = 0.22f + i * 0.17f;
                racer.Configure(path, start, 0.045f + i * 0.006f, 1.32f);
                AddCarTrail(car.transform, accent);
            }
        }

        static Vector3 SamplePath(Vector3[] path, float t)
        {
            if (path == null || path.Length < 2)
                return Vector3.zero;

            var scaled = Mathf.Repeat(t, 1f) * path.Length;
            var index = Mathf.FloorToInt(scaled) % path.Length;
            var nextIndex = (index + 1) % path.Length;
            var localT = scaled - Mathf.Floor(scaled);
            return Vector3.Lerp(path[index], path[nextIndex], localT);
        }

        public void ApplyGaragePreview(HoverBuildDefinition build)
        {
            if (playerShowcaseCar == null || build == null)
                return;

            var visual = playerShowcaseCar.Find("Visual");
            if (visual != null)
                Destroy(visual.gameObject);

            HoverCarVisualBuilder.Build(playerShowcaseCar,
                VehicleCustomizationStore.CreateBuildArgs(bodyTemplate, accentTemplate, build, true));

            var orbit = Object.FindAnyObjectByType<MainMenuOrbitCamera>();
            orbit?.Configure(playerShowcaseCar);
        }

        public void ApplyProfilePreview(VehicleProfileKind kind)
        {
            if (playerShowcaseCar == null)
                return;

            var visual = playerShowcaseCar.Find("Visual");
            if (visual != null)
                Destroy(visual.gameObject);

            HoverCarVisualBuilder.Build(playerShowcaseCar,
                new HoverCarVisualBuilder.BuildArgs(
                    bodyTemplate,
                    accentTemplate,
                    PlayerVehicleProfileStore.GetBodyColor(kind),
                    PlayerVehicleProfileStore.GetAccentColor(kind)));
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

        void BuildGroundPlane()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "ShowcaseGround";
            ground.transform.SetParent(showcaseRoot, false);
            ground.transform.position = new Vector3(0f, -0.35f, 0f);
            ground.transform.localScale = new Vector3(140f, 0.08f, 110f);
            Object.Destroy(ground.GetComponent<Collider>());

            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.SetColor("_BaseColor", new Color(0.025f, 0.02f, 0.05f));
            groundMat.SetFloat("_Smoothness", 0.2f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        }

        void BuildNeonArch(Vector3[] centerline)
        {
            if (centerline == null || centerline.Length < 2)
                return;

            var archRoot = new GameObject("HeroArch").transform;
            archRoot.SetParent(showcaseRoot, false);

            var start = SamplePath(centerline, 0.08f);
            var next = SamplePath(centerline, 0.1f);
            var forward = (next - start).normalized;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var archCenter = start + Vector3.up * 0.2f;

            CreateArchPillar(archRoot, archCenter + right * 7.2f, forward, 9.5f);
            CreateArchPillar(archRoot, archCenter - right * 7.2f, forward, 9.5f);

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "ArchBeam";
            beam.transform.SetParent(archRoot, false);
            beam.transform.position = archCenter + Vector3.up * 9.2f;
            beam.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            beam.transform.localScale = new Vector3(15.5f, 0.45f, 0.7f);
            Object.Destroy(beam.GetComponent<Collider>());
            beam.GetComponent<Renderer>().sharedMaterial = archMat;
        }

        void CreateArchPillar(Transform parent, Vector3 basePosition, Vector3 forward, float height)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "ArchPillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = basePosition + Vector3.up * (height * 0.5f);
            pillar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            pillar.transform.localScale = new Vector3(0.55f, height, 0.55f);
            Object.Destroy(pillar.GetComponent<Collider>());
            pillar.GetComponent<Renderer>().sharedMaterial = archMat;
        }

        void BuildTrack(Vector3[] centerline)
        {
            const float trackWidth = 13f;
            var trackRoot = new GameObject("ShowcaseTrack").transform;
            trackRoot.SetParent(showcaseRoot, false);

            for (var i = 0; i < centerline.Length; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Length];
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

            for (var i = 0; i < 32; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = 42f + (float)random.NextDouble() * 36f;
                var height = 12f + (float)random.NextDouble() * 34f;
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

        void BuildShowcaseLights(Vector3[] centerline)
        {
            var lightsRoot = new GameObject("ShowcaseLights").transform;
            lightsRoot.SetParent(showcaseRoot, false);

            for (var i = 0; i < centerline.Length; i += 3)
            {
                var point = centerline[i] + Vector3.up * 8f;
                var lightGo = new GameObject("TrackLight_" + i);
                lightGo.transform.SetParent(lightsRoot, false);
                lightGo.transform.position = point;

                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.55f, 0.9f, 1f);
                light.intensity = 2.8f;
                light.range = 32f;
            }

            var heroPoint = SamplePath(centerline, 0.08f) + Vector3.up * 2.5f;
            var heroLightGo = new GameObject("HeroSpotlight");
            heroLightGo.transform.SetParent(lightsRoot, false);
            heroLightGo.transform.position = heroPoint + new Vector3(4f, 6f, -5f);
            heroLightGo.transform.LookAt(heroPoint + Vector3.up * 1.2f);
            var heroLight = heroLightGo.AddComponent<Light>();
            heroLight.type = LightType.Spot;
            heroLight.color = new Color(0.75f, 0.95f, 1f);
            heroLight.intensity = 3.2f;
            heroLight.range = 40f;
            heroLight.spotAngle = 48f;
            heroLight.innerSpotAngle = 18f;

            var rimGo = new GameObject("HeroRimLight");
            rimGo.transform.SetParent(lightsRoot, false);
            rimGo.transform.position = heroPoint + new Vector3(-7f, 4f, 6f);
            rimGo.transform.LookAt(heroPoint + Vector3.up * 1f);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Spot;
            rim.color = new Color(1f, 0.45f, 0.85f);
            rim.intensity = 2.1f;
            rim.range = 35f;
            rim.spotAngle = 55f;

            var keyGo = new GameObject("KeyLight");
            keyGo.transform.SetParent(lightsRoot, false);
            keyGo.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.7f, 0.82f, 1f);
            key.intensity = 0.62f;

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(lightsRoot, false);
            fillGo.transform.rotation = Quaternion.Euler(18f, 140f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.35f, 0.55f, 1f);
            fill.intensity = 0.28f;
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

        static Vector3[] BuildDramaticCenterline()
        {
            var points = new List<Vector3>();
            const float straightLength = 64f;
            const float turnRadius = 21f;
            var half = straightLength * 0.5f;

            AppendStraight(points, new Vector3(-half, 0f, turnRadius), new Vector3(half, 0f, turnRadius), 6);
            AppendArc(points, new Vector3(half, 0f, 0f), turnRadius, 90f, -90f, 16);
            AppendStraight(points, new Vector3(half, 0f, -turnRadius), new Vector3(-half, 0f, -turnRadius), 6);
            AppendArc(points, new Vector3(-half, 0f, 0f), turnRadius, -90f, -270f, 16);

            ApplyElevationRidge(points, -turnRadius, turnRadius, 3.8f);
            return points.ToArray();
        }

        static void ApplyElevationRidge(List<Vector3> points, float targetZ, float ridgeRadius, float ridgeHeight)
        {
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var zDelta = Mathf.Abs(p.z - targetZ);
                if (zDelta > ridgeRadius * 0.35f)
                    continue;

                var t = 1f - Mathf.Clamp01(zDelta / (ridgeRadius * 0.35f));
                var lift = Mathf.SmoothStep(0f, ridgeHeight, t);
                points[i] = new Vector3(p.x, p.y + lift, p.z);
            }
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
