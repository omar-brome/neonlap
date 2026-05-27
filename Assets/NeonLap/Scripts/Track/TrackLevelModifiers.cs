using UnityEngine;

namespace NeonLap.Track
{
    [System.Serializable]
    public struct TrackLevelModifierEntry
    {
        public string levelName;
        [Range(0.15f, 2.5f)] public float bananaDensityMultiplier;
        [Range(0.15f, 2.5f)] public float hazardDensityMultiplier;
        [Range(0.15f, 2.5f)] public float pickupDensityMultiplier;
        [Range(0.15f, 2.5f)] public float movingHazardDensityMultiplier;
        public bool shortcutsRequiredForMedal;
        [TextArea(1, 2)] public string modifierNote;
    }

    [CreateAssetMenu(fileName = "TrackLevelModifiers", menuName = "NeonLap/Track Level Modifiers")]
    public class TrackLevelModifiers : ScriptableObject
    {
        public TrackLevelModifierEntry[] levels = new TrackLevelModifierEntry[6];

        public TrackLevelModifierEntry GetEntry(int levelIndex)
        {
            if (levels == null || levels.Length == 0)
                return TrackLevelConfig.GetDefaultEntry(levelIndex);

            var index = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
            return levels[index];
        }

        void OnValidate()
        {
            if (levels == null || levels.Length == 6)
                return;

            System.Array.Resize(ref levels, 6);
        }
    }
}
