using NeonLap.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.Race
{
    public class GhostShareController : MonoBehaviour
    {
        [SerializeField] int trackIndex;
        [SerializeField] Button exportButton;
        [SerializeField] Button importButton;
        [SerializeField] Text statusText;

        string lastExportedCode = string.Empty;

        public static GhostShareController Setup(Transform parent, int track, Button export, Button import, Text status)
        {
            if (!GameRaceModeSettings.IsTimeTrial && !GameRaceModeSettings.IsGhostDuel)
                return null;

            var go = new GameObject("GhostShare");
            go.transform.SetParent(parent, false);
            var controller = go.AddComponent<GhostShareController>();
            controller.Configure(track, export, import, status);
            return controller;
        }

        void Configure(int track, Button export, Button import, Text status)
        {
            trackIndex = track;
            exportButton = export;
            importButton = import;
            statusText = status;

            if (exportButton != null)
            {
                exportButton.onClick.RemoveListener(ExportGhost);
                exportButton.onClick.AddListener(ExportGhost);
            }

            if (importButton != null)
            {
                importButton.onClick.RemoveListener(ImportGhost);
                importButton.onClick.AddListener(ImportGhost);
            }
        }

        void ExportGhost()
        {
            if (!TimeTrialRecordStore.HasPlayerPb(trackIndex))
            {
                SetStatus("No PB ghost to export yet.");
                return;
            }

            var payload = GhostShareCodec.CreateFromTrack(trackIndex);
            lastExportedCode = GhostShareCodec.Encode(payload);
            GUIUtility.systemCopyBuffer = lastExportedCode;
            SetStatus($"Copied ghost code ({lastExportedCode.Length} chars) to clipboard.");
        }

        void ImportGhost()
        {
            var clip = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(clip))
            {
                SetStatus("Clipboard empty — paste a ghost code first.");
                return;
            }

            if (!GhostShareCodec.TryDecode(clip, out var payload))
            {
                SetStatus("Invalid ghost code.");
                return;
            }

            if (!GhostShareCodec.TryImportToTrack(payload, trackIndex, out var message))
            {
                SetStatus(message);
                return;
            }

            SetStatus($"{message} Restart to load the new ghost.");
        }

        void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
