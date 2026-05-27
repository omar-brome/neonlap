using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public enum GhostSaveKind
    {
        Lap,
        Race,
    }

    /// <summary>
    /// Converts <see cref="RaceReplaySystem"/> captures into competitive <see cref="GhostRacer"/> PB data.
    /// </summary>
    public static class GhostReplayBridge
    {
        public static bool CanSaveToPbStore =>
            GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel;

        public static GhostRecordingData BuildRecording(
            IReadOnlyList<ReplayFrameSnapshot> frames,
            float anchorRaceTime = 0f,
            int maxFrames = 720)
        {
            var data = GhostRecordingData.FromFrames(frames, maxFrames);
            if (data != null)
                data.AnchorRaceTime = Mathf.Max(0f, anchorRaceTime);

            return data;
        }

        public static GhostRecordingData BuildRecording(
            RaceReplaySystem replay,
            float startTime,
            float endTime,
            float anchorRaceTime = 0f)
        {
            if (replay == null)
                return null;

            return BuildRecording(replay.ExportPlayerFrames(startTime, endTime), anchorRaceTime);
        }

        public static bool TrySavePbGhost(
            int trackIndex,
            GhostRecordingData recording,
            GhostSaveKind kind,
            float officialTime,
            bool onlyIfFaster = true)
        {
            if (!CanSaveToPbStore || recording == null || !recording.IsValid || officialTime <= 0.05f)
                return false;

            if (onlyIfFaster)
            {
                return kind == GhostSaveKind.Race
                    ? TimeTrialRecordStore.TrySaveBestRace(trackIndex, officialTime, recording)
                    : TimeTrialRecordStore.TrySaveBestLap(trackIndex, officialTime, recording);
            }

            return kind == GhostSaveKind.Race
                ? TimeTrialRecordStore.ForceSaveBestRace(trackIndex, officialTime, recording)
                : TimeTrialRecordStore.ForceSaveBestLap(trackIndex, officialTime, recording);
        }

        /// <summary>User-selected replay clip — force-written as lap PB ghost (clip duration becomes lap time).</summary>
        public static bool SaveHighlightAsLapGhost(int trackIndex, GhostRecordingData recording)
        {
            if (!CanSaveToPbStore || recording == null || !recording.IsValid)
                return false;

            recording.AnchorRaceTime = 0f;
            return TimeTrialRecordStore.ForceSaveBestLap(trackIndex, recording.Duration, recording);
        }

        public static bool SaveFinalLapRacePb(
            int trackIndex,
            float raceTime,
            float finalLapStartRaceTime,
            GhostRecordingData finalLapRecording)
        {
            if (finalLapRecording == null || !finalLapRecording.IsValid)
                return false;

            finalLapRecording.AnchorRaceTime = Mathf.Max(0f, finalLapStartRaceTime);
            return TimeTrialRecordStore.TrySaveBestRace(trackIndex, raceTime, finalLapRecording);
        }
    }
}
