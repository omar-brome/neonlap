using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>
    /// Level 7 — twin-loop crossover (figure-8 style). Two circular lobes linked through the center.
    /// </summary>
    public static class Level7NeonCrossoverTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            var r = turnRadius * 0.74f;
            var sep = straightLength * 0.17f;
            var seg = segmentsPerTurn + 4;

            // Left lobe, then right lobe — path crosses the origin twice per lap (over/under style).
            TrackCenterlineUtility.AppendArc(points, new Vector3(-sep, 0f, 0f), r, -90f, 270f, seg * 2);
            TrackCenterlineUtility.AppendArc(points, new Vector3(sep, 0f, 0f), r, -90f, 270f, seg * 2);

            var crossover = new List<Vector3>
            {
                new Vector3(-sep * 0.42f, 0f, r * 0.62f),
                Vector3.zero,
                new Vector3(sep * 0.42f, 0f, -r * 0.62f),
            };
            result.Shortcuts.Add(TrackShortcutDefinition.Create(
                crossover,
                mergeCheckpointIndex: Mathf.Max(6, points.Count / 5),
                scoreBonus: 210,
                displayName: "Center Cross"));
        }
    }
}
