using NeonLap.Audio;
using NeonLap.Core;
using NeonLap.Track;
using NeonLap.VFX;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.Rendering
{
    public class NeonTrackEdgePulseDriver : MonoBehaviour
    {
        public static readonly int GlobalPulseId = Shader.PropertyToID("_NeonLapEdgePulse");
        public static readonly int GlobalEmissionId = Shader.PropertyToID("_NeonLapEdgeEmission");

        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Material trackEdgeMaterial;
        [SerializeField] float pulseStrength = 1.35f;
        [SerializeField] float minPulse = 0.55f;

        Color baseEmission;
        Color baseColor;
        bool hasBaseColors;

        public static float GlobalPulse { get; private set; } = 1f;
        public static bool BlackoutActive { get; private set; }

        public static void SetBlackoutActive(bool active)
        {
            BlackoutActive = active;
        }

        public static void Ensure(Material edgeMaterial)
        {
            if (FindAnyObjectByType<NeonTrackEdgePulseDriver>() != null)
                return;

            var go = new GameObject("NeonTrackEdgePulse");
            var driver = go.AddComponent<NeonTrackEdgePulseDriver>();
            driver.trackEdgeMaterial = edgeMaterial;
            driver.CacheBaseColors();
        }

        void Awake() => CacheBaseColors();

        void CacheBaseColors()
        {
            if (trackEdgeMaterial == null)
                return;

            TrackRoadMarkingBuilder.ApplyNeonEdgeLook(trackEdgeMaterial);
            if (GameTrackOptions.NightVariant)
            {
                var levelIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
                NightTrackVisuals.Apply(trackEdgeMaterial, levelIndex);
            }

            if (trackEdgeMaterial.HasProperty(EmissionColorId))
            {
                baseEmission = trackEdgeMaterial.GetColor(EmissionColorId);
                hasBaseColors = true;
            }

            if (trackEdgeMaterial.HasProperty(BaseColorId))
                baseColor = trackEdgeMaterial.GetColor(BaseColorId);
        }

        void Update()
        {
            if (BlackoutActive)
            {
                GlobalPulse = 0f;
                Shader.SetGlobalFloat(GlobalPulseId, 0f);
                if (trackEdgeMaterial != null && trackEdgeMaterial.HasProperty(EmissionColorId))
                {
                    trackEdgeMaterial.SetColor(EmissionColorId, Color.black);
                    Shader.SetGlobalColor(GlobalEmissionId, Color.black);
                }

                if (trackEdgeMaterial != null && trackEdgeMaterial.HasProperty(BaseColorId))
                    trackEdgeMaterial.SetColor(BaseColorId, new Color(0.03f, 0.03f, 0.04f));

                return;
            }

            var beatPulse = 1f;
            var music = DynamicRaceMusicController.Instance;
            if (music != null)
                beatPulse = music.BeatPulse;

            GlobalPulse = Mathf.Lerp(minPulse, pulseStrength, beatPulse);
            Shader.SetGlobalFloat(GlobalPulseId, GlobalPulse);

            if (!hasBaseColors || trackEdgeMaterial == null)
                return;

            var emission = baseEmission * GlobalPulse;
            trackEdgeMaterial.SetColor(EmissionColorId, emission);
            Shader.SetGlobalColor(GlobalEmissionId, emission);

            if (trackEdgeMaterial.HasProperty(BaseColorId))
            {
                var tinted = Color.Lerp(baseColor, new Color(0.2f, 0.95f, 1f), beatPulse * 0.22f);
                trackEdgeMaterial.SetColor(BaseColorId, tinted);
            }
        }

        void OnDestroy()
        {
            GlobalPulse = 1f;
            Shader.SetGlobalFloat(GlobalPulseId, 1f);
        }
    }
}
