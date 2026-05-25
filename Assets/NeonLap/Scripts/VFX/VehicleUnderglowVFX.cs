using UnityEngine;

namespace NeonLap.VFX
{
    public class VehicleUnderglowVFX : MonoBehaviour
    {
        static readonly Color[] Palette =
        {
            new(0.2f, 1f, 1f),
            new(1f, 0.35f, 0.95f),
            new(0.45f, 0.55f, 1f),
            new(1f, 0.55f, 0.15f),
            new(0.35f, 1f, 0.55f),
            new(1f, 0.25f, 0.45f),
        };

        [SerializeField] float colorCycleSpeed = 0.22f;
        [SerializeField] float pulseSpeed = 2.4f;
        [SerializeField] float pulseAmount = 0.35f;
        [SerializeField] float stripEmission = 3.2f;
        [SerializeField] float lightIntensity = 1.35f;
        [SerializeField] float lightRange = 2.8f;

        readonly System.Collections.Generic.List<Renderer> stripRenderers = new();
        readonly System.Collections.Generic.List<Material> stripMaterials = new();
        readonly System.Collections.Generic.List<Light> groundLights = new();

        float colorPhase;

        void Awake()
        {
            BuildUnderglow();
        }

        void Update()
        {
            if (stripMaterials.Count == 0)
                return;

            colorPhase += Time.deltaTime * colorCycleSpeed;
            if (colorPhase >= Palette.Length)
                colorPhase -= Palette.Length;

            var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            var color = SamplePalette(colorPhase);
            var emission = color * (stripEmission * pulse);

            for (var i = 0; i < stripMaterials.Count; i++)
            {
                var mat = stripMaterials[i];
                if (mat == null)
                    continue;

                mat.SetColor("_BaseColor", color * 0.35f);
                mat.SetColor("_EmissionColor", emission);
            }

            for (var i = 0; i < groundLights.Count; i++)
            {
                var light = groundLights[i];
                if (light == null)
                    continue;

                light.color = color;
                light.intensity = lightIntensity * pulse;
            }
        }

        void OnDestroy()
        {
            for (var i = 0; i < stripMaterials.Count; i++)
            {
                if (stripMaterials[i] != null)
                    Destroy(stripMaterials[i]);
            }
        }

        void BuildUnderglow()
        {
            var root = new GameObject("UnderglowVFX").transform;
            root.SetParent(transform, false);

            CreateStrip(root, "StripLeft", new Vector3(-0.78f, 0.03f, 0f), new Vector3(0.05f, 0.015f, 2.05f));
            CreateStrip(root, "StripRight", new Vector3(0.78f, 0.03f, 0f), new Vector3(0.05f, 0.015f, 2.05f));
            CreateStrip(root, "StripFront", new Vector3(0f, 0.03f, 1.05f), new Vector3(1.35f, 0.015f, 0.05f));
            CreateStrip(root, "StripRear", new Vector3(0f, 0.03f, -1.12f), new Vector3(1.45f, 0.015f, 0.05f));

            CreateGroundLight(root, "GlowFL", new Vector3(-0.62f, 0.02f, 0.72f));
            CreateGroundLight(root, "GlowFR", new Vector3(0.62f, 0.02f, 0.72f));
            CreateGroundLight(root, "GlowRL", new Vector3(-0.62f, 0.02f, -0.72f));
            CreateGroundLight(root, "GlowRR", new Vector3(0.62f, 0.02f, -0.72f));
            CreateGroundLight(root, "GlowCenter", new Vector3(0f, 0.02f, 0f));
        }

        void CreateStrip(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            var material = CreateGlowMaterial();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            stripRenderers.Add(renderer);
            stripMaterials.Add(material);
        }

        void CreateGroundLight(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Palette[0];
            light.intensity = lightIntensity;
            light.range = lightRange;
            light.shadows = LightShadows.None;
            groundLights.Add(light);
        }

        static Material CreateGlowMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.95f);
            return mat;
        }

        static Color SamplePalette(float phase)
        {
            var index = Mathf.FloorToInt(phase);
            var next = (index + 1) % Palette.Length;
            var t = phase - index;
            return Color.Lerp(Palette[index], Palette[next], t);
        }
    }
}
