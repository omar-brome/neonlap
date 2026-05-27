using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public static class StuntFreestyleRecordStore
    {
        const string BestScoreKey = "NeonLap.Stunt.BestScore";
        const string BestAirKey = "NeonLap.Stunt.BestAir";
        const string TrickCountKey = "NeonLap.Stunt.TrickCount";

        public static int GetBestScore() => PlayerPrefs.GetInt(BestScoreKey, 0);

        public static float GetBestAirSeconds() => PlayerPrefs.GetFloat(BestAirKey, 0f);

        public static int GetTotalTricks() => PlayerPrefs.GetInt(TrickCountKey, 0);

        public static bool TrySaveSession(int score, float bestAirSeconds, int trickCount)
        {
            var improved = false;
            if (score > GetBestScore())
            {
                PlayerPrefs.SetInt(BestScoreKey, score);
                improved = true;
            }

            if (bestAirSeconds > GetBestAirSeconds() + 0.01f)
            {
                PlayerPrefs.SetFloat(BestAirKey, bestAirSeconds);
                improved = true;
            }

            if (trickCount > 0)
            {
                PlayerPrefs.SetInt(TrickCountKey, GetTotalTricks() + trickCount);
                improved = true;
            }

            if (improved)
                PlayerPrefs.Save();

            return improved;
        }

        public static string GetSummaryLine()
        {
            var best = GetBestScore();
            var air = GetBestAirSeconds();
            if (best <= 0 && air <= 0.01f)
                return "No stunt PB yet";

            var airText = air > 0.01f ? $"  Best air {air:0.0}s" : string.Empty;
            return $"Stunt PB {best:N0}{airText}";
        }
    }
}
