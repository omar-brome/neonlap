using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Audio
{
    public class MenuMusicController : MonoBehaviour
    {
        [SerializeField] float volume = 0.38f;

        AudioSource menuSource;
        NeonLapAudioSourceBinding binding;

        public static MenuMusicController Setup(Transform parent)
        {
            NeonLapAudioLibrary.Preload();
            var go = new GameObject("MenuMusic");
            go.transform.SetParent(parent, false);
            var controller = go.AddComponent<MenuMusicController>();
            controller.BuildSource();
            return controller;
        }

        void BuildSource()
        {
            menuSource = NeonLapAudioSourceFactory.CreateLoopSource(transform, "MenuMusicLayer",
                NeonLapAudioLibrary.MusicMenu, volume, true, 0f, NeonLapAudioBus.Music);
            binding = menuSource.GetComponent<NeonLapAudioSourceBinding>();
            binding.LayerVolume = 1f;
        }

        void OnEnable()
        {
            GameAudioSettings.VolumesChanged += RefreshVolume;
            RefreshVolume();
        }

        void OnDisable()
        {
            GameAudioSettings.VolumesChanged -= RefreshVolume;
        }

        void RefreshVolume()
        {
            binding?.Refresh();
        }
    }
}
