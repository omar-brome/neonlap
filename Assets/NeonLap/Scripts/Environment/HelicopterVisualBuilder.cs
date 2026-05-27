using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.Environment
{
    public static class HelicopterVisualBuilder
    {
        public struct BuiltHelicopter
        {
            public Transform Root;
            public Transform MainRotor;
            public Transform TailRotor;
            public Light Searchlight;
        }

        const float VisualScale = 1.18f;

        public static BuiltHelicopter Build(Transform parent, Material bodyMaterial, Material accentMaterial)
        {
            var root = new GameObject("HelicopterVisual").transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * VisualScale;

            var bodyMat = CreateBodyMaterial(bodyMaterial);
            var accentMat = CreateAccentMaterial(accentMaterial);
            var glassMat = CreateGlassMaterial(bodyMaterial);
            var darkMat = CreateDarkMaterial(bodyMaterial);
            var chromeMat = CreateChromeMaterial(bodyMaterial);
            var rotorDiscMat = CreateRotorDiscMaterial();

            BuildFuselage(root, bodyMat, accentMat, darkMat, chromeMat);
            BuildCockpit(root, bodyMat, glassMat, chromeMat);
            BuildTail(root, bodyMat, darkMat, accentMat);
            BuildLandingSkids(root, darkMat, chromeMat);

            var mainRotor = BuildMainRotor(root, darkMat, chromeMat, rotorDiscMat);
            var tailRotor = BuildTailRotor(root, darkMat, chromeMat);
            var searchlight = BuildSearchlight(root, accentMat, darkMat);

            return new BuiltHelicopter
            {
                Root = root,
                MainRotor = mainRotor,
                TailRotor = tailRotor,
                Searchlight = searchlight
            };
        }

        static void BuildFuselage(Transform root, Material bodyMat, Material accentMat, Material darkMat,
            Material chromeMat)
        {
            AddPart(root, "Belly", PrimitiveType.Cube,
                new Vector3(0f, -0.12f, 0.05f), new Vector3(1.22f, 0.62f, 2.55f), Quaternion.identity, bodyMat);
            AddPart(root, "Cabin", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, -0.05f), new Vector3(1.18f, 0.92f, 1.85f), Quaternion.identity, bodyMat);
            AddPart(root, "Nose", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, 1.35f), new Vector3(0.95f, 0.78f, 0.95f), Quaternion.identity, bodyMat);
            AddPart(root, "NoseCap", PrimitiveType.Sphere,
                new Vector3(0f, 0.04f, 1.82f), new Vector3(0.72f, 0.62f, 0.55f), Quaternion.identity, bodyMat);
            AddPart(root, "Roof", PrimitiveType.Cube,
                new Vector3(0f, 0.52f, 0.15f), new Vector3(0.92f, 0.18f, 1.55f), Quaternion.identity, bodyMat);
            AddPart(root, "EngineHump", PrimitiveType.Cylinder,
                new Vector3(0f, 0.48f, -0.35f), new Vector3(0.72f, 0.28f, 0.72f), Quaternion.identity, bodyMat);
            AddPart(root, "Stripe", PrimitiveType.Cube,
                new Vector3(0.58f, 0.08f, 0.05f), new Vector3(0.05f, 0.48f, 2.35f), Quaternion.identity, accentMat);
            AddPart(root, "StripeRear", PrimitiveType.Cube,
                new Vector3(-0.58f, 0.08f, -0.55f), new Vector3(0.05f, 0.42f, 1.35f), Quaternion.identity, accentMat);
            AddPart(root, "DoorLine", PrimitiveType.Cube,
                new Vector3(0.6f, -0.02f, -0.05f), new Vector3(0.03f, 0.52f, 0.85f), Quaternion.identity, darkMat);
            AddPart(root, "Exhaust", PrimitiveType.Cylinder,
                new Vector3(0.42f, 0.38f, -0.72f), new Vector3(0.14f, 0.22f, 0.14f),
                Quaternion.Euler(90f, 0f, 0f), darkMat);
        }

        static void BuildCockpit(Transform root, Material bodyMat, Material glassMat, Material chromeMat)
        {
            AddPart(root, "Windshield", PrimitiveType.Cube,
                new Vector3(0f, 0.28f, 0.95f), new Vector3(1.02f, 0.42f, 0.72f), Quaternion.Euler(-18f, 0f, 0f), glassMat);
            AddPart(root, "WindowL", PrimitiveType.Cube,
                new Vector3(-0.52f, 0.22f, 0.35f), new Vector3(0.04f, 0.34f, 0.82f), Quaternion.Euler(0f, -8f, 0f), glassMat);
            AddPart(root, "WindowR", PrimitiveType.Cube,
                new Vector3(0.52f, 0.22f, 0.35f), new Vector3(0.04f, 0.34f, 0.82f), Quaternion.Euler(0f, 8f, 0f), glassMat);
            AddPart(root, "WindowRear", PrimitiveType.Cube,
                new Vector3(0f, 0.24f, -0.55f), new Vector3(0.88f, 0.3f, 0.06f), Quaternion.identity, glassMat);
            AddPart(root, "FrameTop", PrimitiveType.Cube,
                new Vector3(0f, 0.48f, 0.72f), new Vector3(1.08f, 0.06f, 0.08f), Quaternion.identity, chromeMat);
            AddPart(root, "FrameSideL", PrimitiveType.Cube,
                new Vector3(-0.54f, 0.2f, 0.55f), new Vector3(0.05f, 0.38f, 0.06f), Quaternion.identity, chromeMat);
            AddPart(root, "FrameSideR", PrimitiveType.Cube,
                new Vector3(0.54f, 0.2f, 0.55f), new Vector3(0.05f, 0.38f, 0.06f), Quaternion.identity, chromeMat);
            AddPart(root, "CockpitBulkhead", PrimitiveType.Cube,
                new Vector3(0f, 0.08f, 0.15f), new Vector3(0.95f, 0.7f, 0.12f), Quaternion.identity, bodyMat);
        }

        static void BuildTail(Transform root, Material bodyMat, Material darkMat, Material accentMat)
        {
            AddPart(root, "TailBoom", PrimitiveType.Cylinder,
                new Vector3(0f, 0.1f, -2.35f), new Vector3(0.38f, 1.35f, 0.38f),
                Quaternion.Euler(90f, 0f, 0f), bodyMat);
            AddPart(root, "TailCone", PrimitiveType.Cylinder,
                new Vector3(0f, 0.12f, -3.05f), new Vector3(0.28f, 0.55f, 0.28f),
                Quaternion.Euler(90f, 0f, 0f), bodyMat);
            AddPart(root, "VerticalFin", PrimitiveType.Cube,
                new Vector3(0f, 0.52f, -3.12f), new Vector3(0.1f, 0.95f, 0.62f), Quaternion.Euler(12f, 0f, 0f), darkMat);
            AddPart(root, "FinTip", PrimitiveType.Cube,
                new Vector3(0f, 0.98f, -3.18f), new Vector3(0.06f, 0.22f, 0.28f), Quaternion.Euler(12f, 0f, 0f), accentMat);
            AddPart(root, "StabilizerL", PrimitiveType.Cube,
                new Vector3(-0.42f, 0.22f, -3.02f), new Vector3(0.55f, 0.08f, 0.32f), Quaternion.identity, darkMat);
            AddPart(root, "StabilizerR", PrimitiveType.Cube,
                new Vector3(0.42f, 0.22f, -3.02f), new Vector3(0.55f, 0.08f, 0.32f), Quaternion.identity, darkMat);
            AddPart(root, "TailSkid", PrimitiveType.Cube,
                new Vector3(0f, -0.08f, -3.05f), new Vector3(0.12f, 0.12f, 0.45f), Quaternion.identity, darkMat);
        }

        static void BuildLandingSkids(Transform root, Material darkMat, Material chromeMat)
        {
            AddPart(root, "SkidL", PrimitiveType.Cylinder,
                new Vector3(-0.68f, -0.78f, 0.05f), new Vector3(0.1f, 1.05f, 0.1f),
                Quaternion.Euler(0f, 0f, 90f), darkMat);
            AddPart(root, "SkidR", PrimitiveType.Cylinder,
                new Vector3(0.68f, -0.78f, 0.05f), new Vector3(0.1f, 1.05f, 0.1f),
                Quaternion.Euler(0f, 0f, 90f), darkMat);
            AddPart(root, "SkidFrontL", PrimitiveType.Cube,
                new Vector3(-0.58f, -0.52f, 0.75f), new Vector3(0.07f, 0.32f, 0.07f), Quaternion.identity, chromeMat);
            AddPart(root, "SkidFrontR", PrimitiveType.Cube,
                new Vector3(0.58f, -0.52f, 0.75f), new Vector3(0.07f, 0.32f, 0.07f), Quaternion.identity, chromeMat);
            AddPart(root, "SkidRearL", PrimitiveType.Cube,
                new Vector3(-0.58f, -0.52f, -0.55f), new Vector3(0.07f, 0.32f, 0.07f), Quaternion.identity, chromeMat);
            AddPart(root, "SkidRearR", PrimitiveType.Cube,
                new Vector3(0.58f, -0.52f, -0.55f), new Vector3(0.07f, 0.32f, 0.07f), Quaternion.identity, chromeMat);
        }

        static Transform BuildMainRotor(Transform root, Material darkMat, Material chromeMat, Material rotorDiscMat)
        {
            var mainRotor = new GameObject("MainRotor").transform;
            mainRotor.SetParent(root, false);
            mainRotor.localPosition = new Vector3(0f, 0.82f, 0.12f);

            AddPart(mainRotor, "Mast", PrimitiveType.Cylinder,
                new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.28f, 0.14f), Quaternion.identity, chromeMat);
            AddPart(mainRotor, "Hub", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.42f, 0.1f, 0.42f), Quaternion.identity, darkMat);
            AddPart(mainRotor, "HubCap", PrimitiveType.Sphere,
                new Vector3(0f, 0.06f, 0f), new Vector3(0.28f, 0.12f, 0.28f), Quaternion.identity, chromeMat);
            AddPart(mainRotor, "RotorDisc", PrimitiveType.Cylinder,
                new Vector3(0f, 0.02f, 0f), new Vector3(5.4f, 0.025f, 5.4f), Quaternion.identity, rotorDiscMat);

            for (var i = 0; i < 4; i++)
                AddMainBlade(mainRotor, i * 90f, darkMat, chromeMat);

            return mainRotor;
        }

        static void AddMainBlade(Transform rotor, float yawDegrees, Material darkMat, Material chromeMat)
        {
            var bladeRoot = new GameObject("Blade_" + yawDegrees).transform;
            bladeRoot.SetParent(rotor, false);
            bladeRoot.localPosition = new Vector3(0f, 0.05f, 0f);
            bladeRoot.localRotation = Quaternion.Euler(0f, yawDegrees, 4f);

            AddPart(bladeRoot, "Root", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.55f), new Vector3(0.28f, 0.05f, 1.1f), Quaternion.identity, darkMat);
            AddPart(bladeRoot, "Mid", PrimitiveType.Cube,
                new Vector3(0f, 0.01f, 1.55f), new Vector3(0.22f, 0.04f, 1.35f), Quaternion.identity, darkMat);
            AddPart(bladeRoot, "Tip", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, 2.45f), new Vector3(0.14f, 0.03f, 0.95f), Quaternion.identity, chromeMat);
            AddPart(bladeRoot, "TipCap", PrimitiveType.Sphere,
                new Vector3(0f, 0.02f, 2.98f), new Vector3(0.12f, 0.05f, 0.12f), Quaternion.identity, chromeMat);
        }

        static Transform BuildTailRotor(Transform root, Material darkMat, Material chromeMat)
        {
            var tailRotor = new GameObject("TailRotor").transform;
            tailRotor.SetParent(root, false);
            tailRotor.localPosition = new Vector3(0.22f, 0.38f, -3.22f);
            tailRotor.localRotation = Quaternion.Euler(0f, 90f, 0f);

            AddPart(tailRotor, "TailHub", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.22f, 0.06f, 0.22f), Quaternion.identity, darkMat);
            AddPart(tailRotor, "HubCap", PrimitiveType.Sphere,
                new Vector3(0f, 0f, 0.02f), new Vector3(0.14f, 0.14f, 0.08f), Quaternion.identity, chromeMat);

            var cage = new GameObject("RotorCage").transform;
            cage.SetParent(tailRotor, false);
            AddPart(cage, "StrutTop", PrimitiveType.Cube,
                new Vector3(0f, 0.34f, 0f), new Vector3(0.04f, 0.04f, 0.42f), Quaternion.identity, chromeMat);
            AddPart(cage, "StrutBottom", PrimitiveType.Cube,
                new Vector3(0f, -0.34f, 0f), new Vector3(0.04f, 0.04f, 0.42f), Quaternion.identity, chromeMat);
            AddPart(cage, "StrutFront", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.2f), new Vector3(0.04f, 0.62f, 0.04f), Quaternion.identity, chromeMat);
            AddPart(cage, "StrutBack", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.2f), new Vector3(0.04f, 0.62f, 0.04f), Quaternion.identity, chromeMat);

            for (var i = 0; i < 3; i++)
            {
                AddPart(tailRotor, "TailBlade_" + i, PrimitiveType.Cube,
                    Vector3.zero, new Vector3(0.06f, 0.62f, 0.025f), Quaternion.Euler(0f, 0f, i * 120f), darkMat);
            }

            return tailRotor;
        }

        static Light BuildSearchlight(Transform root, Material accentMat, Material darkMat)
        {
            var searchlightGo = new GameObject("Searchlight");
            searchlightGo.transform.SetParent(root, false);
            searchlightGo.transform.localPosition = new Vector3(0f, -0.28f, 0.92f);
            searchlightGo.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            AddPart(searchlightGo.transform, "Housing", PrimitiveType.Cube,
                Vector3.zero, new Vector3(0.28f, 0.22f, 0.32f), Quaternion.identity, darkMat);
            AddPart(searchlightGo.transform, "Lens", PrimitiveType.Cylinder,
                new Vector3(0f, 0f, 0.18f), new Vector3(0.24f, 0.06f, 0.24f),
                Quaternion.Euler(90f, 0f, 0f), accentMat);
            AddPart(searchlightGo.transform, "Bracket", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, -0.08f), new Vector3(0.12f, 0.1f, 0.12f), Quaternion.identity, darkMat);

            var searchlight = searchlightGo.AddComponent<Light>();
            searchlight.type = LightType.Spot;
            searchlight.color = new Color(1f, 0.96f, 0.82f);
            searchlight.intensity = 2.6f;
            searchlight.range = 90f;
            searchlight.spotAngle = 38f;
            searchlight.innerSpotAngle = 18f;
            searchlight.shadows = LightShadows.None;

            return searchlight;
        }

        static GameObject AddPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition,
            Vector3 localScale, Quaternion localRotation, Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            if (material != null)
                part.GetComponent<Renderer>().sharedMaterial = material;

            return part;
        }

        static Material CreateBodyMaterial(Material template)
        {
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            var mat = template != null ? new Material(template) : new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.2f, 0.22f, 0.26f));
            mat.SetFloat("_Smoothness", 0.58f);
            mat.SetFloat("_Metallic", 0.42f);
            return mat;
        }

        static Material CreateAccentMaterial(Material template)
        {
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            var mat = template != null ? new Material(template) : new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.95f, 0.35f, 0.08f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(2.5f, 0.7f, 0.15f));
            mat.SetFloat("_Smoothness", 0.75f);
            return mat;
        }

        static Material CreateGlassMaterial(Material template)
        {
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            var mat = template != null ? new Material(template) : new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.12f, 0.2f, 0.32f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.15f, 0.45f, 0.85f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Metallic", 0.35f);
            return mat;
        }

        static Material CreateDarkMaterial(Material template)
        {
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            var mat = template != null ? new Material(template) : new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.07f));
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetFloat("_Metallic", 0.15f);
            return mat;
        }

        static Material CreateChromeMaterial(Material template)
        {
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            var mat = template != null ? new Material(template) : new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.35f, 0.37f, 0.4f));
            mat.SetFloat("_Smoothness", 0.88f);
            mat.SetFloat("_Metallic", 0.75f);
            return mat;
        }

        static Material CreateRotorDiscMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.08f, 0.12f, 0.16f, 0.42f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.08f, 0.35f, 0.55f));
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }
    }
}
