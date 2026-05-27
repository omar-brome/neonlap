using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>
    /// Level 4 — wide lightning zigzag (only straight segments, no oval curves).
    /// </summary>
    public static class Level4ZigZagThunderTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            var spanX = straightLength * 0.5f;
            var spanZ = straightLength * 0.28f;
            var sub = straightSubdivisions + 4;

            var a = TrackCenterlineUtility.Point(-spanX, -spanZ * 2.2f);
            var b = TrackCenterlineUtility.Point(spanX, -spanZ * 2.2f);
            var c = TrackCenterlineUtility.Point(-spanX, -spanZ * 0.4f);
            var d = TrackCenterlineUtility.Point(spanX, spanZ * 0.4f);
            var e = TrackCenterlineUtility.Point(-spanX, spanZ * 2.2f);
            var f = TrackCenterlineUtility.Point(spanX, spanZ * 2.2f);
            var g = TrackCenterlineUtility.Point(-spanX, spanZ * 0.4f);
            var h = TrackCenterlineUtility.Point(spanX, -spanZ * 0.4f);

            TrackCenterlineUtility.AppendStraight(points, a, b, sub);
            TrackCenterlineUtility.AppendStraight(points, points[^1], c, sub);
            TrackCenterlineUtility.AppendStraight(points, points[^1], d, sub);
            TrackCenterlineUtility.AppendStraight(points, points[^1], e, sub);
            TrackCenterlineUtility.AppendStraight(points, points[^1], f, sub);
            TrackCenterlineUtility.AppendStraight(points, points[^1], g, sub);
            TrackCenterlineUtility.AppendStraight(points, points[^1], h, sub);
            TrackCenterlineUtility.AppendStraight(points, points[^1], a, sub);

            var innerCut = new List<Vector3>
            {
                Vector3.Lerp(b, c, 0.35f),
                new(0f, 0f, spanZ * 0.05f),
                Vector3.Lerp(g, h, 0.35f),
            };
            result.Shortcuts.Add(TrackShortcutDefinition.Create(
                innerCut,
                mergeCheckpointIndex: 7,
                scoreBonus: 180,
                displayName: "Inner M-Cut"));

            var lowerChicane = new List<Vector3>
            {
                Vector3.Lerp(c, d, 0.25f),
                new(spanX * 0.15f, 0f, -spanZ * 0.05f),
                Vector3.Lerp(c, d, 0.78f),
            };
            result.Shortcuts.Add(TrackShortcutDefinition.Create(
                lowerChicane,
                mergeCheckpointIndex: 3,
                scoreBonus: 140,
                displayName: "Lower Chicane"));
        }
    }
}
