using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonLap.Input
{
    public class PlayerInputReader : MonoBehaviour, IVehicleInputProvider
    {
        [SerializeField] InputActionAsset inputActions;

        InputActionMap racingMap;
        InputAction accelerateAction;
        InputAction brakeAction;
        InputAction steerAction;
        InputAction driftAction;
        InputAction resetAction;
        InputAction pauseAction;
        InputAction switchCameraAction;
        InputAction barrelRollAction;
        InputAction lookBackAction;
        InputAction dropBananaAction;
        InputAction empPulseAction;
        InputAction droneCamAction;
        InputAction hornAction;
        InputAction nitroAction;
        bool initialized;

        public float Accelerate { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public bool DriftHeld { get; private set; }
        public bool ResetPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool SwitchCameraPressed { get; private set; }
        public bool CelebrationJumpPressed { get; private set; }
        public bool BarrelRollPressed { get; private set; }
        public bool LookBackHeld { get; private set; }
        public bool DropBananaPressed { get; private set; }
        public bool EmpPulsePressed { get; private set; }
        public bool DroneCamPressed { get; private set; }
        public bool HornPressed { get; private set; }
        public bool NitroPressed { get; private set; }

        public void Configure(InputActionAsset actions)
        {
            inputActions = actions;
            InitializeActions();
        }

        void Awake()
        {
            InitializeActions();
        }

        void InitializeActions()
        {
            if (initialized || inputActions == null)
                return;

            racingMap = inputActions.FindActionMap("Racing", true);
            accelerateAction = racingMap.FindAction("Accelerate", true);
            brakeAction = racingMap.FindAction("Brake", true);
            steerAction = racingMap.FindAction("Steer", true);
            driftAction = racingMap.FindAction("Drift", true);
            resetAction = racingMap.FindAction("Reset", true);
            pauseAction = racingMap.FindAction("Pause", true);
            switchCameraAction = racingMap.FindAction("SwitchCamera", false);
            barrelRollAction = racingMap.FindAction("BarrelRoll", false);
            lookBackAction = racingMap.FindAction("LookBack", false);
            dropBananaAction = racingMap.FindAction("DropBanana", false);
            empPulseAction = racingMap.FindAction("EmpPulse", false);
            droneCamAction = racingMap.FindAction("DroneCam", false);
            hornAction = racingMap.FindAction("Horn", false);
            nitroAction = racingMap.FindAction("Nitro", false);
            initialized = true;
        }

        void OnEnable()
        {
            racingMap?.Enable();
        }

        void OnDisable()
        {
            racingMap?.Disable();
        }

        void Update()
        {
            if (!initialized)
                return;

            Accelerate = accelerateAction.ReadValue<float>();
            Brake = brakeAction.ReadValue<float>();
            Steer = steerAction.ReadValue<float>();
            DriftHeld = driftAction.IsPressed();
            ResetPressed = resetAction.WasPressedThisFrame();
            PausePressed = pauseAction.WasPressedThisFrame();
            SwitchCameraPressed = switchCameraAction != null && switchCameraAction.WasPressedThisFrame();
            CelebrationJumpPressed = driftAction.WasPressedThisFrame();
            BarrelRollPressed = barrelRollAction != null && barrelRollAction.WasPressedThisFrame();
            LookBackHeld = lookBackAction != null && lookBackAction.IsPressed();
            DropBananaPressed = dropBananaAction != null && dropBananaAction.WasPressedThisFrame();
            EmpPulsePressed = empPulseAction != null && empPulseAction.WasPressedThisFrame();
            DroneCamPressed = droneCamAction != null && droneCamAction.WasPressedThisFrame();
            HornPressed = hornAction != null && hornAction.WasPressedThisFrame();
            NitroPressed = nitroAction != null && nitroAction.WasPressedThisFrame();
        }
    }
}
