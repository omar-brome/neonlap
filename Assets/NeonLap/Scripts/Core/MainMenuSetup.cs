using NeonLap.Core;
using NeonLap.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NeonLap.Core
{
    public class MainMenuSetup : MonoBehaviour
    {
        void Awake()
        {
            EnsureGameManager();
            BuildMenuUi();
            ApplyEnvironmentSettings();
        }

        void EnsureGameManager()
        {
            if (FindAnyObjectByType<GameManager>() != null)
                return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        void ApplyEnvironmentSettings()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.02f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.04f, 0f, 0.08f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.01f;

            var cam = UnityEngine.Camera.main;
            if (cam != null)
                cam.backgroundColor = new Color(0.04f, 0f, 0.08f);
        }

        void BuildMenuUi()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("MainMenuUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var title = CreateText(canvasGo.transform, "Title", "NEONLAP", 72, new Vector2(0f, 140f));
            title.color = new Color(0f, 0.96f, 1f);
            title.raycastTarget = false;

            var startButton = CreateButton(canvasGo.transform, "StartButton", "START RACE", new Vector2(0f, 10f));
            var controlsButton = CreateButton(canvasGo.transform, "ControlsButton", "CONTROLS", new Vector2(0f, -60f));
            var quitButton = CreateButton(canvasGo.transform, "QuitButton", "QUIT", new Vector2(0f, -130f));

            var controlsPanel = BuildControlsPanel(canvasGo.transform);
            var backButton = controlsPanel.transform.Find("BackButton")?.GetComponent<Button>();

            var menu = canvasGo.AddComponent<MainMenuController>();
            menu.Configure(startButton, controlsButton, quitButton, controlsPanel, backButton);
        }

        GameObject BuildControlsPanel(Transform parent)
        {
            var panel = new GameObject("ControlsPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            panel.SetActive(false);

            var title = CreateText(panel.transform, "ControlsTitle", "CONTROLS", 56, new Vector2(0f, 260f));
            title.color = new Color(0f, 0.96f, 1f);
            title.raycastTarget = false;

            var lines = new[]
            {
                "W / Up Arrow       Accelerate",
                "S / Down Arrow     Brake / Reverse",
                "A / D or Left / Right     Steer",
                "Space              Drift",
                "R                  Reset Car",
                "Esc                Pause Menu",
            };

            for (var i = 0; i < lines.Length; i++)
            {
                var line = CreateText(panel.transform, "ControlLine" + i, lines[i], 30, new Vector2(0f, 150f - i * 52f));
                line.alignment = TextAnchor.MiddleCenter;
                line.raycastTarget = false;
                var lineRect = line.GetComponent<RectTransform>();
                lineRect.sizeDelta = new Vector2(900f, 44f);
            }

            var hint = CreateText(panel.transform, "ControlsHint", "Complete 3 laps to win the race!", 24, new Vector2(0f, -170f));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;

            CreateButton(panel.transform, "BackButton", "BACK", new Vector2(0f, -250f));
            return panel;
        }

        Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280f, 56f);
            rect.anchoredPosition = pos;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.1f, 0.05f, 0.2f, 0.9f);

            var button = go.AddComponent<Button>();

            var text = CreateText(go.transform, "Label", label, 28, Vector2.zero);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 100f);
            rect.anchoredPosition = pos;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        static void EnsureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var es = new GameObject("EventSystem");
                eventSystem = es.AddComponent<EventSystem>();
            }

            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
                Object.Destroy(legacyModule);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            eventSystem.sendNavigationEvents = false;
        }
    }
}
