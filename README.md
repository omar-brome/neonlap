# NeonLap

An arcade **neon hover-racing** game built in **Unity 6** with the **Universal Render Pipeline (URP)**. Race a 10-car grid around a procedurally generated oval track, complete **3 laps**, and fight for **1st place** against AI rivals.

![Unity](https://img.shields.io/badge/Unity-6000.4.6f1-blue)
![URP](https://img.shields.io/badge/Render_Pipeline-URP-green)
![Input](https://img.shields.io/badge/Input-New_Input_System-orange)

---

## Features

- **Arcade hover physics** — raycast grounding, grip, drift, and speed-capped steering
- **Procedural oval track** — runtime-built closed loop with straights, turns, barriers, and colliders
- **10-car grid** — 1 player + 9 AI opponents, each with a unique neon color
- **3-lap race flow** — countdown (3…2…1…GO!), lap timers, best lap, finish screen
- **Placement system** — **YOU WON!** on 1st place, or **RACE FINISHED** with your final position
- **Rubber-band AI** — rivals follow dense track waypoints with speed variation
- **Visual polish** — neon materials, fog, drift trails, exhaust smoke, low-poly hover car meshes
- **Full UI flow** — main menu, controls screen, in-race HUD, pause menu
- **Keyboard + gamepad** — New Input System with WASD and arrow keys
- **Data-driven tuning** — `VehicleProfile` and `TrackDefinition` ScriptableObjects

---

## Requirements

| Tool | Version |
|------|---------|
| **Unity Editor** | `6000.4.6f1` (Unity 6) |
| **Render Pipeline** | URP `17.4.0` |
| **Input** | Input System `1.19.0` |

Clone the repo and open the project folder in Unity Hub. Unity will restore packages from `Packages/manifest.json`.

---

## Quick Start

### Option A — Full game flow (recommended)

1. Open **`Assets/NeonLap/Scenes/MainMenu.unity`**
2. Press **Play ▶**
3. Click **START RACE**

### Option B — Jump straight into a race

1. Open **`Assets/Scenes/SampleScene.unity`**
2. Press **Play ▶**

The race scene uses **`NeonLapSceneSetup`** on the **NeonLap** GameObject to build the track, spawn cars, wire the camera, UI, checkpoints, and AI at runtime.

> **Tip:** Switch to the **Game** tab while playing. If both Play and Pause buttons are highlighted in the editor, the game is paused — click Pause once to resume.

---

## Controls

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Accelerate | `W` / `↑` | Right Trigger |
| Brake / Reverse | `S` / `↓` | Left Trigger |
| Steer | `A` / `D` or `←` / `→` | Left stick X |
| Drift | `Space` | Right Bumper |
| Reset car | `R` | Y (North) |
| Pause | `Escape` | Start |

Controls are also listed in the main menu under **CONTROLS**.

Input bindings live in:

`Assets/NeonLap/Input/NeonLap_InputActions.inputactions`

---

## Scenes & Build Order

| Scene | Path | Purpose |
|-------|------|---------|
| **MainMenu** | `Assets/NeonLap/Scenes/MainMenu.unity` | Title screen, controls, quit |
| **SampleScene** | `Assets/Scenes/SampleScene.unity` | Race scene (track + cars built at runtime) |

Build settings (scene 0 = first to load):

1. `MainMenu`
2. `SampleScene`

---

## Project Structure

```
neonlap/
├── Assets/
│   ├── NeonLap/                    # All game code & content
│   │   ├── Input/                  # Input Actions asset
│   │   ├── Materials/              # URP neon materials
│   │   ├── Scenes/                 # MainMenu.unity
│   │   ├── ScriptableObjects/      # Vehicle & track tuning
│   │   ├── Scripts/
│   │   │   ├── Audio/              # Engine / SFX hooks
│   │   │   ├── Camera/             # FollowCamera
│   │   │   ├── Core/               # Bootstrap, GameManager, layers
│   │   │   ├── Input/              # IVehicleInputProvider, PlayerInputReader
│   │   │   ├── Race/               # RaceManager, checkpoints, placement
│   │   │   ├── Track/              # OvalTrackBuilder, TrackDefinition
│   │   │   ├── UI/                 # Menu, race HUD, pause
│   │   │   ├── VFX/                # Drift trails, exhaust smoke
│   │   │   └── Vehicle/            # Physics, AI, visuals
│   │   └── SETUP.md                # Short setup reference
│   ├── Scenes/
│   │   └── SampleScene.unity       # Race scene
│   └── Settings/                   # URP PC + Mobile pipeline assets
├── Packages/
├── ProjectSettings/
└── README.md
```

---

## Architecture Overview

### Runtime bootstrap

- **`MainMenuSetup`** — Creates main menu UI, EventSystem (Input System UI module), and `GameManager` at runtime.
- **`NeonLapSceneSetup`** — Single entry point for the race scene: environment, track, player, 9 AI rivals, camera, race systems, HUD, and pause menu.

### Vehicle stack

| Component | Role |
|-----------|------|
| `VehicleController` | Player arcade hover physics |
| `AIVehicleController` | Waypoint steering + rubber-band pacing |
| `VehicleGroundProbe` | Multi-ray ground detection |
| `VehicleReset` | Fall / off-track recovery |
| `RaceStartGate` | Freezes cars until countdown GO |
| `HoverCarVisualBuilder` | Procedural low-poly car mesh |
| `ExhaustSmokeVFX` | Speed-based particle exhaust |

### Race stack

| Component | Role |
|-----------|------|
| `OvalTrackBuilder` | Builds closed oval centerline, surface, barriers, AI waypoints |
| `TrackCheckpoint` | Lap validation triggers |
| `RacerProgress` | Per-car lap / checkpoint state |
| `RaceManager` | Countdown, lap logic, finish placement |
| `RaceUI` | Lap timer, countdown, **YOU WON!** / finish overlay |

### Scene flow

```
MainMenu → GameManager.LoadRace() → SampleScene
SampleScene → Pause → MAIN MENU → GameManager.LoadMainMenu()
```

---

## Configuration & Tuning

### Vehicle feel

Edit **`Assets/NeonLap/ScriptableObjects/DefaultVehicleProfile.asset`**:

| Field | Effect |
|-------|--------|
| `maxSpeed` / `acceleration` | Top speed and throttle response |
| `turnSpeedLow` / `turnSpeedHigh` | Steering at low vs high speed |
| `grip` / `driftGripMultiplier` | Traction vs drift slip |
| `hoverHeight` / `hoverForce` | Hover stability |

Alternate profiles: `VehicleProfile_Drift.asset`, `VehicleProfile_Speed.asset`

### Track layout

Edit **`Assets/NeonLap/ScriptableObjects/DefaultTrackDefinition.asset`**:

| Field | Default | Effect |
|-------|---------|--------|
| `lapCount` | 3 | Laps to finish |
| `straightLength` | 80 | Straight section length |
| `turnRadius` | 25 | Turn radius |
| `trackWidth` | 14 | Track width |
| `checkpointCount` | 10 | Checkpoints per lap |

### AI grid size

On the **NeonLap** GameObject in SampleScene, adjust **`NeonLapSceneSetup`**:

- `spawnAiRivals` — enable/disable AI
- `aiRivalCount` — default **9** (10 cars total with player)

---

## Tags & Layers

| Name | Type | Usage |
|------|------|-------|
| `Player` | Tag | Player hover car |
| `Track` | Tag + Layer 6 | Track surface raycasts |
| `Checkpoint` | Tag | Lap validation |
| `Barrier` | Tag | Outer wall colliders |
| `Vehicle` | Layer 7 | Player and AI cars |

---

## Rendering

- **PC:** `Assets/Settings/PC_RPAsset.asset`
- **Mobile:** `Assets/Settings/Mobile_RPAsset.asset` (MSAA, 0.8 render scale)

Switch the active pipeline in **Edit → Project Settings → Graphics**. NeonLap uses primitive geometry and a small material set for low draw-call overhead.

---

## Manual Scene Wiring

If rebuilding SampleScene from scratch:

1. Create an empty GameObject named **`NeonLap`**
2. Add **`NeonLapSceneSetup`**
3. Assign references:
   - `DefaultVehicleProfile`
   - `DefaultTrackDefinition`
   - `NeonLap_InputActions`
   - Materials: `M_TrackSurface`, `M_TrackEdgeNeon`, `M_CarBody`, `M_CarNeon`
4. Ensure a **Main Camera** exists (`FollowCamera` is added at runtime)

For the main menu:

1. Open **`Assets/NeonLap/Scenes/MainMenu.unity`**
2. Add an empty GameObject with **`MainMenuSetup`**

See also: [`Assets/NeonLap/SETUP.md`](Assets/NeonLap/SETUP.md)

---

## Git & Unity

This repo uses the standard [Unity `.gitignore`](https://github.com/github/gitignore/blob/main/Unity.gitignore).

**Committed:** `Assets/`, `Packages/`, `ProjectSettings/`, `.meta` files  
**Ignored:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.DS_Store`, IDE caches

Always commit `.meta` files alongside assets — Unity relies on them for GUID references.

---

## Development Roadmap

| Phase | Status | Highlights |
|-------|--------|------------|
| Phase 1 | ✅ | Vehicle, procedural track, camera |
| Phase 2 | ✅ | Checkpoints, countdown, lap UI |
| Phase 3 | ✅ | Neon look, main menu, environment |
| Phase 4 | ✅ | AI grid, rubber-band, pause, mobile RP pass |

### Possible next steps

- Audio clips and mixers
- Additional tracks via `TrackDefinition` variants
- Local multiplayer or time trial leaderboard
- Mobile touch controls
- Hover car prefab for editor placement

---

## License

This project is provided as-is for learning and development. Add a license file here if you plan to open-source or distribute builds.

---

## Author

**Omar Brome** — [github.com/omar-brome/neonlap](https://github.com/omar-brome/neonlap)
