using UnityEngine;

namespace NeonLap.Environment
{
    public class MovingTrackObstacle : MonoBehaviour
    {
        Vector3 center;
        Vector3 axis;
        float distance;
        float speed;
        float phase;
        Rigidbody rb;

        public void Configure(Vector3 worldCenter, Vector3 moveAxis, float moveDistance, float moveSpeed,
            float startPhase)
        {
            center = worldCenter;
            axis = moveAxis.normalized;
            distance = moveDistance;
            speed = moveSpeed;
            phase = startPhase;
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        void FixedUpdate()
        {
            if (rb == null)
                return;

            var offset = Mathf.Sin(Time.time * speed + phase) * distance;
            rb.MovePosition(center + axis * offset);
        }
    }
}
