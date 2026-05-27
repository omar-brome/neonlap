using NeonLap.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class TouchDrivingUI : MonoBehaviour
    {
        [SerializeField] RectTransform steerBackground;
        [SerializeField] RectTransform steerHandle;
        [SerializeField] Button driftButton;
        [SerializeField] Button nitroButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button pauseButton;
        [SerializeField] float steerRadius = 90f;

        Vector2 steerVector;
        bool driftHeld;
        bool nitroPressed;
        bool resetPressed;
        bool pausePressed;
        Vector2 steerCenter;

        public Vector2 SteerVector => steerVector;
        public bool DriftHeld => driftHeld;
        public bool NitroPressed => nitroPressed;
        public bool ResetPressed => resetPressed;
        public bool PausePressed => pausePressed;
        public bool IsActive => gameObject.activeSelf;

        public static TouchDrivingUI Build(Transform canvasRoot)
        {
            if (!ShouldShowTouchUi())
                return null;

            var panel = new GameObject("TouchDrivingUI");
            panel.transform.SetParent(canvasRoot, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var ui = panel.AddComponent<TouchDrivingUI>();
            ui.BuildWidgets(panel.transform);
            return ui;
        }

        static bool ShouldShowTouchUi()
        {
            if (GameTouchSettings.ForceTouchUi)
                return true;

#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return Application.isMobilePlatform;
#endif
        }

        void BuildWidgets(Transform root)
        {
            CreateLabel(root, "SteerLabel", "STEER / THROTTLE", new Vector2(180f, 248f));

            steerBackground = CreateCircle(root, "SteerBackground", new Vector2(180f, 180f), new Vector2(180f, 180f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.05f, 0.08f, 0.14f, 0.55f), 160f);
            steerHandle = CreateCircle(root, "SteerHandle", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(180f, 180f), new Vector2(180f, 180f), new Color(0.2f, 1f, 1f, 0.85f), 56f);

            var steerZone = CreateTouchZone(root, "SteerZone", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(320f, 320f), new Vector2(180f, 180f));

            var driftGo = CreateButton(root, "DriftButton", "DRIFT", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-120f, 200f), new Vector2(140f, 140f), new Color(1f, 0.45f, 0.55f));
            driftButton = driftGo.GetComponent<Button>();
            driftGo.AddComponent<TouchHoldButton>().Configure(driftButton, held => driftHeld = held);

            var nitroGo = CreateButton(root, "NitroButton", "NITRO", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-120f, 340f), new Vector2(140f, 140f), new Color(1f, 0.72f, 0.35f));
            nitroButton = nitroGo.GetComponent<Button>();
            nitroGo.GetComponent<Button>().onClick.AddListener(() => nitroPressed = true);

            var resetGo = CreateButton(root, "ResetButton", "RESET", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-120f, 60f), new Vector2(120f, 72f), new Color(0.55f, 0.75f, 1f));
            resetButton = resetGo.GetComponent<Button>();
            resetGo.GetComponent<Button>().onClick.AddListener(() => resetPressed = true);

            var pauseGo = CreateButton(root, "PauseButton", "PAUSE", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-100f, -80f), new Vector2(120f, 72f), new Color(0.55f, 0.75f, 1f));
            pauseButton = pauseGo.GetComponent<Button>();
            pauseGo.GetComponent<Button>().onClick.AddListener(() => pausePressed = true);

            var steerDrag = steerZone.AddComponent<TouchSteerDrag>();
            steerDrag.Configure(this, steerBackground, steerHandle, steerRadius);
        }

        public void ClearFrameButtons()
        {
            nitroPressed = false;
            resetPressed = false;
            pausePressed = false;
        }

        static RectTransform CreateCircle(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 pivot, Color color, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = anchoredPosition;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static GameObject CreateTouchZone(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = true;
            return go;
        }

        static void CreateLabel(Transform parent, string name, string text, Vector2 anchoredPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(220f, 24f);
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = 14;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.65f, 0.9f, 1f, 0.85f);
            label.raycastTarget = false;
        }

        static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color accent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.05f, 0.06f, 0.12f, 0.92f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.95f);
            colors.pressedColor = new Color(accent.r * 0.4f, accent.g * 0.4f, accent.b * 0.4f, 1f);
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
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = accent;
            return go;
        }

        sealed class TouchSteerDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            TouchDrivingUI owner;
            RectTransform background;
            RectTransform handle;
            float radius;

            public void Configure(TouchDrivingUI ui, RectTransform bg, RectTransform knob, float maxRadius)
            {
                owner = ui;
                background = bg;
                handle = knob;
                radius = maxRadius;
            }

            public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

            public void OnDrag(PointerEventData eventData)
            {
                if (owner == null || background == null || handle == null)
                    return;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position,
                    eventData.pressEventCamera, out var local);
                var clamped = Vector2.ClampMagnitude(local, radius);
                handle.anchoredPosition = clamped;
                owner.steerVector = clamped / radius;
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (handle != null)
                    handle.anchoredPosition = Vector2.zero;
                if (owner != null)
                    owner.steerVector = Vector2.zero;
            }
        }

        sealed class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            Button button;
            System.Action<bool> onHeld;

            public void Configure(Button target, System.Action<bool> heldCallback)
            {
                button = target;
                onHeld = heldCallback;
            }

            public void OnPointerDown(PointerEventData eventData) => onHeld?.Invoke(true);

            public void OnPointerUp(PointerEventData eventData) => onHeld?.Invoke(false);
        }
    }
}
