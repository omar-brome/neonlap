namespace NeonLap.Vehicle
{
    public static class VehicleStatsFormatter
    {
        public static string FormatSummary(VehicleProfile profile)
        {
            if (profile == null)
                return "Stats unavailable";

            var driftGrip = 1f - profile.driftGripMultiplier;
            return
                $"Top Speed {profile.maxSpeed:0}  •  Accel {profile.acceleration:0}  •  Drift Grip {driftGrip * 100f:0}%  •  Grip {profile.grip:0}";
        }

        public static string FormatDetail(VehicleProfile profile)
        {
            if (profile == null)
                return string.Empty;

            var driftGrip = 1f - profile.driftGripMultiplier;
            return
                $"TOP SPEED {profile.maxSpeed:0}\n" +
                $"ACCELERATION {profile.acceleration:0}\n" +
                $"DRIFT GRIP {driftGrip * 100f:0}%\n" +
                $"HANDLING GRIP {profile.grip:0}\n" +
                $"DRIFT ANGLE {profile.driftSteerMultiplier:0.00}x";
        }
    }
}
