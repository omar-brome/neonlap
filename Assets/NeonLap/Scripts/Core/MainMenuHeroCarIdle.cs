using UnityEngine;

namespace NeonLap.Core
{
    /// <summary>
    /// Subtle hover idle for the parked menu hero car.
    /// </summary>
    public class MainMenuHeroCarIdle : MonoBehaviour
    {
        Vector3 basePosition;
        Quaternion baseRotation;
        float phase;

        [SerializeField] float hoverAmplitude = 0.14f;
        [SerializeField] float hoverSpeed = 0.85f;
        [SerializeField] float rollAmplitude = 1.8f;
        [SerializeField] float rollSpeed = 0.45f;

        public void SnapToPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            basePosition = worldPosition;
            baseRotation = worldRotation;
            transform.SetPositionAndRotation(basePosition, baseRotation);
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            var t = Time.unscaledTime * hoverSpeed + phase;
            var hover = Mathf.Sin(t) * hoverAmplitude;
            var roll = Mathf.Sin(Time.unscaledTime * rollSpeed + phase) * rollAmplitude;

            transform.position = basePosition + Vector3.up * hover;
            transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, roll);
        }
    }
}
