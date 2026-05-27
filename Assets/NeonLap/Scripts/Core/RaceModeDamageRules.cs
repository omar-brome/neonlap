using NeonLap.Vehicle;

namespace NeonLap.Core
{
    public static class RaceModeDamageRules
    {
        public static VehicleDamageMode GetDamageMode() => GetDamageProfile().DamageMode;

        public static RaceDamageProfile GetDamageProfile()
        {
            return GameRaceModeSettings.Current switch
            {
                RaceMode.Elimination => FullHealthProfile(),
                RaceMode.Chase => FullHealthProfile(),
                RaceMode.Demolition => new RaceDamageProfile(
                    damageMode: VehicleDamageMode.Full,
                    playerPartDetachMultiplier: 0.28f,
                    aiPartDetachMultiplier: 1.55f,
                    playerHealthDamageMultiplier: 0.55f,
                    aiHealthDamageMultiplier: 1.45f,
                    useHealthForPlayer: true,
                    useHealthForAi: true,
                    hardcoreInstantElimination: false,
                    demolitionWinCondition: true,
                    spawnRepairPads: true),

                RaceMode.Hardcore => new RaceDamageProfile(
                    damageMode: VehicleDamageMode.Full,
                    playerPartDetachMultiplier: 0.65f,
                    aiPartDetachMultiplier: 1.1f,
                    playerHealthDamageMultiplier: 1f,
                    aiHealthDamageMultiplier: 1f,
                    useHealthForPlayer: true,
                    useHealthForAi: true,
                    hardcoreInstantElimination: true,
                    demolitionWinCondition: false,
                    spawnRepairPads: true),

                _ => CosmeticProfile(),
            };
        }

        static RaceDamageProfile CosmeticProfile()
        {
            return new RaceDamageProfile(
                damageMode: VehicleDamageMode.Cosmetic,
                playerPartDetachMultiplier: 0.35f,
                aiPartDetachMultiplier: 1.35f,
                playerHealthDamageMultiplier: 1f,
                aiHealthDamageMultiplier: 1f,
                useHealthForPlayer: false,
                useHealthForAi: false,
                hardcoreInstantElimination: false,
                demolitionWinCondition: false,
                spawnRepairPads: false);
        }

        static RaceDamageProfile FullHealthProfile()
        {
            return new RaceDamageProfile(
                damageMode: VehicleDamageMode.Full,
                playerPartDetachMultiplier: 0.35f,
                aiPartDetachMultiplier: 1.35f,
                playerHealthDamageMultiplier: 1f,
                aiHealthDamageMultiplier: 1f,
                useHealthForPlayer: false,
                useHealthForAi: true,
                hardcoreInstantElimination: false,
                demolitionWinCondition: false,
                spawnRepairPads: false);
        }
    }
}
