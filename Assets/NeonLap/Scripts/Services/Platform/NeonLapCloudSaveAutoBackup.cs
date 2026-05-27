using UnityEngine;

namespace NeonLap.Services.Platform
{
    /// <summary>
    /// Writes a local cloud-save JSON when the app backgrounds (mobile-friendly backup).
    /// </summary>
    public class NeonLapCloudSaveAutoBackup : MonoBehaviour
    {
        void OnApplicationPause(bool paused)
        {
            if (!paused)
                return;

            NeonLapCloudSaveService.TryWriteBackup(out _);
        }

        void OnApplicationQuit()
        {
            NeonLapCloudSaveService.TryWriteBackup(out _);
        }
    }
}
