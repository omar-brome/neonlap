namespace NeonLap.Race
{
    public struct TimeTrialFinishResult
    {
        public TimeTrialMedal RaceMedal;
        public TimeTrialMedal LapMedal;
        public bool ImprovedRaceMedal;
        public bool ImprovedLapMedal;
        public string TrackSummary;
    }

    public static class TimeTrialFinishEvaluator
    {
        public static TimeTrialFinishResult Evaluate(
            int trackIndex,
            float raceTime,
            float bestLapTime,
            bool newRacePb,
            bool newLapPb,
            string extraSummary = null)
        {
            var previousRace = newRacePb
                ? raceTime * 1.05f
                : TimeTrialRecordStore.GetBestRaceTime(trackIndex);
            var previousLap = newLapPb
                ? bestLapTime * 1.05f
                : TimeTrialRecordStore.GetBestLapTime(trackIndex);

            var raceMedal = TimeTrialMedalUtility.EvaluateRace(trackIndex, raceTime, previousRace);
            var lapMedal = bestLapTime > 0.05f
                ? TimeTrialMedalUtility.EvaluateLap(bestLapTime, previousLap)
                : TimeTrialMedal.None;

            var improvedRace = TimeTrialMedalStore.TryImproveRaceMedal(trackIndex, raceMedal);
            var improvedLap = TimeTrialMedalStore.TryImproveLapMedal(trackIndex, lapMedal);

            var summary = TimeTrialMedalStore.GetTrackSummary(trackIndex);
            if (!string.IsNullOrWhiteSpace(extraSummary))
                summary += "  •  " + extraSummary;

            return new TimeTrialFinishResult
            {
                RaceMedal = raceMedal,
                LapMedal = lapMedal,
                ImprovedRaceMedal = improvedRace,
                ImprovedLapMedal = improvedLap,
                TrackSummary = summary,
            };
        }
    }
}
