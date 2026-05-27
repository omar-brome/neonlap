using UnityEngine;

namespace NeonLap.Core
{
    /// <summary>Separates forward vs reverse layout saves so each direction is its own track.</summary>
    public static class TrackVariantStorage
    {
        public const string ReverseSuffix = ".Rev";

        public static bool IsReverseLayout => GameTrackOptions.ReverseCircuit;

        public static string Format(string keyFormat, int trackIndex, bool? reverse = null)
        {
            return Format(keyFormat, reverse, trackIndex);
        }

        public static string Format(string keyFormat, bool? reverse, params object[] args)
        {
            var useReverse = reverse ?? GameTrackOptions.ReverseCircuit;
            return string.Format(keyFormat, args) + (useReverse ? ReverseSuffix : string.Empty);
        }

        public static string ForwardFormat(string keyFormat, params object[] args) => string.Format(keyFormat, args);

        public static bool HasReverseProgress(int trackIndex)
        {
            return PlayerPrefs.HasKey(Format("NeonLap.Career.Score.{0}", trackIndex, true))
                   || PlayerPrefs.HasKey(Format("NeonLap.TT.LapTime.{0}", trackIndex, true));
        }
    }
}
