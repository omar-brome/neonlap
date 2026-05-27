using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Environment
{
    public enum CrowdReactionLevel
    {
        Mild = 0,
        Strong = 1,
        Celebration = 2,
    }

    public class CrowdReactionController : MonoBehaviour
    {
        readonly List<CrowdFanAnimator> fans = new();
        readonly List<CrowdWaveAnimator> waves = new();

        void OnEnable() => CrowdReactionHub.Reaction += HandleHubReaction;

        void OnDisable() => CrowdReactionHub.Reaction -= HandleHubReaction;

        void HandleHubReaction(CrowdReactionKind kind) => React(CrowdReactionHub.ToVisualLevel(kind));

        public void RegisterFan(CrowdFanAnimator fan)
        {
            if (fan != null && !fans.Contains(fan))
                fans.Add(fan);
        }

        public void RegisterWave(CrowdWaveAnimator wave)
        {
            if (wave != null && !waves.Contains(wave))
                waves.Add(wave);
        }

        public void React(CrowdReactionLevel level)
        {
            var waveBoost = level switch
            {
                CrowdReactionLevel.Celebration => 2.8f,
                CrowdReactionLevel.Strong => 1.9f,
                _ => 1.35f,
            };

            var fanBoost = level switch
            {
                CrowdReactionLevel.Celebration => 2.4f,
                CrowdReactionLevel.Strong => 1.65f,
                _ => 1.2f,
            };

            var duration = level switch
            {
                CrowdReactionLevel.Celebration => 3.2f,
                CrowdReactionLevel.Strong => 2.1f,
                _ => 1.4f,
            };

            for (var i = 0; i < waves.Count; i++)
            {
                if (waves[i] != null)
                    waves[i].Boost(waveBoost, duration);
            }

            var fanSample = Mathf.Clamp(Mathf.RoundToInt(fans.Count * 0.22f), 6, fans.Count);
            if (fanSample <= 0)
                return;

            var step = Mathf.Max(1, fans.Count / fanSample);
            for (var i = 0; i < fans.Count; i += step)
            {
                if (fans[i] != null)
                    fans[i].Celebrate(fanBoost, duration);
            }
        }
    }
}
