using UnityEngine;

namespace NeonLap.Core
{
    public class MainMenuCameraDrift : MonoBehaviour
    {
        Vector3 basePosition;
        Vector3 baseEuler;
        [SerializeField] float positionSway = 1.6f;
        [SerializeField] float rotationSway = 2.2f;
        [SerializeField] float speed = 0.22f;

        void Awake()
        {
            basePosition = transform.position;
            baseEuler = transform.eulerAngles;
        }

        void LateUpdate()
        {
            var t = Time.unscaledTime * speed;
            transform.position = basePosition + new Vector3(
                Mathf.Sin(t * 1.1f) * positionSway,
                Mathf.Sin(t * 0.85f) * positionSway * 0.35f,
                Mathf.Cos(t * 0.65f) * positionSway * 0.5f);
            transform.rotation = Quaternion.Euler(
                baseEuler.x + Mathf.Sin(t * 0.7f) * rotationSway,
                baseEuler.y + Mathf.Cos(t * 0.55f) * rotationSway * 1.4f,
                baseEuler.z);
        }
    }
}
