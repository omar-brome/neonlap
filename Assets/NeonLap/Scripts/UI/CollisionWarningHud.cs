using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class CollisionWarningHud : MonoBehaviour
    {
        static readonly Color WarningColor = new(1f, 0.28f, 0.22f, 0.85f);
        static readonly Color CriticalColor = new(1f, 0.12f, 0.12f, 0.95f);

        CollisionProximitySensor sensor;
        Image topEdge;
        Image bottomEdge;
        Image leftEdge;
        Image rightEdge;
        Image directionMarker;
        Text warningText;
        CanvasGroup rootGroup;
        static Sprite WhiteSprite => UiSpriteUtility.White;

        public void Build(Transform canvasRoot)
        {
            var root = new GameObject("CollisionWarning");
            root.transform.SetParent(canvasRoot, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rootGroup = root.AddComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;

            topEdge = CreateEdge(root.transform, "TopEdge", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -90f), Vector2.zero);
            bottomEdge = CreateEdge(root.transform, "BottomEdge", Vector2.zero, new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 90f));
            leftEdge = CreateEdge(root.transform, "LeftEdge", Vector2.zero, new Vector2(0f, 1f),
                Vector2.zero, new Vector2(90f, 0f));
            rightEdge = CreateEdge(root.transform, "RightEdge", new Vector2(1f, 0f), Vector2.one,
                new Vector2(-90f, 0f), Vector2.zero);

            directionMarker = CreateDirectionMarker(root.transform);
            warningText = CreateWarningLabel(root.transform);
        }

        public void Configure(CollisionProximitySensor proximitySensor)
        {
            sensor = proximitySensor;
        }

        void Update()
        {
            if (sensor == null || !sensor.IsWarningActive)
            {
                if (rootGroup != null && rootGroup.alpha > 0f)
                    rootGroup.alpha = 0f;
                return;
            }

            var level = sensor.WarningLevel;
            rootGroup.alpha = 1f;

            var color = Color.Lerp(WarningColor, CriticalColor, level);
            color.a = Mathf.Lerp(0.35f, 0.92f, level);
            var pulse = 0.85f + Mathf.Sin(Time.unscaledTime * (8f + level * 10f)) * 0.15f;
            color.a *= pulse;

            SetEdge(topEdge, color);
            SetEdge(bottomEdge, color);
            SetEdge(leftEdge, color);
            SetEdge(rightEdge, color);

            UpdateDirectionMarker(level, color);
            UpdateWarningText(level, color);
        }

        void UpdateDirectionMarker(float level, Color color)
        {
            if (directionMarker == null || sensor == null)
                return;

            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                directionMarker.enabled = false;
                return;
            }

            var screen = cam.WorldToScreenPoint(sensor.NearestHazardPoint);
            if (screen.z < 0f)
            {
                directionMarker.enabled = false;
                return;
            }

            directionMarker.enabled = true;
            var rect = directionMarker.rectTransform;
            var canvasRect = rect.parent as RectTransform;
            if (canvasRect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var localPoint);
            var clamped = ClampToEdge(localPoint, canvasRect.rect, 120f);
            rect.anchoredPosition = clamped;
            rect.localRotation = Quaternion.Euler(0f, 0f, AngleFromCenter(clamped));

            var markerColor = color;
            markerColor.a = Mathf.Lerp(0.55f, 1f, level);
            directionMarker.color = markerColor;
        }

        void UpdateWarningText(float level, Color color)
        {
            if (warningText == null)
                return;

            warningText.enabled = level > 0.2f;
            warningText.color = color;
            warningText.text = level > 0.72f ? "! IMPACT IMMINENT !" : "! PROXIMITY WARNING !";
            warningText.fontSize = level > 0.72f ? 34 : 28;
        }

        static void SetEdge(Image edge, Color color)
        {
            if (edge == null)
                return;

            edge.enabled = true;
            edge.color = color;
        }

        static Image CreateEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = WarningColor;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        static Image CreateDirectionMarker(Transform parent)
        {
            var go = new GameObject("DirectionMarker");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(34f, 34f);
            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = WarningColor;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        static Text CreateWarningLabel(Transform parent)
        {
            var go = new GameObject("WarningLabel");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 220f);
            rect.sizeDelta = new Vector2(640f, 48f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = WarningColor;
            text.raycastTarget = false;
            text.enabled = false;
            return text;
        }

        static Vector2 ClampToEdge(Vector2 localPoint, Rect canvasRect, float margin)
        {
            var half = canvasRect.size * 0.5f;
            var x = Mathf.Clamp(localPoint.x, -half.x + margin, half.x - margin);
            var y = Mathf.Clamp(localPoint.y, -half.y + margin, half.y - margin);

            if (Mathf.Abs(localPoint.x) > half.x - margin)
                x = localPoint.x > 0f ? half.x - margin : -half.x + margin;
            if (Mathf.Abs(localPoint.y) > half.y - margin)
                y = localPoint.y > 0f ? half.y - margin : -half.y + margin;

            return new Vector2(x, y);
        }

        static float AngleFromCenter(Vector2 edgePoint)
        {
            return Mathf.Atan2(edgePoint.y, edgePoint.x) * Mathf.Rad2Deg - 90f;
        }
    }
}
