using UnityEngine;

namespace NeonLap.Race
{
    public enum TimeTrialMedal
    {
        None = 0,
        B = 1,
        A = 2,
        S = 3,
    }

    public static class TimeTrialMedalUtility
    {
        const float SImprovement = 0.02f;
        const float BWithinPercent = 0.05f;

        public static TimeTrialMedal EvaluateRace(int trackIndex, float raceTime, float previousBestRace)
        {
            if (raceTime <= 0.05f)
                return TimeTrialMedal.None;

            if (previousBestRace <= 0.05f)
                return TimeTrialMedal.S;

            if (raceTime < previousBestRace * (1f - SImprovement))
                return TimeTrialMedal.S;

            if (raceTime <= previousBestRace)
                return TimeTrialMedal.A;

            if (raceTime <= previousBestRace * (1f + BWithinPercent))
                return TimeTrialMedal.B;

            return TimeTrialMedal.None;
        }

        public static TimeTrialMedal EvaluateLap(float lapTime, float previousBestLap)
        {
            if (lapTime <= 0.05f)
                return TimeTrialMedal.None;

            if (previousBestLap <= 0.05f)
                return TimeTrialMedal.S;

            if (lapTime < previousBestLap * (1f - SImprovement))
                return TimeTrialMedal.S;

            if (lapTime <= previousBestLap)
                return TimeTrialMedal.A;

            if (lapTime <= previousBestLap * (1f + BWithinPercent))
                return TimeTrialMedal.B;

            return TimeTrialMedal.None;
        }

        public static int Rank(TimeTrialMedal medal) => (int)medal;

        public static string GetLabel(TimeTrialMedal medal)
        {
            return medal switch
            {
                TimeTrialMedal.S => "S",
                TimeTrialMedal.A => "A",
                TimeTrialMedal.B => "B",
                _ => "—"
            };
        }

        public static string GetTitle(TimeTrialMedal medal)
        {
            return medal switch
            {
                TimeTrialMedal.S => "S RANK — ELITE PACE",
                TimeTrialMedal.A => "A RANK — PERSONAL BEST",
                TimeTrialMedal.B => "B RANK — CLOSE RUN",
                _ => "NO RANK"
            };
        }

        public static Color GetColor(TimeTrialMedal medal)
        {
            return medal switch
            {
                TimeTrialMedal.S => new Color(1f, 0.92f, 0.35f),
                TimeTrialMedal.A => new Color(0.45f, 1f, 1f),
                TimeTrialMedal.B => new Color(0.75f, 0.88f, 1f),
                _ => new Color(0.55f, 0.55f, 0.6f)
            };
        }

        public static string GetRequirementHint(int trackIndex)
        {
            var race = TimeTrialRecordStore.GetBestRaceTime(trackIndex);
            if (race <= 0.05f)
                return "S = set first PB  •  A = match PB  •  B = within 5%";

            return $"S = beat race PB by 2% ({FormatTime(race * (1f - SImprovement))})  •  A ≤ {FormatTime(race)}";
        }

        static string FormatTime(float seconds) => TimeTrialRecordStore.FormatTime(seconds);
    }
}
