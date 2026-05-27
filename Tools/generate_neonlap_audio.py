#!/usr/bin/env python3
"""Generate placeholder WAV clips for Neon Lap (procedural, royalty-free)."""

import math
import random
import struct
import wave
from pathlib import Path

SAMPLE_RATE = 44100
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/NeonLap/Audio/Resources/NeonLap/AudioClips"


def write_wav(name: str, samples: list[float], rate: int = SAMPLE_RATE) -> None:
    path = OUTPUT / f"{name}.wav"
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "w") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(rate)
        frames = bytearray()
        for sample in samples:
            value = int(max(-1.0, min(1.0, sample)) * 32767.0)
            frames.extend(struct.pack("<h", value))
        wf.writeframes(frames)
    print(f"Wrote {path}")


def sine(freq: float, t: float) -> float:
    return math.sin(2.0 * math.pi * freq * t)


def envelope(t: float, attack: float, release: float, duration: float) -> float:
    if t < 0.0 or t > duration:
        return 0.0
    if t < attack:
        return t / max(attack, 1e-6)
    if t > duration - release:
        return max(0.0, (duration - t) / max(release, 1e-6))
    return 1.0


def gen_engine_loop(duration: float = 2.4) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        s = 0.42 * sine(52, t) + 0.28 * sine(104, t) + 0.14 * sine(208, t)
        s += 0.06 * sine(390, t) * (0.5 + 0.5 * sine(7.5, t))
        s += 0.04 * random.uniform(-1.0, 1.0)
        s *= 0.82 + 0.18 * sine(1.7, t)
        out.append(s * 0.55)
    return out


def gen_wind_loop(duration: float = 2.0) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        n = random.uniform(-1.0, 1.0)
        s = n * (0.25 + 0.2 * abs(sine(0.4, t)))
        out.append(s * 0.35)
    return out


def gen_drift_loop(duration: float = 1.2) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        n = random.uniform(-1.0, 1.0)
        s = n * (0.55 + 0.35 * abs(sine(14.0, t)))
        s += 0.12 * sine(2200, t) * random.uniform(0.2, 1.0)
        out.append(s * 0.42)
    return out


def gen_impact(heavy: bool) -> list[float]:
    duration = 0.45 if heavy else 0.28
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        env = envelope(t, 0.002, 0.18 if heavy else 0.12, duration)
        freq = 180.0 if heavy else 260.0
        s = sine(freq, t) * env
        s += random.uniform(-1.0, 1.0) * env * (0.65 if heavy else 0.45)
        s += sine(70, t) * env * 0.35
        out.append(s * (0.9 if heavy else 0.65))
    return out


def gen_nitro(duration: float = 1.6) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        env = envelope(t, 0.05, 0.35, duration)
        sweep = 400.0 + 1800.0 * (t / duration)
        s = sine(sweep, t) * env * 0.35
        s += random.uniform(-1.0, 1.0) * env * 0.25
        s += sine(90, t) * env * 0.2
        out.append(s)
    return out


def gen_beep(high: bool = False) -> list[float]:
    duration = 0.12
    count = int(duration * SAMPLE_RATE)
    freq = 880.0 if high else 660.0
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        env = envelope(t, 0.005, 0.04, duration)
        out.append(sine(freq, t) * env * 0.7)
    return out


def gen_go() -> list[float]:
    duration = 0.35
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        env = envelope(t, 0.01, 0.12, duration)
        s = sine(520, t) + 0.5 * sine(1040, t)
        out.append(s * env * 0.75)
    return out


def gen_lap_complete() -> list[float]:
    duration = 0.55
    count = int(duration * SAMPLE_RATE)
    notes = [523.25, 659.25, 783.99]
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        note_t = (t * len(notes)) / duration
        idx = min(int(note_t), len(notes) - 1)
        local = (t % (duration / len(notes))) / (duration / len(notes))
        env = envelope(local, 0.01, 0.08, 1.0)
        out.append(sine(notes[idx], t) * env * 0.55)
    return out


def gen_finish_sting() -> list[float]:
    duration = 1.4
    count = int(duration * SAMPLE_RATE)
    notes = [392.0, 523.25, 659.25, 783.99, 1046.5]
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        progress = t / duration
        idx = min(int(progress * len(notes)), len(notes) - 1)
        env = envelope(t - idx * (duration / len(notes)), 0.01, 0.25, duration / len(notes))
        s = sine(notes[idx], t) * max(0.0, env)
        s += 0.15 * sine(notes[idx] * 2, t) * max(0.0, env)
        out.append(s * 0.6)
    return out


def gen_music_loop(base_freq: float, bpm: float, duration: float, energy: float) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    beat = 60.0 / bpm
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        kick = 0.55 * sine(base_freq * 0.5, t) * max(0.0, sine(1.0 / beat, t)) ** 6
        bass = 0.35 * sine(base_freq, t) * (0.7 + 0.3 * sine(0.25, t))
        lead = 0.22 * sine(base_freq * 2.0, t) * (0.5 + 0.5 * sine(4.0 / beat, t))
        pad = 0.12 * sine(base_freq * 1.5, t) * (0.5 + 0.5 * sine(0.08, t))
        s = (kick + bass + lead + pad) * energy
        s += 0.03 * random.uniform(-1.0, 1.0)
        out.append(s * 0.42)
    return out


def gen_vo_stinger(base: float, duration: float = 0.55) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        env = envelope(t, 0.02, 0.18, duration)
        s = 0.5 * sine(base, t) + 0.25 * sine(base * 1.5, t) + 0.15 * sine(base * 2, t)
        out.append(s * env * 0.7)
    return out


def gen_police_siren(duration: float = 2.0) -> list[float]:
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        wobble = 0.5 + 0.5 * sine(2.2, t)
        freq = 650.0 + 350.0 * wobble
        s = 0.55 * sine(freq, t) + 0.35 * sine(freq * 1.02, t)
        out.append(s * 0.5)
    return out


def gen_pa_lap(lap_number: int) -> list[float]:
    duration = 0.35 + lap_number * 0.08
    count = int(duration * SAMPLE_RATE)
    out = []
    pulse = 0.09
    gap = 0.07
    for i in range(count):
        t = i / SAMPLE_RATE
        s = 0.0
        for n in range(lap_number):
            start = n * (pulse + gap)
            if start <= t <= start + pulse:
                local = t - start
                env = envelope(local, 0.004, 0.03, pulse)
                s += sine(620.0 + n * 40.0, local) * env * 0.65
        out.append(s)
    return out


def gen_pa_final_lap() -> list[float]:
    duration = 0.95
    count = int(duration * SAMPLE_RATE)
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        s = sine(880.0, t) * envelope(t, 0.01, 0.12, duration) * 0.55
        if t > 0.28:
            s += sine(1174.0, t - 0.28) * envelope(t - 0.28, 0.01, 0.15, duration - 0.28) * 0.6
        if t > 0.55:
            s += sine(1568.0, t - 0.55) * envelope(t - 0.55, 0.01, 0.2, duration - 0.55) * 0.65
        out.append(s * 0.75)
    return out


def main() -> None:
    random.seed(42)
    write_wav("engine_loop", gen_engine_loop())
    write_wav("wind_loop", gen_wind_loop())
    write_wav("drift_scrape", gen_drift_loop())
    write_wav("impact_light", gen_impact(False))
    write_wav("impact_heavy", gen_impact(True))
    write_wav("nitro_whoosh", gen_nitro())
    write_wav("countdown_beep", gen_beep(False))
    write_wav("countdown_go", gen_go())
    write_wav("lap_complete", gen_lap_complete())
    write_wav("finish_sting", gen_finish_sting())
    write_wav("police_siren", gen_police_siren())
    write_wav("music_calm", gen_music_loop(110.0, 92.0, 8.0, 0.55))
    write_wav("music_racing", gen_music_loop(130.0, 128.0, 8.0, 0.85))
    write_wav("music_chase", gen_music_loop(140.0, 148.0, 8.0, 1.0))
    write_wav("music_final_lap", gen_music_loop(150.0, 156.0, 8.0, 1.05))
    write_wav("music_menu", gen_music_loop(105.0, 96.0, 10.0, 0.5))
    write_wav("music_podium", gen_music_loop(160.0, 118.0, 10.0, 0.72))
    write_wav("pa_final_lap", gen_pa_final_lap())
    for lap in range(1, 10):
        write_wav(f"pa_lap_{lap}", gen_pa_lap(lap))
    write_wav("pa_incident", gen_vo_stinger(480.0, 0.55))
    write_wav("vo_generic", gen_vo_stinger(280.0))
    categories = {
        "vo_start": 330.0,
        "vo_lead": 392.0,
        "vo_overtake": 350.0,
        "vo_drop": 220.0,
        "vo_final": 440.0,
        "vo_win": 523.25,
        "vo_finish": 300.0,
    }
    for prefix, base in categories.items():
        write_wav(prefix, gen_vo_stinger(base, 0.6))
        for variant in range(1, 11):
            write_wav(f"{prefix}_{variant:02d}", gen_vo_stinger(base + variant * 8.0, 0.45 + variant * 0.012))
    print("Done.")


if __name__ == "__main__":
    main()
