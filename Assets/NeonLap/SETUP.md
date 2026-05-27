# NeonLap setup

## Quick start

1. Open `Assets/NeonLap/Scenes/MainMenu.unity` (recommended) or `Assets/Scenes/SampleScene.unity`
2. Press **Play**
3. **WASD** drive, **Space** drift, **R** reset / refuel when empty (career modes), **Esc** pause

`NeonLapSceneSetup` builds the track and cars unless prefabs are assigned on `NeonLapContentCatalog`.

## Modes (main menu)

Career · Time Trial · Ghost Duel · Elimination · Outrun · Score Attack · Practice · Team · Custom · Demolition · Hardcore

Configure **laps** (1/2/3/5), **difficulty**, and **track** on the career map before **GO RACE**.

### Career (7 tracks)

- **7 layouts** in the track registry (Neon Circuit → Neon Crossover); career map shows **7 nodes** when the registry is synced.
- **Visual themes** per level (sky, fog, ground palette, backdrop props): City Streets (L1–2), Dockyard Night (L3), Desert Canyon (L4), Mountain Pass (L5–6), Beach Boardwalk (L7). Override on each `TrackDefinition` via **Visual theme**; menu night toggle still stacks on top.
- **Weather variants** (Game Settings): **FORECAST** (lap-by-lap mix), **DRY**, **RAIN** (wet reflections + low grip), **FOG** (heavy mist, limited visibility), **SAND** (sandstorm particles + low grip/visibility).
- **Reverse circuit** (Game Settings → **REVERSE**): flips centerline, checkpoints, AI waypoints, and start grid. Career map unlock uses forward runs only; reverse has its own PBs, ghosts, medals, and score-attack records (`TrackVariantStorage`).
- **Gold / Silver / Bronze** from score thresholds per track (not the same as Time Trial ranks).
- **Stars (★)** per track; beat the previous level with ★ to unlock the next.
- **18/18 ★** unlocks **ENDLESS CIRCUIT**.
- **Daily challenge** — random level + rain/police/laps; bonus ★ + credits.
- **Shortcuts** on Metro Gauntlet and Zigzag Thunder (and others) — merge checkpoint required or the lap won’t count.

### Time Trial & Ghost Duel

- Solo clock mode with **ghost replay** (your PB lap/race recording).
- **No fuel drain** — the gas gauge is hidden so short TT runs aren’t dominated by a 420s tank.
- **Live ±delta** vs PB ghost, **sector PBs** at checkpoints, **GHOST ON/OFF** in race.
- **Game Settings → Time Trial:**
  - **Cycle rivals** — 0–3 AI cars for “race the clock + traffic”
  - **TT police ON/OFF** — optional chase units (separate from career **Police** toggle)
  - **Ghost clip ON/OFF** — penalty when clipping the ghost
  - **Time ranks ON/OFF** — show **S/A/B** finish ranks (time-based, not career medals)
- **S/A/B time ranks** (when enabled): **S** = beat race PB by 2%, **A** = match PB, **B** = within 5%. Stored per track in `TimeTrialMedalStore`.
- Finish panel: **TIME RANK**, race/lap PB lines, optional **STYLE POINTS** breakdown (drift/style — not career score).
- Pause menu: ghost **export/import** (base64). Dev ghost: **Neon Lap → Ghosts → Bake Placeholder Dev Ghosts**.

### Other modes (short)

| Mode | Notes |
|------|--------|
| **Outrun** | Forced police chase; survive / checkpoints |
| **Practice** | Infinite fuel, no police |
| **Elimination / Demolition** | Last standing; full damage where applicable |
| **Score Attack** | Timed scoring run |
| **Custom / Hardcore** | Uses global **Police ON/OFF** in Game Settings |

## Controls

| Action | Keyboard |
|--------|----------|
| Accelerate | W / ↑ |
| Brake | S / ↓ |
| Steer | A / D |
| Drift | Space |
| Reset / refuel | R (when fuel empty in career) |
| Pause | Esc |
| Photo mode | P (after race / podium) |

Green **fuel pads** and **nitro** refill fuel on long **career** races. Time Trial does not use fuel.

## Key assets

- `ScriptableObjects/Tracks/` — authoritative track definitions (menu: **Neon Lap → Content → Create Or Update Track Definition Assets**)
- `Resources/NeonLap/Tracks/` — legacy/runtime copies (synced via registry)
- `Resources/NeonLap/VehicleProfiles/` — car tuning
- `Resources/NeonLap/Garage/` — hover builds / tuning profiles
- `NeonLap_InputActions.inputactions` — input

## Track authoring (editor)

Select a **Track Definition** asset:

- **Scene gizmo** — centerline preview from `TrackCenterlineBuilder.BuildPath`
- **Hazard paint** — toggle waypoint indices used by `TrackHazardBuilder` when **Use authored indices** is on

## Production prefabs (optional)

1. **NeonLap → Production → Create Content Catalog**
2. Bake cars to `Assets/NeonLap/Prefabs/` (see `Prefabs/README.md`)
3. Assign catalog on **NeonLap** scene object or leave in Resources

## Services

- Leaderboards: `persistentDataPath/neonlap_leaderboards.json`
- Achievements: `AchievementIds` + `AchievementTracker.AchievementUnlocked`
- Cloud save / itch export: main menu **OPTIONS → SHARE / SAVE**

Details: [`docs/PRODUCTION.md`](../../docs/PRODUCTION.md)

## Manual race scene wiring

1. Empty GameObject **NeonLap** + `NeonLapSceneSetup`
2. Assign profile, track SO, input actions, materials
3. Main Camera present (`FollowCamera` added at runtime)

## Main menu

`MainMenu.unity` + `MainMenuSetup` (creates UI and `GameManager` at runtime).
