using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.UI
{
    public static class HudMedalProgressFormatter
    {
        public static string GetDashboardLine(RaceManager raceManager, RaceScoreSystem scoreSystem)
        {
            if (raceManager == null)
                return string.Empty;

            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;

            if (GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel)
                return GetTimeTrialMedalLine(trackIndex);

            if (GameRaceModeSettings.IsScoreAttack || GameRaceModeSettings.Rules.ShowRaceScore)
                return GetCareerScoreLine(trackIndex, raceManager, scoreSystem);

            return string.Empty;
        }

        static string GetTimeTrialMedalLine(int trackIndex)
        {
            TimeTrialSettings.Load();
            if (!TimeTrialSettings.ShowTimeRanks)
                return "TIME TRIAL  PB GHOST ACTIVE";

            var racePb = TimeTrialRecordStore.GetBestRaceTime(trackIndex);
            if (racePb <= 0.05f)
                return "TIME RANK  SET A PB FOR S/A/B";

            var raceMedal = TimeTrialMedalStore.GetBestRaceMedal(trackIndex);
            return raceMedal >= TimeTrialMedal.A
                ? $"TIME RANK  {TimeTrialMedalUtility.GetLabel(raceMedal)}  •  PB {TimeTrialRecordStore.FormatTime(racePb)}"
                : $"TIME RANK  {TimeTrialRecordStore.FormatTime(racePb)} FOR A";
        }

        static string GetCareerScoreLine(int trackIndex, RaceManager raceManager, RaceScoreSystem scoreSystem)
        {
            var score = scoreSystem != null ? scoreSystem.Score : 0;
            var placement = raceManager.GetPlayerPosition();
            var bestLap = raceManager.BestLapTime;
            var shortcutMet = RaceShortcutTracker.Instance == null || RaceShortcutTracker.Instance.ShortcutLapValid;
            var currentMedal = RaceMedalUtility.Evaluate(trackIndex, score, placement, bestLap, shortcutMet);

            if (currentMedal >= RaceMedal.Gold)
                return $"MEDAL  GOLD  •  {score:N0} PTS";

            var nextMedal = currentMedal switch
            {
                RaceMedal.None => RaceMedal.Bronze,
                RaceMedal.Bronze => RaceMedal.Silver,
                _ => RaceMedal.Gold,
            };

            var table = CareerMedalTables.Get(trackIndex);
            var target = nextMedal switch
            {
                RaceMedal.Bronze => table.BronzeScore,
                RaceMedal.Silver => table.SilverScore,
                _ => table.GoldScore,
            };

            var letter = RaceMedalUtility.GetMedalLetter(nextMedal);
            return $"{score:N0} / {target:N0} FOR {letter}";
        }
    }
}
