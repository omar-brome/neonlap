using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public sealed class TrackPathResult
    {
        public List<Vector3> Centerline { get; } = new();
        public List<TrackShortcutDefinition> Shortcuts { get; } = new();
    }
}
