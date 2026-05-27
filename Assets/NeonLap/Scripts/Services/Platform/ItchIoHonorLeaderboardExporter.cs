using System.Text;
using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Services.Platform
{
    /// <summary>
    /// Builds a plain-text honor-system leaderboard line for itch.io descriptions or forum posts.
    /// </summary>
    public static class ItchIoHonorLeaderboardExporter
    {
        const string Header = "NEONLAP — TIME TRIAL LEADERBOARD (HONOR SYSTEM)";
        const string Footer = "Paste your best times on the itch.io page. No anti-cheat — community trust.";

        public static string BuildExportText(string playerName = null)
        {
            var name = string.IsNullOrWhiteSpace(playerName) ? "Racer" : playerName.Trim();
            var builder = new StringBuilder();
            builder.AppendLine(Header);
            builder.AppendLine($"Player: {name}");
            builder.AppendLine($"Exported: {System.DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            builder.AppendLine();

            var levelCount = GameManager.Instance != null ? GameManager.Instance.TotalLevels : 6;
            for (var i = 0; i < levelCount; i++)
            {
                var track = GameManager.Instance != null ? GameManager.Instance.GetTrackDefinition(i) : null;
                var trackName = track != null ? track.trackName : $"Level {i + 1}";

                AppendTimeTrialLine(builder, i, trackName, reverse: false);

                if (TrackVariantStorage.HasReverseProgress(i))
                    AppendTimeTrialLine(builder, i, trackName + " ↺", reverse: true);
            }

            builder.AppendLine();
            builder.AppendLine($"Career ★ {CareerScoreStore.GetTotalStars()}  •  Best export: best race time per track");
            builder.AppendLine(Footer);
            return builder.ToString();
        }

        public static string BuildSingleTrackLine(int trackIndex)
        {
            var track = GameManager.Instance != null ? GameManager.Instance.GetTrackDefinition(trackIndex) : null;
            var trackName = track != null ? track.trackName : $"Level {trackIndex + 1}";
            if (GameTrackOptions.ReverseCircuit)
                trackName += " ↺";

            var race = TimeTrialRecordStore.GetBestRaceTime(trackIndex);
            if (race <= 0.05f)
                return $"{trackName}: no PB yet";

            return $"{trackName}: {TimeTrialRecordStore.FormatTime(race)}";
        }

        static void AppendTimeTrialLine(StringBuilder builder, int trackIndex, string trackName, bool reverse)
        {
            var lap = PlayerPrefs.GetFloat(
                TrackVariantStorage.Format("NeonLap.TT.LapTime.{0}", trackIndex, reverse), -1f);
            var race = PlayerPrefs.GetFloat(
                TrackVariantStorage.Format("NeonLap.TT.RaceTime.{0}", trackIndex, reverse), -1f);
            var medal = (TimeTrialMedal)PlayerPrefs.GetInt(
                TrackVariantStorage.Format("NeonLap.TT.Medal.Race.{0}", trackIndex, reverse), 0);
            var medalLabel = medal >= TimeTrialMedal.A ? TimeTrialMedalUtility.GetLabel(medal) : "—";

            builder.Append($"L{trackIndex + 1} {trackName}  |  ");
            builder.Append(lap > 0.05f ? $"Lap {TimeTrialRecordStore.FormatTime(lap)}" : "Lap —");
            builder.Append("  |  ");
            builder.Append(race > 0.05f ? $"Race {TimeTrialRecordStore.FormatTime(race)}" : "Race —");
            builder.AppendLine($"  |  Medal {medalLabel}");
        }

        public static bool TryCopyToClipboard(string playerName = null)
        {
            return TryCopyTextToClipboard(BuildExportText(playerName));
        }

        public static bool TryCopyTrackLineToClipboard(int trackIndex)
        {
            return TryCopyTextToClipboard(BuildSingleTrackLine(trackIndex));
        }

        static bool TryCopyTextToClipboard(string text)
        {
            try
            {
                GUIUtility.systemCopyBuffer = text;
                Debug.Log($"Itch.io export copied:\n{text}");
                return true;
            }
            catch
            {
                Debug.LogWarning("ItchIoHonorLeaderboardExporter: Clipboard copy failed.");
                return false;
            }
        }
    }
}
