using NeonLap.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.VFX
{
    public static class NightTrackVisuals
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        static readonly float[] EmissionBoostByLevel = { 2.2f, 2.35f, 2.5f, 2.65f, 2.85f, 3.05f, 3.15f };
        static readonly float[] EdgeTintByLevel = { 0.32f, 0.34f, 0.35f, 0.36f, 0.38f, 0.4f, 0.41f };
        static readonly float[] DirectionalLightScaleByLevel = { 0.44f, 0.42f, 0.4f, 0.38f, 0.36f, 0.34f, 0.33f };

        public static void Apply(Material trackEdgeMaterial, int levelIndex = 0)
        {
            if (!GameTrackOptions.NightVariant)
                return;

            levelIndex = Mathf.Clamp(levelIndex, 0, EmissionBoostByLevel.Length - 1);
            BoostTrackEdgeEmission(trackEdgeMaterial, EmissionBoostByLevel[levelIndex], EdgeTintByLevel[levelIndex]);
            DimSunlight(DirectionalLightScaleByLevel[levelIndex]);

            RenderSettings.ambientSkyColor = new Color(0.04f, 0.05f, 0.1f);
            RenderSettings.ambientEquatorColor = new Color(0.03f, 0.03f, 0.07f);
            RenderSettings.ambientGroundColor = new Color(0.01f, 0.01f, 0.03f);
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.06f);
        }

        static void DimSunlight(float directionalLightScale)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var light in lights)
            {
                if (light.type != LightType.Directional)
                    continue;

                light.intensity *= directionalLightScale;
                light.color = new Color(0.55f, 0.62f, 0.95f);
                break;
            }
        }

        static void BoostTrackEdgeEmission(Material trackEdgeMaterial, float emissionBoost, float edgeTint)
        {
            if (trackEdgeMaterial == null)
                return;

            trackEdgeMaterial.EnableKeyword("_EMISSION");
            if (trackEdgeMaterial.HasProperty(EmissionColorId))
            {
                var emission = trackEdgeMaterial.GetColor(EmissionColorId);
                trackEdgeMaterial.SetColor(EmissionColorId, emission * emissionBoost + new Color(0.35f, 1.4f, 1.6f));
            }

            if (trackEdgeMaterial.HasProperty(BaseColorId))
            {
                var baseColor = trackEdgeMaterial.GetColor(BaseColorId);
                trackEdgeMaterial.SetColor(BaseColorId, Color.Lerp(baseColor, new Color(0.2f, 0.95f, 1f), edgeTint));
            }
        }
    }
}
