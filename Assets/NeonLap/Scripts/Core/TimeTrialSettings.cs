using UnityEngine;

namespace NeonLap.Core
{
    public static class TimeTrialSettings
    {
        const string GhostVisibleKey = "NeonLap.TT.GhostVisible";
        const string GhostCollisionKey = "NeonLap.TT.GhostCollision";
        const string RivalCountKey = "NeonLap.TT.RivalCount";
        const string PoliceEnabledKey = "NeonLap.TT.PoliceEnabled";
        const string ShowTimeRanksKey = "NeonLap.TT.ShowTimeRanks";

        public static bool GhostVisible { get; private set; } = true;
        public static bool GhostCollisionPenalty { get; private set; }
        public static int RivalCount { get; private set; }
        public static bool PoliceEnabled { get; private set; }
        public static bool ShowTimeRanks { get; private set; } = true;

        public static void Load()
        {
            GhostVisible = PlayerPrefs.GetInt(GhostVisibleKey, 1) == 1;
            GhostCollisionPenalty = PlayerPrefs.GetInt(GhostCollisionKey, 0) == 1;
            RivalCount = Mathf.Clamp(PlayerPrefs.GetInt(RivalCountKey, 0), 0, 3);
            PoliceEnabled = PlayerPrefs.GetInt(PoliceEnabledKey, 0) == 1;
            ShowTimeRanks = PlayerPrefs.GetInt(ShowTimeRanksKey, 1) == 1;
        }

        public static void SetGhostVisible(bool visible)
        {
            GhostVisible = visible;
            PlayerPrefs.SetInt(GhostVisibleKey, visible ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleGhostVisible() => SetGhostVisible(!GhostVisible);

        public static void SetGhostCollisionPenalty(bool enabled)
        {
            GhostCollisionPenalty = enabled;
            PlayerPrefs.SetInt(GhostCollisionKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetRivalCount(int count)
        {
            RivalCount = Mathf.Clamp(count, 0, 3);
            PlayerPrefs.SetInt(RivalCountKey, RivalCount);
            PlayerPrefs.Save();
        }

        public static void SetPoliceEnabled(bool enabled)
        {
            PoliceEnabled = enabled;
            PlayerPrefs.SetInt(PoliceEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetShowTimeRanks(bool show)
        {
            ShowTimeRanks = show;
            PlayerPrefs.SetInt(ShowTimeRanksKey, show ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static string GetRivalCountLabel() => RivalCount switch
        {
            0 => "SOLO",
            1 => "1 RIVAL",
            2 => "2 RIVALS",
            _ => "3 RIVALS",
        };

        public static string GetSummaryLine()
        {
            var clip = GhostCollisionPenalty ? "ON" : "OFF";
            var police = PoliceEnabled ? "ON" : "OFF";
            var ranks = ShowTimeRanks ? "ON" : "OFF";
            return
                $"Rivals {GetRivalCountLabel()}  •  Police {police}  •  Ghost clip {clip}  •  Time ranks {ranks}";
        }
    }
}
