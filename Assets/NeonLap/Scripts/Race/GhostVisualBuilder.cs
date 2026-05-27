using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public static class GhostVisualBuilder
    {
        static readonly Color GhostBody = new(0.35f, 0.95f, 1f, 0.42f);
        static readonly Color GhostAccent = new(0.2f, 1.8f, 2.2f, 0.55f);

        public static void Build(Transform root, Material bodyTemplate, Material accentTemplate, float alphaMultiplier)
        {
            Build(root, bodyTemplate, accentTemplate, GhostBody, GhostAccent, alphaMultiplier);
        }

        public static void Build(Transform root, Material bodyTemplate, Material accentTemplate, Color bodyColor,
            Color accentColor, float alphaMultiplier)
        {
            var body = bodyTemplate != null ? new Material(bodyTemplate) : CreateLitMaterial(bodyColor);
            var accent = accentTemplate != null ? new Material(accentTemplate) : CreateLitMaterial(accentColor);
            ApplyGhostMaterial(body, bodyColor, alphaMultiplier);
            ApplyGhostMaterial(accent, accentColor, alphaMultiplier * 1.1f);

            HoverCarVisualBuilder.Build(root,
                new HoverCarVisualBuilder.BuildArgs(body, accent, bodyColor, accentColor, false));
            DisableGameplay(root);
        }

        static Material CreateLitMaterial(Color color)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(lit);
            mat.SetColor("_BaseColor", color);
            return mat;
        }

        static void ApplyGhostMaterial(Material material, Color baseColor, float alpha)
        {
            if (material == null)
                return;

            var color = baseColor;
            color.a = Mathf.Clamp01(baseColor.a * alpha);
            material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        static void DisableGameplay(Transform root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                Object.Destroy(collider);

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>())
                Object.Destroy(rb);

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour is GhostRacer)
                    continue;

                behaviour.enabled = false;
            }
        }
    }
}
