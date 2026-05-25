using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleNitroBoost : MonoBehaviour
    {
        [SerializeField] float boostDuration = 3.5f;
        [SerializeField] float speedMultiplier = 1.45f;
        [SerializeField] float accelerationMultiplier = 1.65f;
        [SerializeField] float pickupImpulse = 5f;

        Rigidbody rb;
        float boostEndTime;

        public bool IsActive => Time.time < boostEndTime;
        public float ActiveSpeedMultiplier => IsActive ? speedMultiplier : 1f;
        public float ActiveAccelerationMultiplier => IsActive ? accelerationMultiplier : 1f;
        public float RemainingSeconds => IsActive ? boostEndTime - Time.time : 0f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void ActivateFromPickup()
        {
            boostEndTime = Time.time + boostDuration;

            if (rb == null || rb.isKinematic)
                return;

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return;

            rb.AddForce(forward.normalized * pickupImpulse, ForceMode.VelocityChange);
        }
    }
}
