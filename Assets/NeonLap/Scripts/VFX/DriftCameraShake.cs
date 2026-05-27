using UnityEngine;

namespace NeonLap.VFX
{
    [DefaultExecutionOrder(100)]
    public class DriftCameraShake : MonoBehaviour
    {
        [SerializeField] float maxOffset = 0.22f;
        [SerializeField] float decaySpeed = 7f;

        float shakeIntensity;
        float shakePhase;

        public void ReportDriftIntensity(float intensity)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, Mathf.Clamp01(intensity));
        }

        void LateUpdate()
        {
            shakeIntensity = Mathf.MoveTowards(shakeIntensity, 0f, decaySpeed * Time.deltaTime);
            if (shakeIntensity <= 0.001f)
                return;

            shakePhase += Time.deltaTime * (18f + shakeIntensity * 24f);
            var amount = maxOffset * shakeIntensity;
            var offset = new Vector3(
                Mathf.Sin(shakePhase * 1.7f) * amount,
                Mathf.Sin(shakePhase * 2.3f) * amount * 0.65f,
                0f);
            transform.position += offset;
        }
    }
}
