using NeonLap.Core;
using NeonLap.Services.Achievements;
using NeonLap.Services.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    /// <summary>
    /// Adds Share / Save platform panel to the main menu (itch.io export, cloud backup, touch options).
    /// </summary>
    public class PlatformMenuExtension : MonoBehaviour
    {
        GameObject panel;
        Text statusText;

        void Awake()
        {
            GameTouchSettings.Load();
            BuildUi();
        }

        void BuildUi()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                return;

            panel = new GameObject("PlatformPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            panel.SetActive(false);

            CreateLabel(panel.transform, "PlatformTitle", "SHARE & SAVE", 52, new Vector2(0f, 260f),
                new Color(0.2f, 1f, 1f));

            statusText = CreateLabel(panel.transform, "PlatformStatus", "Honor-system exports and local cloud backup.",
                20, new Vector2(0f, 200f), new Color(0.75f, 0.88f, 1f));
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(900f, 80f);

            WireButton(panel.transform, "ExportItchButton", "COPY ITCH.IO LEADERBOARD", new Vector2(0f, 110f),
                new Color(0.2f, 1f, 1f), OnExportItch);
            WireButton(panel.transform, "CloudBackupButton", "CLOUD BACKUP (SAVE FILE)", new Vector2(0f, 40f),
                new Color(0.55f, 0.95f, 1f), OnCloudBackup);
            WireButton(panel.transform, "CloudRestoreButton", "RESTORE CLOUD BACKUP", new Vector2(0f, -30f),
                new Color(0.85f, 0.55f, 1f), OnCloudRestore);
            WireButton(panel.transform, "TouchForceButton", "TOGGLE FORCE TOUCH UI", new Vector2(0f, -110f),
                new Color(1f, 0.72f, 0.35f), OnToggleForceTouch);
            WireButton(panel.transform, "TouchAutoGasButton", "TOGGLE AUTO ACCELERATE", new Vector2(0f, -180f),
                new Color(1f, 0.55f, 0.85f), OnToggleAutoAccelerate);
            WireButton(panel.transform, "PlatformBackButton", "BACK", new Vector2(0f, -260f),
                new Color(0.55f, 0.75f, 1f), () => panel.SetActive(false));

            var navGo = GameObject.Find("PlatformNavButton");
            if (navGo != null)
            {
                var nav = navGo.GetComponent<Button>();
                if (nav != null)
                    nav.onClick.AddListener(() => panel.SetActive(true));
            }
        }

        void OnExportItch()
        {
            var ok = ItchIoHonorLeaderboardExporter.TryCopyToClipboard();
            SetStatus(ok
                ? "Copied Time Trial honor leaderboard to clipboard."
                : "Export built — see player log if clipboard failed.");
        }

        void OnCloudBackup()
        {
            var ok = NeonLapCloudSaveService.TryWriteBackup(out var message);
            SetStatus(ok ? message : $"Backup failed: {message}");
        }

        void OnCloudRestore()
        {
            var ok = NeonLapCloudSaveService.TryRestoreBackup(mergeIntoPlayerPrefs: true, out var message);
            SetStatus(ok ? message : $"Restore failed: {message}");
            if (ok)
                CareerAchievementEvaluator.SyncAll();
        }

        void OnToggleForceTouch()
        {
            GameTouchSettings.ForceTouchUi = !GameTouchSettings.ForceTouchUi;
            SetStatus(GameTouchSettings.GetSummaryLine());
        }

        void OnToggleAutoAccelerate()
        {
            GameTouchSettings.AutoAccelerate = !GameTouchSettings.AutoAccelerate;
            SetStatus(GameTouchSettings.GetSummaryLine());
        }

        void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        static Text CreateLabel(Transform parent, string name, string text, int size, Vector2 pos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(800f, 40f);
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static void WireButton(Transform parent, string name, string label, Vector2 pos, Color accent,
            UnityEngine.Events.UnityAction action)
        {
            var button = CreateNeonButton(parent, name, label, pos, accent);
            button.onClick.AddListener(action);
        }

        static Button CreateNeonButton(Transform parent, string name, string label, Vector2 anchoredPosition, Color accent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 56f);
            rect.anchoredPosition = anchoredPosition;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.05f, 0.06f, 0.12f, 0.95f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.3f, 1f);
            colors.pressedColor = new Color(accent.r * 0.5f, accent.g * 0.5f, accent.b * 0.5f, 1f);
            button.colors = colors;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = accent;
            return button;
        }
    }
}
