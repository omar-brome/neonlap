using System;
using System.Collections.Generic;
using System.IO;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Services.Platform
{
    /// <summary>
    /// Backs up all NeonLap PlayerPrefs keys to a JSON file under persistentDataPath (mobile-friendly).
    /// </summary>
    public static class NeonLapCloudSaveService
    {
        const string SaveFileName = "neonlap_cloud_save.json";
        const string KeyPrefix = "NeonLap";

        [Serializable]
        public class CloudSavePayload
        {
            public int version = 1;
            public string savedAtUtc;
            public List<CloudSaveEntry> entries = new();
        }

        [Serializable]
        public class CloudSaveEntry
        {
            public string key;
            public string value;
            public int valueType;
        }

        public enum ValueType
        {
            String = 0,
            Int = 1,
            Float = 2,
        }

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool HasBackupFile() => File.Exists(SaveFilePath);

        public static CloudSavePayload CreateBackupPayload()
        {
            var payload = new CloudSavePayload
            {
                savedAtUtc = DateTime.UtcNow.ToString("o"),
            };

            foreach (var key in EnumerateNeonLapKeys())
            {
                if (!PlayerPrefs.HasKey(key))
                    continue;

                var entry = CaptureEntry(key);
                if (entry != null)
                    payload.entries.Add(entry);
            }

            return payload;
        }

        public static bool TryWriteBackup(out string message)
        {
            try
            {
                var payload = CreateBackupPayload();
                var json = JsonUtility.ToJson(payload, true);
                File.WriteAllText(SaveFilePath, json);
                message = $"Saved {payload.entries.Count} values to {SaveFilePath}";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool TryRestoreBackup(bool mergeIntoPlayerPrefs, out string message)
        {
            if (!HasBackupFile())
            {
                message = "No backup file found.";
                return false;
            }

            try
            {
                var json = File.ReadAllText(SaveFilePath);
                var payload = JsonUtility.FromJson<CloudSavePayload>(json);
                if (payload?.entries == null || payload.entries.Count == 0)
                {
                    message = "Backup file is empty.";
                    return false;
                }

                ApplyPayload(payload, mergeIntoPlayerPrefs);
                PlayerPrefs.Save();
                message = $"Restored {payload.entries.Count} values from {payload.savedAtUtc}";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static void ApplyPayload(CloudSavePayload payload, bool merge)
        {
            if (payload?.entries == null)
                return;

            if (!merge)
            {
                foreach (var key in EnumerateNeonLapKeys())
                {
                    if (PlayerPrefs.HasKey(key))
                        PlayerPrefs.DeleteKey(key);
                }
            }

            foreach (var entry in payload.entries)
            {
                if (string.IsNullOrEmpty(entry.key) || !entry.key.StartsWith(KeyPrefix, StringComparison.Ordinal))
                    continue;

                switch ((ValueType)entry.valueType)
                {
                    case ValueType.Int:
                        if (int.TryParse(entry.value, out var intVal))
                            PlayerPrefs.SetInt(entry.key, intVal);
                        break;
                    case ValueType.Float:
                        if (float.TryParse(entry.value, out var floatVal))
                            PlayerPrefs.SetFloat(entry.key, floatVal);
                        break;
                    default:
                        PlayerPrefs.SetString(entry.key, entry.value ?? string.Empty);
                        break;
                }
            }
        }

        static CloudSaveEntry CaptureEntry(string key)
        {
            if (!PlayerPrefs.HasKey(key))
                return null;

            if (key.Contains("Ghost", StringComparison.Ordinal))
            {
                return new CloudSaveEntry
                {
                    key = key,
                    value = PlayerPrefs.GetString(key, string.Empty),
                    valueType = (int)ValueType.String,
                };
            }

            if (key.Contains("Time", StringComparison.Ordinal)
                || key.Contains("BestLap", StringComparison.Ordinal)
                || key.Contains("Sector", StringComparison.Ordinal))
            {
                return new CloudSaveEntry
                {
                    key = key,
                    value = PlayerPrefs.GetFloat(key, 0f)
                        .ToString(System.Globalization.CultureInfo.InvariantCulture),
                    valueType = (int)ValueType.Float,
                };
            }

            return new CloudSaveEntry
            {
                key = key,
                value = PlayerPrefs.GetInt(key, 0).ToString(),
                valueType = (int)ValueType.Int,
            };
        }

        static IEnumerable<string> EnumerateNeonLapKeys()
        {
            var known = new HashSet<string>
            {
                "NeonLap_CurrentLevelIndex",
            };

            foreach (var id in NeonLap.Services.Achievements.AchievementStore.AllIds)
                known.Add("NeonLap.Achievement." + id);

            for (var i = 0; i < 12; i++)
            {
                known.Add($"NeonLap.Career.Score.{i}");
                known.Add($"NeonLap.Career.BestLap.{i}");
                known.Add($"NeonLap.Career.Medal.{i}");
                known.Add($"NeonLap.Career.Stars.{i}");
                known.Add($"NeonLap.TT.LapTime.{i}");
                known.Add($"NeonLap.TT.RaceTime.{i}");
                known.Add($"NeonLap.TT.LapGhost.{i}");
                known.Add($"NeonLap.TT.RaceGhost.{i}");
                known.Add($"NeonLap.TT.Medal.Race.{i}");
                known.Add($"NeonLap.TT.Medal.Lap.{i}");
                known.Add($"NeonLap.ScoreAttack.High.{i}");
                for (var s = 0; s < 32; s++)
                    known.Add($"NeonLap.TT.Sector.{i}.{s}");

                var rev = TrackVariantStorage.ReverseSuffix;
                known.Add($"NeonLap.Career.Score.{i}{rev}");
                known.Add($"NeonLap.Career.BestLap.{i}{rev}");
                known.Add($"NeonLap.Career.Medal.{i}{rev}");
                known.Add($"NeonLap.Career.Stars.{i}{rev}");
                known.Add($"NeonLap.TT.LapTime.{i}{rev}");
                known.Add($"NeonLap.TT.RaceTime.{i}{rev}");
                known.Add($"NeonLap.TT.LapGhost.{i}{rev}");
                known.Add($"NeonLap.TT.RaceGhost.{i}{rev}");
                known.Add($"NeonLap.TT.Medal.Race.{i}{rev}");
                known.Add($"NeonLap.TT.Medal.Lap.{i}{rev}");
                known.Add($"NeonLap.ScoreAttack.High.{i}{rev}");
                for (var s = 0; s < 32; s++)
                    known.Add($"NeonLap.TT.Sector.{i}.{s}{rev}");
            }

            for (var i = 0; i < 8; i++)
            {
                known.Add($"NeonLap.Cosmetic.Unlocked.trail_{i}");
                known.Add($"NeonLap.Cosmetic.Unlocked.horn");
            }

            known.Add("NeonLap.Garage.SelectedIndex");
            known.Add("NeonLap.Customize.Paint");
            known.Add("NeonLap.Customize.Decal");
            known.Add("NeonLap.Customize.Rim");
            known.Add("NeonLap.Career.TotalXp");
            known.Add("NeonLap.Vehicle.ProfileKind");
            known.Add("NeonLap.Vehicle.UnderglowIndex");
            known.Add("NeonLap.Career.EndlessUnlocked");
            known.Add("NeonLap.Career.EndlessCosmetic");
            known.Add("NeonLap.Cosmetic.Trail");
            known.Add("NeonLap.Cosmetic.Horn");
            known.Add("NeonLap.Achievement.WinCount");
            known.Add("NeonLap.Achievement.StylePoints");
            known.Add("NeonLap.Touch.ForceUi");
            known.Add("NeonLap.Touch.AutoAccelerate");

            return known;
        }
    }
}
