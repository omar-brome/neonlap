using UnityEngine;

namespace NeonLap.Vehicle
{
    public struct RivalIdentityProfile
    {
        public string DisplayName;
        public string ShortName;
        public Color BodyColor;
        public Color AccentColor;
        public Color HudColor;
    }

    public class RivalIdentity : MonoBehaviour
    {
        public int RivalIndex { get; private set; }
        public string DisplayName { get; private set; }
        public string ShortName { get; private set; }
        public Color HudColor { get; private set; }

        public void Configure(int rivalIndex, RivalIdentityProfile profile)
        {
            RivalIndex = rivalIndex;
            DisplayName = profile.DisplayName;
            ShortName = profile.ShortName;
            HudColor = profile.HudColor;
        }
    }
}
