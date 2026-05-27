using UnityEngine;

namespace NeonLap.Core.Content
{
    /// <summary>
    /// Marker on baked hover-car prefabs. Used by the editor bake menu and spawn pipeline.
    /// </summary>
    public class NeonLapCarPrefabRoot : MonoBehaviour
    {
        [SerializeField] bool isPlayerTemplate;

        public bool IsPlayerTemplate => isPlayerTemplate;

        public void SetTemplate(bool player) => isPlayerTemplate = player;
    }
}
