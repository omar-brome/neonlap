using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Race
{
    public class RaceShortcutTracker : MonoBehaviour
    {
        public static RaceShortcutTracker Instance { get; private set; }

        int levelIndex;
        bool usedShortcutThisLap;
        bool usedShortcutThisRace;
        int pendingMergeCheckpoint = -1;
        bool mergeCheckpointCleared;
        int lastShortcutScoreBonus;
        bool lastShortcutBeatGhost;

        public bool UsedShortcut => usedShortcutThisRace;
        public bool UsedShortcutThisLap => usedShortcutThisLap;
        public bool RequiresShortcutForMedal => TrackLevelConfig.RequiresShortcutForMedal(levelIndex);
        public bool ShortcutRequirementMet => !RequiresShortcutForMedal || usedShortcutThisRace;
        public bool ShortcutLapValid => !usedShortcutThisLap || mergeCheckpointCleared;
        public int LastShortcutScoreBonus => lastShortcutScoreBonus;
        public bool LastShortcutBeatGhost => lastShortcutBeatGhost;

        public void Configure(int trackLevelIndex)
        {
            levelIndex = trackLevelIndex;
            ResetForRace();
        }

        public void BeginShortcut(TrackShortcutDefinition definition)
        {
            if (definition == null)
                return;

            usedShortcutThisLap = true;
            usedShortcutThisRace = true;
            pendingMergeCheckpoint = definition.MergeCheckpointIndex;
            mergeCheckpointCleared = false;
            lastShortcutScoreBonus = definition.ScoreBonus;
            lastShortcutBeatGhost = false;
        }

        public bool TryAuthorizeMergeCheckpoint(int checkpointIndex, RacerProgress racer)
        {
            if (!usedShortcutThisLap || pendingMergeCheckpoint < 0)
                return false;

            if (checkpointIndex != pendingMergeCheckpoint)
                return false;

            if (racer != null && checkpointIndex > racer.NextCheckpointIndex)
                racer.NextCheckpointIndex = checkpointIndex;

            mergeCheckpointCleared = true;
            pendingMergeCheckpoint = -1;
            return true;
        }

        public bool ValidateLapAfterShortcut()
        {
            if (!usedShortcutThisLap)
                return true;

            return mergeCheckpointCleared;
        }

        public void SetShortcutBeatGhost(bool beatGhost)
        {
            lastShortcutBeatGhost = beatGhost;
        }

        public void ResetLapShortcutState()
        {
            usedShortcutThisLap = false;
            pendingMergeCheckpoint = -1;
            mergeCheckpointCleared = false;
            lastShortcutScoreBonus = 0;
            lastShortcutBeatGhost = false;
        }

        public void ResetForRace()
        {
            usedShortcutThisRace = false;
            ResetLapShortcutState();
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
