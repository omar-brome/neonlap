using UnityEngine;

namespace NeonLap.Race
{
    public class RacerProgress : MonoBehaviour
    {
        public bool IsPlayer { get; private set; }
        public int CurrentLap { get; private set; } = 1;
        public int NextCheckpointIndex { get; set; }
        public bool IsFinished { get; private set; }
        public bool IsEliminated { get; private set; }
        public float FinishTime { get; private set; }
        public float EliminationTime { get; private set; }
        public bool CanTriggerFinish { get; set; } = true;

        public void Configure(bool isPlayer)
        {
            IsPlayer = isPlayer;
        }

        public void ResetProgress(int firstCheckpointIndex)
        {
            CurrentLap = 1;
            NextCheckpointIndex = firstCheckpointIndex;
            IsFinished = false;
            IsEliminated = false;
            FinishTime = 0f;
            EliminationTime = 0f;
            CanTriggerFinish = false;
        }

        public void MarkFinished(float finishTime)
        {
            IsFinished = true;
            FinishTime = finishTime;
        }

        public void MarkEliminated(float eliminationTime)
        {
            IsEliminated = true;
            EliminationTime = eliminationTime;
        }

        public void AdvanceLap(int firstCheckpointIndex)
        {
            CurrentLap++;
            NextCheckpointIndex = firstCheckpointIndex;
        }
    }
}
