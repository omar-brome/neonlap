using UnityEngine;

namespace NeonLap.VFX
{
    /// <summary>Animates track asphalt smoothness/metallic for rain-soaked reflections.</summary>
    public class WetTrackSurfaceController : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        Material surfaceMaterial;
        Color dryBaseColor;
        float drySmoothness = 0.16f;
        float dryMetallic = 0.02f;
        float currentWetness;

        public static WetTrackSurfaceController Instance { get; private set; }

        public static void Register(Material surface)
        {
            if (surface == null)
                return;

            var controller = Instance;
            if (controller == null)
            {
                var go = new GameObject("WetTrackSurface");
                controller = go.AddComponent<WetTrackSurfaceController>();
            }

            controller.Bind(surface);
        }

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Bind(Material surface)
        {
            surfaceMaterial = surface;
            if (surfaceMaterial == null)
                return;

            if (surfaceMaterial.HasProperty(BaseColorId))
                dryBaseColor = surfaceMaterial.GetColor(BaseColorId);
            if (surfaceMaterial.HasProperty(SmoothnessId))
                drySmoothness = surfaceMaterial.GetFloat(SmoothnessId);
            if (surfaceMaterial.HasProperty(MetallicId))
                dryMetallic = surfaceMaterial.GetFloat(MetallicId);

            ApplyWetness(currentWetness);
        }

        public void SetWetness(float wetness)
        {
            currentWetness = Mathf.Clamp01(wetness);
            ApplyWetness(currentWetness);
        }

        void ApplyWetness(float wetness)
        {
            if (surfaceMaterial == null)
                return;

            var wetColor = dryBaseColor * 0.68f + new Color(0.04f, 0.05f, 0.07f);
            var smoothness = Mathf.Lerp(drySmoothness, 0.84f, wetness);
            var metallic = Mathf.Lerp(dryMetallic, 0.48f, wetness);

            if (surfaceMaterial.HasProperty(BaseColorId))
                surfaceMaterial.SetColor(BaseColorId, Color.Lerp(dryBaseColor, wetColor, wetness * 0.72f));
            if (surfaceMaterial.HasProperty(SmoothnessId))
                surfaceMaterial.SetFloat(SmoothnessId, smoothness);
            if (surfaceMaterial.HasProperty(MetallicId))
                surfaceMaterial.SetFloat(MetallicId, metallic);
        }
    }
}
