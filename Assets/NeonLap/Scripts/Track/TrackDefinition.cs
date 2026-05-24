using UnityEngine;

namespace NeonLap.Track
{
    [CreateAssetMenu(fileName = "TrackDefinition", menuName = "NeonLap/Track Definition")]
    public class TrackDefinition : ScriptableObject
    {
        public string trackName = "Oval Circuit";
        public string sceneName = "SampleScene";
        public int lapCount = 3;
        public int checkpointCount = 10;
        public float straightLength = 60f;
        public float turnRadius = 25f;
        public float trackWidth = 14f;
    }
}
