using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public static class ScoreAttackRecordStore
    {
        const string HighScoreKey = "NeonLap.ScoreAttack.High.{0}";

        public static int GetHighScore(int trackIndex) =>
            PlayerPrefs.GetInt(TrackVariantStorage.Format(HighScoreKey, trackIndex), 0);

        public static bool TrySaveHighScore(int trackIndex, int score)
        {
            var previous = GetHighScore(trackIndex);
            if (score <= previous)
                return false;

            PlayerPrefs.SetInt(TrackVariantStorage.Format(HighScoreKey, trackIndex), score);
            PlayerPrefs.Save();
            return true;
        }

        public static string GetTrackSummary(int trackIndex)
        {
            var high = GetHighScore(trackIndex);
            return high > 0 ? $"PB {high:N0}" : "NO PB YET";
        }

        public static int GetGlobalBestScore()
        {
            var best = 0;
            var trackCount = GameManager.Instance != null ? GameManager.Instance.TotalLevels : 6;
            for (var i = 0; i < trackCount; i++)
                best = Mathf.Max(best, GetHighScore(i));

            return best;
        }
    }
}
