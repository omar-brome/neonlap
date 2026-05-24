using UnityEngine;

namespace NeonLap.Race
{
    public class RacerProgress : MonoBehaviour
    {
        public bool IsPlayer { get; private set; }
        public int CurrentLap { get; private set; } = 1;
        public int NextCheckpointIndex { get; set; }
        public bool IsFinished { get; private set; }
        public float FinishTime { get; private set; }

        public void Configure(bool isPlayer)
        {
            IsPlayer = isPlayer;
        }

        public void ResetProgress(int firstCheckpointIndex)
        {
            CurrentLap = 1;
            NextCheckpointIndex = firstCheckpointIndex;
            IsFinished = false;
            FinishTime = 0f;
        }

        public void MarkFinished(float finishTime)
        {
            IsFinished = true;
            FinishTime = finishTime;
        }

        public void AdvanceLap(int firstCheckpointIndex)
        {
            CurrentLap++;
            NextCheckpointIndex = firstCheckpointIndex;
        }
    }
}
