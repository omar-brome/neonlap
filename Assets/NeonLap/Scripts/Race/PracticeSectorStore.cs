using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public static class PracticeSectorStore
    {
        const string SectorKey = "NeonLap.Practice.Sector.{0}.{1}";

        public static float GetBestSector(int trackIndex, int checkpointIndex) =>
            PlayerPrefs.GetFloat(TrackVariantStorage.Format(SectorKey, null, trackIndex, checkpointIndex), -1f);

        public static bool TrySaveSector(int trackIndex, int checkpointIndex, float sectorTime)
        {
            if (sectorTime <= 0.01f)
                return false;

            var previous = GetBestSector(trackIndex, checkpointIndex);
            if (previous > 0f && sectorTime >= previous)
                return false;

            PlayerPrefs.SetFloat(TrackVariantStorage.Format(SectorKey, null, trackIndex, checkpointIndex), sectorTime);
            PlayerPrefs.Save();
            return true;
        }

        public static string FormatDelta(float current, float best)
        {
            if (best <= 0.01f)
                return " NEW PB";

            var delta = current - best;
            return $" Δ{delta:+0.00;-0.00}s";
        }
    }
}
