using UnityEngine;

namespace NeonLap.Race
{
    public class RacerTeamMarker : MonoBehaviour
    {
        public RaceTeam Team { get; private set; }

        public void Configure(RaceTeam team)
        {
            Team = team;
        }
    }
}
