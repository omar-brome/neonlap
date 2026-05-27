using UnityEngine;

namespace NeonLap.Core
{
    public enum RaceMode
    {
        Career = 0,
        TimeTrial = 1,
        Elimination = 2,
        Chase = 3,
        ScoreAttack = 4,
        Practice = 5,
        Custom = 6,
        TeamRace = 7,
        Demolition = 8,
        Hardcore = 9,
        GhostDuel = 10,
        StuntFreestyle = 11,
    }

    public static class GameRaceModeSettings
    {
        const string PrefKey = "NeonLap.RaceMode";

        public static RaceMode Current { get; private set; } = RaceMode.Career;
        public static RaceModeRules Rules => RaceModeRules.For(Current);

        public static bool IsCareer => Current == RaceMode.Career;
        public static bool IsTimeTrial => Current == RaceMode.TimeTrial;
        public static bool IsElimination => Current == RaceMode.Elimination;
        public static bool IsChase => Current == RaceMode.Chase;
        public static bool IsScoreAttack => Current == RaceMode.ScoreAttack;
        public static bool IsPractice => Current == RaceMode.Practice;
        public static bool IsCustom => Current == RaceMode.Custom;
        public static bool IsTeamRace => Current == RaceMode.TeamRace;
        public static bool IsDemolition => Current == RaceMode.Demolition;
        public static bool IsHardcore => Current == RaceMode.Hardcore;
        public static bool IsGhostDuel => Current == RaceMode.GhostDuel;
        public static bool IsStuntFreestyle => Current == RaceMode.StuntFreestyle;

        public static void Load()
        {
            var stored = PlayerPrefs.GetInt(PrefKey, (int)RaceMode.Career);
            Current = (RaceMode)Mathf.Clamp(stored, 0, (int)RaceMode.StuntFreestyle);
        }

        public static void SetMode(RaceMode mode)
        {
            Current = mode;
            PlayerPrefs.SetInt(PrefKey, (int)mode);
            PlayerPrefs.Save();
        }

        public static string GetDisplayName(RaceMode mode)
        {
            return mode switch
            {
                RaceMode.Career => "CAREER",
                RaceMode.TimeTrial => "TIME TRIAL",
                RaceMode.Elimination => "ELIMINATION",
                RaceMode.Chase => "OUTRUN",
                RaceMode.ScoreAttack => "SCORE ATTACK",
                RaceMode.Practice => "PRACTICE",
                RaceMode.Custom => "CUSTOM RACE",
                RaceMode.TeamRace => "TEAM RACE",
                RaceMode.Demolition => "DEMOLITION",
                RaceMode.Hardcore => "HARDCORE",
                RaceMode.GhostDuel => "GHOST DUEL",
                RaceMode.StuntFreestyle => "STUNT PARK",
                _ => "CAREER"
            };
        }

        public static string GetShortName(RaceMode mode)
        {
            return mode switch
            {
                RaceMode.Career => "CAREER",
                RaceMode.TimeTrial => "TRIAL",
                RaceMode.Elimination => "ELIM",
                RaceMode.Chase => "CHASE",
                RaceMode.ScoreAttack => "SCORE",
                RaceMode.Practice => "PRACTICE",
                RaceMode.Custom => "CUSTOM",
                RaceMode.TeamRace => "TEAM",
                RaceMode.Demolition => "DEMO",
                RaceMode.Hardcore => "HARD",
                RaceMode.GhostDuel => "DUEL",
                RaceMode.StuntFreestyle => "STUNT",
                _ => "CAREER"
            };
        }
    }
}
