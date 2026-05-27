using NeonLap.UI;
using UnityEngine;

namespace NeonLap.Input
{
    public class CompositeVehicleInputProvider : MonoBehaviour, IVehicleInputProvider
    {
        PlayerInputReader keyboard;
        TouchVehicleInputProvider touch;

        public float Accelerate => Mathf.Max(keyboard != null ? keyboard.Accelerate : 0f,
            touch != null ? touch.Accelerate : 0f);

        public float Brake => Mathf.Max(keyboard != null ? keyboard.Brake : 0f, touch != null ? touch.Brake : 0f);

        public float Steer
        {
            get
            {
                if (touch != null && Mathf.Abs(touch.Steer) > 0.05f)
                    return touch.Steer;
                return keyboard != null ? keyboard.Steer : 0f;
            }
        }

        public bool DriftHeld => (keyboard != null && keyboard.DriftHeld) || (touch != null && touch.DriftHeld);

        public bool NitroPressed => (keyboard != null && keyboard.NitroPressed) || (touch != null && touch.NitroPressed);

        public bool ResetPressed => (keyboard != null && keyboard.ResetPressed) || (touch != null && touch.ResetPressed);

        public bool PausePressed => (keyboard != null && keyboard.PausePressed) || (touch != null && touch.PausePressed);

        public bool SwitchCameraPressed => keyboard != null && keyboard.SwitchCameraPressed;

        public bool CelebrationJumpPressed => keyboard != null && keyboard.CelebrationJumpPressed;

        public bool BarrelRollPressed => keyboard != null && keyboard.BarrelRollPressed;

        public bool LookBackHeld => keyboard != null && keyboard.LookBackHeld;

        public bool DroneCamPressed => keyboard != null && keyboard.DroneCamPressed;

        public bool HornPressed => keyboard != null && keyboard.HornPressed;

        public static CompositeVehicleInputProvider Setup(GameObject playerCar, PlayerInputReader reader,
            TouchDrivingUI touchUi)
        {
            var composite = playerCar.GetComponent<CompositeVehicleInputProvider>();
            if (composite == null)
                composite = playerCar.AddComponent<CompositeVehicleInputProvider>();

            composite.keyboard = reader;
            composite.touch = touchUi != null ? playerCar.GetComponent<TouchVehicleInputProvider>() : null;
            if (composite.touch == null && touchUi != null)
            {
                composite.touch = playerCar.AddComponent<TouchVehicleInputProvider>();
                composite.touch.Bind(touchUi);
            }

            return composite;
        }
    }
}
