# NeonLap Project Rules

- Arcade racer, not simulation. No WheelCollider-based vehicle physics.
- All game code lives under `Assets/NeonLap/`.
- Use ScriptableObjects for tunable data (VehicleProfile, TrackDefinition).
- Input goes through `IVehicleInputProvider` — never read raw input inside VehicleController.
- Physics in `FixedUpdate`; input read in `Update`; camera in `LateUpdate`.
- One MVP phase per PR/session when possible.
- Prefer prefabs + scene references over runtime Find calls.
- No third-party paid assets without explicit approval.
