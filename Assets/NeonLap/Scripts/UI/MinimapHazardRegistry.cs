using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.UI
{
    public enum MinimapHazardKind
    {
        Barrel = 0,
        Crate = 1,
        Cone = 2,
        Debris = 3,
    }

    public struct MinimapHazardMarker
    {
        public MinimapHazardKind Kind;
        public Vector3 WorldPosition;
        public int WaypointIndex;
    }

    public static class MinimapHazardRegistry
    {
        static readonly List<MinimapHazardMarker> markers = new();

        public static IReadOnlyList<MinimapHazardMarker> Markers => markers;

        public static void Clear()
        {
            markers.Clear();
        }

        public static void Register(MinimapHazardKind kind, Vector3 worldPosition, int waypointIndex)
        {
            markers.Add(new MinimapHazardMarker
            {
                Kind = kind,
                WorldPosition = worldPosition,
                WaypointIndex = waypointIndex,
            });
        }
    }
}
