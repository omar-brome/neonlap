using UnityEngine;

namespace NeonLap.Core
{
    /// <summary>
    /// Slow cinematic orbit around the menu hero car.
    /// </summary>
    public class MainMenuOrbitCamera : MonoBehaviour
    {
        Transform target;
        UnityEngine.Camera menuCamera;
        float yawDegrees;
        Vector3 smoothedPosition;
        float smoothedFov;
        bool initialized;

        [SerializeField] float orbitRadius = 15.5f;
        [SerializeField] float orbitHeight = 5.8f;
        [SerializeField] float lookHeight = 1.15f;
        [SerializeField] float degreesPerSecond = 9.5f;
        [SerializeField] float verticalBobAmplitude = 0.55f;
        [SerializeField] float verticalBobSpeed = 0.32f;
        [SerializeField] float positionSmoothing = 3.2f;
        [SerializeField] float rotationSmoothing = 4.5f;
        [SerializeField] float baseFieldOfView = 54f;
        [SerializeField] float fieldOfViewPulse = 1.8f;

        public void Configure(Transform focus, float startYawDegrees = 35f)
        {
            target = focus;
            yawDegrees = startYawDegrees;
            menuCamera = GetComponent<UnityEngine.Camera>();
            if (menuCamera == null)
                menuCamera = UnityEngine.Camera.main;

            if (target != null)
            {
                smoothedPosition = ComputeOrbitPosition();
                transform.position = smoothedPosition;
                transform.rotation = ComputeOrbitRotation(smoothedPosition);
                initialized = true;
            }

            if (menuCamera != null)
            {
                menuCamera.fieldOfView = baseFieldOfView;
                smoothedFov = baseFieldOfView;
            }
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            yawDegrees += degreesPerSecond * Time.unscaledDeltaTime;

            var desiredPosition = ComputeOrbitPosition();
            if (!initialized)
            {
                smoothedPosition = desiredPosition;
                initialized = true;
            }
            else
            {
                var blend = 1f - Mathf.Exp(-positionSmoothing * Time.unscaledDeltaTime);
                smoothedPosition = Vector3.Lerp(smoothedPosition, desiredPosition, blend);
            }

            transform.position = smoothedPosition;

            var desiredRotation = ComputeOrbitRotation(smoothedPosition);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                1f - Mathf.Exp(-rotationSmoothing * Time.unscaledDeltaTime));

            if (menuCamera != null)
            {
                var targetFov = baseFieldOfView
                                + Mathf.Sin(Time.unscaledTime * verticalBobSpeed) * fieldOfViewPulse;
                smoothedFov = Mathf.Lerp(smoothedFov, targetFov, Time.unscaledDeltaTime * 2f);
                menuCamera.fieldOfView = smoothedFov;
            }
        }

        Vector3 GetLookPoint()
        {
            if (target == null)
                return transform.position + transform.forward * 5f;

            return target.position + Vector3.up * lookHeight;
        }

        Vector3 ComputeOrbitPosition()
        {
            var anchor = target.position;
            var bob = Mathf.Sin(Time.unscaledTime * verticalBobSpeed * Mathf.PI * 2f) * verticalBobAmplitude;
            var rad = yawDegrees * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Sin(rad) * orbitRadius, orbitHeight + bob, Mathf.Cos(rad) * orbitRadius);
            return anchor + offset;
        }

        Quaternion ComputeOrbitRotation(Vector3 cameraPosition)
        {
            var lookPoint = GetLookPoint();
            var direction = lookPoint - cameraPosition;
            if (direction.sqrMagnitude < 0.0001f)
                return transform.rotation;

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
