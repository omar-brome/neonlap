using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackGeometryPaths
    {
        public static void ReverseCenterlineInPlace(IList<Vector3> centerline)
        {
            if (centerline == null || centerline.Count < 2)
                return;

            var count = centerline.Count;
            for (var i = 0; i < count / 2; i++)
            {
                var swap = centerline[i];
                centerline[i] = centerline[count - 1 - i];
                centerline[count - 1 - i] = swap;
            }
        }

        public static void ReverseShortcutsInPlace(IList<TrackShortcutDefinition> shortcuts)
        {
            if (shortcuts == null)
                return;

            foreach (var shortcut in shortcuts)
                ReverseCenterlineInPlace(shortcut.Path);
        }
    }
}
