using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>
    /// Level 3 — technical circuit: long back straight, esses, and a tight hairpin. Built from arcs/straights only.
    /// </summary>
    public static class Level3MetroGauntletTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            var half = straightLength * 0.5f;
            var r = turnRadius * 0.9f;
            var rTight = turnRadius * 0.58f;
            var sub = straightSubdivisions + 1;
            var seg = segmentsPerTurn + 2;

            var zTop = r * 1.55f;
            var zMid = -r * 0.15f;
            var zLow = -r * 1.35f;

            TrackCenterlineUtility.AppendStraight(points,
                new Vector3(-half, 0f, zTop), new Vector3(half * 0.35f, 0f, zTop), sub);

            TrackCenterlineUtility.AppendArc(points,
                new Vector3(half * 0.35f + r * 0.75f, 0f, zTop - r * 0.75f), r * 0.82f, 120f, 15f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1],
                new Vector3(half + r * 0.25f, 0f, zMid + r * 0.35f), sub / 2 + 2);

            TrackCenterlineUtility.AppendArc(points,
                new Vector3(half + r * 0.15f, 0f, zMid - rTight * 0.55f), rTight, 5f, -195f, seg + 4);

            TrackCenterlineUtility.AppendStraight(points, points[^1],
                new Vector3(half * 0.55f, 0f, zLow + r * 0.55f), 4);
            TrackCenterlineUtility.AppendArc(points,
                new Vector3(half * 0.2f, 0f, zLow + r * 0.2f), r * 0.62f, -25f, -95f, seg);
            TrackCenterlineUtility.AppendArc(points,
                new Vector3(-half * 0.25f, 0f, zLow + r * 0.15f), r * 0.62f, 85f, 165f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1],
                new Vector3(-half, 0f, zLow + r * 0.45f), sub);

            TrackCenterlineUtility.AppendStraight(points, points[^1],
                new Vector3(-half, 0f, zTop - r), sub + 2);
            TrackCenterlineUtility.AppendArc(points,
                new Vector3(-half + r, 0f, zTop - r), r, 180f, 90f, seg);

            var hairpinCut = new List<Vector3>();
            TrackCenterlineUtility.AppendStraight(hairpinCut,
                new Vector3(half * 0.42f, 0f, zMid + r * 0.1f),
                new Vector3(half * 0.12f, 0f, zMid - rTight * 0.2f), 3);
            TrackCenterlineUtility.AppendStraight(hairpinCut, hairpinCut[^1],
                new Vector3(-half * 0.05f, 0f, zLow + r * 0.28f), 3);
            TrackCenterlineUtility.AppendStraight(hairpinCut, hairpinCut[^1],
                new Vector3(-half * 0.55f, 0f, zTop - r * 0.72f), 4);

            result.Shortcuts.Add(TrackShortcutDefinition.Create(
                hairpinCut,
                mergeCheckpointIndex: 6,
                scoreBonus: 220,
                displayName: "Metro Hairpin Cut"));
        }
    }
}
