using UnityEngine;

namespace NeonLap.Race
{
    [CreateAssetMenu(fileName = "DevGhost", menuName = "NeonLap/Dev Ghost Recording")]
    public class DevGhostAsset : ScriptableObject
    {
        public int trackIndex;
        public float referenceLapTime = 60f;
        public GhostRecordingData recording;

        public bool IsValid => recording != null && recording.IsValid;
    }
}
