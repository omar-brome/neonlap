# NeonLap Setup

## Quick start

1. Open `Assets/Scenes/SampleScene.unity`
2. Press **Play**
3. Drive with **W/A/S/D**, drift with **Space**, reset with **R**, pause with **Escape**

The scene uses `NeonLapSceneSetup` on the **NeonLap** GameObject to build the track, spawn the player car, wire the camera, race UI, checkpoints, and AI rival at runtime.

## Controls

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Accelerate | W | RT |
| Brake / Reverse | S | LT |
| Steer | A / D | Left stick X |
| Drift | Space | RB |
| Reset | R | Y |
| Pause | Escape | Start |

## Tags & layers

| Name | Type | Usage |
|------|------|-------|
| Player | Tag | Player hover-car |
| Track | Tag + Layer 6 | Track surface raycasts |
| Checkpoint | Tag | Lap validation triggers |
| Barrier | Tag | Outer wall colliders |
| Vehicle | Layer 7 | Player / AI cars |

## Key assets

- `Assets/NeonLap/ScriptableObjects/DefaultVehicleProfile.asset` — car tuning
- `Assets/NeonLap/ScriptableObjects/DefaultTrackDefinition.asset` — track + lap config
- `Assets/NeonLap/Input/NeonLap_InputActions.inputactions` — racing input map
- `Assets/NeonLap/Materials/` — neon URP materials

## Scenes

- **SampleScene** — full race loop (3 laps, UI, AI rival, pause menu)
- **MainMenu** — title screen → loads SampleScene via `GameManager`

## Manual scene wiring (if rebuilding)

1. Create empty GameObject named `NeonLap`
2. Add `NeonLapSceneSetup`
3. Assign:
   - DefaultVehicleProfile
   - DefaultTrackDefinition
   - NeonLap_InputActions
   - M_TrackSurface, M_TrackEdgeNeon, M_CarBody, M_CarNeon materials
4. Ensure Main Camera exists (FollowCamera is added at runtime)

## Main menu wiring

1. Open `Assets/NeonLap/Scenes/MainMenu.unity`
2. Add empty GameObject with `MainMenuSetup` — UI and GameManager are created at runtime

## Tuning feel

Edit `DefaultVehicleProfile` values:

- **maxSpeed / acceleration** — top speed and pickup
- **turnSpeedLow / turnSpeedHigh** — steering at low vs high speed
- **grip / driftGripMultiplier** — drift slip amount
- **hoverHeight / hoverForce** — hover stability

## Mobile rendering

The project includes URP Mobile RP assets (`Assets/Settings/Mobile_RPAsset.asset`) with MSAA and 0.8 render scale. NeonLap uses primitive geometry and a small material set, so mobile draw calls stay low. Switch the active pipeline asset in **Project Settings → Graphics** to test mobile quality.

- Phase 1: driveable vehicle + oval track
- Phase 2: checkpoints, countdown, lap UI
- Phase 3: neon environment, fog, bloom, main menu
- Phase 4: extra vehicle profiles, track definition SO, rubber-band AI, pause menu
