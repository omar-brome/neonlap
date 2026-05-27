using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Core
{
    public static class GameTeamRaceSettings
    {
        const string TeamKey = "NeonLap.TeamRace.PlayerTeam";

        public static RaceTeam PlayerTeam { get; private set; } = RaceTeam.Blue;

        public static void Load()
        {
            PlayerTeam = (RaceTeam)Mathf.Clamp(PlayerPrefs.GetInt(TeamKey, (int)RaceTeam.Blue), 1, 2);
        }

        public static void SetPlayerTeam(RaceTeam team)
        {
            if (team == RaceTeam.None)
                team = RaceTeam.Blue;

            PlayerTeam = team;
            PlayerPrefs.SetInt(TeamKey, (int)team);
            PlayerPrefs.Save();
        }

        public static string GetDisplayName(RaceTeam team)
        {
            return team switch
            {
                RaceTeam.Blue => "TEAM BLUE",
                RaceTeam.Red => "TEAM RED",
                _ => "NO TEAM",
            };
        }

        public static Color GetTeamColor(RaceTeam team)
        {
            return team switch
            {
                RaceTeam.Blue => new Color(0.35f, 0.65f, 1f),
                RaceTeam.Red => new Color(1f, 0.38f, 0.42f),
                _ => Color.white,
            };
        }
    }
}
