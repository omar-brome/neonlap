using NeonLap.Core;
using NeonLap.Services.Achievements;
using UnityEngine;

namespace NeonLap.Race
{
    public struct CareerRaceResult
    {
        public int TrackIndex;
        public int Score;
        public int Placement;
        public RaceMedal Medal;
        public int Stars;
        public int PreviousHighScore;
        public int HighScore;
        public bool NewHighScore;
        public bool ImprovedMedal;
        public int PreviousStars;
        public int TotalStars;
    }

    public static class CareerScoreStore
    {
        const string HighScoreKey = "NeonLap.Career.Score.{0}";
        const string BestLapKey = "NeonLap.Career.BestLap.{0}";
        const string MedalKey = "NeonLap.Career.Medal.{0}";
        const string StarsKey = "NeonLap.Career.Stars.{0}";

        public static int GetHighScore(int trackIndex) =>
            PlayerPrefs.GetInt(TrackVariantStorage.Format(HighScoreKey, trackIndex), 0);

        public static RaceMedal GetBestMedal(int trackIndex) =>
            (RaceMedal)PlayerPrefs.GetInt(TrackVariantStorage.Format(MedalKey, trackIndex), 0);

        public static int GetStars(int trackIndex) =>
            PlayerPrefs.GetInt(TrackVariantStorage.Format(StarsKey, trackIndex), 0);

        public static int GetForwardStars(int trackIndex) =>
            PlayerPrefs.GetInt(TrackVariantStorage.ForwardFormat(StarsKey, trackIndex), 0);

        public static float GetBestLapTime(int trackIndex) =>
            PlayerPrefs.GetFloat(TrackVariantStorage.Format(BestLapKey, trackIndex), 0f);

        public static int GetTotalStars()
        {
            var total = 0;
            var levelCount = GameManager.Instance != null
                ? GameManager.Instance.TotalLevels
                : 6;

            for (var i = 0; i < levelCount; i++)
                total += GetForwardStars(i);

            return total;
        }

        public static bool IsTrackUnlocked(int trackIndex)
        {
            if (trackIndex <= 0)
                return true;

            return GetForwardStars(trackIndex - 1) >= 1;
        }

        public static CareerRaceResult RecordRace(
            int trackIndex,
            int score,
            int placement,
            float bestLapTime,
            bool shortcutRequirementMet = true)
        {
            var previousHigh = GetHighScore(trackIndex);
            var previousBestLap = GetBestLapTime(trackIndex);
            var previousMedal = GetBestMedal(trackIndex);
            var previousStars = GetStars(trackIndex);

            var requiresShortcut = Track.TrackLevelConfig.RequiresShortcutForMedal(trackIndex);
            var medal = RaceMedalUtility.Evaluate(trackIndex, score, placement, bestLapTime,
                !requiresShortcut || shortcutRequirementMet);
            var stars = RaceMedalUtility.StarsFromMedal(medal);
            var newHigh = score > previousHigh;

            if (newHigh)
                PlayerPrefs.SetInt(TrackVariantStorage.Format(HighScoreKey, trackIndex), score);

            if (bestLapTime > 0.05f && (previousBestLap <= 0.05f || bestLapTime < previousBestLap))
                PlayerPrefs.SetFloat(TrackVariantStorage.Format(BestLapKey, trackIndex), bestLapTime);

            if ((int)medal > (int)previousMedal)
                PlayerPrefs.SetInt(TrackVariantStorage.Format(MedalKey, trackIndex), (int)medal);

            if (stars > previousStars)
                PlayerPrefs.SetInt(TrackVariantStorage.Format(StarsKey, trackIndex), stars);

            if (newHigh
                || (bestLapTime > 0.05f && (previousBestLap <= 0.05f || bestLapTime < previousBestLap))
                || stars > previousStars
                || (int)medal > (int)previousMedal)
                PlayerPrefs.Save();

            var result = new CareerRaceResult
            {
                TrackIndex = trackIndex,
                Score = score,
                Placement = placement,
                Medal = medal,
                Stars = stars,
                PreviousHighScore = previousHigh,
                HighScore = Mathf.Max(previousHigh, score),
                NewHighScore = newHigh,
                ImprovedMedal = (int)medal > (int)previousMedal,
                PreviousStars = previousStars,
                TotalStars = GetTotalStars()
            };

            CareerAchievementEvaluator.SyncAll();
            return result;
        }

        public static string GetTrackSummary(int trackIndex)
        {
            var high = GetHighScore(trackIndex);
            var stars = GetStars(trackIndex);
            var medal = RaceMedalUtility.GetMedalLabel(GetBestMedal(trackIndex));
            var bestLap = GetBestLapTime(trackIndex);

            if (high <= 0)
                return "NO SCORE YET";

            var lapText = bestLap > 0.05f ? $"  Lap {FormatLap(bestLap)}" : string.Empty;
            return $"{RaceMedalUtility.FormatCompactProgress(stars, GetBestMedal(trackIndex))}  PB {high:N0}{lapText}";
        }

        public static string GetLevelButtonLabel(int trackIndex, string trackName)
        {
            var stars = GetForwardStars(trackIndex);
            var forwardMedal = (RaceMedal)PlayerPrefs.GetInt(TrackVariantStorage.ForwardFormat(MedalKey, trackIndex), 0);
            var medalLetter = RaceMedalUtility.GetMedalLetter(forwardMedal);
            var progress = RaceMedalUtility.FormatCompactProgress(stars, forwardMedal);
            var reverseBadge = TrackVariantStorage.HasReverseProgress(trackIndex) ? "  ↺" : string.Empty;

            if (!IsTrackUnlocked(trackIndex))
            {
                var need = CareerProgressionGate.GetUnlockRequirementForTrack(trackIndex);
                var cupTag = CareerCupCatalog.GetShortTag(CareerCupCatalog.GetCupForTrack(trackIndex));
                return $"[{cupTag}] L{trackIndex + 1}  {progress}  LOCKED  •  {need}";
            }

            var cup = CareerCupCatalog.GetShortTag(CareerCupCatalog.GetCupForTrack(trackIndex));
            return $"[{cup}] L{trackIndex + 1}  {trackName}{reverseBadge}  {progress}  {medalLetter}";
        }

        public static bool TryAwardDailyBonusStar(int trackIndex)
        {
            if (GetForwardStars(trackIndex) >= 3)
                return false;

            var current = GetForwardStars(trackIndex);
            PlayerPrefs.SetInt(TrackVariantStorage.ForwardFormat(StarsKey, trackIndex), Mathf.Min(3, current + 1));
            var medal = (RaceMedal)PlayerPrefs.GetInt(TrackVariantStorage.ForwardFormat(MedalKey, trackIndex), 0);
            if ((int)medal < (int)RaceMedal.Bronze)
                PlayerPrefs.SetInt(TrackVariantStorage.ForwardFormat(MedalKey, trackIndex), (int)RaceMedal.Bronze);
            PlayerPrefs.Save();
            CareerProgressionGate.IsEndlessUnlocked();
            return true;
        }

        static string FormatLap(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = Mathf.FloorToInt(seconds % 60f);
            var frac = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);
            return minutes > 0 ? $"{minutes}:{secs:00}.{frac:00}" : $"{secs}.{frac:00}s";
        }
    }
}
