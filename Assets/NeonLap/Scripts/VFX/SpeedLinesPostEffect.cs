using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NeonLap.VFX
{
    /// <summary>
    /// URP volume boost (motion blur + chromatic aberration) when speed exceeds 90% of max.
    /// </summary>
    public class SpeedLinesPostEffect : MonoBehaviour
    {
        const float SpeedThresholdRatio = 0.9f;

        [SerializeField] float maxMotionBlur = 0.42f;
        [SerializeField] float maxChromatic = 0.35f;
        [SerializeField] float blendSpeed = 6f;

        Volume volume;
        MotionBlur motionBlur;
        ChromaticAberration chromaticAberration;
        VehicleController vehicle;
        float currentIntensity;

        public static SpeedLinesPostEffect Ensure(UnityEngine.Camera camera, Transform player)
        {
            if (camera == null)
                return null;

            var effect = camera.GetComponent<SpeedLinesPostEffect>();
            if (effect == null)
                effect = camera.gameObject.AddComponent<SpeedLinesPostEffect>();

            effect.BindPlayer(player);
            return effect;
        }

        public void BindPlayer(Transform player)
        {
            vehicle = player != null ? player.GetComponent<VehicleController>() : null;
        }

        void Awake()
        {
            InitializeVolume();
        }

        void InitializeVolume()
        {
            volume = GetComponent<Volume>();
            if (volume == null)
                volume = gameObject.AddComponent<Volume>();

            volume.isGlobal = false;
            volume.priority = 18f;
            volume.weight = 0f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            motionBlur = volume.profile.Add<MotionBlur>(true);
            motionBlur.active = true;
            motionBlur.mode.Override(MotionBlurMode.CameraOnly);
            motionBlur.quality.Override(MotionBlurQuality.Medium);
            motionBlur.intensity.Override(0f);

            chromaticAberration = volume.profile.Add<ChromaticAberration>(true);
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0f);
        }

        void LateUpdate()
        {
            if (vehicle == null)
            {
                volume.weight = Mathf.MoveTowards(volume.weight, 0f, blendSpeed * Time.deltaTime);
                return;
            }

            var maxSpeed = Mathf.Max(vehicle.Profile != null ? vehicle.Profile.maxSpeed : 45f, 1f);
            var speedRatio = vehicle.CurrentSpeed / maxSpeed;
            var target = speedRatio >= SpeedThresholdRatio
                ? Mathf.InverseLerp(SpeedThresholdRatio, 1f, speedRatio)
                : 0f;

            currentIntensity = Mathf.MoveTowards(currentIntensity, target, blendSpeed * Time.deltaTime);
            volume.weight = currentIntensity;

            if (motionBlur != null)
                motionBlur.intensity.Override(maxMotionBlur * currentIntensity);

            if (chromaticAberration != null)
                chromaticAberration.intensity.Override(maxChromatic * currentIntensity);
        }
    }
}
