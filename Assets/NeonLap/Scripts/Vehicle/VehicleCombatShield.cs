using NeonLap.Environment;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class VehicleCombatShield : MonoBehaviour
    {
        [SerializeField] int maxCharges = 1;

        int charges;
        float absorbCooldownEnd;

        public bool IsActive => charges > 0;
        public int Charges => charges;
        public int MaxCharges => maxCharges;

        public void ResetShield()
        {
            charges = maxCharges;
            absorbCooldownEnd = 0f;
        }

        public bool TryAbsorbHit()
        {
            if (charges <= 0 || Time.time < absorbCooldownEnd)
                return false;

            charges--;
            absorbCooldownEnd = Time.time + 0.15f;
            StadiumIncidentHub.Report("SHIELD BROKEN");
            return true;
        }
    }
}
