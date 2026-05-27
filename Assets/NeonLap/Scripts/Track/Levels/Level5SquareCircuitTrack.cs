using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>
    /// Level 5 — large perfect square (equal sides, tight 90° corners). Not an oval.
    /// </summary>
    public static class Level5SquareCircuitTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            // Equal sides — envelope is roughly square (similar X and Z extent).
            var side = straightLength * 0.5f;
            var r = turnRadius * 0.55f;
            var sub = straightSubdivisions + 3;
            var seg = segmentsPerTurn + 2;

            var zN = side;
            var zS = -side;
            var xE = side;
            var xW = -side;

            TrackCenterlineUtility.AppendStraight(points,
                new Vector3(xW + r, 0f, zN), new Vector3(xE - r, 0f, zN), sub);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xE - r, 0f, zN - r), r, 90f, 0f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xE, 0f, zS + r), sub);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xE - r, 0f, zS + r), r, 0f, -90f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xW + r, 0f, zS), sub);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xW + r, 0f, zS + r), r, -90f, -180f, seg);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xW, 0f, zN - r), sub);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xW + r, 0f, zN - r), r, 180f, 90f, seg);
        }
    }
}
