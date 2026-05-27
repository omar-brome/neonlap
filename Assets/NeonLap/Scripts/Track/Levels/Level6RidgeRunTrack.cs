using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>
    /// Level 6 — square loop with climbs, crests, and dips (centerline Y drives sloped track mesh).
    /// </summary>
    public static class Level6RidgeRunTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            var side = straightLength * 0.38f;
            var r = turnRadius * 0.8f;
            var sub = straightSubdivisions + 3;
            var seg = segmentsPerTurn;
            var climb = Mathf.Clamp(turnRadius * 0.42f, 6f, 12f);
            var dip = -climb * 0.55f;

            var zN = side;
            var zS = -side;
            var xE = side;
            var xW = -side;

            // North straight — climb.
            TrackCenterlineUtility.AppendStraight(points,
                new Vector3(xW + r, 0f, zN), new Vector3(xE - r, 0f, zN), sub, 0f, climb);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xE - r, climb, zN - r), r, 90f, 0f, seg);
            // East straight — elevated.
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xE, climb, zS + r), sub, climb, climb);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xE - r, climb, zS + r), r, 0f, -90f, seg);
            // South straight — descent.
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xW + r, 0f, zS), sub, climb, 0f);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xW + r, 0f, zS + r), r, -90f, -180f, seg);
            // West straight — dip then rise back to start height.
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xW, dip, zN - r), sub / 2 + 2, 0f, dip);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(xW, 0f, zN - r), sub / 2 + 2, dip, 0f);
            TrackCenterlineUtility.AppendArc(points, new Vector3(xW + r, 0f, zN - r), r, 180f, 90f, seg);
        }
    }
}
