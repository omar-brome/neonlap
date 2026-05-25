using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Environment
{
    public class PatrolHelicopter : MonoBehaviour
    {
        [SerializeField] float leadDistance = 48f;
        [SerializeField] float hoverHeight = 24f;
        [SerializeField] float lateralOffset = 14f;
        [SerializeField] float positionSmooth = 2.2f;
        [SerializeField] float rotationSmooth = 3.5f;
        [SerializeField] float bobAmplitude = 0.85f;
        [SerializeField] float bobSpeed = 1.35f;
        [SerializeField] float swayAmplitude = 5f;
        [SerializeField] float swaySpeed = 0.45f;
        [SerializeField] float mainRotorSpeed = 720f;
        [SerializeField] float tailRotorSpeed = 980f;
        [SerializeField] float maxBankAngle = 14f;

        Transform followTarget;
        Transform mainRotor;
        Transform tailRotor;
        Light searchlight;
        RaceManager raceManager;
        float phaseOffset;
        Vector3 velocity;

        public void Configure(Transform target, HelicopterVisualBuilder.BuiltHelicopter visual, RaceManager manager)
        {
            followTarget = target;
            mainRotor = visual.MainRotor;
            tailRotor = visual.TailRotor;
            searchlight = visual.Searchlight;
            raceManager = manager;
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        void LateUpdate()
        {
            if (followTarget == null)
                return;

            if (raceManager != null && raceManager.State == RaceState.Waiting)
                return;

            UpdateFlightPosition();
            UpdateFlightRotation();
            UpdateSearchlight();
        }

        void Update()
        {
            if (mainRotor != null)
                mainRotor.Rotate(Vector3.up, mainRotorSpeed * Time.deltaTime, Space.Self);

            if (tailRotor != null)
                tailRotor.Rotate(Vector3.forward, tailRotorSpeed * Time.deltaTime, Space.Self);
        }

        void UpdateFlightPosition()
        {
            var forward = followTarget.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var sway = Mathf.Sin(Time.time * swaySpeed + phaseOffset) * swayAmplitude;
            var bob = Mathf.Sin(Time.time * bobSpeed + phaseOffset) * bobAmplitude;

            var desiredPosition = followTarget.position
                                  + forward * leadDistance
                                  + right * (lateralOffset + sway)
                                  + Vector3.up * (hoverHeight + bob);

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity,
                1f / Mathf.Max(positionSmooth, 0.01f));
        }

        void UpdateFlightRotation()
        {
            var lookTarget = followTarget.position + Vector3.up * 2f;
            var toTarget = lookTarget - transform.position;
            if (toTarget.sqrMagnitude < 0.01f)
                return;

            var bank = Mathf.Sin(Time.time * swaySpeed + phaseOffset) * maxBankAngle;
            var lookRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            var bankRotation = Quaternion.Euler(0f, 0f, bank);
            var desiredRotation = lookRotation * bankRotation;

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmooth * Time.deltaTime);
        }

        void UpdateSearchlight()
        {
            if (searchlight == null || followTarget == null)
                return;

            var aimPoint = followTarget.position + Vector3.up * 1.2f;
            var lightTransform = searchlight.transform;
            var direction = aimPoint - lightTransform.position;
            if (direction.sqrMagnitude < 0.01f)
                return;

            lightTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            var active = raceManager == null || raceManager.State == RaceState.Racing ||
                         raceManager.State == RaceState.Finished;
            searchlight.enabled = active;
        }
    }
}
