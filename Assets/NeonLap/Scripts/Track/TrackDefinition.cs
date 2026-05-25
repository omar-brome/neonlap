using UnityEngine;

namespace NeonLap.Track
{
    public enum TrackLayout
    {
        Oval,
        TriOvalSpeedway,
        TechnicalRing,
    }

    [CreateAssetMenu(fileName = "TrackDefinition", menuName = "NeonLap/Track Definition")]
    public class TrackDefinition : ScriptableObject
    {
        public string trackName = "Oval Circuit";
        public string sceneName = "SampleScene";
        public TrackLayout layout = TrackLayout.Oval;
        public int lapCount = 1;
        public int checkpointCount = 10;
        public float straightLength = 60f;
        public float turnRadius = 25f;
        public float trackWidth = 26f;
    }
}
