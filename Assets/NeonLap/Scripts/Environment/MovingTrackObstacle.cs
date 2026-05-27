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
        bool rollVisual;

        public void Configure(Vector3 worldCenter, Vector3 moveAxis, float moveDistance, float moveSpeed,
            float startPhase, bool enableRollVisual = false)
        {
            center = worldCenter;
            axis = moveAxis.normalized;
            distance = moveDistance;
            speed = moveSpeed;
            phase = startPhase;
            rollVisual = enableRollVisual;
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

            if (!rollVisual)
                return;

            var rollAxis = Vector3.Cross(Vector3.up, axis).normalized;
            if (rollAxis.sqrMagnitude < 0.01f)
                rollAxis = transform.right;

            transform.Rotate(rollAxis, speed * distance * 55f * Time.fixedDeltaTime, Space.World);
        }
    }
}
