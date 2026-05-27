namespace NeonLap.Input
{
    public interface IVehicleInputProvider
    {
        float Accelerate { get; }
        float Brake { get; }
        float Steer { get; }
        bool DriftHeld { get; }
        bool NitroPressed { get; }
        bool ResetPressed { get; }
        bool PausePressed { get; }
    }
}
