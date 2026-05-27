using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public static class TimeTrialRecordStore
    {
        const string LapTimeKey = "NeonLap.TT.LapTime.{0}";
        const string LapGhostKey = "NeonLap.TT.LapGhost.{0}";
        const string RaceTimeKey = "NeonLap.TT.RaceTime.{0}";
        const string RaceGhostKey = "NeonLap.TT.RaceGhost.{0}";
        const string SectorKey = "NeonLap.TT.Sector.{0}.{1}";

        public static float GetBestLapTime(int trackIndex) =>
            PlayerPrefs.GetFloat(TrackVariantStorage.Format(LapTimeKey, trackIndex), -1f);

        public static float GetBestRaceTime(int trackIndex) =>
            PlayerPrefs.GetFloat(TrackVariantStorage.Format(RaceTimeKey, trackIndex), -1f);

        public static float GetBestSector(int trackIndex, int checkpointIndex) =>
            PlayerPrefs.GetFloat(TrackVariantStorage.Format(SectorKey, null, trackIndex, checkpointIndex), -1f);

        public static bool HasLapGhost(int trackIndex) =>
            PlayerPrefs.HasKey(TrackVariantStorage.Format(LapGhostKey, trackIndex));

        public static bool HasRaceGhost(int trackIndex) =>
            PlayerPrefs.HasKey(TrackVariantStorage.Format(RaceGhostKey, trackIndex));

        public static bool HasPlayerPb(int trackIndex) => HasLapGhost(trackIndex) || HasRaceGhost(trackIndex);

        public static GhostRecordingData LoadBestLapGhost(int trackIndex) => LoadGhost(LapGhostKey, trackIndex);

        public static GhostRecordingData LoadBestRaceGhost(int trackIndex) => LoadGhost(RaceGhostKey, trackIndex);

        public static GhostRecordingData LoadPlaybackGhost(int trackIndex)
        {
            var lap = LoadBestLapGhost(trackIndex);
            if (lap != null && lap.IsValid)
                return lap;

            var race = LoadBestRaceGhost(trackIndex);
            if (race != null && race.IsValid)
                return race;

            return DevGhostLibrary.Load(trackIndex);
        }

        public static bool IsUsingDevGhost(int trackIndex) =>
            !HasPlayerPb(trackIndex) && DevGhostLibrary.Load(trackIndex) != null;

        public static bool TrySaveBestLap(int trackIndex, float lapTime, GhostRecordingData recording)
        {
            if (lapTime <= 0.05f || recording == null || !recording.IsValid)
                return false;

            var previous = GetBestLapTime(trackIndex);
            if (previous > 0f && lapTime >= previous)
                return false;

            return ForceSaveBestLap(trackIndex, lapTime, recording);
        }

        public static bool TrySaveBestRace(int trackIndex, float raceTime, GhostRecordingData recording)
        {
            if (raceTime <= 0.05f || recording == null || !recording.IsValid)
                return false;

            var previous = GetBestRaceTime(trackIndex);
            if (previous > 0f && raceTime >= previous)
                return false;

            return ForceSaveBestRace(trackIndex, raceTime, recording);
        }

        public static bool ForceSaveBestLap(int trackIndex, float lapTime, GhostRecordingData recording)
        {
            if (lapTime <= 0.05f || recording == null || !recording.IsValid)
                return false;

            PlayerPrefs.SetFloat(string.Format(LapTimeKey, trackIndex), lapTime);
            SaveGhost(LapGhostKey, trackIndex, recording);
            PlayerPrefs.Save();
            return true;
        }

        public static bool ForceSaveBestRace(int trackIndex, float raceTime, GhostRecordingData recording)
        {
            if (raceTime <= 0.05f || recording == null || !recording.IsValid)
                return false;

            PlayerPrefs.SetFloat(TrackVariantStorage.Format(RaceTimeKey, trackIndex), raceTime);
            SaveGhost(RaceGhostKey, trackIndex, recording);
            PlayerPrefs.Save();
            return true;
        }

        public static bool TrySaveSector(int trackIndex, int checkpointIndex, float sectorTime)
        {
            if (sectorTime <= 0.01f)
                return false;

            var previous = GetBestSector(trackIndex, checkpointIndex);
            if (previous > 0f && sectorTime >= previous)
                return false;

            PlayerPrefs.SetFloat(string.Format(SectorKey, trackIndex, checkpointIndex), sectorTime);
            PlayerPrefs.Save();
            return true;
        }

        public static string FormatSectorDelta(float current, float best)
        {
            if (best <= 0.01f)
                return " NEW PB";

            var delta = current - best;
            return $" Δ{delta:+0.00;-0.00}s";
        }

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f)
                return "--:--.--";

            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = seconds % 60f;
            return minutes > 0 ? $"{minutes}:{secs:00.00}" : $"{secs:0.00}s";
        }

        public static string GetTrackSummary(int trackIndex)
        {
            var lap = GetBestLapTime(trackIndex);
            var race = GetBestRaceTime(trackIndex);
            if (lap <= 0f && race <= 0f)
            {
                var devTime = DevGhostLibrary.GetReferenceLapTime(trackIndex);
                return devTime > 0f
                    ? $"NO PB  •  DEV GHOST {FormatTime(devTime)}"
                    : "NO PB YET";
            }

            if (lap > 0f && race > 0f)
                return $"PB LAP {FormatTime(lap)}  •  PB RACE {FormatTime(race)}";

            if (lap > 0f)
                return $"PB LAP {FormatTime(lap)}";

            return $"PB RACE {FormatTime(race)}";
        }

        static GhostRecordingData LoadGhost(string keyFormat, int trackIndex)
        {
            var json = PlayerPrefs.GetString(TrackVariantStorage.Format(keyFormat, trackIndex), string.Empty);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonUtility.FromJson<GhostRecordingData>(json);
        }

        static void SaveGhost(string keyFormat, int trackIndex, GhostRecordingData recording)
        {
            PlayerPrefs.SetString(TrackVariantStorage.Format(keyFormat, trackIndex), JsonUtility.ToJson(recording));
        }
    }
}
