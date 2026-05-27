using UnityEngine;

namespace NeonLap.Audio
{
    /// <summary>
    /// Factory helpers for constructing routed AudioSources with NeonLap bus bindings.
    /// </summary>
    public static class NeonLapAudioSourceFactory
    {
        public static AudioSource CreateLoopSource(Transform parent, string name, AudioClip clip, float volume = 1f,
            bool playOnAwake = true, float spatialBlend = 0f, NeonLapAudioBus bus = NeonLapAudioBus.Sfx)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = playOnAwake;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            var binding = go.AddComponent<NeonLapAudioSourceBinding>();
            binding.Configure(volume, bus, 1f);
            if (clip != null && playOnAwake)
                source.Play();
            return source;
        }

        public static AudioSource CreateOneShotSource(Transform parent, string name, float volume = 1f,
            float spatialBlend = 0f, NeonLapAudioBus bus = NeonLapAudioBus.Sfx)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            var binding = go.AddComponent<NeonLapAudioSourceBinding>();
            binding.Configure(volume, bus, 1f);
            return source;
        }

        public static AudioSource PlayOneShot(Transform parent, string name, AudioClip clip, float volume = 1f,
            float spatialBlend = 0f, float pitch = 1f, NeonLapAudioBus bus = NeonLapAudioBus.Sfx)
        {
            if (clip == null)
                return null;

            var source = CreateOneShotSource(parent, name, volume, spatialBlend, bus);
            source.pitch = pitch;
            source.PlayOneShot(clip, 1f);
            return source;
        }
    }
}
