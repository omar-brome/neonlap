using UnityEngine;

namespace NeonLap.Track
{
    /// <summary>
    /// Shared centerline preview settings (matches <see cref="OvalTrackBuilder"/> tuning).
    /// </summary>
    public static class TrackAuthoringUtility
    {
        public struct BuildTuning
        {
            public int LevelIndex;
            public int SegmentsPerTurn;
            public int StraightSubdivisions;
        }

        public static BuildTuning GetBuildTuning(TrackDefinition definition)
        {
            var layout = definition != null
                ? TrackLayoutUtility.Normalize(definition.layout)
                : TrackLayout.Level1NeonCircuit;
            var levelIndex = TrackLayoutUtility.LevelIndexForLayout(layout);

            var segmentsPerTurn = 14;
            var straightSubdivisions = 6;
            switch (levelIndex)
            {
                case 1:
                    segmentsPerTurn = 18;
                    straightSubdivisions = 8;
                    break;
                case 2:
                    segmentsPerTurn = 16;
                    straightSubdivisions = 8;
                    break;
                case 3:
                    segmentsPerTurn = 12;
                    straightSubdivisions = 9;
                    break;
                case 4:
                    segmentsPerTurn = 16;
                    straightSubdivisions = 8;
                    break;
                case 5:
                    segmentsPerTurn = 14;
                    straightSubdivisions = 10;
                    break;
                case 6:
                    segmentsPerTurn = 18;
                    straightSubdivisions = 9;
                    break;
            }

            return new BuildTuning
            {
                LevelIndex = levelIndex,
                SegmentsPerTurn = segmentsPerTurn,
                StraightSubdivisions = straightSubdivisions,
            };
        }

        public static TrackPathResult BuildPreviewPath(TrackDefinition definition)
        {
            if (definition == null)
                return new TrackPathResult();

            var tuning = GetBuildTuning(definition);
            return TrackCenterlineBuilder.BuildPath(
                definition.layout,
                definition.straightLength,
                definition.turnRadius,
                tuning.SegmentsPerTurn,
                tuning.StraightSubdivisions);
        }

        public static int EstimateWaypointCount(TrackDefinition definition)
        {
            var path = BuildPreviewPath(definition);
            return path.Centerline.Count;
        }
    }
}
