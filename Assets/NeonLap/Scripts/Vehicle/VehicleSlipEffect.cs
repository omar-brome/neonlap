using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleSlipEffect : MonoBehaviour
    {
        public event System.Action SlipApplied;

        [SerializeField] float bananaGripMultiplier = 0.07f;
        [SerializeField] float bananaSlipDuration = 1.35f;
        [SerializeField] float retriggerCooldown = 0.4f;

        Rigidbody rb;
        float slipEndTime;
        float activeGripMultiplier = 1f;
        float lastSlipTime;

        public float GripMultiplier => Time.time < slipEndTime ? activeGripMultiplier : 1f;
        public bool IsSlipping => Time.time < slipEndTime;
        public float SlipTimeRemaining => Mathf.Max(0f, slipEndTime - Time.time);
        public float SlipDuration => bananaSlipDuration;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (Time.time >= slipEndTime)
                activeGripMultiplier = 1f;
        }

        public void ApplyBananaSlip(Vector3 worldPosition, float impactSpeed)
        {
            if (rb == null || rb.isKinematic)
                return;

            var shield = GetComponent<VehicleCombatShield>();
            if (shield != null && shield.TryAbsorbHit())
                return;

            if (Time.time - lastSlipTime < retriggerCooldown)
                return;

            lastSlipTime = Time.time;
            slipEndTime = Time.time + bananaSlipDuration;
            activeGripMultiplier = bananaGripMultiplier;
            SlipApplied?.Invoke();

            var toCar = transform.position - worldPosition;
            toCar.y = 0f;
            if (toCar.sqrMagnitude < 0.01f)
                toCar = transform.right;

            var lateral = Vector3.Cross(Vector3.up, rb.linearVelocity.sqrMagnitude > 1f
                ? rb.linearVelocity.normalized
                : transform.forward).normalized;

            if (Vector3.Dot(lateral, toCar) < 0f)
                lateral = -lateral;

            lateral += toCar.normalized * 0.65f;
            lateral.y = 0f;
            lateral.Normalize();

            var kickStrength = Mathf.Lerp(3.5f, 8.5f, Mathf.InverseLerp(4f, 28f, impactSpeed));
            rb.AddForce(lateral * kickStrength, ForceMode.VelocityChange);
            rb.AddForce(Vector3.up * 0.35f, ForceMode.VelocityChange);

            var yawSpin = Random.Range(-1f, 1f) * Mathf.Lerp(1.2f, 3.5f, kickStrength / 8.5f);
            rb.AddTorque(Vector3.up * yawSpin, ForceMode.VelocityChange);
        }
    }
}
