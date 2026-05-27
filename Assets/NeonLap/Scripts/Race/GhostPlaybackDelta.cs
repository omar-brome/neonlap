using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Race
{
    public static class GhostPlaybackDelta
    {
        public static bool TryComputeSeconds(
            IReadOnlyList<ReplayFrameSnapshot> frames,
            Vector3 playerPosition,
            float referenceTime,
            out float deltaSeconds)
        {
            deltaSeconds = 0f;
            if (frames == null || frames.Count < 2)
                return false;

            if (!TryFindClosestFrameIndex(frames, playerPosition, out var closestIndex))
                return false;

            var ghostTime = frames[closestIndex].Time;
            deltaSeconds = referenceTime - ghostTime;
            return true;
        }

        public static string FormatDelta(float deltaSeconds)
        {
            if (Mathf.Abs(deltaSeconds) < 0.005f)
                return "±0.00";

            return deltaSeconds > 0f ? $"+{deltaSeconds:0.00}" : $"{deltaSeconds:0.00}";
        }

        static bool TryFindClosestFrameIndex(IReadOnlyList<ReplayFrameSnapshot> frames, Vector3 position,
            out int closestIndex)
        {
            closestIndex = 0;
            var bestDistSq = float.MaxValue;

            for (var i = 0; i < frames.Count; i++)
            {
                var distSq = (frames[i].Position - position).sqrMagnitude;
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                closestIndex = i;
            }

            return bestDistSq < float.MaxValue;
        }
    }
}
