using NeonLap.Race;
using UnityEngine;

namespace NeonLap.UI
{
    public static class GhostAheadWarning
    {
        const float AheadDeltaThreshold = -0.025f;
        const float StrongAheadDelta = -0.08f;
        const float MaxLateralMeters = 5.5f;
        const float MinDistance = 3f;
        const float MaxDistance = 26f;

        public static bool TryEvaluate(GhostRacer ghost, Transform player, out float warningLevel, out Vector3 worldPoint)
        {
            warningLevel = 0f;
            worldPoint = player != null ? player.position : Vector3.zero;

            if (ghost == null || player == null || !ghost.IsVisible || !ghost.HasGhost)
                return false;

            if (!ghost.TryGetDeltaSeconds(player.position, out var deltaSeconds))
                return false;

            if (deltaSeconds >= AheadDeltaThreshold)
                return false;

            var toGhost = ghost.transform.position - player.position;
            toGhost.y = 0f;
            var distance = toGhost.magnitude;
            if (distance < MinDistance || distance > MaxDistance)
                return false;

            var lateral = Mathf.Abs(Vector3.Dot(player.right, toGhost.normalized) * distance);
            if (lateral > MaxLateralMeters)
                return false;

            var distanceFactor = 1f - Mathf.InverseLerp(MaxDistance, MinDistance, distance);
            var deltaFactor = Mathf.InverseLerp(AheadDeltaThreshold, StrongAheadDelta, deltaSeconds);
            var lineFactor = 1f - Mathf.InverseLerp(MaxLateralMeters, 1.5f, lateral);
            warningLevel = Mathf.Clamp01(distanceFactor * deltaFactor * lineFactor);
            worldPoint = ghost.transform.position;
            return warningLevel > 0.04f;
        }
    }
}
