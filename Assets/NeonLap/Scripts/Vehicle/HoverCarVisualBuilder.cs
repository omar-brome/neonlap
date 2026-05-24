using UnityEngine;

namespace NeonLap.Vehicle
{
    public static class HoverCarVisualBuilder
    {
        public readonly struct BuildArgs
        {
            public BuildArgs(Material bodyTemplate, Material accentTemplate, Color bodyColor, Color accentEmission)
            {
                BodyTemplate = bodyTemplate;
                AccentTemplate = accentTemplate;
                BodyColor = bodyColor;
                AccentEmission = accentEmission;
            }

            public Material BodyTemplate { get; }
            public Material AccentTemplate { get; }
            public Color BodyColor { get; }
            public Color AccentEmission { get; }
        }

        public static Transform Build(Transform root, BuildArgs args)
        {
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(root, false);

            var bodyMat = CreateBodyMaterial(args);
            var accentMat = CreateAccentMaterial(args);
            var trimMat = CreateTrimMaterial(args.BodyColor, args.BodyTemplate);

            AddPart(visualRoot, "Chassis", PrimitiveType.Cube,
                new Vector3(0f, 0.14f, 0f), new Vector3(1.55f, 0.22f, 2.55f), Quaternion.identity, bodyMat);
            AddPart(visualRoot, "Nose", PrimitiveType.Cube,
                new Vector3(0f, 0.18f, 1.05f), new Vector3(0.95f, 0.18f, 0.95f), Quaternion.identity, bodyMat);
            AddPart(visualRoot, "Cabin", PrimitiveType.Cube,
                new Vector3(0f, 0.36f, 0.15f), new Vector3(1.05f, 0.32f, 1.05f), Quaternion.identity, bodyMat);
            AddPart(visualRoot, "Canopy", PrimitiveType.Cube,
                new Vector3(0f, 0.48f, 0.2f), new Vector3(0.82f, 0.12f, 0.72f),
                Quaternion.Euler(8f, 0f, 0f), accentMat);
            AddPart(visualRoot, "RearDeck", PrimitiveType.Cube,
                new Vector3(0f, 0.22f, -0.95f), new Vector3(1.35f, 0.14f, 0.75f), Quaternion.identity, bodyMat);
            AddPart(visualRoot, "SpoilerStrutL", PrimitiveType.Cube,
                new Vector3(-0.42f, 0.38f, -1.18f), new Vector3(0.08f, 0.22f, 0.08f), Quaternion.identity, trimMat);
            AddPart(visualRoot, "SpoilerStrutR", PrimitiveType.Cube,
                new Vector3(0.42f, 0.38f, -1.18f), new Vector3(0.08f, 0.22f, 0.08f), Quaternion.identity, trimMat);
            AddPart(visualRoot, "Spoiler", PrimitiveType.Cube,
                new Vector3(0f, 0.5f, -1.22f), new Vector3(1.55f, 0.06f, 0.22f), Quaternion.identity, accentMat);

            AddPart(visualRoot, "SkirtL", PrimitiveType.Cube,
                new Vector3(-0.78f, 0.12f, 0f), new Vector3(0.08f, 0.12f, 2.1f), Quaternion.identity, trimMat);
            AddPart(visualRoot, "SkirtR", PrimitiveType.Cube,
                new Vector3(0.78f, 0.12f, 0f), new Vector3(0.08f, 0.12f, 2.1f), Quaternion.identity, trimMat);

            AddHoverPod(visualRoot, "PodFL", new Vector3(-0.62f, 0.06f, 0.85f), accentMat, trimMat);
            AddHoverPod(visualRoot, "PodFR", new Vector3(0.62f, 0.06f, 0.85f), accentMat, trimMat);
            AddHoverPod(visualRoot, "PodRL", new Vector3(-0.62f, 0.06f, -0.85f), accentMat, trimMat);
            AddHoverPod(visualRoot, "PodRR", new Vector3(0.62f, 0.06f, -0.85f), accentMat, trimMat);

            AddPart(visualRoot, "HeadlightL", PrimitiveType.Sphere,
                new Vector3(-0.42f, 0.2f, 1.42f), new Vector3(0.16f, 0.12f, 0.08f), Quaternion.identity, accentMat);
            AddPart(visualRoot, "HeadlightR", PrimitiveType.Sphere,
                new Vector3(0.42f, 0.2f, 1.42f), new Vector3(0.16f, 0.12f, 0.08f), Quaternion.identity, accentMat);
            AddPart(visualRoot, "TailLightL", PrimitiveType.Cube,
                new Vector3(-0.55f, 0.24f, -1.28f), new Vector3(0.18f, 0.08f, 0.05f), Quaternion.identity, accentMat);
            AddPart(visualRoot, "TailLightR", PrimitiveType.Cube,
                new Vector3(0.55f, 0.24f, -1.28f), new Vector3(0.18f, 0.08f, 0.05f), Quaternion.identity, accentMat);

            AddPart(visualRoot, "IntakeL", PrimitiveType.Cube,
                new Vector3(-0.48f, 0.28f, -0.35f), new Vector3(0.12f, 0.08f, 0.45f), Quaternion.Euler(0f, 0f, 18f), trimMat);
            AddPart(visualRoot, "IntakeR", PrimitiveType.Cube,
                new Vector3(0.48f, 0.28f, -0.35f), new Vector3(0.12f, 0.08f, 0.45f), Quaternion.Euler(0f, 0f, -18f), trimMat);

            return visualRoot;
        }

        static void AddHoverPod(Transform parent, string name, Vector3 position, Material accentMat, Material trimMat)
        {
            AddPart(parent, name + "Housing", PrimitiveType.Cylinder,
                position, new Vector3(0.34f, 0.05f, 0.34f), Quaternion.identity, trimMat);
            AddPart(parent, name + "Glow", PrimitiveType.Cylinder,
                position + new Vector3(0f, -0.03f, 0f), new Vector3(0.24f, 0.02f, 0.24f), Quaternion.identity, accentMat);
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
                part.GetComponent<Renderer>().material = material;

            return part;
        }

        static Material CreateBodyMaterial(BuildArgs args)
        {
            if (args.BodyTemplate == null)
                return null;

            var mat = new Material(args.BodyTemplate);
            mat.SetColor("_BaseColor", args.BodyColor);
            mat.SetFloat("_Metallic", 0.55f);
            mat.SetFloat("_Smoothness", 0.72f);
            return mat;
        }

        static Material CreateAccentMaterial(BuildArgs args)
        {
            if (args.AccentTemplate == null)
                return null;

            var mat = new Material(args.AccentTemplate);
            mat.SetColor("_BaseColor", args.AccentEmission * 0.15f);
            mat.SetColor("_EmissionColor", args.AccentEmission);
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Smoothness", 0.85f);
            return mat;
        }

        static Material CreateTrimMaterial(Color bodyColor, Material bodyTemplate)
        {
            if (bodyTemplate == null)
                return null;

            var mat = new Material(bodyTemplate);
            var trimColor = new Color(bodyColor.r * 0.35f, bodyColor.g * 0.35f, bodyColor.b * 0.35f, 1f);
            mat.SetColor("_BaseColor", trimColor);
            mat.SetFloat("_Metallic", 0.65f);
            mat.SetFloat("_Smoothness", 0.55f);
            return mat;
        }
    }
}
