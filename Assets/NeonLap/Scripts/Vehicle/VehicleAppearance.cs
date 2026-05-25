using UnityEngine;

namespace NeonLap.Vehicle
{
    public class VehicleAppearance : MonoBehaviour
    {
        HoverCarVisualBuilder.BuildArgs buildArgs;
        public bool HasBuildArgs => buildArgs.BodyTemplate != null;

        public void Configure(HoverCarVisualBuilder.BuildArgs args)
        {
            buildArgs = args;
        }

        public HoverCarVisualBuilder.BuildArgs GetBuildArgs() => buildArgs;
    }
}
