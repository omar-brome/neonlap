using UnityEngine;

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

        public static BuiltHelicopter Build(Transform parent, Material bodyMaterial, Material accentMaterial)
        {
            var root = new GameObject("HelicopterVisual").transform;
            root.SetParent(parent, false);

            var bodyMat = CreateBodyMaterial(bodyMaterial);
            var accentMat = CreateAccentMaterial(accentMaterial);
            var glassMat = CreateGlassMaterial(bodyMaterial);
            var darkMat = CreateDarkMaterial(bodyMaterial);

            AddPart(root, "Fuselage", PrimitiveType.Capsule,
                new Vector3(0f, 0f, 0f), new Vector3(1.35f, 1.1f, 2.8f), Quaternion.Euler(0f, 0f, 90f), bodyMat);
            AddPart(root, "Nose", PrimitiveType.Cube,
                new Vector3(0f, -0.05f, 1.55f), new Vector3(0.85f, 0.75f, 0.65f), Quaternion.identity, bodyMat);
            AddPart(root, "Cockpit", PrimitiveType.Cube,
                new Vector3(0f, 0.18f, 0.55f), new Vector3(1.05f, 0.55f, 1.05f), Quaternion.identity, glassMat);
            AddPart(root, "Cabin", PrimitiveType.Cube,
                new Vector3(0f, -0.05f, -0.15f), new Vector3(1.15f, 0.85f, 1.35f), Quaternion.identity, bodyMat);
            AddPart(root, "Stripe", PrimitiveType.Cube,
                new Vector3(0.55f, 0.05f, 0.1f), new Vector3(0.06f, 0.55f, 2.1f), Quaternion.identity, accentMat);
            AddPart(root, "TailBoom", PrimitiveType.Cube,
                new Vector3(0f, 0.08f, -2.15f), new Vector3(0.35f, 0.35f, 2.4f), Quaternion.identity, bodyMat);
            AddPart(root, "TailFin", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, -3.05f), new Vector3(0.08f, 0.75f, 0.55f), Quaternion.Euler(8f, 0f, 0f), darkMat);
            AddPart(root, "SkidL", PrimitiveType.Cube,
                new Vector3(-0.62f, -0.72f, 0.05f), new Vector3(0.08f, 0.08f, 2.2f), Quaternion.identity, darkMat);
            AddPart(root, "SkidR", PrimitiveType.Cube,
                new Vector3(0.62f, -0.72f, 0.05f), new Vector3(0.08f, 0.08f, 2.2f), Quaternion.identity, darkMat);
            AddPart(root, "SkidStrutL", PrimitiveType.Cube,
                new Vector3(-0.55f, -0.45f, 0.35f), new Vector3(0.06f, 0.35f, 0.06f), Quaternion.identity, darkMat);
            AddPart(root, "SkidStrutR", PrimitiveType.Cube,
                new Vector3(0.55f, -0.45f, 0.35f), new Vector3(0.06f, 0.35f, 0.06f), Quaternion.identity, darkMat);

            var mainRotor = new GameObject("MainRotor").transform;
            mainRotor.SetParent(root, false);
            mainRotor.localPosition = new Vector3(0f, 0.72f, 0.15f);
            AddPart(mainRotor, "Hub", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.35f, 0.08f, 0.35f), Quaternion.identity, darkMat);
            for (var i = 0; i < 4; i++)
            {
                var blade = AddPart(mainRotor, "Blade_" + i, PrimitiveType.Cube,
                    Vector3.zero, new Vector3(0.14f, 0.03f, 4.6f), Quaternion.Euler(0f, i * 90f, 0f), darkMat);
                blade.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            }

            var tailRotor = new GameObject("TailRotor").transform;
            tailRotor.SetParent(root, false);
            tailRotor.localPosition = new Vector3(0.18f, 0.35f, -3.18f);
            tailRotor.localRotation = Quaternion.Euler(0f, 0f, 90f);
            AddPart(tailRotor, "TailHub", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.18f, 0.05f, 0.18f), Quaternion.identity, darkMat);
            for (var i = 0; i < 2; i++)
            {
                AddPart(tailRotor, "TailBlade_" + i, PrimitiveType.Cube,
                    Vector3.zero, new Vector3(0.08f, 0.55f, 0.03f), Quaternion.Euler(0f, 0f, i * 90f), darkMat);
            }

            var searchlightGo = new GameObject("Searchlight");
            searchlightGo.transform.SetParent(root, false);
            searchlightGo.transform.localPosition = new Vector3(0f, -0.35f, 0.85f);
            searchlightGo.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            AddPart(searchlightGo.transform, "Lens", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.22f, 0.08f, 0.22f), Quaternion.Euler(90f, 0f, 0f), accentMat);

            var searchlight = searchlightGo.AddComponent<Light>();
            searchlight.type = LightType.Spot;
            searchlight.color = new Color(1f, 0.96f, 0.82f);
            searchlight.intensity = 2.6f;
            searchlight.range = 90f;
            searchlight.spotAngle = 38f;
            searchlight.innerSpotAngle = 18f;
            searchlight.shadows = LightShadows.None;

            return new BuiltHelicopter
            {
                Root = root,
                MainRotor = mainRotor,
                TailRotor = tailRotor,
                Searchlight = searchlight
            };
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
            mat.SetColor("_BaseColor", new Color(0.14f, 0.16f, 0.18f));
            mat.SetFloat("_Smoothness", 0.55f);
            mat.SetFloat("_Metallic", 0.35f);
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
            mat.SetColor("_BaseColor", new Color(0.08f, 0.12f, 0.16f, 1f));
            mat.SetFloat("_Smoothness", 0.92f);
            mat.SetFloat("_Metallic", 0.2f);
            return mat;
        }

        static Material CreateDarkMaterial(Material template)
        {
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            var mat = template != null ? new Material(template) : new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.06f));
            mat.SetFloat("_Smoothness", 0.35f);
            return mat;
        }
    }
}
