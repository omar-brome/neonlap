using System;
using System.Text;
using UnityEngine;

namespace NeonLap.Race
{
    [Serializable]
    public class GhostSharePayload
    {
        public const int CurrentFormatVersion = 1;

        public int formatVersion = CurrentFormatVersion;
        public int trackIndex;
        public float lapTime;
        public float raceTime;
        public GhostRecordingData lapGhost;
        public GhostRecordingData raceGhost;
    }

    public static class GhostShareCodec
    {
        public static string Encode(GhostSharePayload payload)
        {
            if (payload == null)
                return string.Empty;

            payload.formatVersion = GhostSharePayload.CurrentFormatVersion;
            var json = JsonUtility.ToJson(payload);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static bool TryDecode(string encoded, out GhostSharePayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(encoded))
                return false;

            try
            {
                var trimmed = encoded.Trim();
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(trimmed));
                payload = JsonUtility.FromJson<GhostSharePayload>(json);
                if (payload == null)
                    return false;

                if (payload.formatVersion <= 0)
                    payload.formatVersion = 1;

                var hasLap = payload.lapGhost != null && payload.lapGhost.IsValid;
                var hasRace = payload.raceGhost != null && payload.raceGhost.IsValid;
                return hasLap || hasRace;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GhostShareCodec: failed to decode ghost payload ({ex.Message}).");
                return false;
            }
        }

        public static GhostSharePayload CreateFromTrack(int trackIndex)
        {
            return new GhostSharePayload
            {
                formatVersion = GhostSharePayload.CurrentFormatVersion,
                trackIndex = trackIndex,
                lapTime = TimeTrialRecordStore.GetBestLapTime(trackIndex),
                raceTime = TimeTrialRecordStore.GetBestRaceTime(trackIndex),
                lapGhost = TimeTrialRecordStore.LoadBestLapGhost(trackIndex),
                raceGhost = TimeTrialRecordStore.LoadBestRaceGhost(trackIndex),
            };
        }

        public static bool TryImportToTrack(GhostSharePayload payload, int trackIndex, out string message,
            bool forceOverwrite = true)
        {
            message = string.Empty;
            if (payload == null)
            {
                message = "Invalid ghost data.";
                return false;
            }

            if (payload.trackIndex >= 0 && payload.trackIndex != trackIndex)
            {
                message = $"Ghost is for track {payload.trackIndex + 1}, not this track.";
                return false;
            }

            var imported = false;
            if (payload.lapGhost != null && payload.lapGhost.IsValid && payload.lapTime > 0.05f)
            {
                if (forceOverwrite
                        ? TimeTrialRecordStore.ForceSaveBestLap(trackIndex, payload.lapTime, payload.lapGhost)
                        : TimeTrialRecordStore.TrySaveBestLap(trackIndex, payload.lapTime, payload.lapGhost))
                    imported = true;
            }

            if (payload.raceGhost != null && payload.raceGhost.IsValid && payload.raceTime > 0.05f)
            {
                if (forceOverwrite
                        ? TimeTrialRecordStore.ForceSaveBestRace(trackIndex, payload.raceTime, payload.raceGhost)
                        : TimeTrialRecordStore.TrySaveBestRace(trackIndex, payload.raceTime, payload.raceGhost))
                    imported = true;
            }

            if (!imported)
            {
                message = "Nothing imported — no valid lap or race ghost in payload.";
                return false;
            }

            message = "Ghost imported!";
            return true;
        }
    }
}
