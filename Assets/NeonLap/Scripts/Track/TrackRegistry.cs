using UnityEngine;

namespace NeonLap.Track
{
    [CreateAssetMenu(fileName = "TrackRegistry", menuName = "NeonLap/Track Registry")]
    public class TrackRegistry : ScriptableObject
    {
        public TrackDefinition[] tracks;

        public int Count => tracks != null ? tracks.Length : 0;

        public TrackDefinition GetTrack(int index)
        {
            if (tracks == null || tracks.Length == 0)
                return null;

            index = Mathf.Clamp(index, 0, tracks.Length - 1);
            return tracks[index];
        }
    }
}
