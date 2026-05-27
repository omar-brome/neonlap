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

        public float LastLapTime { get; private set; }
        public float PersonalBestLapTime { get; private set; } = float.MaxValue;
        public bool HasPersonalBestLap => PersonalBestLapTime < float.MaxValue;

        float lapSegmentStartTime;

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
            LastLapTime = 0f;
            PersonalBestLapTime = float.MaxValue;
            lapSegmentStartTime = 0f;
            GetComponent<RepairPadLapTracker>()?.ResetForRace();
        }

        public void BeginLapSegment(float raceTime)
        {
            lapSegmentStartTime = raceTime;
        }

        public bool TryCompleteLapSegment(float raceTime)
        {
            if (lapSegmentStartTime <= 0f)
                BeginLapSegment(raceTime);

            var lapTime = Mathf.Max(0f, raceTime - lapSegmentStartTime);
            LastLapTime = lapTime;
            lapSegmentStartTime = raceTime;

            if (lapTime <= 0.05f)
                return false;

            if (lapTime < PersonalBestLapTime)
            {
                PersonalBestLapTime = lapTime;
                return true;
            }

            return false;
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
