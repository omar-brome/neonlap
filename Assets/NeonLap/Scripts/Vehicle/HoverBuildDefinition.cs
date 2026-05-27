using UnityEngine;

namespace NeonLap.Vehicle
{
    [CreateAssetMenu(fileName = "HoverBuild", menuName = "NeonLap/Hover Build")]
    public class HoverBuildDefinition : ScriptableObject
    {
        public string buildId = "neon_pulse";
        public string displayName = "Neon Pulse";
        [TextArea(2, 3)] public string tagline = "Balanced all-rounder.";
        public VehicleProfile profile;
        public Color bodyColor = new(0.1f, 0.35f, 0.45f);
        public Color accentColor = new(0f, 3.5f, 4f);

        [Header("Class")]
        public VehicleClass vehicleClass = VehicleClass.Rookie;

        [Header("Unlock")]
        public bool unlockedByDefault = true;
        public int requiredCareerStars;
        public int requiredScoreAttackBest;
        [Tooltip("Alternative unlock: spend career credits in the garage.")]
        public int creditCost;

        public string GetClassLabel() => VehicleClassLabels.GetDisplayName(vehicleClass);

        public string GetUnlockHint()
        {
            if (unlockedByDefault)
                return $"{GetClassLabel()}  •  Unlocked";

            var parts = 0;
            var hint = $"{GetClassLabel()}  •  Unlock:";
            if (requiredCareerStars > 0)
            {
                hint += $" {requiredCareerStars} career ★";
                parts++;
            }

            if (requiredScoreAttackBest > 0)
            {
                hint += parts > 0 ? $" OR {requiredScoreAttackBest:N0} score PB" : $" {requiredScoreAttackBest:N0} score PB";
                parts++;
            }

            if (creditCost > 0)
            {
                hint += parts > 0 ? $" OR {creditCost:N0} credits" : $" {creditCost:N0} credits";
                parts++;
            }

            return parts > 0 ? hint : "Locked";
        }
    }
}
