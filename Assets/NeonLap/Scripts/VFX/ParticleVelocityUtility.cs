using UnityEngine;

namespace NeonLap.VFX
{
    public static class ParticleVelocityUtility
    {
        public static void ConfigureRandomAxes(
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime,
            float xMin,
            float xMax,
            float yMin,
            float yMax,
            float zMin,
            float zMax)
        {
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = CreateAxisCurve(xMin, xMax);
            velocityOverLifetime.y = CreateAxisCurve(yMin, yMax);
            velocityOverLifetime.z = CreateAxisCurve(zMin, zMax);
        }

        static ParticleSystem.MinMaxCurve CreateAxisCurve(float min, float max)
        {
            return new ParticleSystem.MinMaxCurve
            {
                mode = ParticleSystemCurveMode.TwoConstants,
                constantMin = min,
                constantMax = max
            };
        }
    }
}
