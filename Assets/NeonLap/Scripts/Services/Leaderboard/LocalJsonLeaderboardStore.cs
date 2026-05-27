using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonLap.Services.Leaderboard
{
    [Serializable]
    class LeaderboardFile
    {
        public List<LeaderboardEntry> Entries = new();
    }

    /// <summary>
    /// Offline leaderboard persisted to Application.persistentDataPath/neonlap_leaderboards.json.
    /// Swap to <see cref="UnityGamingServicesLeaderboardBackend"/> when going online.
    /// </summary>
    public sealed class LocalJsonLeaderboardStore : ILeaderboardBackend
    {
        const string FileName = "neonlap_leaderboards.json";
        const int MaxEntriesPerBoard = 25;

        readonly string filePath;
        LeaderboardFile data;

        public string BackendId => "local_json";

        public LocalJsonLeaderboardStore()
        {
            filePath = Path.Combine(Application.persistentDataPath, FileName);
            Load();
        }

        public void Submit(LeaderboardEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.BoardId))
                return;

            entry.UnixTimeMs = entry.UnixTimeMs > 0 ? entry.UnixTimeMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            data.Entries.Add(entry);
            TrimBoard(entry.BoardId);
            Save();
        }

        public IReadOnlyList<LeaderboardEntry> GetTop(string boardId, int maxEntries)
        {
            var list = new List<LeaderboardEntry>();
            foreach (var entry in data.Entries)
            {
                if (entry.BoardId == boardId)
                    list.Add(entry);
            }

            list.Sort(CompareEntries);
            if (list.Count > maxEntries)
                list.RemoveRange(maxEntries, list.Count - maxEntries);
            return list;
        }

        void TrimBoard(string boardId)
        {
            var boardEntries = new List<LeaderboardEntry>();
            for (var i = data.Entries.Count - 1; i >= 0; i--)
            {
                if (data.Entries[i].BoardId != boardId)
                    continue;

                boardEntries.Add(data.Entries[i]);
                data.Entries.RemoveAt(i);
            }

            boardEntries.Sort(CompareEntries);
            var keep = Mathf.Min(boardEntries.Count, MaxEntriesPerBoard);
            for (var i = 0; i < keep; i++)
                data.Entries.Add(boardEntries[i]);
        }

        static int CompareEntries(LeaderboardEntry a, LeaderboardEntry b)
        {
            var valueCompare = a.PrimaryValue.CompareTo(b.PrimaryValue);
            if (valueCompare != 0)
                return valueCompare;

            return b.SecondaryValue.CompareTo(a.SecondaryValue);
        }

        void Load()
        {
            data = new LeaderboardFile();
            if (!File.Exists(filePath))
                return;

            try
            {
                var json = File.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(json))
                    data = JsonUtility.FromJson<LeaderboardFile>(json) ?? new LeaderboardFile();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LocalJsonLeaderboardStore: failed to load ({ex.Message}). Starting fresh.");
                data = new LeaderboardFile();
            }
        }

        void Save()
        {
            try
            {
                File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LocalJsonLeaderboardStore: failed to save ({ex.Message}).");
            }
        }
    }
}
