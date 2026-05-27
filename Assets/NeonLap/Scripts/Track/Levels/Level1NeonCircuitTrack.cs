using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>
    /// Level 1 — beginner-friendly classic oval. Wide turns, predictable racing line.
    /// </summary>
    public static class Level1NeonCircuitTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            var half = straightLength * 0.5f;

            TrackCenterlineUtility.AppendStraight(points,
                new Vector3(-half, 0f, turnRadius), new Vector3(half, 0f, turnRadius), straightSubdivisions);
            TrackCenterlineUtility.AppendArc(points, new Vector3(half, 0f, 0f), turnRadius, 90f, -90f, segmentsPerTurn);
            TrackCenterlineUtility.AppendStraight(points,
                new Vector3(half, 0f, -turnRadius), new Vector3(-half, 0f, -turnRadius), straightSubdivisions);
            TrackCenterlineUtility.AppendArc(points, new Vector3(-half, 0f, 0f), turnRadius, -90f, -270f,
                segmentsPerTurn);
        }
    }
}
