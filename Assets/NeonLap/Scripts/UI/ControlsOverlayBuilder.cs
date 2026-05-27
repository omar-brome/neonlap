using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public static class ControlsOverlayBuilder
    {
        public static readonly string[] ControlLines =
        {
            "W / Up Arrow       Accelerate",
            "S / Down Arrow     Brake / Reverse",
            "A / D or Left / Right     Steer",
            "Space              Drift",
            "S / Down x2        Barrel roll (20+ mph)",
            "Shift / RB         Nitro boost (needs charge)",
            "Shift / LB         Look back",
            "C                  Switch Camera (follow / hood / close)",
            "V                  Drone cam (2s, needs patrol helicopter)",
            "R                  Reset Car / Refuel (empty tank)",
            "Green pads         Pit fuel (+45%)",
            "Nitro pickups      Boost charge + fuel refill",
            "E                  Drop banana (3 charges)",
            "Q                  EMP pulse (disables rival nitro)",
            "Shield             Blocks one slip or hard hit",
            "M                  Toggle minimap rotation (north / heading up)",
            "Level select       Pick CAREER or TIME TRIAL, then choose a track",
            "Esc                Pause Menu",
        };

        public const string GamepadHint =
            "Gamepad: left stick steer, RT accelerate, LT brake, RB nitro, D-pad Down x2 barrel roll.";

        public static GameObject Build(Transform parent, string panelName, out Button backButton, string backLabel = "BACK")
        {
            var panel = CreateOverlayPanel(parent, panelName);

            var title = CreateText(panel.transform, "ControlsTitle", "CONTROLS", 56, new Vector2(0f, 260f));
            title.color = new Color(0.2f, 1f, 1f);
            title.raycastTarget = false;

            const float lineSpacing = 40f;
            const float firstLineY = 210f;
            for (var i = 0; i < ControlLines.Length; i++)
            {
                var line = CreateText(panel.transform, "ControlLine" + i, ControlLines[i], 26,
                    new Vector2(0f, firstLineY - i * lineSpacing));
                line.alignment = TextAnchor.MiddleCenter;
                line.raycastTarget = false;
                line.color = new Color(0.88f, 0.92f, 1f);
                var lineRect = line.GetComponent<RectTransform>();
                lineRect.sizeDelta = new Vector2(920f, 38f);
            }

            var hintY = firstLineY - ControlLines.Length * lineSpacing - 24f;
            var hint = CreateText(panel.transform, "ControlsHint", GamepadHint, 20, new Vector2(0f, hintY));
            hint.color = new Color(0.75f, 0.85f, 1f);
            hint.raycastTarget = false;
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(920f, 60f);

            backButton = CreateNeonButton(panel.transform, "ControlsBackButton", backLabel,
                new Vector2(0f, hintY - 70f), new Color(0.55f, 0.75f, 1f));
            panel.SetActive(false);
            return panel;
        }

        static GameObject CreateOverlayPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.02f, 0.03f, 0.08f, 0.94f);
            return panel;
        }

        static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 80f);
            rect.anchoredPosition = anchoredPosition;
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        static Button CreateNeonButton(Transform parent, string name, string label, Vector2 pos, Color accent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(320f, 56f);
            rect.anchoredPosition = pos;

            var fill = new Color(0.05f, 0.06f, 0.12f, 0.96f);
            go.AddComponent<Image>().color = fill;
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = Color.Lerp(fill, accent, 0.22f);
            colors.pressedColor = Color.Lerp(fill, accent, 0.35f);
            button.colors = colors;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 28;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = new Color(0.97f, 0.98f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return button;
        }
    }
}
