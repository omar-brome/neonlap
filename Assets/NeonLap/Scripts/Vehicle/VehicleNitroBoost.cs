using NeonLap.Core;
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
        [SerializeField] int maxCharges = 2;

        Rigidbody rb;
        float boostEndTime;
        float disabledUntil;
        int charges;
        float zoneSpeedBonus = 1f;
        float zoneAccelBonus = 1f;
        float zoneNitroStackSpeed = 1f;
        float zoneNitroStackAccel = 1f;

        public bool IsDisabled => Time.time < disabledUntil;
        public bool IsActive => !IsDisabled && Time.time < boostEndTime;

        public float ActiveSpeedMultiplier
        {
            get
            {
                if (IsActive)
                    return speedMultiplier * zoneSpeedBonus * zoneNitroStackSpeed;
                return zoneSpeedBonus;
            }
        }

        public float ActiveAccelerationMultiplier
        {
            get
            {
                if (IsActive)
                    return accelerationMultiplier * zoneAccelBonus * zoneNitroStackAccel;
                return zoneAccelBonus;
            }
        }
        public float RemainingSeconds => IsActive ? boostEndTime - Time.time : 0f;
        public int Charges => charges;
        public int MaxCharges => maxCharges;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void ActivateFromPickup()
        {
            ActivateBoost(usePickupImpulse: true);
        }

        public void AddCharge(int amount = 1)
        {
            charges = Mathf.Clamp(charges + Mathf.Max(0, amount), 0, Mathf.Max(1, maxCharges));
        }

        public bool TryActivateFromInput()
        {
            if (IsDisabled || IsActive || charges <= 0)
                return false;

            charges--;
            ActivateBoost(usePickupImpulse: false);
            return true;
        }

        public void DisableBoost(float durationSeconds)
        {
            disabledUntil = Mathf.Max(disabledUntil, Time.time + Mathf.Max(durationSeconds, 0.5f));
            boostEndTime = 0f;
        }

        public void SetZoneBonuses(float passiveSpeed, float passiveAccel, float nitroStackSpeed, float nitroStackAccel)
        {
            zoneSpeedBonus = Mathf.Max(1f, passiveSpeed);
            zoneAccelBonus = Mathf.Max(1f, passiveAccel);
            zoneNitroStackSpeed = Mathf.Max(1f, nitroStackSpeed);
            zoneNitroStackAccel = Mathf.Max(1f, nitroStackAccel);
        }

        public void ClearZoneBonuses()
        {
            zoneSpeedBonus = 1f;
            zoneAccelBonus = 1f;
            zoneNitroStackSpeed = 1f;
            zoneNitroStackAccel = 1f;
        }

        public void ActivateBoost(bool usePickupImpulse = false)
        {
            if (IsDisabled)
                return;

            boostEndTime = Time.time + boostDuration;
            RefillFuelFromBoost();

            if (!usePickupImpulse || rb == null || rb.isKinematic)
                return;

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return;

            rb.AddForce(forward.normalized * pickupImpulse, ForceMode.VelocityChange);
        }

        void RefillFuelFromBoost()
        {
            var fuel = GetComponent<VehicleFuelSystem>();
            if (fuel == null || fuel.IsInfinite)
                return;

            fuel.AddFuelFraction(GameFuelEconomy.NitroFuelRefillFraction);
        }
    }
}
