using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    public class NitroZone : MonoBehaviour
    {
        [SerializeField] float aiRetriggerCooldown = 6f;

        float nextAiTriggerTime;

        void OnTriggerEnter(Collider other)
        {
            if (GameDifficultySettings.Current != DifficultyLevel.Hard)
                return;

            var raceManager = FindAnyObjectByType<RaceManager>();
            if (raceManager != null && raceManager.State != RaceState.Racing)
                return;

            var aiCombat = other.GetComponentInParent<AICombatController>();
            if (aiCombat == null)
                return;

            if (Time.time < nextAiTriggerTime)
                return;

            nextAiTriggerTime = Time.time + aiRetriggerCooldown;
            aiCombat.OnNitroZoneEntered();
        }
    }
}
