using UnityEngine;

namespace NeonLap.Core
{
    public enum DifficultyLevel
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
    }

    public struct DifficultyPreset
    {
        public float AiSpeedScale;
        public float RivalSpeedBase;
        public float RivalSpeedStep;
        public float RubberBandStrength;
        public float RubberBandCatchUpScale;
        public float RubberBandSlowdownScale;
        public float ProgressRubberBandStrength;
        public float ProgressAheadSlowdownScale;
        public float ProgressBehindCatchUpScale;
        public float AiCombatAggression;
        public float LookAheadMin;
        public float LookAheadMax;
        public float SteerResponseDivisor;
        public float CornerSpeedMin;
        public float CornerAccelMin;
        public bool AiCollectsNitroPickups;
        public bool AiSeeksNitroPickups;
        public bool AiUsesNitroZones;
    }

    public static class GameDifficultySettings
    {
        const string PrefKey = "NeonLap.DifficultyLevel";

        public static DifficultyLevel Current { get; private set; } = DifficultyLevel.Medium;

        public static DifficultyPreset Preset => GetPreset(Current);

        public static void Load()
        {
            Current = (DifficultyLevel)PlayerPrefs.GetInt(PrefKey, (int)DifficultyLevel.Medium);
        }

        public static void SetLevel(DifficultyLevel level)
        {
            Current = level;
            PlayerPrefs.SetInt(PrefKey, (int)level);
            PlayerPrefs.Save();
        }

        public static string GetDisplayName(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => "EASY",
                DifficultyLevel.Medium => "MEDIUM",
                DifficultyLevel.Hard => "HARD",
                _ => level.ToString().ToUpperInvariant(),
            };
        }

        public static DifficultyPreset GetPreset(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => new DifficultyPreset
                {
                    AiSpeedScale = 0.62f,
                    RivalSpeedBase = 0.66f,
                    RivalSpeedStep = 0.008f,
                    RubberBandStrength = 0.1f,
                    RubberBandCatchUpScale = 0.35f,
                    RubberBandSlowdownScale = 1.25f,
                    ProgressRubberBandStrength = 0.28f,
                    ProgressAheadSlowdownScale = 1.45f,
                    ProgressBehindCatchUpScale = 0.55f,
                    AiCombatAggression = 0.72f,
                    LookAheadMin = 6f,
                    LookAheadMax = 17f,
                    SteerResponseDivisor = 70f,
                    CornerSpeedMin = 0.2f,
                    CornerAccelMin = 0.16f,
                    AiCollectsNitroPickups = false,
                    AiSeeksNitroPickups = false,
                    AiUsesNitroZones = false,
                },
                DifficultyLevel.Hard => new DifficultyPreset
                {
                    AiSpeedScale = 0.96f,
                    RivalSpeedBase = 0.9f,
                    RivalSpeedStep = 0.014f,
                    RubberBandStrength = 0.22f,
                    RubberBandCatchUpScale = 1.35f,
                    RubberBandSlowdownScale = 0.25f,
                    ProgressRubberBandStrength = 0.08f,
                    ProgressAheadSlowdownScale = 0.2f,
                    ProgressBehindCatchUpScale = 1.25f,
                    AiCombatAggression = 1.35f,
                    LookAheadMin = 11f,
                    LookAheadMax = 34f,
                    SteerResponseDivisor = 40f,
                    CornerSpeedMin = 0.44f,
                    CornerAccelMin = 0.42f,
                    AiCollectsNitroPickups = true,
                    AiSeeksNitroPickups = true,
                    AiUsesNitroZones = true,
                },
                _ => new DifficultyPreset
                {
                    AiSpeedScale = 0.8f,
                    RivalSpeedBase = 0.76f,
                    RivalSpeedStep = 0.012f,
                    RubberBandStrength = 0.18f,
                    RubberBandCatchUpScale = 1f,
                    RubberBandSlowdownScale = 0.35f,
                    ProgressRubberBandStrength = 0.16f,
                    ProgressAheadSlowdownScale = 0.75f,
                    ProgressBehindCatchUpScale = 0.9f,
                    AiCombatAggression = 1f,
                    LookAheadMin = 8f,
                    LookAheadMax = 24f,
                    SteerResponseDivisor = 55f,
                    CornerSpeedMin = 0.28f,
                    CornerAccelMin = 0.25f,
                    AiCollectsNitroPickups = true,
                    AiSeeksNitroPickups = false,
                    AiUsesNitroZones = true,
                },
            };
        }
    }
}
