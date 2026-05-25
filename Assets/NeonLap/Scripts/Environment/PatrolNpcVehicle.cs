using UnityEngine;

namespace NeonLap.Environment
{
    public class PatrolNpcVehicle : MonoBehaviour
    {
        Vector3 pointA;
        Vector3 pointB;
        float speed = 12f;
        int direction = 1;
        Rigidbody rb;

        public void Configure(Vector3 start, Vector3 end, float moveSpeed)
        {
            pointA = start;
            pointB = end;
            speed = moveSpeed;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (rb == null)
                return;

            var target = direction > 0 ? pointB : pointA;
            var position = rb.position;
            var toTarget = target - position;
            toTarget.y = 0f;

            if (toTarget.magnitude < 1.25f)
            {
                direction *= -1;
                return;
            }

            var step = toTarget.normalized * (speed * Time.fixedDeltaTime);
            rb.MovePosition(position + step);
            if (step.sqrMagnitude > 0.0001f)
                rb.MoveRotation(Quaternion.LookRotation(toTarget.normalized, Vector3.up));
        }
    }
}
