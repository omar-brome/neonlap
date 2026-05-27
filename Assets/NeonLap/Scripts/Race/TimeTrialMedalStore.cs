using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public static class TimeTrialMedalStore
    {
        const string BestRaceMedalKey = "NeonLap.TT.Medal.Race.{0}";
        const string BestLapMedalKey = "NeonLap.TT.Medal.Lap.{0}";

        public static TimeTrialMedal GetBestRaceMedal(int trackIndex) =>
            (TimeTrialMedal)PlayerPrefs.GetInt(TrackVariantStorage.Format(BestRaceMedalKey, trackIndex), 0);

        public static TimeTrialMedal GetBestLapMedal(int trackIndex) =>
            (TimeTrialMedal)PlayerPrefs.GetInt(TrackVariantStorage.Format(BestLapMedalKey, trackIndex), 0);

        public static bool TryImproveRaceMedal(int trackIndex, TimeTrialMedal medal)
        {
            if ((int)medal <= (int)GetBestRaceMedal(trackIndex))
                return false;

            PlayerPrefs.SetInt(TrackVariantStorage.Format(BestRaceMedalKey, trackIndex), (int)medal);
            PlayerPrefs.Save();
            return true;
        }

        public static bool TryImproveLapMedal(int trackIndex, TimeTrialMedal medal)
        {
            if ((int)medal <= (int)GetBestLapMedal(trackIndex))
                return false;

            PlayerPrefs.SetInt(TrackVariantStorage.Format(BestLapMedalKey, trackIndex), (int)medal);
            PlayerPrefs.Save();
            return true;
        }

        public static string GetTrackSummary(int trackIndex)
        {
            var raceMedal = GetBestRaceMedal(trackIndex);
            var lapMedal = GetBestLapMedal(trackIndex);
            var pb = TimeTrialRecordStore.GetTrackSummary(trackIndex);

            if (raceMedal == TimeTrialMedal.None && lapMedal == TimeTrialMedal.None)
                return pb;

            return $"{pb}  •  R {TimeTrialMedalUtility.GetLabel(raceMedal)}  L {TimeTrialMedalUtility.GetLabel(lapMedal)}";
        }
    }
}
