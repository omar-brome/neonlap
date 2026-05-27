using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.Camera
{
    public class CameraModeIndicator : MonoBehaviour
    {
        [SerializeField] FollowCamera followCamera;
        [SerializeField] Text labelText;
        [SerializeField] float visibleDuration = 1.8f;

        FollowCameraMode lastMode;
        float hideAt = -1f;

        public void Configure(FollowCamera camera, Text label)
        {
            followCamera = camera;
            labelText = label;
            if (followCamera != null)
            {
                lastMode = followCamera.Mode;
                ShowLabel(followCamera.ModeLabel, false);
            }
            else if (labelText != null)
            {
                labelText.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (followCamera == null || labelText == null)
                return;

            var spectacle = CameraSpectacleDirector.Instance != null
                ? CameraSpectacleDirector.Instance.GetSpectacleLabel()
                : null;

            if (!string.IsNullOrEmpty(spectacle))
            {
                ShowLabel(spectacle, false);
                return;
            }

            if (followCamera.Mode != lastMode)
            {
                lastMode = followCamera.Mode;
                ShowLabel(followCamera.ModeLabel, true);
            }

            if (followCamera.IsLookingBack)
            {
                ShowLabel("REAR VIEW", false);
                hideAt = -1f;
            }
            else if (hideAt > 0f && Time.unscaledTime >= hideAt)
            {
                labelText.gameObject.SetActive(false);
                hideAt = -1f;
            }
        }

        void ShowLabel(string text, bool timed)
        {
            labelText.text = text;
            labelText.gameObject.SetActive(true);
            hideAt = timed ? Time.unscaledTime + visibleDuration : -1f;
        }
    }
}
