using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public static class PodiumBuilder
    {
        public struct Slot
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public float PlatformHeight;
            public string Label;
            public Color AccentColor;
        }

        public struct BuiltPodium
        {
            public Transform Root;
            public Slot[] Slots;
            public Vector3 CameraPosition;
            public Vector3 LookTarget;
        }

        public static BuiltPodium Build(Vector3 worldCenter, Quaternion worldRotation, Material baseMaterial,
            Material accentMaterial)
        {
            var root = new GameObject("RacePodium").transform;
            root.SetPositionAndRotation(worldCenter, worldRotation);

            var slots = new[]
            {
                new Slot
                {
                    LocalPosition = new Vector3(-3.4f, 1.35f, 0f),
                    LocalRotation = Quaternion.identity,
                    PlatformHeight = 1.35f,
                    Label = "2ND",
                    AccentColor = new Color(0.78f, 0.82f, 0.9f)
                },
                new Slot
                {
                    LocalPosition = new Vector3(0f, 2.15f, 0f),
                    LocalRotation = Quaternion.identity,
                    PlatformHeight = 2.15f,
                    Label = "1ST",
                    AccentColor = new Color(1f, 0.86f, 0.28f)
                },
                new Slot
                {
                    LocalPosition = new Vector3(3.4f, 0.85f, 0f),
                    LocalRotation = Quaternion.identity,
                    PlatformHeight = 0.85f,
                    Label = "3RD",
                    AccentColor = new Color(0.92f, 0.55f, 0.28f)
                },
            };

            CreatePlatform(root, baseMaterial, accentMaterial, new Vector3(0f, 0.08f, 0f),
                new Vector3(11.5f, 0.16f, 4.8f));

            CreateStep(root, baseMaterial, accentMaterial, slots[0], 0);
            CreateStep(root, baseMaterial, accentMaterial, slots[1], 1);
            CreateStep(root, baseMaterial, accentMaterial, slots[2], 2);

            CreateNeonStrip(root, accentMaterial, new Vector3(0f, 0.18f, 2.35f), new Vector3(11f, 0.08f, 0.12f),
                new Color(0.2f, 1f, 1f, 1f));

            var cameraPos = worldCenter + worldRotation * new Vector3(0f, 3.8f, -9.5f);
            var lookTarget = worldCenter + worldRotation * new Vector3(0f, 1.8f, 0.5f);

            return new BuiltPodium
            {
                Root = root,
                Slots = slots,
                CameraPosition = cameraPos,
                LookTarget = lookTarget
            };
        }

        static void CreateStep(Transform root, Material baseMaterial, Material accentMaterial, Slot slot, int index)
        {
            var stepRoot = new GameObject("PodiumStep_" + slot.Label).transform;
            stepRoot.SetParent(root, false);
            stepRoot.localPosition = new Vector3(slot.LocalPosition.x, slot.PlatformHeight * 0.5f, slot.LocalPosition.z);

            CreatePlatform(stepRoot, baseMaterial, accentMaterial, Vector3.zero,
                new Vector3(3.1f, slot.PlatformHeight, 2.8f), slot.AccentColor);

            CreateLabel(stepRoot, slot.Label, slot.AccentColor,
                new Vector3(0f, slot.PlatformHeight * 0.5f + 0.55f, -1.05f));
            CreateLabel(stepRoot, (index + 1).ToString(), slot.AccentColor,
                new Vector3(0f, slot.PlatformHeight * 0.5f + 1.05f, -1.05f), 96);
        }

        static void CreatePlatform(Transform parent, Material baseMaterial, Material accentMaterial, Vector3 localPos,
            Vector3 size, Color? accent = null)
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.SetParent(parent, false);
            platform.transform.localPosition = localPos;
            platform.transform.localScale = size;
            platform.layer = NeonLapLayers.Track;
            platform.tag = "Track";
            platform.GetComponent<Renderer>().sharedMaterial = baseMaterial;

            var platformCollider = platform.GetComponent<BoxCollider>();
            if (platformCollider != null)
                platformCollider.isTrigger = false;

            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "Trim";
            trim.transform.SetParent(platform.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            trim.transform.localScale = new Vector3(1.02f, 0.08f, 1.04f);
            Object.Destroy(trim.GetComponent<Collider>());
            trim.layer = NeonLapLayers.Track;

            var trimMat = new Material(accentMaterial);
            trimMat.SetColor("_BaseColor", accent ?? new Color(0.15f, 0.85f, 1f));
            trimMat.SetColor("_EmissionColor", (accent ?? new Color(0.15f, 0.85f, 1f)) * 2.2f);
            trimMat.EnableKeyword("_EMISSION");
            trim.GetComponent<Renderer>().sharedMaterial = trimMat;
        }

        static void CreateNeonStrip(Transform parent, Material accentMaterial, Vector3 localPos, Vector3 size,
            Color color)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "NeonStrip";
            strip.transform.SetParent(parent, false);
            strip.transform.localPosition = localPos;
            strip.transform.localScale = size;
            Object.Destroy(strip.GetComponent<Collider>());

            var mat = new Material(accentMaterial);
            mat.SetColor("_BaseColor", color * 0.25f);
            mat.SetColor("_EmissionColor", color * 3f);
            mat.EnableKeyword("_EMISSION");
            strip.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void CreateLabel(Transform parent, string text, Color color, Vector3 localPos, int fontSize = 54)
        {
            var labelGo = new GameObject("Label_" + text);
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = localPos;
            labelGo.transform.localRotation = Quaternion.identity;

            var textMesh = labelGo.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.fontSize = fontSize;
            textMesh.characterSize = fontSize > 60 ? 0.07f : 0.05f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.fontStyle = FontStyle.Bold;
        }
    }
}
