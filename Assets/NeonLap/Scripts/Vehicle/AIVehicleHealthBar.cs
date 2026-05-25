using NeonLap.Core;
using NeonLap.Race;
using NeonLap.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.Vehicle
{
    public class AIVehicleHealthBar : MonoBehaviour
    {
        [SerializeField] float localHeight = 2.15f;
        [SerializeField] float barWidth = 1.35f;
        [SerializeField] float barHeight = 0.14f;
        [SerializeField] float lowHealthThreshold = 0.3f;

        static readonly Color HealthyColor = new(0.22f, 0.95f, 0.38f);
        static readonly Color LowColor = new(0.98f, 0.22f, 0.18f);
        static readonly Color BackgroundColor = new(0.04f, 0.05f, 0.07f, 0.85f);

        RectTransform fillRect;
        Image fillImage;
        Canvas canvas;
        UnityEngine.Camera mainCamera;

        public void Build(Transform parent)
        {
            var root = new GameObject("HealthBar");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, localHeight, 0f);

            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 40;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            var canvasRect = root.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f);
            canvasRect.localScale = Vector3.one * 0.01f;

            var background = CreateBarImage(root.transform, "Background", BackgroundColor,
                new Vector2(barWidth * 100f, barHeight * 100f));
            background.transform.SetAsFirstSibling();

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f);
            fillImage = fillGo.AddComponent<Image>();
            fillImage.sprite = UiSpriteUtility.White;
            fillImage.color = HealthyColor;
        }

        public void SetFill(float normalizedHealth)
        {
            if (fillRect == null || fillImage == null)
                return;

            normalizedHealth = Mathf.Clamp01(normalizedHealth);
            fillRect.sizeDelta = new Vector2(barWidth * 100f * normalizedHealth, barHeight * 100f);
            fillImage.color = normalizedHealth <= lowHealthThreshold
                ? Color.Lerp(LowColor, LowColor * 0.75f, normalizedHealth / lowHealthThreshold)
                : Color.Lerp(LowColor, HealthyColor,
                    (normalizedHealth - lowHealthThreshold) / (1f - lowHealthThreshold));
        }

        public void SetVisible(bool visible)
        {
            if (canvas != null)
                canvas.enabled = visible;
        }

        void LateUpdate()
        {
            if (canvas == null || !canvas.enabled)
                return;

            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
                return;

            var direction = transform.position - mainCamera.transform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        static Image CreateBarImage(Transform parent, string name, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.sprite = UiSpriteUtility.White;
            image.color = color;
            return image;
        }
    }
}
