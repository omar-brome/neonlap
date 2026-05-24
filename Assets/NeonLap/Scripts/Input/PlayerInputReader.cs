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
        bool initialized;

        public float Accelerate { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public bool DriftHeld { get; private set; }
        public bool ResetPressed { get; private set; }
        public bool PausePressed { get; private set; }

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
        }
    }
}
