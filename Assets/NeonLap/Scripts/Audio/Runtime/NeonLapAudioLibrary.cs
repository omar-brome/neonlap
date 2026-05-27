using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Audio
{
    /// <summary>
    /// Loads clips from Resources/NeonLap/AudioClips, with procedural fallbacks when
    /// WAV assets are absent. Cached statically for the lifetime of the application.
    /// </summary>
    public static class NeonLapAudioLibrary
    {
        const string ClipRoot = "NeonLap/AudioClips/";
        const int PaLapCacheSize = 10;

        static bool loaded;
        static AudioClip engineLoop;
        static AudioClip windLoop;
        static AudioClip driftScrape;
        static AudioClip impactLight;
        static AudioClip impactHeavy;
        static AudioClip nitroWhoosh;
        static AudioClip countdownBeep;
        static AudioClip countdownGo;
        static AudioClip lapComplete;
        static AudioClip finishSting;
        static AudioClip policeSiren;
        static AudioClip musicMenu;
        static AudioClip musicCalm;
        static AudioClip musicRacing;
        static AudioClip musicChase;
        static AudioClip musicFinalLap;
        static AudioClip musicPodium;
        static AudioClip crowdLoop;
        static AudioClip crowdSwell;
        static AudioClip crowdCheer;
        static AudioClip crowdGroan;
        static readonly AudioClip[] paLapClips = new AudioClip[PaLapCacheSize];
        static AudioClip paFinalLapClip;
        static readonly Dictionary<string, AudioClip> paIncidentCache = new();

        public static AudioClip EngineLoop => Get(ref engineLoop, "engine_loop", NeonLapProceduralClipFactory.CreateEngineLoop);
        public static AudioClip WindLoop => Get(ref windLoop, "wind_loop", NeonLapProceduralClipFactory.CreateWindLoop);
        public static AudioClip DriftScrape => Get(ref driftScrape, "drift_scrape", NeonLapProceduralClipFactory.CreateDriftLoop);
        public static AudioClip ImpactLight => Get(ref impactLight, "impact_light", () => NeonLapProceduralClipFactory.CreateImpact(false));
        public static AudioClip ImpactHeavy => Get(ref impactHeavy, "impact_heavy", () => NeonLapProceduralClipFactory.CreateImpact(true));
        public static AudioClip NitroWhoosh => Get(ref nitroWhoosh, "nitro_whoosh", NeonLapProceduralClipFactory.CreateNitroWhoosh);
        public static AudioClip CountdownBeep => Get(ref countdownBeep, "countdown_beep", () => NeonLapProceduralClipFactory.CreateCountdownBeep(false));
        public static AudioClip CountdownGo => Get(ref countdownGo, "countdown_go", NeonLapProceduralClipFactory.CreateCountdownGo);
        public static AudioClip LapComplete => Get(ref lapComplete, "lap_complete", NeonLapProceduralClipFactory.CreateLapComplete);
        public static AudioClip FinishSting => Get(ref finishSting, "finish_sting", NeonLapProceduralClipFactory.CreateFinishSting);
        public static AudioClip PoliceSiren => Get(ref policeSiren, "police_siren", NeonLapProceduralClipFactory.CreatePoliceSiren);
        public static AudioClip MusicMenu => Get(ref musicMenu, "music_menu", () => NeonLapProceduralClipFactory.CreateMusic(NeonLapProceduralClipFactory.MusicPreset.Menu));
        public static AudioClip MusicCalm => Get(ref musicCalm, "music_calm", () => NeonLapProceduralClipFactory.CreateMusic(NeonLapProceduralClipFactory.MusicPreset.Calm));
        public static AudioClip MusicRacing => Get(ref musicRacing, "music_racing", () => NeonLapProceduralClipFactory.CreateMusic(NeonLapProceduralClipFactory.MusicPreset.Racing));
        public static AudioClip MusicChase => Get(ref musicChase, "music_chase", () => NeonLapProceduralClipFactory.CreateMusic(NeonLapProceduralClipFactory.MusicPreset.Chase));
        public static AudioClip MusicFinalLap => Get(ref musicFinalLap, "music_final_lap", () => NeonLapProceduralClipFactory.CreateMusic(NeonLapProceduralClipFactory.MusicPreset.FinalLap));
        public static AudioClip MusicPodium => Get(ref musicPodium, "music_podium", () => NeonLapProceduralClipFactory.CreateMusic(NeonLapProceduralClipFactory.MusicPreset.Podium));
        public static AudioClip CrowdLoop => Get(ref crowdLoop, "crowd_loop", NeonLapProceduralClipFactory.CreateCrowdLoop);
        public static AudioClip CrowdSwell => Get(ref crowdSwell, "crowd_swell", NeonLapProceduralClipFactory.CreateCrowdSwell);
        public static AudioClip CrowdCheer => Get(ref crowdCheer, "crowd_cheer", NeonLapProceduralClipFactory.CreateCrowdCheer);
        public static AudioClip CrowdGroan => Get(ref crowdGroan, "crowd_groan", NeonLapProceduralClipFactory.CreateCrowdGroan);

        public static bool IsLoaded => loaded;
        public static bool UsesProceduralFallback { get; private set; }

        public static AudioClip GetVoiceClip(string clipName) => Resources.Load<AudioClip>(ClipRoot + clipName);

        public static AudioClip GetCommentaryClip(CommentaryCategory category, int variantIndex)
        {
            variantIndex = Mathf.Clamp(variantIndex, 0, 9);
            var padded = $"{GetVoicePrefix(category)}_{variantIndex + 1:00}";
            var clip = GetVoiceClip(padded);
            if (clip != null)
                return clip;

            var legacy = GetVoiceClip(GetVoicePrefix(category));
            if (legacy != null)
                return legacy;

            UsesProceduralFallback = true;
            return NeonLapProceduralClipFactory.CreateCommentaryStinger(category, variantIndex);
        }

        public static AudioClip GetPaLapClip(int lapNumber, bool finalLap)
        {
            if (finalLap)
            {
                if (paFinalLapClip == null)
                {
                    paFinalLapClip = GetVoiceClip("pa_final_lap");
                    if (paFinalLapClip == null)
                    {
                        UsesProceduralFallback = true;
                        paFinalLapClip = NeonLapProceduralClipFactory.CreatePaLapCall(1, true);
                    }
                }

                return paFinalLapClip;
            }

            lapNumber = Mathf.Clamp(lapNumber, 1, PaLapCacheSize);
            var index = lapNumber - 1;
            if (paLapClips[index] == null)
            {
                paLapClips[index] = GetVoiceClip($"pa_lap_{lapNumber}");
                if (paLapClips[index] == null)
                {
                    UsesProceduralFallback = true;
                    paLapClips[index] = NeonLapProceduralClipFactory.CreatePaLapCall(lapNumber, false);
                }
            }

            return paLapClips[index];
        }

        public static AudioClip GetPaIncidentClip(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return null;

            var key = keyword.ToUpperInvariant();
            if (paIncidentCache.TryGetValue(key, out var cached))
                return cached;

            var clip = GetVoiceClip("pa_incident");
            if (clip == null)
            {
                UsesProceduralFallback = true;
                clip = NeonLapProceduralClipFactory.CreatePaIncident(key);
            }

            paIncidentCache[key] = clip;
            return clip;
        }

        static string GetVoicePrefix(CommentaryCategory category) => category switch
        {
            CommentaryCategory.RaceStart => "vo_start",
            CommentaryCategory.TakeLead => "vo_lead",
            CommentaryCategory.Overtake => "vo_overtake",
            CommentaryCategory.BigGain => "vo_overtake",
            CommentaryCategory.Drop => "vo_drop",
            CommentaryCategory.LastPlace => "vo_drop",
            CommentaryCategory.FinalLap => "vo_final",
            CommentaryCategory.Win => "vo_win",
            CommentaryCategory.Finish => "vo_finish",
            _ => "vo_generic",
        };

        static AudioClip Get(ref AudioClip cache, string clipName, System.Func<AudioClip> fallback)
        {
            if (cache == null)
                cache = Resources.Load<AudioClip>(ClipRoot + clipName);

            if (cache == null && fallback != null)
            {
                UsesProceduralFallback = true;
                cache = fallback();
            }

            return cache;
        }

        public static void Preload()
        {
            _ = EngineLoop;
            _ = WindLoop;
            _ = DriftScrape;
            _ = ImpactLight;
            _ = ImpactHeavy;
            _ = NitroWhoosh;
            _ = CountdownBeep;
            _ = CountdownGo;
            _ = LapComplete;
            _ = FinishSting;
            _ = PoliceSiren;
            _ = MusicMenu;
            _ = MusicCalm;
            _ = MusicRacing;
            _ = MusicChase;
            _ = MusicFinalLap;
            _ = MusicPodium;
            _ = CrowdLoop;
            _ = CrowdSwell;
            _ = CrowdCheer;
            _ = CrowdGroan;
            loaded = EngineLoop != null;
        }
    }
}
