# NeonLap

An arcade **neon hover-racing** game built in **Unity 6** with the **Universal Render Pipeline (URP)**. Race on **seven** procedural layouts, earn **career** medals and stars, run **time trials** against your ghost, dodge **police** in Outrun (or optional TT chase), and chase high scores across multiple modes.

![Unity](https://img.shields.io/badge/Unity-6000.4.6f1-blue)
![URP](https://img.shields.io/badge/Render_Pipeline-URP-green)
![Input](https://img.shields.io/badge/Input-New_Input_System-orange)

---

## Features

### Core driving
- **Arcade hover physics** — grounding, grip, drift, barrel rolls, nitro boost
- **Fuel strategy** (career / grid modes) — lap-scaled tank, **fuel pads**, nitro refuel; empty tank stops acceleration. **Time Trial** uses infinite fuel (gauge hidden).
- **10-car grid** — player + AI rivals with personality profiles and rubber-band pacing (TT can add 0–3 rivals)
- **Weather** — dynamic rain/sun; lower grip in rain, higher drift score risk/reward

### Race modes
| Mode | Description |
|------|-------------|
| **Career** | Up to **7 tracks** with distinct **visual themes** (city, dockyard, desert, mountains, beach), **reverse circuit** variant per layout (free “second track”), **Gold / Silver / Bronze** score medals, ★ unlocks, garage + credits, podium finish |
| **Time Trial** | Solo clock, **ghost PB**, sector splits, optional **police chase**, **S/A/B time ranks** (not career medals) |
| **Ghost Duel** | Head-to-head ghost rules; same TT settings (fuel off, optional police) |
| **Elimination** | Last-place rivals eliminated each lap; **full** vehicle damage |
| **Outrun / Chase** | Forced police heat; checkpoint & survival wins |
| **Score Attack** | Timed scoring — drift, style, collisions, progress |
| **Practice** | Infinite fuel, no police, casual damage |

### Content & progression
- **7 track layouts** — neon oval, turbo sprint, metro gauntlet, zigzag thunder, square circuit, ridge run (elevation), neon crossover (figure-8)
- **Career medals** — score thresholds per track (G/S/B). **Time Trial ranks** — separate S/A/B vs your PB times.
- **Garage** — tuning profiles (Balanced / Drift / Speed), underglow unlocks via career ★
- **Pickups** — nitro, bananas (AI can drop), fuel pads
- **Hazards** — track obstacles; editor **hazard paint** on waypoint indices
- **Police chase** — career/custom toggle; **optional in Time Trial** (Game Settings)

### Presentation
- Main menu — mode strip, **career map** (7 nodes), lap count (1/2/3/5), difficulty, track browser, garage
- Race HUD — fuel (hidden in TT), heat (Outrun), lap timer + **PB lap** column (TT), ghost ±delta, **TIME RANK** progress (TT), career medal progress (score modes)
- **Photo mode** (P) on podium / after race, dynamic music layers, gamepad haptics, mobile touch UI
- Finish flow — career medals + credits, **TIME RANK S/A/B** + PB lines (TT), style breakdown (labeled separately from career), podium sequence

### Production services (offline-first)
- **Prefab catalog** — optional `NeonLapContentCatalog` for artist-built cars (runtime fallback)
- **Local JSON leaderboards** — `persistentDataPath/neonlap_leaderboards.json`
- **Achievements** — `AchievementIds` + `RaceEventHub` for Steam/mobile SDK mapping
- See [`docs/PRODUCTION.md`](docs/PRODUCTION.md) for Addressables & build checklist

---

## Requirements

| Tool | Version |
|------|---------|
| **Unity Editor** | `6000.4.6f1` (Unity 6) |
| **Render Pipeline** | URP `17.4.0` |
| **Input** | Input System `1.19.0` |

Clone the repo and open the project in Unity Hub. Packages restore from `Packages/manifest.json`.

---

## Quick Start

### Full game flow
1. Open **`Assets/NeonLap/Scenes/MainMenu.unity`**
2. Press **Play**
3. Pick a **mode**, **track**, **laps**, and **GO RACE**

### Direct race scene
1. Open **`Assets/Scenes/SampleScene.unity`**
2. Press **Play**

`NeonLapSceneSetup` on the **NeonLap** GameObject builds the track, spawns cars (prefab or runtime), wires camera, UI, checkpoints, AI, and mode controllers.

---

## Controls

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Accelerate | `W` / `↑` | Right Trigger |
| Brake / Reverse | `S` / `↓` | Left Trigger |
| Steer | `A` / `D` or `←` / `→` | Left stick X |
| Drift | `Space` | Right Bumper |
| Barrel roll | (bound in Input Actions) | |
| Reset / refuel (empty) | `R` | Y |
| Pause | `Escape` | Start |
| Photo mode | `P` (after race) | |

Fuel pads and nitro refill fuel in **career** modes. Time Trial has no fuel drain. Full list: main menu **CONTROLS**.

Input asset: `Assets/NeonLap/Input/NeonLap_InputActions.inputactions`

---

## Scenes & build order

| Scene | Path | Purpose |
|-------|------|---------|
| **MainMenu** | `Assets/NeonLap/Scenes/MainMenu.unity` | Modes, garage, track select |
| **SampleScene** | `Assets/Scenes/SampleScene.unity` | Race (runtime or prefab cars) |

Build settings: **MainMenu** first, then **SampleScene**.

---

## Project structure

```
neonlap/
├── Assets/
│   ├── NeonLap/
│   │   ├── Input/
│   │   ├── Materials/
│   │   ├── Prefabs/              # Optional artist cars (see README there)
│   │   ├── Resources/NeonLap/    # Tracks, garage, catalog, profiles
│   │   ├── Scenes/MainMenu.unity
│   │   └── Scripts/
│   │       ├── Audio/
│   │       ├── Camera/           # Follow, photo mode
│   │       ├── Core/             # GameManager, modes, fuel, content catalog
│   │       ├── Environment/      # Hazards, pickups, fuel pads
│   │       ├── Race/             # Modes, scoring, ghost, career
│   │       ├── Services/         # Leaderboards, achievements, RaceEventHub
│   │       ├── Track/            # Builders, TrackDefinition, 6 levels
│   │       ├── UI/
│   │       ├── VFX/              # Weather, rain, trails
│   │       └── Vehicle/
│   ├── Scenes/SampleScene.unity
│   └── Settings/                 # URP PC + Mobile
├── docs/PRODUCTION.md
├── Tools/generate_neonlap_audio.py
└── README.md
```

---

## Architecture

### Bootstrap
- **`MainMenuSetup`** — Menu UI, `GameManager` (persistent), settings load
- **`NeonLapSceneSetup`** — Track, environment, racers, race systems, UI
- **`NeonLapServicesBootstrap`** — Local leaderboards + achievement tracker

### Race flow
```
MainMenu → GameManager.LoadRace() → SampleScene
RaceManager: Countdown → Racing → Finished
OnRaceFinished → UI, career, ghosts, RaceEventHub → achievements / leaderboards
```

### Key systems

| Area | Components |
|------|------------|
| Vehicle | `VehicleController`, `AIVehicleController`, `VehicleFuelSystem`, `VehicleNitroBoost`, `VehicleBarrelRoll` |
| Track | `OvalTrackBuilder`, `TrackCenterlineBuilder`, `TrackDefinition`, level builders |
| Race | `RaceManager`, `RacerProgress`, `RaceScoreSystem`, mode controllers |
| Police | `PoliceChaseSystem`, `PlayerHeatSystem`, `ChaseModeController` |
| Replay | `RaceReplaySystem`, `GhostRacer`, `TimeTrialRecordStore` |
| Scoring | `RaceScoreSystem`, `CareerScoreStore`, `ScoreAttackRecordStore` |

### Runtime vs prefabs
- **Default:** cars and track meshes are **built in code** (fast iteration).
- **Shipping:** assign prefabs on `NeonLapContentCatalog` (`Resources/NeonLap/` or scene reference). Menu: **NeonLap → Production**.

---

## Configuration

| Asset / setting | Purpose |
|-----------------|---------|
| `Resources/NeonLap/VehicleProfiles/*` | Handling profiles |
| `Resources/NeonLap/Tracks/*` | Per-level track stats |
| `GameLapSettings` | 1 / 2 / 3 / 5 laps (menu) |
| `GameDifficultySettings` | Easy / Medium / Hard AI |
| `GameRaceModeSettings` | Active mode rules |
| `GamePoliceSettings` | Police for career/custom (TT uses `TimeTrialSettings.PoliceEnabled`) |
| `TimeTrialSettings` | TT rivals, ghost clip, optional police, time ranks on/off |
| `GameQualitySettings` | AI count, rain, helicopter, pickup density |

---

## Leaderboards & achievements (local)

**Leaderboards** — `LeaderboardService` writes JSON next to player saves. Boards are per mode + track (time or score).

**Achievements** — unlocked into `PlayerPrefs`; hook platform SDKs:

```csharp
AchievementTracker.AchievementUnlocked += id => {
    // Map AchievementIds.* to Steam / Game Center / Play Games
};
```

**Events** — `RaceEventHub.RaceFinished`, `LapPersonalBest`, `PoliceEscaped` (also used internally).

---

## Audio

Procedural placeholder clips can be generated:

```bash
python3 Tools/generate_neonlap_audio.py
```

See `Assets/NeonLap/Audio/README.md` for clip names.

---

## Rendering

- **PC:** `Assets/Settings/PC_RPAsset.asset`
- **Mobile:** `Assets/Settings/Mobile_RPAsset.asset`

Switch active pipeline in **Project Settings → Graphics**.

---

## Git & Unity

Standard Unity `.gitignore`. Commit `.meta` files with assets.

**Ignored:** `Library/`, `Temp/`, `UserSettings/`, etc.

---

## Docs

| Doc | Contents |
|-----|----------|
| [`Assets/NeonLap/SETUP.md`](Assets/NeonLap/SETUP.md) | Short wiring reference |
| [`docs/PRODUCTION.md`](docs/PRODUCTION.md) | Prefabs, Addressables, builds, SDK hooks |
| [`Assets/NeonLap/Prefabs/README.md`](Assets/NeonLap/Prefabs/README.md) | Baking hover-car prefabs |

---

## License

Provided as-is for learning and development. Add a license file before distribution.

---

## Author

**Omar Brome** — [github.com/omar-brome/neonlap](https://github.com/omar-brome/neonlap)
