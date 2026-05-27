using UnityEngine;

namespace NeonLap.Environment
{
    public class CrowdWaveAnimator : MonoBehaviour
    {
        [SerializeField] float waveAmplitude = 0.06f;
        [SerializeField] float waveSpeed = 1.6f;
        [SerializeField] float phaseOffset;

        Vector3 startLocalPosition;
        float boostMultiplier = 1f;
        float boostEndTime;

        void Awake()
        {
            startLocalPosition = transform.localPosition;
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        public void Boost(float multiplier, float durationSeconds)
        {
            boostMultiplier = Mathf.Max(boostMultiplier, multiplier);
            boostEndTime = Time.time + Mathf.Max(durationSeconds, 0.25f);
        }

        void Update()
        {
            if (Time.time >= boostEndTime && boostMultiplier > 1f)
                boostMultiplier = Mathf.MoveTowards(boostMultiplier, 1f, Time.deltaTime * 2.5f);

            var wave = Mathf.Sin(Time.time * waveSpeed + phaseOffset) * waveAmplitude * boostMultiplier;
            transform.localPosition = startLocalPosition + new Vector3(0f, wave, 0f);
        }
    }
}
