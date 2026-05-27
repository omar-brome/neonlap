namespace NeonLap.Vehicle
{
    /// <summary>Performance tier — Rookie (C), Pro (B), Elite (A).</summary>
    public enum VehicleClass
    {
        Rookie = 0,
        Pro = 1,
        Elite = 2,
    }

    public static class VehicleClassLabels
    {
        public static string GetShortLabel(VehicleClass vehicleClass) =>
            vehicleClass switch
            {
                VehicleClass.Pro => "B",
                VehicleClass.Elite => "A",
                _ => "C",
            };

        public static string GetDisplayName(VehicleClass vehicleClass) =>
            vehicleClass switch
            {
                VehicleClass.Pro => "Pro",
                VehicleClass.Elite => "Elite",
                _ => "Rookie",
            };

        public static string GetCupName(VehicleClass vehicleClass) =>
            vehicleClass switch
            {
                VehicleClass.Pro => "Pro Cup",
                VehicleClass.Elite => "Elite Cup",
                _ => "Rookie Cup",
            };
    }
}
