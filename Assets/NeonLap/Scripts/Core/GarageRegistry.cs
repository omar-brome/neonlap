using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Core
{
    [CreateAssetMenu(fileName = "GarageRegistry", menuName = "NeonLap/Garage Registry")]
    public class GarageRegistry : ScriptableObject
    {
        public HoverBuildDefinition[] builds;

        public int Count => builds != null ? builds.Length : 0;

        public HoverBuildDefinition GetBuild(int index)
        {
            if (builds == null || builds.Length == 0)
                return null;

            index = Mathf.Clamp(index, 0, builds.Length - 1);
            return builds[index];
        }
    }
}
