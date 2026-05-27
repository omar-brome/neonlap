using System;
using UnityEngine;

namespace NeonLap.Core
{
    public static class GameAudioSettings
    {
        const string MasterKey = "NeonLap.Audio.Master";
        const string SfxKey = "NeonLap.Audio.Sfx";
        const string MusicKey = "NeonLap.Audio.Music";

        public static float MasterVolume { get; private set; } = 1f;
        public static float SfxVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;

        public static float SfxMix => MasterVolume * SfxVolume;
        public static float MusicMix => MasterVolume * MusicVolume;

        public static event Action VolumesChanged;

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
            ApplyListener();
            VolumesChanged?.Invoke();
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            PlayerPrefs.Save();
            ApplyListener();
            NotifyChanged();
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            PlayerPrefs.Save();
            NotifyChanged();
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.Save();
            NotifyChanged();
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        public static string GetSummaryLine()
        {
            return $"Master {Mathf.RoundToInt(MasterVolume * 100f)}%  •  SFX {Mathf.RoundToInt(SfxVolume * 100f)}%  •  Music {Mathf.RoundToInt(MusicVolume * 100f)}%";
        }

        static void ApplyListener()
        {
            AudioListener.volume = MasterVolume;
        }

        static void NotifyChanged()
        {
            VolumesChanged?.Invoke();
        }
    }
}
