using NeonLap.Race;
using NeonLap.Services.Race;

namespace NeonLap.UI
{
    public struct RaceFinishSummary
    {
        public bool HasData;
        public string Title;
        public UnityEngine.Color TitleColor;
        public int Placement;
        public int PlacementStars;
        public int CareerStars;
        public RaceMedal Medal;
        public int XpEarned;
        public int TotalXp;
        public int CreditsEarned;
        public float BestLapTime;
        public float RaceTime;
        public int Score;
        public bool NewHighScore;
        public string Subtitle;
        public string Breakdown;
    }

    public static class RaceFinishSummaryBuilder
    {
        public static RaceFinishSummary FromCareer(
            int placement,
            int score,
            CareerRaceResult result,
            float raceTime,
            float bestLapTime)
        {
            var meta = RaceMetagameBridge.Latest;
            var placementStars = RaceFinishRewards.GetPlacementStars(placement);
            var xp = meta.XpEarned > 0 ? meta.XpEarned : RaceFinishRewards.GetXpEarned(score, placement);
            var totalXp = meta.TotalXpAfter > 0 ? meta.TotalXpAfter : CareerXpStore.TotalXp;

            return new RaceFinishSummary
            {
                HasData = true,
                Title = placement == 1 ? "YOU WON!" : "RACE FINISHED",
                TitleColor = result.Medal == RaceMedal.Gold
                    ? new UnityEngine.Color(1f, 0.92f, 0.35f)
                    : new UnityEngine.Color(0.4f, 1f, 1f),
                Placement = placement,
                PlacementStars = placementStars,
                CareerStars = result.Stars,
                Medal = result.Medal,
                XpEarned = xp,
                TotalXp = totalXp,
                CreditsEarned = meta.CreditsEarned,
                BestLapTime = bestLapTime,
                RaceTime = raceTime,
                Score = score,
                NewHighScore = result.NewHighScore,
                Subtitle = BuildCareerSubtitle(placement, score, result, meta),
                Breakdown = string.Empty,
            };
        }

        public static RaceFinishSummary FromMode(string title, UnityEngine.Color titleColor, int placement, string subtitle)
        {
            return new RaceFinishSummary
            {
                HasData = true,
                Title = title,
                TitleColor = titleColor,
                Placement = placement,
                PlacementStars = RaceFinishRewards.GetPlacementStars(placement),
                Subtitle = subtitle,
            };
        }

        static string BuildCareerSubtitle(
            int placement,
            int score,
            CareerRaceResult result,
            RaceMetagameResult meta)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (result.NewHighScore)
                parts.Add("NEW HIGH SCORE");

            if (result.ImprovedMedal && result.Medal != RaceMedal.None)
                parts.Add($"NEW {RaceMedalUtility.GetMedalLabel(result.Medal)} MEDAL");
            else if (result.Medal != RaceMedal.None)
                parts.Add($"{RaceMedalUtility.GetMedalLabel(result.Medal)} MEDAL");

            if (result.Stars > 0)
                parts.Add($"Career {RaceMedalUtility.FormatStars(result.Stars)}");

            if (meta.DailyCompleted && meta.DailyBonusStars > 0)
                parts.Add($"Daily +{meta.DailyBonusStars} ★");

            parts.Add($"Score {score:N0}");

            return string.Join("  •  ", parts);
        }
    }
}
