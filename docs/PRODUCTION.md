# NeonLap production guide

## Content pipeline (today)

| Layer | Runtime (default) | Artist path |
|-------|-------------------|-------------|
| Hover cars | `NeonLapSceneSetup.BuildCar` | `NeonLapContentCatalog` + prefabs |
| Track colliders / AI path | `OvalTrackBuilder` | Keep procedural; add visual prefab parent |
| Tracks tuning | `TrackDefinition` assets in `Resources/NeonLap/Tracks` | Same SOs |
| Garage builds | `HoverBuildDefinition` in `Resources/NeonLap/Garage` | Same |

`NeonLapCarSpawner` tries catalog prefabs first, then falls back to code-built cars.

## Addressables (recommended for shipping)

1. Install **Addressables** (`com.unity.addressables`) via Package Manager.
2. Create groups: `Cars`, `Tracks`, `UI`, `Audio`.
3. Mark `HoverCar_Player.prefab`, AI prefab, and track environment art as addressable.
4. Replace `Resources.Load<NeonLapContentCatalog>` with an Addressables load in `NeonLapContentCatalog.LoadDefault()`.
5. Keep `OvalTrackBuilder` for gameplay geometry; stream only cosmetic meshes/audio.

Until Addressables land, **prefabs + Resources catalog** is enough for a PC/mobile build.

## Leaderboards

- **Offline:** `LocalJsonLeaderboardStore` → `Application.persistentDataPath/neonlap_leaderboards.json`
- **API:** `LeaderboardService` + `ILeaderboardBackend`
- **UGS stub:** `UnityGamingServicesLeaderboardBackend` (falls back to JSON until implemented)

Submissions are driven by `RaceLeaderboardBridge` on:

- Time Trial (race + lap boards)
- Score Attack (score board)
- Chase / Outrun (survival time on win)

## Achievements

IDs live in `AchievementIds` (Steam / mobile mapping). Unlocks use `PlayerPrefs` via `AchievementStore`.

Subscribe platform SDKs to:

```csharp
AchievementTracker.AchievementUnlocked += id => { /* SteamUserStats.SetAchievement ... */ };
```

Events wired through `RaceEventHub`:

| Event | Example achievements |
|-------|----------------------|
| `RaceFinished` (P1) | First win, 10 wins, 5-lap finish, career gold, score 100k |
| `LapPersonalBest` | Personal best lap |
| `PoliceEscaped` | Outrun escape |

## Build checklist

- [ ] Bake player + AI prefabs; assign `NeonLapContentCatalog`
- [ ] Verify `MainMenu` → `SampleScene` build order
- [ ] Generate audio (`Tools/generate_neonlap_audio.py`) if clips missing
- [ ] Test Elimination (full damage) vs Career (cosmetic damage)
- [ ] Export leaderboards JSON path for QA
- [ ] Hook `AchievementTracker.AchievementUnlocked` on target platform
