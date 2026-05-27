namespace NeonLap.Vehicle
{
    public enum AIPersonalityKind
    {
        Balanced = 0,
        Aggressive = 1,
        Defensive = 2,
        Opportunist = 3,
    }

    public struct AIPersonalityProfile
    {
        public AIPersonalityKind Kind;
        public float LookAheadMultiplier;
        public float CornerSpeedMultiplier;
        public float CornerAccelMultiplier;
        public float SteerAggression;
        public float CenteringWeight;
        public float PassingSteerBias;
        public float NitroChanceMultiplier;
        public float BananaChanceMultiplier;
    }

    public static class AIPersonalityCatalog
    {
        public static AIPersonalityProfile GetForRivalIndex(int rivalIndex)
        {
            return Get((AIPersonalityKind)(rivalIndex % 4));
        }

        public static AIPersonalityProfile Get(AIPersonalityKind kind)
        {
            return kind switch
            {
                AIPersonalityKind.Aggressive => new AIPersonalityProfile
                {
                    Kind = kind,
                    LookAheadMultiplier = 1.2f,
                    CornerSpeedMultiplier = 1.1f,
                    CornerAccelMultiplier = 1.08f,
                    SteerAggression = 1.18f,
                    CenteringWeight = 0.62f,
                    PassingSteerBias = 0.14f,
                    NitroChanceMultiplier = 1.05f,
                    BananaChanceMultiplier = 0.85f,
                },
                AIPersonalityKind.Defensive => new AIPersonalityProfile
                {
                    Kind = kind,
                    LookAheadMultiplier = 0.86f,
                    CornerSpeedMultiplier = 0.9f,
                    CornerAccelMultiplier = 0.92f,
                    SteerAggression = 0.88f,
                    CenteringWeight = 1.42f,
                    PassingSteerBias = -0.06f,
                    NitroChanceMultiplier = 0.75f,
                    BananaChanceMultiplier = 1.15f,
                },
                AIPersonalityKind.Opportunist => new AIPersonalityProfile
                {
                    Kind = kind,
                    LookAheadMultiplier = 1.06f,
                    CornerSpeedMultiplier = 1.02f,
                    CornerAccelMultiplier = 1.04f,
                    SteerAggression = 1.05f,
                    CenteringWeight = 0.9f,
                    PassingSteerBias = 0.05f,
                    NitroChanceMultiplier = 1.45f,
                    BananaChanceMultiplier = 1.35f,
                },
                _ => new AIPersonalityProfile
                {
                    Kind = AIPersonalityKind.Balanced,
                    LookAheadMultiplier = 1f,
                    CornerSpeedMultiplier = 1f,
                    CornerAccelMultiplier = 1f,
                    SteerAggression = 1f,
                    CenteringWeight = 1f,
                    PassingSteerBias = 0f,
                    NitroChanceMultiplier = 1f,
                    BananaChanceMultiplier = 1f,
                },
            };
        }
    }
}
