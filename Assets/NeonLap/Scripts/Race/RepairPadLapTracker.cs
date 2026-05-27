using UnityEngine;

namespace NeonLap.Race
{
    public class RepairPadLapTracker : MonoBehaviour
    {
        int repairedOnLap = -1;

        public bool CanUseRepairPad(int currentLap) => repairedOnLap != currentLap;

        public void MarkRepaired(int currentLap)
        {
            repairedOnLap = currentLap;
        }

        public void ResetForRace()
        {
            repairedOnLap = -1;
        }
    }
}
