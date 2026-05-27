using System;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public readonly struct DailyChallengeResult
    {
        public bool Completed { get; init; }
        public int BonusCredits { get; init; }
        public int BonusStarsAwarded { get; init; }
        public string Description { get; init; }
    }

    public readonly struct DailyChallengeDefinition
    {
        public readonly int TargetLevelIndex;
        public readonly float MaxRaceTimeSeconds;
        public readonly bool RequirePolice;
        public readonly bool ForceRain;
        public readonly int LapCount;
        public readonly int BonusCredits;

        public DailyChallengeDefinition(
            int levelIndex,
            float maxTime,
            bool requirePolice,
            bool forceRain,
            int lapCount,
            int bonusCredits)
        {
            TargetLevelIndex = levelIndex;
            MaxRaceTimeSeconds = maxTime;
            RequirePolice = requirePolice;
            ForceRain = forceRain;
            LapCount = lapCount;
            BonusCredits = bonusCredits;
        }
    }

    public static class DailyChallengeService
    {
        const string CompletedDayKey = "NeonLap.DailyChallenge.CompletedDay";
        const string ChallengeSeedKey = "NeonLap.DailyChallenge.SeedDay";

        public static DailyChallengeDefinition GetTodayChallenge() => BuildChallenge(GetTodayDayId());

        public static string GetTodayDescription()
        {
            var challenge = GetTodayChallenge();
            var level = challenge.TargetLevelIndex + 1;
            var time = FormatTime(challenge.MaxRaceTimeSeconds);
            var mods = BuildModifierSummary(challenge);
            return $"Daily: Level {level} under {time}{mods}";
        }

        public static string GetMenuBannerLine()
        {
            var challenge = GetTodayChallenge();
            var status = IsCompletedToday() ? "CLAIMED" : "ACTIVE";
            return $"{GetTodayDescription()}  •  +{challenge.BonusCredits} CR  •  BONUS ★  •  {status}";
        }

        public static bool IsTodayTargetTrack(int trackIndex) =>
            GetTodayChallenge().TargetLevelIndex == trackIndex;

        public static void ApplyTodayModifiersForRace(int trackIndex)
        {
            if (!IsTodayTargetTrack(trackIndex) || IsCompletedToday())
                return;

            var challenge = GetTodayChallenge();
            if (challenge.ForceRain)
                GameTrackOptions.SetWeatherChoice(TrackWeatherChoice.ForceRain);
            if (challenge.RequirePolice)
                GamePoliceSettings.SetEnabled(true);
            if (challenge.LapCount > 0)
                GameLapSettings.SetLaps(challenge.LapCount);
        }

        public static DailyChallengeResult EvaluateRace(
            int trackIndex,
            float raceTime,
            bool policeWasEnabled)
        {
            var challenge = GetTodayChallenge();
            var description = GetTodayDescription();

            if (trackIndex != challenge.TargetLevelIndex)
                return new DailyChallengeResult { Description = description };

            if (raceTime <= 0.05f || raceTime > challenge.MaxRaceTimeSeconds)
                return new DailyChallengeResult { Description = description };

            if (challenge.RequirePolice && !policeWasEnabled)
                return new DailyChallengeResult { Description = description + "  •  POLICE REQUIRED" };

            if (IsCompletedToday())
            {
                return new DailyChallengeResult
                {
                    Completed = true,
                    BonusCredits = 0,
                    Description = description + "  •  CLAIMED"
                };
            }

            MarkCompletedToday();
            var bonusStar = CareerScoreStore.TryAwardDailyBonusStar(trackIndex) ? 1 : 0;
            return new DailyChallengeResult
            {
                Completed = true,
                BonusCredits = challenge.BonusCredits,
                BonusStarsAwarded = bonusStar,
                Description = description + (bonusStar > 0 ? "  •  +1 ★" : string.Empty)
            };
        }

        public static bool IsCompletedToday() =>
            PlayerPrefs.GetInt(CompletedDayKey, -1) == GetTodayDayId();

        static DailyChallengeDefinition BuildChallenge(int dayId)
        {
            var storedDay = PlayerPrefs.GetInt(ChallengeSeedKey, int.MinValue);
            if (storedDay == dayId)
            {
                var level = PlayerPrefs.GetInt("NeonLap.Daily.Level", 0);
                var time = PlayerPrefs.GetFloat("NeonLap.Daily.Time", 150f);
                var police = PlayerPrefs.GetInt("NeonLap.Daily.Police", 0) == 1;
                var rain = PlayerPrefs.GetInt("NeonLap.Daily.Rain", 0) == 1;
                var laps = PlayerPrefs.GetInt("NeonLap.Daily.Laps", 3);
                var credits = PlayerPrefs.GetInt("NeonLap.Daily.Credits", 600);
                return new DailyChallengeDefinition(level, time, police, rain, laps, credits);
            }

            var rng = new System.Random(dayId);
            var templates = new[]
            {
                new DailyChallengeDefinition(2, 150f, true, false, 3, 650),
                new DailyChallengeDefinition(4, 165f, true, true, 3, 800),
                new DailyChallengeDefinition(0, 100f, false, false, 1, 450),
                new DailyChallengeDefinition(5, 185f, true, true, 5, 950),
                new DailyChallengeDefinition(1, 125f, false, true, 2, 550),
                new DailyChallengeDefinition(3, 160f, true, false, 5, 750),
            };

            var pick = templates[Math.Abs(rng.Next()) % templates.Length];
            PlayerPrefs.SetInt(ChallengeSeedKey, dayId);
            PlayerPrefs.SetInt("NeonLap.Daily.Level", pick.TargetLevelIndex);
            PlayerPrefs.SetFloat("NeonLap.Daily.Time", pick.MaxRaceTimeSeconds);
            PlayerPrefs.SetInt("NeonLap.Daily.Police", pick.RequirePolice ? 1 : 0);
            PlayerPrefs.SetInt("NeonLap.Daily.Rain", pick.ForceRain ? 1 : 0);
            PlayerPrefs.SetInt("NeonLap.Daily.Laps", pick.LapCount);
            PlayerPrefs.SetInt("NeonLap.Daily.Credits", pick.BonusCredits);
            PlayerPrefs.Save();
            return pick;
        }

        static int GetTodayDayId() => DateTime.UtcNow.Year * 1000 + DateTime.UtcNow.DayOfYear;

        static void MarkCompletedToday()
        {
            PlayerPrefs.SetInt(CompletedDayKey, GetTodayDayId());
            PlayerPrefs.Save();
        }

        static string BuildModifierSummary(DailyChallengeDefinition challenge)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (challenge.LapCount > 0)
                parts.Add($"{challenge.LapCount} laps");
            if (challenge.RequirePolice)
                parts.Add("police");
            if (challenge.ForceRain)
                parts.Add("rain");
            return parts.Count > 0 ? $" ({string.Join(", ", parts)})" : string.Empty;
        }

        static string FormatTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = Mathf.FloorToInt(seconds % 60f);
            return minutes > 0 ? $"{minutes}:{secs:00}" : $"{secs:00}s";
        }
    }
}
