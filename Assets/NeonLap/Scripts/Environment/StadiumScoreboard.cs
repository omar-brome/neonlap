using NeonLap.Race;
using NeonLap.Track;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.Environment
{
    public class StadiumScoreboard : MonoBehaviour
    {
        [SerializeField] Text titleText;
        [SerializeField] Text positionText;
        [SerializeField] Text lapText;
        [SerializeField] Text timerText;
        [SerializeField] Text statusText;

        RaceManager raceManager;

        public void Configure(RaceManager manager, Text title, Text position, Text lap, Text timer, Text status)
        {
            raceManager = manager;
            titleText = title;
            positionText = position;
            lapText = lap;
            timerText = timer;
            statusText = status;
        }

        void Update()
        {
            if (raceManager == null)
                return;

            if (titleText != null)
                titleText.text = "NEON LAP LIVE";

            if (positionText != null)
            {
                var position = raceManager.GetPlayerPosition();
                var total = Mathf.Max(raceManager.TotalRacers, 1);
                positionText.text = $"P{position}/{total}";
            }

            if (lapText != null)
            {
                var lap = Mathf.Min(raceManager.CurrentLap, raceManager.TotalLaps);
                lapText.text = $"LAP {lap}/{raceManager.TotalLaps}";
            }

            if (timerText != null)
                timerText.text = FormatTime(raceManager.RaceTime);

            if (statusText != null)
            {
                statusText.text = raceManager.State switch
                {
                    RaceState.Countdown => raceManager.CountdownValue > 0
                        ? $"STARTS IN {raceManager.CountdownValue}"
                        : "GO!",
                    RaceState.Racing => "RACE LIVE",
                    RaceState.Finished => "FINISH",
                    _ => "NEON GRAND PRIX"
                };
            }
        }

        static string FormatTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = seconds % 60f;
            return $"{minutes:00}:{secs:00.0}";
        }
    }
}
