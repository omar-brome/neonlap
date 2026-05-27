using NeonLap.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(VehicleController))]
    public class VehicleHapticsController : MonoBehaviour
    {
        [SerializeField] float driftMotorLow = 0.18f;
        [SerializeField] float driftMotorHigh = 0.08f;
        [SerializeField] float collisionMotorLow = 0.55f;
        [SerializeField] float collisionMotorHigh = 0.75f;
        [SerializeField] float collisionMinSpeed = 6f;
        [SerializeField] float collisionCooldown = 0.25f;

        VehicleController vehicle;
        float lastCollisionPulse;
        bool wasDrifting;

        public static void Setup(GameObject playerCar)
        {
            if (playerCar == null || playerCar.GetComponent<VehicleHapticsController>() != null)
                return;

            playerCar.AddComponent<VehicleHapticsController>();
        }

        void Awake()
        {
            vehicle = GetComponent<VehicleController>();
        }

        void Update()
        {
            if (!GameHapticsSettings.Enabled || vehicle == null)
                return;

            var drifting = vehicle.IsDrifting && vehicle.CurrentSpeed > 4f;
            if (drifting && !wasDrifting)
                Pulse(driftMotorLow, driftMotorHigh, 0.12f);

            wasDrifting = drifting;

            if (drifting)
                SetMotors(driftMotorLow, driftMotorHigh);
            else
                ClearMotors();
        }

        public void PulseCollision(float impactSpeed)
        {
            if (!GameHapticsSettings.Enabled || impactSpeed < collisionMinSpeed)
                return;

            if (Time.time - lastCollisionPulse < collisionCooldown)
                return;

            lastCollisionPulse = Time.time;
            var t = Mathf.Clamp01((impactSpeed - collisionMinSpeed) / 12f);
            Pulse(
                Mathf.Lerp(collisionMotorLow * 0.5f, collisionMotorHigh, t),
                Mathf.Lerp(collisionMotorHigh * 0.5f, collisionMotorHigh, t),
                0.18f);
        }

        void Pulse(float low, float high, float duration)
        {
            SetMotors(low, high);
            CancelInvoke(nameof(ClearMotors));
            Invoke(nameof(ClearMotors), duration);
        }

        static void SetMotors(float low, float high)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
                return;

            gamepad.SetMotorSpeeds(low, high);
        }

        static void ClearMotors()
        {
            var gamepad = Gamepad.current;
            gamepad?.SetMotorSpeeds(0f, 0f);
        }

        void OnDisable()
        {
            ClearMotors();
        }
    }
}
