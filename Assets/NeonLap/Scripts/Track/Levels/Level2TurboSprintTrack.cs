using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>
    /// Level 2 — stadium rectangle with a chicane on the back straight. Clearly wider and squarer than the oval.
    /// </summary>
    public static class Level2TurboSprintTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            var hw = straightLength * 0.46f;
            var hl = straightLength * 0.34f;
            var r = turnRadius * 0.86f;
            var sub = straightSubdivisions + 2;
            var seg = segmentsPerTurn;

            var zN = hl;
            var zS = -hl;
            var xW = -hw;
            var xE = hw;

            // Top straight (start / finish on the left, racing +X).
            TrackCenterlineUtility.AppendStraight(points,
                new Vector3(xW + r, 0f, zN), new Vector3(xE - r, 0f, zN), sub);

            // NE corner → east straight.
            TrackCenterlineUtility.AppendArc(points, new Vector3(xE - r, 0f, zN - r), r, 90f, 0f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xE, 0f, zS + r), sub);

            // SE corner → chicane on south straight (z = zS).
            TrackCenterlineUtility.AppendArc(points, new Vector3(xE - r, 0f, zS + r), r, 0f, -90f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xE * 0.58f, 0f, zS - r * 0.42f), 4);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xE * 0.12f, 0f, zS + r * 0.32f), 4);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(-xE * 0.18f, 0f, zS - r * 0.28f), 4);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xW + r, 0f, zS), sub);

            // SW corner → west straight.
            TrackCenterlineUtility.AppendArc(points, new Vector3(xW + r, 0f, zS + r), r, -90f, -180f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xW, 0f, zN - r), sub);

            // NW corner → back to start straight.
            TrackCenterlineUtility.AppendArc(points, new Vector3(xW + r, 0f, zN - r), r, 180f, 90f, seg);
        }
    }
}
