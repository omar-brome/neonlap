using UnityEngine;

namespace NeonLap.UI
{
    public class MainMenuTitlePulse : MonoBehaviour
    {
        [SerializeField] float pulseSpeed = 1.8f;
        [SerializeField] float scaleAmplitude = 0.04f;
        [SerializeField] Color colorA = new(0.2f, 1f, 1f);
        [SerializeField] Color colorB = new(0.55f, 0.85f, 1f);

        RectTransform rect;
        Vector3 baseScale;
        UnityEngine.UI.Text label;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            label = GetComponent<UnityEngine.UI.Text>();
            if (rect != null)
                baseScale = rect.localScale;
        }

        void Update()
        {
            var wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            if (rect != null)
                rect.localScale = baseScale * (1f + scaleAmplitude * wave);

            if (label != null)
                label.color = Color.Lerp(colorA, colorB, wave);
        }
    }
}
