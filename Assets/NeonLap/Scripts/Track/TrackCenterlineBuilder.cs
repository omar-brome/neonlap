using NeonLap.Track.Levels;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackCenterlineBuilder
    {
        public static System.Collections.Generic.List<Vector3> Build(
            TrackLayout layout,
            float straightLength,
            float turnRadius,
            int segmentsPerTurn,
            int straightSubmotion)
        {
            return BuildPath(layout, straightLength, turnRadius, segmentsPerTurn, straightSubmotion).Centerline;
        }

        public static TrackPathResult BuildPath(
            TrackLayout layout,
            float straightLength,
            float turnRadius,
            int segmentsPerTurn,
            int straightSubmotion)
        {
            var result = new TrackPathResult();
            BuildPathInto(result, TrackLayoutUtility.LevelIndexForLayout(layout), straightLength, turnRadius,
                segmentsPerTurn, straightSubmotion);
            return result;
        }

        public static void BuildPathInto(
            TrackPathResult result,
            int levelIndex,
            float straightLength,
            float turnRadius,
            int segmentsPerTurn,
            int straightSubmotion)
        {
            switch (levelIndex)
            {
                case 1:
                    Level2TurboSprintTrack.Build(result, straightLength, turnRadius, segmentsPerTurn, straightSubmotion);
                    break;
                case 2:
                    Level3MetroGauntletTrack.Build(result, straightLength, turnRadius, segmentsPerTurn, straightSubmotion);
                    break;
                case 3:
                    Level4ZigZagThunderTrack.Build(result, straightLength, turnRadius, segmentsPerTurn, straightSubmotion);
                    break;
                case 4:
                    Level5SquareCircuitTrack.Build(result, straightLength, turnRadius, segmentsPerTurn, straightSubmotion);
                    break;
                case 5:
                    Level6RidgeRunTrack.Build(result, straightLength, turnRadius, segmentsPerTurn, straightSubmotion);
                    break;
                case 6:
                    Level7NeonCrossoverTrack.Build(result, straightLength, turnRadius, segmentsPerTurn, straightSubmotion);
                    break;
                default:
                    Level1NeonCircuitTrack.Build(result, straightLength, turnRadius, segmentsPerTurn, straightSubmotion);
                    break;
            }

            var minSpacing = levelIndex is 2 or 3 or 6 ? 0.7f : 0.85f;
            TrackGeometryUtility.SanitizeCenterline(result.Centerline, minSpacing);
        }

        public static Vector2 ComputeEnvironmentHalfExtents(
            System.Collections.Generic.IReadOnlyList<Vector3> centerline, float trackWidth)
        {
            if (centerline == null || centerline.Count == 0)
                return new Vector2(80f, 50f);

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            foreach (var point in centerline)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            var padding = trackWidth + 48f;
            var halfX = Mathf.Min(Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX)) + padding, 112f);
            var halfZ = Mathf.Min(Mathf.Max(Mathf.Abs(minZ), Mathf.Abs(maxZ)) + padding, 112f);
            return new Vector2(halfX, halfZ);
        }
    }
}
