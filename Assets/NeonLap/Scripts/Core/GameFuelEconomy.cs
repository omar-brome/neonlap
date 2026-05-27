using UnityEngine;

namespace NeonLap.Core
{
    public static class GameFuelEconomy
    {
        public const float SecondsPerLap = 52f;
        public const float NitroFuelRefillFraction = 0.34f;
        public const float FuelPadRefillFraction = 0.45f;
        public const float LowFuelPadFullThreshold = 0.18f;

        public static float GetTankDuration(int laps)
        {
            return SecondsPerLap * Mathf.Max(laps, 1);
        }
    }
}
