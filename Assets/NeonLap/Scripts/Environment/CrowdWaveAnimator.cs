using UnityEngine;

namespace NeonLap.Environment
{
    public class CrowdWaveAnimator : MonoBehaviour
    {
        [SerializeField] float waveAmplitude = 0.06f;
        [SerializeField] float waveSpeed = 1.6f;
        [SerializeField] float phaseOffset;

        Vector3 startLocalPosition;

        void Awake()
        {
            startLocalPosition = transform.localPosition;
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            var wave = Mathf.Sin(Time.time * waveSpeed + phaseOffset) * waveAmplitude;
            transform.localPosition = startLocalPosition + new Vector3(0f, wave, 0f);
        }
    }
}
