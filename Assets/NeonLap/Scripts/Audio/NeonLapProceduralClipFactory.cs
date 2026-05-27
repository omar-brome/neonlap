using UnityEngine;

namespace NeonLap.Audio
{
    /// <summary>
    /// Runtime procedural clips when Resources WAVs are missing (dev-friendly, royalty-free placeholders).
    /// </summary>
    public static class NeonLapProceduralClipFactory
    {
        const int SampleRate = 44100;

        public enum MusicPreset
        {
            Menu,
            Calm,
            Racing,
            Chase,
            FinalLap,
            Podium,
        }

        public static AudioClip CreateEngineLoop() => CreateLoop("engine_loop_proc", 2.4f, (t, i) =>
        {
            var noise = PseudoNoise(i) * 0.22f;
            var rumble = Sin(52f, t) * 0.42f + Sin(104f, t) * 0.28f + Sin(208f, t) * 0.14f;
            var flutter = Sin(390f, t) * 0.06f * (0.5f + 0.5f * Sin(7.5f, t));
            return (rumble + noise + flutter) * (0.82f + 0.18f * Sin(1.7f, t)) * 0.55f;
        });

        public static AudioClip CreateWindLoop() => CreateLoop("wind_loop_proc", 2f, (t, i) =>
        {
            var n = PseudoNoise(i);
            return n * (0.25f + 0.2f * Mathf.Abs(Sin(0.4f, t))) * 0.35f;
        });

        public static AudioClip CreateDriftLoop() => CreateLoop("drift_loop_proc", 1.2f, (t, i) =>
        {
            var n = PseudoNoise(i);
            var scrape = n * (0.55f + 0.35f * Mathf.Abs(Sin(14f, t)));
            scrape += Sin(2200f, t) * 0.12f * Random01(i);
            return scrape * 0.42f;
        });

        public static AudioClip CreateImpact(bool heavy)
        {
            var duration = heavy ? 0.45f : 0.28f;
            return CreateOneShot("impact_proc", duration, (t, i) =>
            {
                var env = Envelope(t, 0.002f, heavy ? 0.18f : 0.12f, duration);
                var freq = heavy ? 180f : 260f;
                var s = Sin(freq, t) * env;
                s += PseudoNoise(i) * env * (heavy ? 0.65f : 0.45f);
                s += Sin(70f, t) * env * 0.35f;
                return s * (heavy ? 0.9f : 0.65f);
            });
        }

        public static AudioClip CreateNitroWhoosh() => CreateOneShot("nitro_proc", 1.6f, (t, i) =>
        {
            var env = Envelope(t, 0.05f, 0.35f, 1.6f);
            var sweep = 400f + 1800f * (t / 1.6f);
            var s = Sin(sweep, t) * env * 0.35f;
            s += PseudoNoise(i) * env * 0.25f;
            s += Sin(90f, t) * env * 0.2f;
            return s;
        });

        public static AudioClip CreateCountdownBeep(bool high = false) => CreateOneShot("beep_proc", 0.12f, (t, _) =>
        {
            var env = Envelope(t, 0.005f, 0.04f, 0.12f);
            return Sin(high ? 880f : 660f, t) * env * 0.7f;
        });

        public static AudioClip CreateCountdownGo() => CreateOneShot("go_proc", 0.35f, (t, _) =>
        {
            var env = Envelope(t, 0.01f, 0.12f, 0.35f);
            return (Sin(520f, t) + Sin(1040f, t) * 0.5f) * env * 0.75f;
        });

        public static AudioClip CreateLapComplete() => CreateOneShot("lap_proc", 0.55f, (t, _) =>
        {
            var notes = new[] { 523.25f, 659.25f, 783.99f };
            var noteT = (t * notes.Length) / 0.55f;
            var idx = Mathf.Min(Mathf.FloorToInt(noteT), notes.Length - 1);
            var local = (t % (0.55f / notes.Length)) / (0.55f / notes.Length);
            var env = Envelope(local, 0.01f, 0.08f, 1f);
            return Sin(notes[idx], t) * env * 0.55f;
        });

        public static AudioClip CreateFinishSting() => CreateOneShot("finish_proc", 1.4f, (t, _) =>
        {
            var notes = new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f };
            var progress = t / 1.4f;
            var idx = Mathf.Min(Mathf.FloorToInt(progress * notes.Length), notes.Length - 1);
            var slice = 1.4f / notes.Length;
            var env = Envelope(t - idx * slice, 0.01f, 0.25f, slice);
            var s = Sin(notes[idx], t) * Mathf.Max(0f, env);
            s += Sin(notes[idx] * 2f, t) * Mathf.Max(0f, env) * 0.15f;
            return s * 0.6f;
        });

        public static AudioClip CreatePoliceSiren() => CreateLoop("siren_proc", 2f, (t, _) =>
        {
            var wobble = 0.5f + 0.5f * Sin(2.2f, t);
            var freq = 650f + 350f * wobble;
            return (Sin(freq, t) * 0.55f + Sin(freq * 1.02f, t) * 0.35f) * 0.5f;
        });

        public static AudioClip CreateCrowdLoop() => CreateLoop("crowd_loop_proc", 4f, (t, i) =>
        {
            var cheer = 0.35f + 0.25f * Sin(0.7f, t) + 0.15f * Sin(1.3f, t + 0.4f);
            var noise = PseudoNoise(i) * 0.08f;
            return (cheer + noise) * 0.22f;
        });

        public static AudioClip CreateCrowdSwell() => CreateOneShot("crowd_swell_proc", 1.2f, (t, i) =>
        {
            var env = Envelope(t, 0.02f, 0.35f, 1.2f);
            var cheer = Sin(420f, t) * 0.25f + Sin(880f, t) * 0.18f + PseudoNoise(i) * 0.12f;
            return cheer * env * 0.65f;
        });

        public static AudioClip CreateCrowdCheer() => CreateOneShot("crowd_cheer_proc", 1.45f, (t, i) =>
        {
            var env = Envelope(t, 0.01f, 0.4f, 1.45f);
            var cheer = Sin(360f, t) * 0.22f + Sin(720f, t) * 0.2f + Sin(1080f, t) * 0.14f + PseudoNoise(i) * 0.1f;
            return cheer * env * 0.75f;
        });

        public static AudioClip CreateCrowdGroan() => CreateOneShot("crowd_groan_proc", 0.95f, (t, i) =>
        {
            var env = Envelope(t, 0.02f, 0.25f, 0.95f);
            var tone = Sin(180f, t) * 0.35f + Sin(95f, t) * 0.25f;
            var noise = PseudoNoise(i) * 0.18f;
            return (tone + noise) * env * 0.55f;
        });

        public static AudioClip CreateMusic(MusicPreset preset) => preset switch
        {
            MusicPreset.Menu => CreateMusicLoop("music_menu_proc", 105f, 96f, 10f, 0.5f),
            MusicPreset.Calm => CreateMusicLoop("music_calm_proc", 110f, 92f, 8f, 0.55f),
            MusicPreset.Racing => CreateMusicLoop("music_racing_proc", 130f, 128f, 8f, 0.85f),
            MusicPreset.Chase => CreateMusicLoop("music_chase_proc", 140f, 148f, 8f, 1f),
            MusicPreset.FinalLap => CreateMusicLoop("music_final_proc", 150f, 156f, 8f, 1.05f),
            MusicPreset.Podium => CreateMusicLoop("music_podium_proc", 160f, 118f, 10f, 0.72f),
            _ => CreateMusicLoop("music_generic_proc", 120f, 120f, 8f, 0.7f),
        };

        public static AudioClip CreateCommentaryStinger(CommentaryCategory category, int variantIndex)
        {
            var seed = (int)category * 997 + variantIndex * 131;
            var baseFreq = category switch
            {
                CommentaryCategory.RaceStart => 330f,
                CommentaryCategory.TakeLead => 392f,
                CommentaryCategory.Overtake => 350f,
                CommentaryCategory.BigGain => 370f,
                CommentaryCategory.Drop => 220f,
                CommentaryCategory.LastPlace => 200f,
                CommentaryCategory.FinalLap => 440f,
                CommentaryCategory.Win => 523.25f,
                CommentaryCategory.Finish => 300f,
                _ => 280f,
            };

            baseFreq += variantIndex * 8f;
            var duration = 0.45f + variantIndex * 0.012f;
            return CreateOneShot($"vo_{category}_{variantIndex:00}_proc", duration, (t, i) =>
            {
                var env = Envelope(t, 0.02f, 0.18f, duration);
                var vibrato = Sin(6f + variantIndex * 0.3f, t) * 0.04f;
                var s = Sin(baseFreq * (1f + vibrato), t) * 0.5f;
                s += Sin(baseFreq * 1.5f, t) * 0.25f;
                s += Sin(baseFreq * 2f, t) * 0.15f;
                s += PseudoNoiseSeeded(i, seed) * 0.04f;
                return s * env * 0.7f;
            });
        }

        public static AudioClip CreatePaLapCall(int lapNumber, bool finalLap)
        {
            lapNumber = Mathf.Clamp(lapNumber, 1, 9);
            var duration = finalLap ? 0.95f : 0.35f + lapNumber * 0.08f;
            return CreateOneShot(finalLap ? "pa_final_lap_proc" : $"pa_lap_{lapNumber}_proc", duration, (t, _) =>
            {
                if (finalLap)
                {
                    var env = Envelope(t, 0.01f, 0.12f, duration);
                    var s = Sin(880f, t) * env * 0.55f;
                    if (t > 0.28f)
                        s += Sin(1174f, t - 0.28f) * Envelope(t - 0.28f, 0.01f, 0.15f, duration - 0.28f) * 0.6f;
                    if (t > 0.55f)
                        s += Sin(1568f, t - 0.55f) * Envelope(t - 0.55f, 0.01f, 0.2f, duration - 0.55f) * 0.65f;
                    return s * 0.75f;
                }

                var pulseDuration = 0.09f;
                var gap = 0.07f;
                var total = 0f;
                for (var n = 0; n < lapNumber; n++)
                    total += pulseDuration + gap;

                var sOut = 0f;
                for (var n = 0; n < lapNumber; n++)
                {
                    var start = n * (pulseDuration + gap);
                    if (t < start || t > start + pulseDuration)
                        continue;

                    var local = t - start;
                    var env = Envelope(local, 0.004f, 0.03f, pulseDuration);
                    var freq = 620f + n * 40f;
                    sOut += Sin(freq, local) * env * 0.65f;
                }

                return sOut;
            });
        }

        public static AudioClip CreatePaIncident(string keyword)
        {
            var hash = keyword?.GetHashCode() ?? 0;
            return CreateOneShot("pa_incident_proc", 0.55f, (t, i) =>
            {
                var env = Envelope(t, 0.01f, 0.14f, 0.55f);
                var freq = 480f + (hash % 120);
                var s = Sin(freq, t) * env * 0.45f;
                s += PseudoNoiseSeeded(i, hash) * env * 0.18f;
                return s;
            });
        }

        static AudioClip CreateMusicLoop(string name, float baseFreq, float bpm, float duration, float energy)
        {
            return CreateLoop(name, duration, (t, i) =>
            {
                var beat = 60f / bpm;
                var kick = Sin(baseFreq * 0.5f, t) * Mathf.Pow(Mathf.Max(0f, Sin(1f / beat, t)), 6f) * 0.55f;
                var bass = Sin(baseFreq, t) * (0.7f + 0.3f * Sin(0.25f, t)) * 0.35f;
                var lead = Sin(baseFreq * 2f, t) * (0.5f + 0.5f * Sin(4f / beat, t)) * 0.22f;
                var pad = Sin(baseFreq * 1.5f, t) * (0.5f + 0.5f * Sin(0.08f, t)) * 0.12f;
                var s = (kick + bass + lead + pad) * energy;
                s += PseudoNoise(i) * 0.03f;
                return s * 0.42f;
            });
        }

        delegate float SampleFunc(float time, int index);

        static AudioClip CreateLoop(string name, float duration, SampleFunc sample)
        {
            var count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            for (var i = 0; i < count; i++)
                data[i] = sample(i / (float)SampleRate, i);
            return ToClip(name, data);
        }

        static AudioClip CreateOneShot(string name, float duration, SampleFunc sample)
        {
            var count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            for (var i = 0; i < count; i++)
                data[i] = sample(i / (float)SampleRate, i);
            return ToClip(name, data);
        }

        static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Sin(float freq, float t) => Mathf.Sin(Mathf.PI * 2f * freq * t);

        static float Envelope(float t, float attack, float release, float duration)
        {
            if (t < 0f || t > duration)
                return 0f;
            if (t < attack)
                return t / Mathf.Max(attack, 1e-6f);
            if (t > duration - release)
                return Mathf.Max(0f, (duration - t) / Mathf.Max(release, 1e-6f));
            return 1f;
        }

        static float PseudoNoise(int index)
        {
            var x = Mathf.Sin(index * 12.9898f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        static float PseudoNoiseSeeded(int index, int seed)
        {
            var x = Mathf.Sin((index + seed) * 12.9898f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        static float Random01(int index) => (PseudoNoise(index) + 1f) * 0.5f;
    }
}
