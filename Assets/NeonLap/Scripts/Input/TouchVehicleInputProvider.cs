using NeonLap.Core;
using NeonLap.UI;
using UnityEngine;

namespace NeonLap.Input
{
    public class TouchVehicleInputProvider : MonoBehaviour, IVehicleInputProvider
    {
        TouchDrivingUI drivingUi;

        public float Accelerate { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public bool DriftHeld { get; private set; }
        public bool NitroPressed { get; private set; }
        public bool ResetPressed { get; private set; }
        public bool PausePressed { get; private set; }

        public void Bind(TouchDrivingUI ui)
        {
            drivingUi = ui;
        }

        void Update()
        {
            if (drivingUi == null || !drivingUi.IsActive)
            {
                Accelerate = 0f;
                Brake = 0f;
                Steer = 0f;
                DriftHeld = false;
                ResetPressed = false;
                PausePressed = false;
                return;
            }

            var stick = drivingUi.SteerVector;
            Steer = stick.x;
            var forward = Mathf.Clamp01(stick.y);
            var braking = stick.y < -0.2f;
            Brake = braking ? Mathf.Abs(stick.y) : 0f;
            Accelerate = braking
                ? 0f
                : GameTouchSettings.AutoAccelerate
                    ? Mathf.Max(forward, 0.92f)
                    : forward;
            DriftHeld = drivingUi.DriftHeld;
            NitroPressed = drivingUi.NitroPressed;
            ResetPressed = drivingUi.ResetPressed;
            PausePressed = drivingUi.PausePressed;

            drivingUi.ClearFrameButtons();
        }
    }
}
