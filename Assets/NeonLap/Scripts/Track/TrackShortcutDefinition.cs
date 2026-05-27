using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public sealed class TrackShortcutDefinition
    {
        public List<Vector3> Path { get; } = new();
        public int MergeCheckpointIndex;
        public int ScoreBonus = 200;
        public string DisplayName = "Shortcut";

        public static TrackShortcutDefinition Create(
            IReadOnlyList<Vector3> path,
            int mergeCheckpointIndex,
            int scoreBonus = 200,
            string displayName = "Shortcut")
        {
            var definition = new TrackShortcutDefinition
            {
                MergeCheckpointIndex = mergeCheckpointIndex,
                ScoreBonus = scoreBonus,
                DisplayName = displayName,
            };

            if (path != null)
                definition.Path.AddRange(path);

            return definition;
        }
    }
}
