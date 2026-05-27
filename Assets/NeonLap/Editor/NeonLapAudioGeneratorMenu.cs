#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonLap.Editor
{
    public static class NeonLapAudioGeneratorMenu
    {
        const string ScriptRelativePath = "Tools/generate_neonlap_audio.py";

        [MenuItem("NeonLap/Regenerate Audio Clips")]
        public static void RegenerateAudioClips()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                EditorUtility.DisplayDialog("Neon Lap Audio", "Could not locate project root.", "OK");
                return;
            }

            var scriptPath = Path.Combine(projectRoot, ScriptRelativePath);
            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("Neon Lap Audio",
                    "Missing generator script:\n" + scriptPath, "OK");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                EditorUtility.DisplayDialog("Neon Lap Audio", "Failed to start python3.", "OK");
                return;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                EditorUtility.DisplayDialog("Neon Lap Audio",
                    "Generator failed:\n" + error, "OK");
                return;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Neon Lap Audio",
                "Audio clips regenerated.\n\n" + output, "OK");
        }
    }
}
#endif
