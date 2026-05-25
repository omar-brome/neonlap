using NeonLap.Input;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class PodiumJumpController : MonoBehaviour
    {
        enum CelebrationMode
        {
            None,
            PlayerInput,
            AiAuto,
        }

        [SerializeField] float jumpVelocity = 7.5f;
        [SerializeField] float flipTorque = 14f;
        [SerializeField] float jumpCooldown = 0.55f;
        [SerializeField] float aiJumpIntervalMin = 1.4f;
        [SerializeField] float aiJumpIntervalMax = 3.6f;
        [SerializeField] float danceBobAmount = 0.12f;
        [SerializeField] float danceBobSpeed = 2.8f;
        [SerializeField] float danceYawAmount = 9f;
        [SerializeField] float danceYawSpeed = 1.6f;
        [SerializeField] float danceRollAmount = 6f;
        [SerializeField] float danceRollSpeed = 2.1f;
        [SerializeField] float groundedVelocityThreshold = 0.35f;

        Rigidbody rb;
        PlayerInputReader inputReader;
        CelebrationMode mode;
        float lastJumpTime;
        float nextAiJumpTime;
        float danceTime;
        float dancePhase;
        Vector3 anchorPosition;
        Quaternion anchorRotation;
        bool anchorSet;
        bool wasAirborne;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            inputReader = GetComponent<PlayerInputReader>();
            dancePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void SetJumpEnabled(bool enabled)
        {
            if (!enabled)
                mode = CelebrationMode.None;
            else if (inputReader != null)
                mode = CelebrationMode.PlayerInput;
        }

        public void EnableCelebration()
        {
            mode = CelebrationMode.PlayerInput;
            PrepareRigidbody();
            CaptureAnchor();
        }

        public void EnableAiCelebration()
        {
            mode = CelebrationMode.AiAuto;
            PrepareRigidbody();
            CaptureAnchor();
            ScheduleNextAiJump();
        }

        void PrepareRigidbody()
        {
            if (rb == null)
                return;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        void CaptureAnchor()
        {
            anchorPosition = transform.position;
            anchorRotation = transform.rotation;
            anchorSet = true;
            danceTime = 0f;
        }

        void Update()
        {
            if (mode == CelebrationMode.None || rb == null)
                return;

            if (mode == CelebrationMode.PlayerInput)
                UpdatePlayerJump();

            if (mode == CelebrationMode.AiAuto)
                UpdateAiJumpTimer();
        }

        void FixedUpdate()
        {
            if (mode != CelebrationMode.AiAuto || rb == null || !anchorSet)
                return;

            var airborne = IsAirborne();
            if (wasAirborne && !airborne)
                CaptureAnchor();

            wasAirborne = airborne;

            if (airborne)
                return;

            ApplyDanceMotion();
        }

        void UpdatePlayerJump()
        {
            if (inputReader == null || Time.time - lastJumpTime < jumpCooldown)
                return;

            if (!inputReader.CelebrationJumpPressed)
                return;

            PerformJump();
        }

        void UpdateAiJumpTimer()
        {
            if (Time.time < nextAiJumpTime || IsAirborne())
                return;

            PerformJump();
            ScheduleNextAiJump();
        }

        void ScheduleNextAiJump()
        {
            nextAiJumpTime = Time.time + Random.Range(aiJumpIntervalMin, aiJumpIntervalMax);
        }

        bool IsAirborne()
        {
            return Mathf.Abs(rb.linearVelocity.y) > groundedVelocityThreshold;
        }

        void ApplyDanceMotion()
        {
            danceTime += Time.fixedDeltaTime;

            var bob = Mathf.Sin(danceTime * danceBobSpeed + dancePhase) * danceBobAmount;
            var yaw = Mathf.Sin(danceTime * danceYawSpeed + dancePhase) * danceYawAmount;
            var roll = Mathf.Cos(danceTime * danceRollSpeed + dancePhase * 1.37f) * danceRollAmount;
            var pitch = Mathf.Sin(danceTime * danceRollSpeed * 0.85f) * danceRollAmount * 0.35f;

            var targetPos = anchorPosition + Vector3.up * bob;
            var targetRot = anchorRotation * Quaternion.Euler(pitch, yaw, roll);

            rb.MovePosition(targetPos);
            rb.MoveRotation(targetRot);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        void PerformJump()
        {
            CaptureAnchor();

            var velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            var jumpScale = mode == CelebrationMode.AiAuto ? Random.Range(0.75f, 1.15f) : 1f;
            rb.AddForce(Vector3.up * jumpVelocity * jumpScale, ForceMode.VelocityChange);

            var torqueAxis = transform.right + transform.forward * Random.Range(-0.35f, 0.35f);
            var torqueScale = mode == CelebrationMode.AiAuto ? Random.Range(0.55f, 1.25f) : 1f;
            rb.AddTorque(torqueAxis.normalized * flipTorque * torqueScale, ForceMode.Impulse);

            lastJumpTime = Time.time;
        }
    }
}
