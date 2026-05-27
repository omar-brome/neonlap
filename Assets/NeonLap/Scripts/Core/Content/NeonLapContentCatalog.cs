using UnityEngine;

namespace NeonLap.Core.Content
{
    /// <summary>
    /// Optional prefab references for artist-driven content. When prefabs are assigned and
    /// <see cref="UsePrefabsWhenAssigned"/> is true, <see cref="NeonLapSceneSetup"/> instantiates
    /// them instead of building cars at runtime. Track geometry still comes from <see cref="Track.OvalTrackBuilder"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NeonLapContentCatalog", menuName = "NeonLap/Content Catalog")]
    public class NeonLapContentCatalog : ScriptableObject
    {
        [Header("Vehicles (optional)")]
        [Tooltip("Player hover car with all gameplay components wired.")]
        public GameObject PlayerCarPrefab;

        [Tooltip("AI rival template; colors are applied per rival at spawn.")]
        public GameObject AiRivalCarPrefab;

        [Header("Track art (optional)")]
        [Tooltip("Decorative environment chunk parented under the race root — does not replace procedural colliders.")]
        public GameObject TrackEnvironmentPrefab;

        [Header("Behaviour")]
        public bool UsePrefabsWhenAssigned = true;

        public bool HasPlayerCarPrefab => PlayerCarPrefab != null;
        public bool HasAiRivalCarPrefab => AiRivalCarPrefab != null;

        public static NeonLapContentCatalog LoadDefault()
        {
            return Resources.Load<NeonLapContentCatalog>("NeonLap/NeonLapContentCatalog");
        }
    }
}
