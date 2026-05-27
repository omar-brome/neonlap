using NeonLap.Core;
using NeonLap.Race;

namespace NeonLap.UI
{
    public static class PauseMenuStatusText
    {
        public static string Build()
        {
            var mode = GameRaceModeSettings.GetDisplayName(GameRaceModeSettings.Current);
            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var track = GameManager.Instance != null ? GameManager.Instance.GetCurrentTrackDefinition() : null;
            var trackName = track != null ? track.trackName : $"Track {trackIndex + 1}";
            var pbLine = BuildPersonalBestLine(trackIndex);
            return string.IsNullOrWhiteSpace(pbLine)
                ? $"{mode}  •  {trackName}"
                : $"{mode}  •  {trackName}\n{pbLine}";
        }

        static string BuildPersonalBestLine(int trackIndex)
        {
            if (GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel)
                return TimeTrialRecordStore.GetTrackSummary(trackIndex);

            if (GameRaceModeSettings.IsScoreAttack)
                return ScoreAttackRecordStore.GetTrackSummary(trackIndex);

            if (GameRaceModeSettings.IsCareer)
                return CareerScoreStore.GetTrackSummary(trackIndex);

            return string.Empty;
        }
    }
}
