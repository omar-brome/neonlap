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

        public void Configure(bool isJumping, float phaseOffset)
        {
            jumping = isJumping;
            phase = phaseOffset;
            amplitude = isJumping ? Random.Range(0.35f, 0.55f) : Random.Range(0.03f, 0.07f);
            speed = isJumping ? Random.Range(2.8f, 4.2f) : Random.Range(1f, 1.8f);
        }

        void Awake()
        {
            startLocalPosition = transform.localPosition;
        }

        void Update()
        {
            if (jumping)
            {
                var t = Time.time * speed + phase;
                var hop = Mathf.Abs(Mathf.Sin(t));
                hop *= hop;
                transform.localPosition = startLocalPosition + Vector3.up * (hop * amplitude);
                return;
            }

            var sway = Mathf.Sin(Time.time * speed + phase) * amplitude;
            transform.localPosition = startLocalPosition + new Vector3(0f, sway, 0f);
        }
    }
}
