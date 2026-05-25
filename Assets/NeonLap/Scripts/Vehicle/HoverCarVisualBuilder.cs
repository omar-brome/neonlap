using UnityEngine;

namespace NeonLap.Vehicle
{
    public static class HoverCarVisualBuilder
    {
        public readonly struct BuildArgs
        {
            public BuildArgs(Material bodyTemplate, Material accentTemplate, Color bodyColor, Color accentEmission,
                bool isPlayer = false)
            {
                BodyTemplate = bodyTemplate;
                AccentTemplate = accentTemplate;
                BodyColor = bodyColor;
                AccentEmission = accentEmission;
                IsPlayer = isPlayer;
            }

            public Material BodyTemplate { get; }
            public Material AccentTemplate { get; }
            public Color BodyColor { get; }
            public Color AccentEmission { get; }
            public bool IsPlayer { get; }
        }

        public static Transform Build(Transform root, BuildArgs args)
        {
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(root, false);

            var bodyMat = CreateBodyMaterial(args);
            var accentMat = CreateAccentMaterial(args);
            var trimMat = CreateTrimMaterial(args.BodyColor, args.BodyTemplate);
            var glassMat = CreateGlassMaterial(args.AccentTemplate, args.AccentEmission);
            var carbonMat = CreateCarbonMaterial(args.BodyTemplate);
            var stripeMat = CreateStripeMaterial(args.BodyColor, args.AccentEmission, args.BodyTemplate);

            BuildChassis(visualRoot, bodyMat, trimMat, carbonMat, stripeMat);
            BuildCockpit(visualRoot, bodyMat, accentMat, trimMat, glassMat);
            BuildAero(visualRoot, args, bodyMat, accentMat, trimMat, carbonMat);
            BuildHoverPods(visualRoot, accentMat, trimMat, carbonMat);
            BuildLighting(visualRoot, accentMat, trimMat, carbonMat);
            BuildSurfaceDetails(visualRoot, bodyMat, accentMat, trimMat, carbonMat);
            MarkDetachableParts(visualRoot);

            return visualRoot;
        }

        public static void MarkDetachableParts(Transform visualRoot)
        {
            if (visualRoot == null)
                return;

            foreach (var partTransform in visualRoot.GetComponentsInChildren<Transform>(true))
            {
                if (partTransform == visualRoot)
                    continue;

                if (IsProtectedPart(partTransform.name))
                    continue;

                if (partTransform.GetComponent<DetachableVehiclePart>() != null)
                    continue;

                var part = partTransform.gameObject.AddComponent<DetachableVehiclePart>();
                part.Configure(
                    DetachableVehiclePart.EstimateMass(partTransform),
                    DetachableVehiclePart.EstimateBreakThreshold(partTransform.name));
            }
        }

        static bool IsProtectedPart(string partName)
        {
            if (partName.StartsWith("Pod"))
                return true;

            return partName is "Chassis" or "Cabin" or "NoseLower" or "RearDeck" or "SkirtL" or "SkirtR"
                or "Diffuser" or "DashAccent" or "SpoilerPedestal" or "SpoilerWingMain" or "SpoilerWingTop"
                || partName.Contains("SteerPivot") || partName.Contains("SpinPivot");
        }

        static void BuildChassis(Transform root, Material bodyMat, Material trimMat, Material carbonMat,
            Material stripeMat)
        {
            AddPart(root, "Chassis", PrimitiveType.Cube,
                new Vector3(0f, 0.14f, 0f), new Vector3(1.55f, 0.22f, 2.55f), Quaternion.identity, bodyMat);

            AddPart(root, "NoseUpper", PrimitiveType.Cube,
                new Vector3(0f, 0.2f, 1.08f), new Vector3(0.88f, 0.14f, 0.72f), Quaternion.identity, bodyMat);
            AddPart(root, "NoseLower", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, 1.12f), new Vector3(1.02f, 0.1f, 0.55f),
                Quaternion.Euler(-8f, 0f, 0f), bodyMat);

            AddPart(root, "FenderFL", PrimitiveType.Cube,
                new Vector3(-0.72f, 0.2f, 0.55f), new Vector3(0.22f, 0.14f, 1.05f),
                Quaternion.Euler(0f, 0f, 14f), bodyMat);
            AddPart(root, "FenderFR", PrimitiveType.Cube,
                new Vector3(0.72f, 0.2f, 0.55f), new Vector3(0.22f, 0.14f, 1.05f),
                Quaternion.Euler(0f, 0f, -14f), bodyMat);
            AddPart(root, "FenderRL", PrimitiveType.Cube,
                new Vector3(-0.74f, 0.2f, -0.55f), new Vector3(0.2f, 0.12f, 0.95f),
                Quaternion.Euler(0f, 0f, 10f), bodyMat);
            AddPart(root, "FenderRR", PrimitiveType.Cube,
                new Vector3(0.74f, 0.2f, -0.55f), new Vector3(0.2f, 0.12f, 0.95f),
                Quaternion.Euler(0f, 0f, -10f), bodyMat);

            AddPart(root, "RearDeck", PrimitiveType.Cube,
                new Vector3(0f, 0.24f, -0.92f), new Vector3(1.38f, 0.12f, 0.82f), Quaternion.identity, bodyMat);
            AddPart(root, "RearEngineCover", PrimitiveType.Cube,
                new Vector3(0f, 0.32f, -0.72f), new Vector3(0.95f, 0.1f, 0.55f),
                Quaternion.Euler(-6f, 0f, 0f), bodyMat);

            AddPart(root, "FrontSplitter", PrimitiveType.Cube,
                new Vector3(0f, 0.06f, 1.38f), new Vector3(1.62f, 0.04f, 0.28f),
                Quaternion.Euler(4f, 0f, 0f), carbonMat);
            AddPart(root, "SplitterStrutL", PrimitiveType.Cube,
                new Vector3(-0.48f, 0.1f, 1.22f), new Vector3(0.06f, 0.06f, 0.18f), Quaternion.identity, trimMat);
            AddPart(root, "SplitterStrutR", PrimitiveType.Cube,
                new Vector3(0.48f, 0.1f, 1.22f), new Vector3(0.06f, 0.06f, 0.18f), Quaternion.identity, trimMat);

            AddPart(root, "HoodStripe", PrimitiveType.Cube,
                new Vector3(0f, 0.26f, 0.72f), new Vector3(0.12f, 0.02f, 1.05f), Quaternion.identity, stripeMat);
        }

        static void BuildCockpit(Transform root, Material bodyMat, Material accentMat, Material trimMat,
            Material glassMat)
        {
            AddPart(root, "Cabin", PrimitiveType.Cube,
                new Vector3(0f, 0.36f, 0.12f), new Vector3(1.02f, 0.28f, 1.02f), Quaternion.identity, bodyMat);
            AddPart(root, "CabinSideL", PrimitiveType.Cube,
                new Vector3(-0.54f, 0.34f, 0.08f), new Vector3(0.06f, 0.18f, 0.82f),
                Quaternion.Euler(0f, 0f, 12f), trimMat);
            AddPart(root, "CabinSideR", PrimitiveType.Cube,
                new Vector3(0.54f, 0.34f, 0.08f), new Vector3(0.06f, 0.18f, 0.82f),
                Quaternion.Euler(0f, 0f, -12f), trimMat);

            AddPart(root, "Canopy", PrimitiveType.Cube,
                new Vector3(0f, 0.5f, 0.18f), new Vector3(0.78f, 0.1f, 0.68f),
                Quaternion.Euler(10f, 0f, 0f), glassMat);
            AddPart(root, "WindshieldFrame", PrimitiveType.Cube,
                new Vector3(0f, 0.46f, 0.52f), new Vector3(0.86f, 0.06f, 0.08f),
                Quaternion.Euler(18f, 0f, 0f), trimMat);
            AddPart(root, "RearWindow", PrimitiveType.Cube,
                new Vector3(0f, 0.44f, -0.28f), new Vector3(0.72f, 0.08f, 0.06f),
                Quaternion.Euler(-22f, 0f, 0f), glassMat);

            AddPart(root, "RollHoopL", PrimitiveType.Cube,
                new Vector3(-0.38f, 0.52f, -0.08f), new Vector3(0.05f, 0.18f, 0.05f),
                Quaternion.Euler(0f, 0f, 8f), trimMat);
            AddPart(root, "RollHoopR", PrimitiveType.Cube,
                new Vector3(0.38f, 0.52f, -0.08f), new Vector3(0.05f, 0.18f, 0.05f),
                Quaternion.Euler(0f, 0f, -8f), trimMat);
            AddPart(root, "RollHoopTop", PrimitiveType.Cube,
                new Vector3(0f, 0.58f, -0.08f), new Vector3(0.72f, 0.04f, 0.05f), Quaternion.identity, trimMat);

            AddPart(root, "DashAccent", PrimitiveType.Cube,
                new Vector3(0f, 0.38f, 0.48f), new Vector3(0.62f, 0.04f, 0.06f),
                Quaternion.Euler(14f, 0f, 0f), accentMat);
        }

        static void BuildAero(Transform root, BuildArgs args, Material bodyMat, Material accentMat, Material trimMat,
            Material carbonMat)
        {
            AddPart(root, "SkirtL", PrimitiveType.Cube,
                new Vector3(-0.8f, 0.1f, 0f), new Vector3(0.1f, 0.1f, 2.15f), Quaternion.identity, trimMat);
            AddPart(root, "SkirtR", PrimitiveType.Cube,
                new Vector3(0.8f, 0.1f, 0f), new Vector3(0.1f, 0.1f, 2.15f), Quaternion.identity, trimMat);

            if (!args.IsPlayer)
            {
                AddPart(root, "UnderglowL", PrimitiveType.Cube,
                    new Vector3(-0.76f, 0.04f, 0f), new Vector3(0.04f, 0.02f, 1.85f), Quaternion.identity, accentMat);
                AddPart(root, "UnderglowR", PrimitiveType.Cube,
                    new Vector3(0.76f, 0.04f, 0f), new Vector3(0.04f, 0.02f, 1.85f), Quaternion.identity, accentMat);
            }

            AddPart(root, "DivePlaneL", PrimitiveType.Cube,
                new Vector3(-0.62f, 0.14f, 1.05f), new Vector3(0.22f, 0.03f, 0.42f),
                Quaternion.Euler(8f, 0f, 22f), carbonMat);
            AddPart(root, "DivePlaneR", PrimitiveType.Cube,
                new Vector3(0.62f, 0.14f, 1.05f), new Vector3(0.22f, 0.03f, 0.42f),
                Quaternion.Euler(8f, 0f, -22f), carbonMat);

            AddPart(root, "SideWingL", PrimitiveType.Cube,
                new Vector3(-0.82f, 0.22f, -0.15f), new Vector3(0.14f, 0.04f, 0.38f),
                Quaternion.Euler(0f, 0f, 28f), carbonMat);
            AddPart(root, "SideWingR", PrimitiveType.Cube,
                new Vector3(0.82f, 0.22f, -0.15f), new Vector3(0.14f, 0.04f, 0.38f),
                Quaternion.Euler(0f, 0f, -28f), carbonMat);

            if (args.IsPlayer)
                BuildSportSpoiler(root, accentMat, trimMat, carbonMat);
            else
                BuildStandardSpoiler(root, accentMat, trimMat, carbonMat);

            AddPart(root, "Diffuser", PrimitiveType.Cube,
                new Vector3(0f, 0.08f, -1.32f), new Vector3(1.42f, 0.06f, 0.32f),
                Quaternion.Euler(-10f, 0f, 0f), carbonMat);
            AddPart(root, "DiffuserFinL", PrimitiveType.Cube,
                new Vector3(-0.42f, 0.12f, -1.28f), new Vector3(0.04f, 0.1f, 0.22f),
                Quaternion.Euler(-18f, 0f, 8f), carbonMat);
            AddPart(root, "DiffuserFinC", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, -1.3f), new Vector3(0.04f, 0.1f, 0.22f),
                Quaternion.Euler(-18f, 0f, 0f), carbonMat);
            AddPart(root, "DiffuserFinR", PrimitiveType.Cube,
                new Vector3(0.42f, 0.12f, -1.28f), new Vector3(0.04f, 0.1f, 0.22f),
                Quaternion.Euler(-18f, 0f, -8f), carbonMat);

            AddPart(root, "IntakeL", PrimitiveType.Cube,
                new Vector3(-0.5f, 0.28f, -0.32f), new Vector3(0.14f, 0.1f, 0.48f),
                Quaternion.Euler(0f, 0f, 18f), trimMat);
            AddPart(root, "IntakeR", PrimitiveType.Cube,
                new Vector3(0.5f, 0.28f, -0.32f), new Vector3(0.14f, 0.1f, 0.48f),
                Quaternion.Euler(0f, 0f, -18f), trimMat);
            AddPart(root, "IntakeGlowL", PrimitiveType.Cube,
                new Vector3(-0.5f, 0.28f, -0.08f), new Vector3(0.06f, 0.04f, 0.18f),
                Quaternion.Euler(0f, 0f, 18f), accentMat);
            AddPart(root, "IntakeGlowR", PrimitiveType.Cube,
                new Vector3(0.5f, 0.28f, -0.08f), new Vector3(0.06f, 0.04f, 0.18f),
                Quaternion.Euler(0f, 0f, -18f), accentMat);
        }

        static void BuildStandardSpoiler(Transform root, Material accentMat, Material trimMat, Material carbonMat)
        {
            AddPart(root, "SpoilerStrutL", PrimitiveType.Cube,
                new Vector3(-0.48f, 0.4f, -1.18f), new Vector3(0.08f, 0.24f, 0.08f), Quaternion.identity, trimMat);
            AddPart(root, "SpoilerStrutR", PrimitiveType.Cube,
                new Vector3(0.48f, 0.4f, -1.18f), new Vector3(0.08f, 0.24f, 0.08f), Quaternion.identity, trimMat);
            AddPart(root, "Spoiler", PrimitiveType.Cube,
                new Vector3(0f, 0.52f, -1.24f), new Vector3(1.62f, 0.06f, 0.24f), Quaternion.identity, accentMat);
            AddPart(root, "SpoilerEndL", PrimitiveType.Cube,
                new Vector3(-0.82f, 0.5f, -1.24f), new Vector3(0.06f, 0.14f, 0.18f), Quaternion.identity, carbonMat);
            AddPart(root, "SpoilerEndR", PrimitiveType.Cube,
                new Vector3(0.82f, 0.5f, -1.24f), new Vector3(0.06f, 0.14f, 0.18f), Quaternion.identity, carbonMat);
            AddPart(root, "GurneyFlap", PrimitiveType.Cube,
                new Vector3(0f, 0.56f, -1.12f), new Vector3(1.48f, 0.04f, 0.06f),
                Quaternion.Euler(12f, 0f, 0f), carbonMat);
        }

        static void BuildSportSpoiler(Transform root, Material accentMat, Material trimMat, Material carbonMat)
        {
            AddPart(root, "SpoilerPedestal", PrimitiveType.Cube,
                new Vector3(0f, 0.34f, -1.02f), new Vector3(0.42f, 0.08f, 0.28f), Quaternion.identity, carbonMat);

            AddPart(root, "SpoilerStrutL", PrimitiveType.Cube,
                new Vector3(-0.56f, 0.48f, -1.2f), new Vector3(0.1f, 0.34f, 0.1f), Quaternion.identity, trimMat);
            AddPart(root, "SpoilerStrutR", PrimitiveType.Cube,
                new Vector3(0.56f, 0.48f, -1.2f), new Vector3(0.1f, 0.34f, 0.1f), Quaternion.identity, trimMat);

            AddPart(root, "SpoilerWingMain", PrimitiveType.Cube,
                new Vector3(0f, 0.66f, -1.28f), new Vector3(1.95f, 0.08f, 0.34f),
                Quaternion.Euler(10f, 0f, 0f), carbonMat);
            AddPart(root, "SpoilerWingTop", PrimitiveType.Cube,
                new Vector3(0f, 0.74f, -1.22f), new Vector3(1.72f, 0.04f, 0.18f),
                Quaternion.Euler(14f, 0f, 0f), accentMat);

            AddPart(root, "SpoilerEndL", PrimitiveType.Cube,
                new Vector3(-0.98f, 0.64f, -1.26f), new Vector3(0.08f, 0.28f, 0.24f),
                Quaternion.Euler(10f, 0f, 0f), carbonMat);
            AddPart(root, "SpoilerEndR", PrimitiveType.Cube,
                new Vector3(0.98f, 0.64f, -1.26f), new Vector3(0.08f, 0.28f, 0.24f),
                Quaternion.Euler(10f, 0f, 0f), carbonMat);

            AddPart(root, "Spoiler", PrimitiveType.Cube,
                new Vector3(0f, 0.58f, -1.34f), new Vector3(1.55f, 0.03f, 0.08f),
                Quaternion.Euler(18f, 0f, 0f), accentMat);
            AddPart(root, "GurneyFlap", PrimitiveType.Cube,
                new Vector3(0f, 0.7f, -1.1f), new Vector3(1.62f, 0.05f, 0.08f),
                Quaternion.Euler(16f, 0f, 0f), accentMat);
        }

        static void BuildHoverPods(Transform root, Material accentMat, Material trimMat, Material carbonMat)
        {
            AddHoverPod(root, "PodFL", new Vector3(-0.62f, 0.06f, 0.85f), accentMat, trimMat, carbonMat, true);
            AddHoverPod(root, "PodFR", new Vector3(0.62f, 0.06f, 0.85f), accentMat, trimMat, carbonMat, true);
            AddHoverPod(root, "PodRL", new Vector3(-0.62f, 0.06f, -0.85f), accentMat, trimMat, carbonMat, false);
            AddHoverPod(root, "PodRR", new Vector3(0.62f, 0.06f, -0.85f), accentMat, trimMat, carbonMat, false);
        }

        static void BuildLighting(Transform root, Material accentMat, Material trimMat, Material carbonMat)
        {
            var tailLightMat = CreateTailLightMaterial(accentMat);
            var brakeLightMat = CreateBrakeLightMaterial(accentMat);
            var turnSignalMat = CreateTurnSignalMaterial(accentMat);

            AddPart(root, "HeadlightBezelL", PrimitiveType.Cylinder,
                new Vector3(-0.42f, 0.2f, 1.4f), new Vector3(0.2f, 0.04f, 0.2f),
                Quaternion.Euler(90f, 0f, 0f), trimMat);
            AddPart(root, "HeadlightBezelR", PrimitiveType.Cylinder,
                new Vector3(0.42f, 0.2f, 1.4f), new Vector3(0.2f, 0.04f, 0.2f),
                Quaternion.Euler(90f, 0f, 0f), trimMat);
            AddPart(root, "HeadlightL", PrimitiveType.Sphere,
                new Vector3(-0.42f, 0.2f, 1.44f), new Vector3(0.14f, 0.1f, 0.06f), Quaternion.identity, accentMat);
            AddPart(root, "HeadlightR", PrimitiveType.Sphere,
                new Vector3(0.42f, 0.2f, 1.44f), new Vector3(0.14f, 0.1f, 0.06f), Quaternion.identity, accentMat);

            AddPart(root, "TailLightBar", PrimitiveType.Cube,
                new Vector3(0f, 0.26f, -1.3f), new Vector3(0.72f, 0.05f, 0.04f), Quaternion.identity, tailLightMat);
            AddPart(root, "TailLightL", PrimitiveType.Cube,
                new Vector3(-0.58f, 0.26f, -1.3f), new Vector3(0.22f, 0.08f, 0.05f), Quaternion.identity, tailLightMat);
            AddPart(root, "TailLightR", PrimitiveType.Cube,
                new Vector3(0.58f, 0.26f, -1.3f), new Vector3(0.22f, 0.08f, 0.05f), Quaternion.identity, tailLightMat);
            AddPart(root, "BrakeLight", PrimitiveType.Cube,
                new Vector3(0f, 0.32f, -1.28f), new Vector3(0.38f, 0.04f, 0.04f), Quaternion.identity, brakeLightMat);

            AddPart(root, "ReverseLightL", PrimitiveType.Cube,
                new Vector3(-0.28f, 0.22f, -1.31f), new Vector3(0.1f, 0.04f, 0.03f), Quaternion.identity, trimMat);
            AddPart(root, "ReverseLightR", PrimitiveType.Cube,
                new Vector3(0.28f, 0.22f, -1.31f), new Vector3(0.1f, 0.04f, 0.03f), Quaternion.identity, trimMat);

            AddPart(root, "TurnSignalFL", PrimitiveType.Cube,
                new Vector3(-0.74f, 0.24f, 0.95f), new Vector3(0.08f, 0.05f, 0.04f),
                Quaternion.Euler(0f, -24f, 0f), turnSignalMat);
            AddPart(root, "TurnSignalFR", PrimitiveType.Cube,
                new Vector3(0.74f, 0.24f, 0.95f), new Vector3(0.08f, 0.05f, 0.04f),
                Quaternion.Euler(0f, 24f, 0f), turnSignalMat);
            AddPart(root, "TurnSignalRL", PrimitiveType.Cube,
                new Vector3(-0.62f, 0.26f, -1.32f), new Vector3(0.1f, 0.06f, 0.04f), Quaternion.identity, turnSignalMat);
            AddPart(root, "TurnSignalRR", PrimitiveType.Cube,
                new Vector3(0.62f, 0.26f, -1.32f), new Vector3(0.1f, 0.06f, 0.04f), Quaternion.identity, turnSignalMat);
        }

        static void BuildSurfaceDetails(Transform root, Material bodyMat, Material accentMat, Material trimMat,
            Material carbonMat)
        {
            AddPart(root, "MirrorL", PrimitiveType.Cube,
                new Vector3(-0.58f, 0.42f, 0.38f), new Vector3(0.08f, 0.05f, 0.12f),
                Quaternion.Euler(0f, -18f, 0f), trimMat);
            AddPart(root, "MirrorR", PrimitiveType.Cube,
                new Vector3(0.58f, 0.42f, 0.38f), new Vector3(0.08f, 0.05f, 0.12f),
                Quaternion.Euler(0f, 18f, 0f), trimMat);

            AddPart(root, "Antenna", PrimitiveType.Cylinder,
                new Vector3(0.18f, 0.58f, -0.05f), new Vector3(0.03f, 0.12f, 0.03f), Quaternion.identity, trimMat);
            AddPart(root, "AntennaTip", PrimitiveType.Sphere,
                new Vector3(0.18f, 0.66f, -0.05f), new Vector3(0.05f, 0.05f, 0.05f), Quaternion.identity, accentMat);

            AddPart(root, "VentSlotL", PrimitiveType.Cube,
                new Vector3(-0.32f, 0.34f, -0.62f), new Vector3(0.04f, 0.02f, 0.22f),
                Quaternion.Euler(-8f, 0f, 0f), carbonMat);
            AddPart(root, "VentSlotR", PrimitiveType.Cube,
                new Vector3(0.32f, 0.34f, -0.62f), new Vector3(0.04f, 0.02f, 0.22f),
                Quaternion.Euler(-8f, 0f, 0f), carbonMat);
            AddPart(root, "VentSlotC", PrimitiveType.Cube,
                new Vector3(0f, 0.34f, -0.64f), new Vector3(0.04f, 0.02f, 0.22f),
                Quaternion.Euler(-8f, 0f, 0f), carbonMat);

            AddPart(root, "EnergyCore", PrimitiveType.Cylinder,
                new Vector3(0f, 0.28f, -1.02f), new Vector3(0.28f, 0.04f, 0.28f),
                Quaternion.Euler(90f, 0f, 0f), accentMat);
            AddPart(root, "CoreRing", PrimitiveType.Cylinder,
                new Vector3(0f, 0.28f, -1.02f), new Vector3(0.36f, 0.02f, 0.36f),
                Quaternion.Euler(90f, 0f, 0f), trimMat);

            AddPart(root, "PanelLineL", PrimitiveType.Cube,
                new Vector3(-0.38f, 0.24f, 0.35f), new Vector3(0.02f, 0.01f, 0.85f), Quaternion.identity, trimMat);
            AddPart(root, "PanelLineR", PrimitiveType.Cube,
                new Vector3(0.38f, 0.24f, 0.35f), new Vector3(0.02f, 0.01f, 0.85f), Quaternion.identity, trimMat);

            AddPart(root, "NoseBadge", PrimitiveType.Cube,
                new Vector3(0f, 0.22f, 1.28f), new Vector3(0.18f, 0.04f, 0.04f), Quaternion.identity, accentMat);
            AddPart(root, "SideBadgeL", PrimitiveType.Cube,
                new Vector3(-0.68f, 0.26f, 0.05f), new Vector3(0.02f, 0.12f, 0.28f),
                Quaternion.Euler(0f, 0f, 6f), bodyMat);
            AddPart(root, "SideBadgeR", PrimitiveType.Cube,
                new Vector3(0.68f, 0.26f, 0.05f), new Vector3(0.02f, 0.12f, 0.28f),
                Quaternion.Euler(0f, 0f, -6f), bodyMat);
        }

        static void AddHoverPod(Transform parent, string name, Vector3 position, Material accentMat, Material trimMat,
            Material carbonMat, bool steerable)
        {
            AddPart(parent, name + "Arm", PrimitiveType.Cube,
                position + new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.06f, 0.12f), Quaternion.identity, trimMat);

            var wheelRoot = CreatePivot(parent, steerable ? name + "SteerPivot" : name + "SpinPivot", position);
            Transform spinRoot;
            if (steerable)
                spinRoot = CreatePivot(wheelRoot, name + "SpinPivot", Vector3.zero);
            else
                spinRoot = wheelRoot;

            AddPart(spinRoot, name + "Housing", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.36f, 0.06f, 0.36f), Quaternion.identity, carbonMat);
            AddPart(spinRoot, name + "Ring", PrimitiveType.Cylinder,
                new Vector3(0f, -0.01f, 0f), new Vector3(0.42f, 0.015f, 0.42f),
                Quaternion.identity, trimMat);
            AddPart(spinRoot, name + "Glow", PrimitiveType.Cylinder,
                new Vector3(0f, -0.035f, 0f), new Vector3(0.26f, 0.025f, 0.26f),
                Quaternion.identity, accentMat);
            AddPart(spinRoot, name + "Core", PrimitiveType.Sphere,
                new Vector3(0f, -0.04f, 0f), new Vector3(0.12f, 0.04f, 0.12f),
                Quaternion.identity, accentMat);

            AddPart(spinRoot, name + "VaneN", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, 0.2f), new Vector3(0.04f, 0.06f, 0.1f), Quaternion.identity, trimMat);
            AddPart(spinRoot, name + "VaneS", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, -0.2f), new Vector3(0.04f, 0.06f, 0.1f), Quaternion.identity, trimMat);
            AddPart(spinRoot, name + "VaneE", PrimitiveType.Cube,
                new Vector3(0.2f, 0.02f, 0f), new Vector3(0.1f, 0.06f, 0.04f), Quaternion.identity, trimMat);
            AddPart(spinRoot, name + "VaneW", PrimitiveType.Cube,
                new Vector3(-0.2f, 0.02f, 0f), new Vector3(0.1f, 0.06f, 0.04f), Quaternion.identity, trimMat);
        }

        static Transform CreatePivot(Transform parent, string name, Vector3 localPosition)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            return pivot;
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
            mat.SetFloat("_Metallic", 0.62f);
            mat.SetFloat("_Smoothness", 0.78f);
            return mat;
        }

        static Material CreateAccentMaterial(BuildArgs args)
        {
            if (args.AccentTemplate == null)
                return null;

            var mat = new Material(args.AccentTemplate);
            mat.SetColor("_BaseColor", args.AccentEmission * 0.18f);
            mat.SetColor("_EmissionColor", args.AccentEmission);
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Smoothness", 0.92f);
            return mat;
        }

        static Material CreateTrimMaterial(Color bodyColor, Material bodyTemplate)
        {
            if (bodyTemplate == null)
                return null;

            var mat = new Material(bodyTemplate);
            var trimColor = new Color(bodyColor.r * 0.32f, bodyColor.g * 0.32f, bodyColor.b * 0.32f, 1f);
            mat.SetColor("_BaseColor", trimColor);
            mat.SetFloat("_Metallic", 0.72f);
            mat.SetFloat("_Smoothness", 0.58f);
            return mat;
        }

        static Material CreateGlassMaterial(Material accentTemplate, Color accentEmission)
        {
            if (accentTemplate == null)
                return null;

            var mat = new Material(accentTemplate);
            mat.SetColor("_BaseColor", new Color(0.04f, 0.07f, 0.11f, 1f));
            mat.SetFloat("_Metallic", 0.15f);
            mat.SetFloat("_Smoothness", 0.96f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", accentEmission * 0.06f);
            return mat;
        }

        static Material CreateCarbonMaterial(Material bodyTemplate)
        {
            if (bodyTemplate == null)
                return null;

            var mat = new Material(bodyTemplate);
            mat.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.06f));
            mat.SetFloat("_Metallic", 0.42f);
            mat.SetFloat("_Smoothness", 0.38f);
            return mat;
        }

        static Material CreateStripeMaterial(Color bodyColor, Color accentEmission, Material bodyTemplate)
        {
            if (bodyTemplate == null)
                return null;

            var mat = new Material(bodyTemplate);
            var stripe = Color.Lerp(bodyColor * 1.35f, accentEmission * 0.12f, 0.45f);
            mat.SetColor("_BaseColor", stripe);
            mat.SetFloat("_Metallic", 0.5f);
            mat.SetFloat("_Smoothness", 0.82f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", accentEmission * 0.25f);
            return mat;
        }

        static Material CreateTailLightMaterial(Material accentTemplate)
        {
            if (accentTemplate == null)
                return null;

            var mat = new Material(accentTemplate);
            mat.SetColor("_BaseColor", new Color(0.42f, 0.03f, 0.03f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(2.2f, 0.12f, 0.1f));
            mat.SetFloat("_Smoothness", 0.92f);
            return mat;
        }

        static Material CreateBrakeLightMaterial(Material accentTemplate)
        {
            if (accentTemplate == null)
                return null;

            var mat = new Material(accentTemplate);
            mat.SetColor("_BaseColor", new Color(0.18f, 0.02f, 0.02f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.35f, 0.03f, 0.03f));
            mat.SetFloat("_Smoothness", 0.94f);
            return mat;
        }

        static Material CreateTurnSignalMaterial(Material accentTemplate)
        {
            if (accentTemplate == null)
                return null;

            var mat = new Material(accentTemplate);
            mat.SetColor("_BaseColor", new Color(0.18f, 0.12f, 0.02f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.08f, 0.05f, 0.01f));
            mat.SetFloat("_Smoothness", 0.9f);
            return mat;
        }
    }
}
