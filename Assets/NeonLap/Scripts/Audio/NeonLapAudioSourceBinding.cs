using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class NeonLapAudioSourceBinding : MonoBehaviour
    {
        [SerializeField] NeonLapAudioBus bus = NeonLapAudioBus.Sfx;
        [SerializeField] float baseVolume = 1f;

        AudioSource source;
        float layerVolume = 1f;

        public float LayerVolume
        {
            get => layerVolume;
            set
            {
                layerVolume = Mathf.Max(0f, value);
                Refresh();
            }
        }

        public void Configure(float volume, NeonLapAudioBus audioBus, float initialLayer = 1f)
        {
            baseVolume = Mathf.Max(0f, volume);
            bus = audioBus;
            layerVolume = Mathf.Max(0f, initialLayer);
            Refresh();
        }

        void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            GameAudioSettings.VolumesChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameAudioSettings.VolumesChanged -= Refresh;
        }

        public void Refresh()
        {
            if (source == null)
                source = GetComponent<AudioSource>();

            if (source == null)
                return;

            var mix = bus == NeonLapAudioBus.Music ? GameAudioSettings.MusicMix : GameAudioSettings.SfxMix;
            source.volume = baseVolume * layerVolume * mix;
        }
    }
}
