# Neon Lap Audio

Clips live in `Resources/NeonLap/AudioClips/` and are loaded at runtime by `NeonLapAudioLibrary`. Procedural fallbacks are generated when a WAV is missing.

## Vehicle & race SFX

| Clip | Use |
|------|-----|
| `engine_loop` | Player + AI engine (3D on rivals) |
| `wind_loop` | High-speed wind |
| `drift_scrape` | Drift tire scrape |
| `impact_light` / `impact_heavy` | Barrier & hazard collisions |
| `nitro_whoosh` | Nitro boost |
| `countdown_beep` / `countdown_go` | Race start countdown |
| `lap_complete` | Lap crossed (not final lap) |
| `finish_sting` | Race finish |
| `police_siren` | Chase ambience when police enabled |
| `crowd_loop` | Stadium crowd bed (optional) |
| `crowd_swell` | Ambient swell on overtakes / lead changes (optional) |
| `crowd_cheer` | Cheer sting when leading / winning (optional) |
| `crowd_groan` | Disappointed crowd on player crashes (optional) |

## Music stems (looping)

| Clip | Layer |
|------|--------|
| `music_menu` | Main menu |
| `music_calm` | Countdown / pre-race |
| `music_racing` | Green flag racing |
| `music_chase` | Police chase active |
| `music_final_lap` | Final lap |
| `music_podium` | Post-race podium |

`DynamicRaceMusicController` crossfades stems. `PoliceChaseAudio` ducks music while the siren plays.

## Voice / PA

Commentary and PA clips use `vo_*` and `pa_*` filenames (see `NeonLapAudioLibrary`).

## Mix

Options → **AUDIO**: master, SFX, and music sliders (`GameAudioSettings`).

## Regenerate procedural fallbacks

```bash
python3 Tools/generate_neonlap_audio.py
```

Or in Unity: **NeonLap → Regenerate Audio Clips**

Replace any `.wav` in `AudioClips/` with your own assets (keep filenames) to upgrade quality without code changes.
