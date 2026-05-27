using UnityEngine;

namespace NeonLap.Environment
{
    public class CrowdFanAnimator : MonoBehaviour
    {
        Vector3 startLocalPosition;
        float amplitude;
        float speed;
        float phase;
        bool jumping;
        float celebrateMultiplier = 1f;
        float celebrateEndTime;

        public void Configure(bool isJumping, float phaseOffset)
        {
            jumping = isJumping;
            phase = phaseOffset;
            amplitude = isJumping ? Random.Range(0.35f, 0.55f) : Random.Range(0.03f, 0.07f);
            speed = isJumping ? Random.Range(2.8f, 4.2f) : Random.Range(1f, 1.8f);
        }

        public void Celebrate(float multiplier, float durationSeconds)
        {
            celebrateMultiplier = Mathf.Max(celebrateMultiplier, multiplier);
            celebrateEndTime = Time.time + Mathf.Max(durationSeconds, 0.25f);
            jumping = true;
        }

        void Awake()
        {
            startLocalPosition = transform.localPosition;
        }

        void Update()
        {
            if (Time.time >= celebrateEndTime && celebrateMultiplier > 1f)
                celebrateMultiplier = Mathf.MoveTowards(celebrateMultiplier, 1f, Time.deltaTime * 2f);

            var amp = amplitude * celebrateMultiplier;

            if (jumping)
            {
                var t = Time.time * speed + phase;
                var hop = Mathf.Abs(Mathf.Sin(t));
                hop *= hop;
                transform.localPosition = startLocalPosition + Vector3.up * (hop * amp);
                return;
            }

            var sway = Mathf.Sin(Time.time * speed + phase) * amp;
            transform.localPosition = startLocalPosition + new Vector3(0f, sway, 0f);
        }
    }
}
