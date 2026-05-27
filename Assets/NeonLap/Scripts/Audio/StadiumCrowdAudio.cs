using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using NeonLap.UI;
using UnityEngine;

namespace NeonLap.Audio
{
    public class StadiumCrowdAudio : MonoBehaviour
    {
        [SerializeField] float ambientVolume = 0.14f;
        [SerializeField] float swellBoost = 0.38f;
        [SerializeField] float swellDecaySpeed = 1.6f;
        [SerializeField] float swellDuration = 2.4f;
        [SerializeField] float cheerVolume = 0.62f;
        [SerializeField] float celebrationVolume = 0.78f;
        [SerializeField] float groanVolume = 0.55f;
        [SerializeField] float reactionCooldown = 1.15f;

        AudioSource ambientSource;
        float swellTimer;
        float swellAmount;
        float nextReactionTime;
        RaceCommentarySystem commentary;

        public static StadiumCrowdAudio Setup(Transform parent, RaceCommentarySystem commentarySystem)
        {
            NeonLapAudioLibrary.Preload();
            var go = new GameObject("StadiumCrowdAudio");
            go.transform.SetParent(parent, false);
            var crowd = go.AddComponent<StadiumCrowdAudio>();
            crowd.commentary = commentarySystem;
            crowd.BuildSources();
            crowd.Subscribe(commentarySystem);
            return crowd;
        }

        void OnEnable() => CrowdReactionHub.Reaction += HandleHubReaction;

        void OnDisable() => CrowdReactionHub.Reaction -= HandleHubReaction;

        void BuildSources()
        {
            var clip = NeonLapAudioLibrary.CrowdLoop;
            if (clip == null)
                return;

            ambientSource = NeonLapAudioSourceFactory.CreateLoopSource(transform, "CrowdAmbient", clip, ambientVolume,
                true, 0f);
            ambientSource.minDistance = 40f;
            ambientSource.maxDistance = 220f;
            ambientSource.spatialBlend = 0f;
        }

        void Subscribe(RaceCommentarySystem commentarySystem)
        {
            if (commentarySystem == null)
                return;

            commentarySystem.OnCommentaryLine -= HandleCommentaryLine;
            commentarySystem.OnCommentaryLine += HandleCommentaryLine;
        }

        void OnDestroy()
        {
            CrowdReactionHub.Reaction -= HandleHubReaction;
            if (commentary != null)
                commentary.OnCommentaryLine -= HandleCommentaryLine;
        }

        void Update()
        {
            if (ambientSource == null)
                return;

            if (swellTimer > 0f)
                swellTimer -= Time.deltaTime;

            swellAmount = Mathf.MoveTowards(swellAmount, swellTimer > 0f ? 1f : 0f, swellDecaySpeed * Time.deltaTime);
            var binding = ambientSource.GetComponent<NeonLapAudioSourceBinding>();
            if (binding != null)
                binding.LayerVolume = 1f + swellAmount * swellBoost;
            else
                ambientSource.volume = ambientVolume * (1f + swellAmount * swellBoost) * GameAudioSettings.SfxMix;
        }

        void HandleHubReaction(CrowdReactionKind kind)
        {
            switch (kind)
            {
                case CrowdReactionKind.Celebration:
                    TriggerSwell(swellDuration * 1.15f);
                    PlayCheer(celebrationVolume, 1.02f);
                    break;
                case CrowdReactionKind.Cheer:
                    TriggerSwell();
                    PlayCheer(cheerVolume, 1f);
                    break;
                case CrowdReactionKind.Mild:
                    TriggerSwell(swellDuration * 0.65f);
                    break;
                case CrowdReactionKind.Groan:
                    PlayGroan();
                    break;
            }
        }

        void HandleCommentaryLine(CommentaryCategory category)
        {
            switch (category)
            {
                case CommentaryCategory.Overtake:
                case CommentaryCategory.BigGain:
                    TriggerSwell();
                    break;
                case CommentaryCategory.TakeLead:
                    TriggerSwell(swellDuration * 1.1f);
                    break;
                case CommentaryCategory.Win:
                    TriggerSwell(swellDuration * 1.35f);
                    CrowdReactionHub.Emit(CrowdReactionKind.Celebration);
                    break;
            }
        }

        void TriggerSwell(float duration = -1f)
        {
            if (duration < 0f)
                duration = swellDuration;

            swellTimer = duration;
            var clip = NeonLapAudioLibrary.CrowdSwell;
            if (clip == null)
                return;

            var oneShot = NeonLapAudioSourceFactory.PlayOneShot(transform, "CrowdSwell", clip,
                0.35f + swellAmount * 0.15f, 0f, 0.95f);
            if (oneShot != null)
                oneShot.pitch = Random.Range(0.92f, 1.08f);
        }

        void PlayCheer(float volume, float pitchCenter)
        {
            if (Time.time < nextReactionTime)
                return;

            nextReactionTime = Time.time + reactionCooldown;
            var clip = NeonLapAudioLibrary.CrowdCheer;
            if (clip == null)
                return;

            var oneShot = NeonLapAudioSourceFactory.PlayOneShot(transform, "CrowdCheer", clip, volume, 0f, 0.98f);
            if (oneShot != null)
                oneShot.pitch = Random.Range(pitchCenter - 0.06f, pitchCenter + 0.06f);
        }

        void PlayGroan()
        {
            if (Time.time < nextReactionTime)
                return;

            nextReactionTime = Time.time + reactionCooldown * 1.6f;
            var clip = NeonLapAudioLibrary.CrowdGroan;
            if (clip == null)
                return;

            var oneShot = NeonLapAudioSourceFactory.PlayOneShot(transform, "CrowdGroan", clip, groanVolume, 0f, 0.95f);
            if (oneShot != null)
                oneShot.pitch = Random.Range(0.9f, 1.05f);
        }
    }
}
