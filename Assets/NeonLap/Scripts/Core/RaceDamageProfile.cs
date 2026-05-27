using NeonLap.Vehicle;

namespace NeonLap.Core
{
    public readonly struct RaceDamageProfile
    {
        public readonly VehicleDamageMode DamageMode;
        public readonly float PlayerPartDetachMultiplier;
        public readonly float AiPartDetachMultiplier;
        public readonly float PlayerHealthDamageMultiplier;
        public readonly float AiHealthDamageMultiplier;
        public readonly bool UseHealthForPlayer;
        public readonly bool UseHealthForAi;
        public readonly bool HardcoreInstantElimination;
        public readonly bool DemolitionWinCondition;
        public readonly bool SpawnRepairPads;

        public RaceDamageProfile(
            VehicleDamageMode damageMode,
            float playerPartDetachMultiplier,
            float aiPartDetachMultiplier,
            float playerHealthDamageMultiplier,
            float aiHealthDamageMultiplier,
            bool useHealthForPlayer,
            bool useHealthForAi,
            bool hardcoreInstantElimination,
            bool demolitionWinCondition,
            bool spawnRepairPads)
        {
            DamageMode = damageMode;
            PlayerPartDetachMultiplier = playerPartDetachMultiplier;
            AiPartDetachMultiplier = aiPartDetachMultiplier;
            PlayerHealthDamageMultiplier = playerHealthDamageMultiplier;
            AiHealthDamageMultiplier = aiHealthDamageMultiplier;
            UseHealthForPlayer = useHealthForPlayer;
            UseHealthForAi = useHealthForAi;
            HardcoreInstantElimination = hardcoreInstantElimination;
            DemolitionWinCondition = demolitionWinCondition;
            SpawnRepairPads = spawnRepairPads;
        }

        public bool UsesHealth(bool isPlayer) => isPlayer ? UseHealthForPlayer : UseHealthForAi;

        public float GetHealthDamageMultiplier(bool isPlayer) =>
            isPlayer ? PlayerHealthDamageMultiplier : AiHealthDamageMultiplier;
    }
}
