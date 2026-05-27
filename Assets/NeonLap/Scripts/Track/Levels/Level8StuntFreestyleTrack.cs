using UnityEngine;

namespace NeonLap.Track.Levels
{
    /// <summary>Bonus stunt park — ramps, vertical loop, twin half-pipes, and open freestyle pads.</summary>
    public static class Level8StuntFreestyleTrack
    {
        public static void Build(TrackPathResult result, float straightLength, float turnRadius, int segmentsPerTurn,
            int straightSubdivisions)
        {
            var points = result.Centerline;
            var sub = Mathf.Max(6, straightSubdivisions);
            var seg = Mathf.Max(20, segmentsPerTurn);
            var loopRadius = Mathf.Clamp(turnRadius * 0.65f, 12f, 18f);
            var pipeRadius = Mathf.Clamp(turnRadius * 0.55f, 10f, 14f);

            var z = 0f;
            var x = 0f;

            TrackCenterlineUtility.AppendStraight(points, new Vector3(x, 0f, z), new Vector3(x, 0f, z + 55f), sub);
            z = points[^1].z;

            TrackCenterlineUtility.AppendLaunchRamp(points, 38f, 14f, sub + 4);
            TrackCenterlineUtility.AppendVerticalLoop(points, loopRadius, seg + 12, Vector3.forward);
            TrackCenterlineUtility.AppendStraight(points, points[^1], points[^1] + new Vector3(0f, 0f, 28f), sub, points[^1].y, 0f);
            z = points[^1].z;

            TrackCenterlineUtility.AppendHalfPipe(points, pipeRadius, 52f, seg, Vector3.right * 0.15f);
            TrackCenterlineUtility.AppendStraight(points, points[^1], points[^1] + new Vector3(42f, 0f, 0f), sub);
            x = points[^1].x;

            TrackCenterlineUtility.AppendHalfPipe(points, pipeRadius * 1.08f, 48f, seg, Vector3.left * 0.12f);
            TrackCenterlineUtility.AppendStraight(points, points[^1], points[^1] + new Vector3(-42f, 0f, 0f), sub);

            TrackCenterlineUtility.AppendLaunchRamp(points, 32f, 10f, sub + 2);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(x, 0f, z + 18f), sub, points[^1].y, 0f);

            TrackCenterlineUtility.AppendArc(points, new Vector3(x - 48f, 0f, z + 8f), 48f, 200f, 270f, seg);
            TrackCenterlineUtility.AppendArc(points, new Vector3(x, 0f, z - 36f), 36f, 270f, 180f, seg / 2);
            TrackCenterlineUtility.AppendStraight(points, points[^1], new Vector3(x, 0f, z), sub);
        }
    }
}
