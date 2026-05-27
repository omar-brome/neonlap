using UnityEngine;

namespace NeonLap.Race
{
    public static class DevGhostLibrary
    {
        const string ResourcePathFormat = "NeonLap/DevGhosts/DevGhost_Level{0}";

        static DevGhostAsset[] cached;

        public static GhostRecordingData Load(int trackIndex)
        {
            var asset = LoadAsset(trackIndex);
            return asset != null && asset.IsValid ? asset.recording : null;
        }

        public static float GetReferenceLapTime(int trackIndex)
        {
            var asset = LoadAsset(trackIndex);
            return asset != null && asset.referenceLapTime > 0.05f ? asset.referenceLapTime : -1f;
        }

        static DevGhostAsset LoadAsset(int trackIndex)
        {
            EnsureCache();
            if (cached == null || trackIndex < 0 || trackIndex >= cached.Length)
            {
                return Resources.Load<DevGhostAsset>(string.Format(ResourcePathFormat, trackIndex + 1));
            }

            return cached[trackIndex];
        }

        static void EnsureCache()
        {
            if (cached != null)
                return;

            cached = new DevGhostAsset[6];
            for (var i = 0; i < cached.Length; i++)
                cached[i] = Resources.Load<DevGhostAsset>(string.Format(ResourcePathFormat, i + 1));
        }
    }
}
