#if UNITY_EDITOR
using System.Collections.Generic;
using NeonLap.Race;
using UnityEditor;
using UnityEngine;

namespace NeonLap.Editor
{
    public static class NeonLapDevGhostMenu
    {
        const string OutputFolder = "Assets/NeonLap/Resources/NeonLap/DevGhosts";

        static readonly float[] LevelLapSeconds = { 68f, 62f, 74f, 58f, 70f, 66f };

        [MenuItem("NeonLap/Ghosts/Bake Placeholder Dev Ghosts (All Levels)")]
        public static void BakeAllDevGhosts()
        {
            EnsureFolder(OutputFolder);
            for (var i = 0; i < LevelLapSeconds.Length; i++)
                BakeLevel(i, LevelLapSeconds[i]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Baked {LevelLapSeconds.Length} dev ghost assets under {OutputFolder}.");
        }

        [MenuItem("NeonLap/Ghosts/Bake Placeholder Dev Ghost (Selected Level Index)")]
        public static void BakeSelectedLevelDevGhost()
        {
            var index = EditorPrefs.GetInt("NeonLap_CurrentLevelIndex", 0);
            index = Mathf.Clamp(index, 0, LevelLapSeconds.Length - 1);
            EnsureFolder(OutputFolder);
            BakeLevel(index, LevelLapSeconds[index]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Baked dev ghost for level {index + 1}.");
        }

        static void BakeLevel(int levelIndex, float lapSeconds)
        {
            var path = $"{OutputFolder}/DevGhost_Level{levelIndex + 1}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DevGhostAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DevGhostAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.trackIndex = levelIndex;
            asset.referenceLapTime = lapSeconds;
            asset.recording = BuildOvalRecording(lapSeconds, 40f + levelIndex * 4f, levelIndex + 2);
            EditorUtility.SetDirty(asset);
        }

        static GhostRecordingData BuildOvalRecording(float duration, float radius, int loops)
        {
            const int frameCount = 180;
            var frames = new List<ReplayFrameSnapshot>(frameCount);
            for (var i = 0; i < frameCount; i++)
            {
                var t = i / (float)(frameCount - 1) * duration;
                var angle = t / duration * Mathf.PI * 2f * loops;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var rot = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 90f, 0f);
                frames.Add(ReplayFrameSnapshot.FromTransform(t, pos, rot));
            }

            return GhostRecordingData.FromFrames(frames, maxFrames: frameCount);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            const string root = "Assets/NeonLap/Resources/NeonLap";
            if (!AssetDatabase.IsValidFolder(root))
            {
                if (!AssetDatabase.IsValidFolder("Assets/NeonLap/Resources"))
                    AssetDatabase.CreateFolder("Assets/NeonLap", "Resources");
                AssetDatabase.CreateFolder("Assets/NeonLap/Resources", "NeonLap");
            }

            AssetDatabase.CreateFolder(root, "DevGhosts");
        }
    }
}
#endif
