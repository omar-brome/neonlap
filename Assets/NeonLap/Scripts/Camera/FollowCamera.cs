using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new(0f, 4f, -10f);
        [SerializeField] float positionSmooth = 8f;
        [SerializeField] float rotationSmooth = 10f;
        [SerializeField] float baseFov = 60f;
        [SerializeField] float maxFov = 75f;
        [SerializeField] float fovSpeedReference = 45f;
        [SerializeField] float maxRoll = 5f;

        UnityEngine.Camera cam;
        VehicleController vehicle;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                vehicle = target != null ? target.GetComponent<VehicleController>() : null;
            }
        }

        void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            if (vehicle == null)
                vehicle = target.GetComponent<VehicleController>();

            var speed = vehicle != null ? vehicle.CurrentSpeed : 0f;
            var steer = vehicle != null ? vehicle.SteerInput : 0f;

            var desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmooth * Time.deltaTime);

            var lookRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            var roll = Quaternion.Euler(0f, 0f, -steer * maxRoll);
            var desiredRotation = lookRotation * roll;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmooth * Time.deltaTime);

            var fovT = Mathf.Clamp01(speed / fovSpeedReference);
            cam.fieldOfView = Mathf.Lerp(baseFov, maxFov, fovT);
        }
    }
}
