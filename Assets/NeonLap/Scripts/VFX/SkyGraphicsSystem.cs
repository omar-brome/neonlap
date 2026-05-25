using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.VFX
{
    public class SkyGraphicsSystem : MonoBehaviour
    {
        const float DomeRadius = 420f;
        static readonly Vector3 SunDirection = new Vector3(-0.38f, 0.62f, -0.69f).normalized;
        static readonly Color SunnySkyTint = new(1f, 0.96f, 0.78f);
        static readonly Color SunnySunCoreEmission = new(7.5f, 6.2f, 2.4f);
        static readonly Color SunnySunHaloEmission = new(4.8f, 3.2f, 0.9f);

        Transform followTarget;
        Transform cloudRoot;
        Transform rainbowRoot;
        Material skyMaterial;
        Material sunCoreMaterial;
        Material sunHaloMaterial;
        readonly List<Material> sunRayMaterials = new();
        readonly List<Color> sunRayBaseEmissions = new();
        readonly List<Material> cloudMaterials = new();
        readonly List<Color> cloudBaseColors = new();
        readonly List<Color> cloudBaseEmissions = new();
        readonly List<Material> rainbowMaterials = new();
        readonly List<Color> rainbowBaseEmissions = new();
        Texture2D skyGradientTexture;
        Color rainySunCoreEmission;
        Color rainySunHaloEmission;

        public static SkyGraphicsSystem Ensure(UnityEngine.Camera camera = null)
        {
            var existing = Object.FindAnyObjectByType<SkyGraphicsSystem>();
            if (existing != null)
            {
                existing.SetFollowTarget(camera != null ? camera.transform : null);
                return existing;
            }

            var go = new GameObject("SkyGraphics");
            var system = go.AddComponent<SkyGraphicsSystem>();
            system.Build(camera != null ? camera.transform : UnityEngine.Camera.main?.transform);
            return system;
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        void Build(Transform cameraTransform)
        {
            followTarget = cameraTransform;
            var density = Mathf.Clamp(GameQualitySettings.Preset.EnvironmentDensity, 0.25f, 1f);

            BuildSkyDome();
            BuildSun();
            BuildClouds(Mathf.RoundToInt(Mathf.Lerp(5f, 18f, density)));
            if (density >= 0.4f)
                BuildRainbow(Mathf.Lerp(0.55f, 1f, density));

            AlignDirectionalLight();
            SetCameraBackground();
        }

        void LateUpdate()
        {
            if (followTarget == null)
                return;

            transform.position = followTarget.position;
        }

        public void ApplyWeatherBlend(float sunnyBlend)
        {
            sunnyBlend = Mathf.Clamp01(sunnyBlend);

            if (skyMaterial != null)
            {
                var tint = Color.Lerp(Color.white, SunnySkyTint, sunnyBlend);
                skyMaterial.SetColor("_BaseColor", tint);
            }

            if (sunCoreMaterial != null)
                sunCoreMaterial.SetColor("_EmissionColor", Color.Lerp(rainySunCoreEmission, SunnySunCoreEmission, sunnyBlend));

            if (sunHaloMaterial != null)
                sunHaloMaterial.SetColor("_EmissionColor", Color.Lerp(rainySunHaloEmission, SunnySunHaloEmission, sunnyBlend));

            for (var i = 0; i < sunRayMaterials.Count; i++)
            {
                if (sunRayMaterials[i] == null)
                    continue;

                var boosted = sunRayBaseEmissions[i] * Mathf.Lerp(1f, 1.8f, sunnyBlend);
                sunRayMaterials[i].SetColor("_EmissionColor", boosted);
            }

            for (var i = 0; i < cloudMaterials.Count; i++)
            {
                var cloudMat = cloudMaterials[i];
                if (cloudMat == null)
                    continue;

                var sunnyCloud = new Color(1f, 0.98f, 0.88f,
                    Mathf.Lerp(cloudBaseColors[i].a, cloudBaseColors[i].a * 0.45f, sunnyBlend));
                cloudMat.SetColor("_BaseColor", Color.Lerp(cloudBaseColors[i], sunnyCloud, sunnyBlend * 0.85f));
                cloudMat.SetColor("_EmissionColor", cloudBaseEmissions[i] * Mathf.Lerp(1f, 0.35f, sunnyBlend));
            }

            if (rainbowRoot != null)
            {
                var showRainbow = sunnyBlend > 0.25f;
                if (rainbowRoot.gameObject.activeSelf != showRainbow)
                    rainbowRoot.gameObject.SetActive(showRainbow);

                if (showRainbow)
                {
                    var rainbowStrength = Mathf.InverseLerp(0.25f, 0.85f, sunnyBlend);
                    for (var i = 0; i < rainbowMaterials.Count; i++)
                    {
                        if (rainbowMaterials[i] == null)
                            continue;

                        rainbowMaterials[i].SetColor("_EmissionColor",
                            rainbowBaseEmissions[i] * (1f + rainbowStrength * 1.4f));
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (skyMaterial != null)
                Destroy(skyMaterial);
            if (skyGradientTexture != null)
                Destroy(skyGradientTexture);
        }

        void BuildSkyDome()
        {
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "SkyDome";
            dome.transform.SetParent(transform, false);
            dome.transform.localScale = Vector3.one * DomeRadius * 2f;
            Object.Destroy(dome.GetComponent<Collider>());

            skyGradientTexture = CreateSkyGradientTexture(4, 256);
            skyMaterial = CreateSkyMaterial(skyGradientTexture);
            dome.GetComponent<MeshRenderer>().sharedMaterial = skyMaterial;
            dome.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            dome.GetComponent<MeshRenderer>().receiveShadows = false;
        }

        void BuildSun()
        {
            var sunRoot = new GameObject("Sun").transform;
            sunRoot.SetParent(transform, false);
            sunRoot.localPosition = SunDirection * (DomeRadius * 0.92f);

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "SunCore";
            core.transform.SetParent(sunRoot, false);
            core.transform.localScale = Vector3.one * 26f;
            Object.Destroy(core.GetComponent<Collider>());
            var coreMat = CreateEmissiveMaterial(new Color(1f, 0.92f, 0.55f), new Color(4.5f, 3.8f, 1.6f), 0f);
            core.GetComponent<MeshRenderer>().sharedMaterial = coreMat;
            sunCoreMaterial = coreMat;
            rainySunCoreEmission = coreMat.GetColor("_EmissionColor");
            core.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "SunHalo";
            halo.transform.SetParent(sunRoot, false);
            halo.transform.localScale = Vector3.one * 58f;
            Object.Destroy(halo.GetComponent<Collider>());
            var haloMat = CreateEmissiveMaterial(new Color(1f, 0.78f, 0.35f, 0.18f), new Color(2.8f, 1.8f, 0.5f), 0.72f);
            halo.GetComponent<MeshRenderer>().sharedMaterial = haloMat;
            sunHaloMaterial = haloMat;
            rainySunHaloEmission = haloMat.GetColor("_EmissionColor");
            halo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var rays = new GameObject("SunRays");
            rays.transform.SetParent(sunRoot, false);
            rays.transform.localRotation = Quaternion.identity;
            for (var i = 0; i < 8; i++)
            {
                var ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ray.name = "SunRay_" + i;
                ray.transform.SetParent(rays.transform, false);
                ray.transform.localRotation = Quaternion.Euler(0f, 0f, i * 22.5f);
                ray.transform.localPosition = Vector3.forward * 20f;
                ray.transform.localScale = new Vector3(0.35f, 0.08f, 42f);
                Object.Destroy(ray.GetComponent<Collider>());
                var rayMat = CreateEmissiveMaterial(new Color(1f, 0.85f, 0.45f, 0.12f), new Color(2f, 1.4f, 0.35f), 0.82f);
                ray.GetComponent<MeshRenderer>().sharedMaterial = rayMat;
                sunRayMaterials.Add(rayMat);
                sunRayBaseEmissions.Add(rayMat.GetColor("_EmissionColor"));
                ray.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        void BuildClouds(int cloudCount)
        {
            cloudRoot = new GameObject("Clouds").transform;
            cloudRoot.SetParent(transform, false);
            cloudRoot.gameObject.AddComponent<CloudDrift>();

            var random = new System.Random(5150);
            for (var i = 0; i < cloudCount; i++)
            {
                var cloud = new GameObject("Cloud_" + i).transform;
                cloud.SetParent(cloudRoot, false);

                var azimuth = (float)random.NextDouble() * Mathf.PI * 2f;
                var elevation = 0.18f + (float)random.NextDouble() * 0.42f;
                var direction = new Vector3(
                    Mathf.Cos(azimuth) * Mathf.Cos(elevation),
                    Mathf.Sin(elevation),
                    Mathf.Sin(azimuth) * Mathf.Cos(elevation));
                cloud.localPosition = direction.normalized * DomeRadius * (0.55f + (float)random.NextDouble() * 0.22f);
                cloud.localRotation = Quaternion.Euler(
                    (float)random.NextDouble() * 25f,
                    (float)random.NextDouble() * 360f,
                    (float)random.NextDouble() * 18f);

                var puffCount = 3 + random.Next(3);
                for (var puff = 0; puff < puffCount; puff++)
                {
                    var puffGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puffGo.name = "Puff_" + puff;
                    puffGo.transform.SetParent(cloud, false);
                    var scale = 12f + (float)random.NextDouble() * 18f;
                    puffGo.transform.localScale = new Vector3(
                        scale * (0.9f + (float)random.NextDouble() * 0.35f),
                        scale * (0.35f + (float)random.NextDouble() * 0.18f),
                        scale * (0.85f + (float)random.NextDouble() * 0.4f));
                    puffGo.transform.localPosition = new Vector3(
                        ((float)random.NextDouble() - 0.5f) * 16f,
                        ((float)random.NextDouble() - 0.5f) * 3f,
                        ((float)random.NextDouble() - 0.5f) * 12f);
                    Object.Destroy(puffGo.GetComponent<Collider>());

                    var alpha = Mathf.Lerp(0.12f, 0.28f, (float)random.NextDouble());
                    var cloudMat = CreateEmissiveMaterial(
                        new Color(0.92f, 0.95f, 1f, alpha),
                        new Color(0.35f, 0.55f, 0.95f, alpha * 0.35f),
                        0.78f);
                    puffGo.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;
                    cloudMaterials.Add(cloudMat);
                    cloudBaseColors.Add(cloudMat.GetColor("_BaseColor"));
                    cloudBaseEmissions.Add(cloudMat.GetColor("_EmissionColor"));
                    puffGo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        void BuildRainbow(float intensity)
        {
            rainbowRoot = new GameObject("Rainbow").transform;
            rainbowRoot.SetParent(transform, false);

            var opposite = -SunDirection;
            rainbowRoot.localPosition = opposite * (DomeRadius * 0.68f) + new Vector3(0f, 24f, 0f);
            var yaw = Mathf.Atan2(opposite.x, opposite.z) * Mathf.Rad2Deg;
            rainbowRoot.localRotation = Quaternion.Euler(10f, yaw, 0f);

            var colors = new[]
            {
                new Color(1f, 0.18f, 0.22f, 0.55f),
                new Color(1f, 0.55f, 0.12f, 0.55f),
                new Color(1f, 0.92f, 0.18f, 0.55f),
                new Color(0.25f, 0.95f, 0.35f, 0.55f),
                new Color(0.2f, 0.55f, 1f, 0.55f),
                new Color(0.45f, 0.22f, 0.95f, 0.55f),
                new Color(0.85f, 0.25f, 0.95f, 0.55f),
            };

            const int segments = 56;
            const float arcDegrees = 92f;
            const float innerRadius = 46f;
            const float bandWidth = 2.4f;
            var startAngle = -arcDegrees * 0.5f;

            for (var band = 0; band < colors.Length; band++)
            {
                var bandRoot = new GameObject("RainbowBand_" + band).transform;
                bandRoot.SetParent(rainbowRoot, false);

                var mesh = CreateArcBandMesh(segments, startAngle, arcDegrees, innerRadius + band * bandWidth, bandWidth);
                var bandGo = new GameObject("BandMesh");
                bandGo.transform.SetParent(bandRoot, false);
                var filter = bandGo.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = bandGo.AddComponent<MeshRenderer>();

                var bandColor = colors[band];
                bandColor.a *= intensity;
                var emission = new Color(bandColor.r, bandColor.g, bandColor.b, 1f) * 1.8f;
                renderer.sharedMaterial = CreateEmissiveMaterial(bandColor, emission, 0.68f);
                rainbowMaterials.Add(renderer.sharedMaterial);
                rainbowBaseEmissions.Add(renderer.sharedMaterial.GetColor("_EmissionColor"));
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var glow = new GameObject("RainbowGlow").transform;
            glow.SetParent(rainbowRoot, false);
            glow.localPosition = Vector3.zero;
            var glowMesh = CreateArcBandMesh(segments, startAngle, arcDegrees, innerRadius - 2f, colors.Length * bandWidth + 8f);
            var glowGo = new GameObject("GlowMesh");
            glowGo.transform.SetParent(glow, false);
            glowGo.AddComponent<MeshFilter>().sharedMesh = glowMesh;
            var glowRenderer = glowGo.AddComponent<MeshRenderer>();
            glowRenderer.sharedMaterial = CreateEmissiveMaterial(
                new Color(1f, 0.85f, 1f, 0.08f * intensity),
                new Color(1.2f, 0.8f, 1.4f, 0.25f),
                0.88f);
            rainbowMaterials.Add(glowRenderer.sharedMaterial);
            rainbowBaseEmissions.Add(glowRenderer.sharedMaterial.GetColor("_EmissionColor"));
            glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            rainbowRoot.gameObject.SetActive(false);
        }

        static Mesh CreateArcBandMesh(int segments, float startAngle, float arcDegrees, float radius, float width)
        {
            var mesh = new Mesh { name = "RainbowArcBand" };
            var vertCount = (segments + 1) * 2;
            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[segments * 6];

            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = (startAngle + arcDegrees * t) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f);
                var inner = dir * radius;
                var outer = dir * (radius + width);
                var index = i * 2;
                vertices[index] = inner;
                vertices[index + 1] = outer;
                uvs[index] = new Vector2(t, 0f);
                uvs[index + 1] = new Vector2(t, 1f);
            }

            var tri = 0;
            for (var i = 0; i < segments; i++)
            {
                var v = i * 2;
                triangles[tri++] = v;
                triangles[tri++] = v + 2;
                triangles[tri++] = v + 1;
                triangles[tri++] = v + 1;
                triangles[tri++] = v + 2;
                triangles[tri++] = v + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        static Texture2D CreateSkyGradientTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var zenith = new Color(0.05f, 0.06f, 0.2f);
            var mid = new Color(0.16f, 0.12f, 0.38f);
            var horizon = new Color(0.52f, 0.18f, 0.52f);
            var sunGlow = new Color(0.95f, 0.42f, 0.28f);

            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                Color color;
                if (t < 0.42f)
                    color = Color.Lerp(horizon, mid, t / 0.42f);
                else
                    color = Color.Lerp(mid, zenith, (t - 0.42f) / 0.58f);

                if (t < 0.22f)
                    color = Color.Lerp(color, sunGlow, (0.22f - t) / 0.22f * 0.35f);

                for (var x = 0; x < width; x++)
                    texture.SetPixel(x, y, color);
            }

            texture.Apply();
            return texture;
        }

        static Material CreateSkyMaterial(Texture2D gradientTexture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            var material = new Material(shader);
            material.SetTexture("_BaseMap", gradientTexture);
            material.mainTexture = gradientTexture;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Cull", 1f);
            material.renderQueue = (int)RenderQueue.Background;
            return material;
        }

        static Material CreateEmissiveMaterial(Color baseColor, Color emission, float alpha)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            material.SetColor("_BaseColor", baseColor);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.15f);

            if (alpha < 0.99f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            material.SetFloat("_Cull", 0f);
            return material;
        }

        static void AlignDirectionalLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var light in lights)
            {
                if (light.type != LightType.Directional)
                    continue;

                light.transform.rotation = Quaternion.LookRotation(-SunDirection, Vector3.up);
                light.color = Color.Lerp(new Color(1f, 0.88f, 0.72f), new Color(0.72f, 0.82f, 1f), 0.35f);
                break;
            }
        }

        static void SetCameraBackground()
        {
            var cameras = Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Exclude);
            foreach (var camera in cameras)
            {
                if (!camera.CompareTag("MainCamera"))
                    continue;

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.08f, 0.05f, 0.14f);
            }
        }

        sealed class CloudDrift : MonoBehaviour
        {
            [SerializeField] float yawSpeed = 1.6f;
            [SerializeField] float bobAmplitude = 1.8f;
            [SerializeField] float bobSpeed = 0.18f;

            Vector3 startLocalPosition;

            void Awake()
            {
                startLocalPosition = transform.localPosition;
            }

            void Update()
            {
                transform.Rotate(Vector3.up, yawSpeed * Time.deltaTime, Space.Self);
                var bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
                transform.localPosition = startLocalPosition + Vector3.up * bob;
            }
        }
    }
}
