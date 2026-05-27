using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Audio
{
    public class CommentaryVoiceover : MonoBehaviour
    {
        const int VariantsPerCategory = 10;

        [SerializeField] AudioSource voiceSource;
        [SerializeField] float minGap = 1.8f;

        float lastPlayTime;
        readonly Dictionary<CommentaryCategory, AudioClip[]> clipBuckets = new();
        readonly Dictionary<CommentaryCategory, int> nextVariantIndex = new();

        public static CommentaryVoiceover Setup(Transform parent)
        {
            NeonLapAudioLibrary.Preload();
            var go = new GameObject("CommentaryVoiceover");
            go.transform.SetParent(parent, false);
            var voice = go.AddComponent<CommentaryVoiceover>();
            voice.voiceSource = NeonLapAudioSourceFactory.CreateOneShotSource(go.transform, "CommentaryVoice", 0.95f, 0f);
            voice.LoadClips();
            return voice;
        }

        void LoadClips()
        {
            foreach (CommentaryCategory category in System.Enum.GetValues(typeof(CommentaryCategory)))
            {
                if (category == CommentaryCategory.Generic)
                    continue;

                clipBuckets[category] = LoadCategoryBucket(category);
                nextVariantIndex[category] = 0;
            }

            clipBuckets[CommentaryCategory.Generic] = LoadCategoryBucket(CommentaryCategory.Generic);
        }

        static AudioClip[] LoadCategoryBucket(CommentaryCategory category)
        {
            var clips = new List<AudioClip>();
            for (var i = 0; i < VariantsPerCategory; i++)
            {
                var clip = NeonLapAudioLibrary.GetCommentaryClip(category, i);
                if (clip != null)
                    clips.Add(clip);
            }

            if (clips.Count == 0)
            {
                var fallback = NeonLapAudioLibrary.GetCommentaryClip(CommentaryCategory.Generic, 0);
                if (fallback != null)
                    clips.Add(fallback);
            }

            return clips.ToArray();
        }

        public void Play(CommentaryCategory category, bool force = false)
        {
            if (voiceSource == null)
                return;

            if (!force && Time.unscaledTime - lastPlayTime < minGap)
                return;

            if (!clipBuckets.TryGetValue(category, out var clips) || clips.Length == 0)
            {
                if (!clipBuckets.TryGetValue(CommentaryCategory.Generic, out clips) || clips.Length == 0)
                    return;
            }

            var clip = PickClip(category, clips);
            if (clip == null)
                return;

            voiceSource.pitch = Random.Range(0.96f, 1.04f);
            voiceSource.PlayOneShot(clip, 0.9f * GameAudioSettings.SfxMix);
            lastPlayTime = Time.unscaledTime;
        }

        AudioClip PickClip(CommentaryCategory category, AudioClip[] clips)
        {
            if (clips.Length == 1)
                return clips[0];

            if (!nextVariantIndex.TryGetValue(category, out var index))
                index = 0;

            var clip = clips[index % clips.Length];
            nextVariantIndex[category] = (index + 1) % clips.Length;
            return clip;
        }
    }
}
