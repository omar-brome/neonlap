using System.Collections;
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

        [SerializeField] float jumpHeight = 2.2f;
        [SerializeField] float jumpDuration = 0.62f;
        [SerializeField] float flipDegrees = 360f;
        [SerializeField] float jumpCooldown = 0.55f;
        [SerializeField] float aiJumpIntervalMin = 1.4f;
        [SerializeField] float aiJumpIntervalMax = 3.6f;
        [SerializeField] float danceBobAmount = 0.12f;
        [SerializeField] float danceBobSpeed = 2.8f;
        [SerializeField] float danceYawAmount = 9f;
        [SerializeField] float danceYawSpeed = 1.6f;
        [SerializeField] float danceRollAmount = 6f;
        [SerializeField] float danceRollSpeed = 2.1f;

        Rigidbody rb;
        PlayerInputReader inputReader;
        CelebrationMode mode;
        Coroutine jumpRoutine;
        float lastJumpTime;
        float nextAiJumpTime;
        float danceTime;
        float dancePhase;
        Vector3 anchorPosition;
        Quaternion anchorRotation;
        bool anchorSet;
        bool isJumping;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            inputReader = GetComponent<PlayerInputReader>();
            dancePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void SetJumpEnabled(bool enabled)
        {
            if (!enabled)
            {
                StopJumpRoutine();
                mode = CelebrationMode.None;
                return;
            }

            mode = inputReader != null ? CelebrationMode.PlayerInput : CelebrationMode.AiAuto;
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

            StopJumpRoutine();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
            if (mode == CelebrationMode.None || rb == null || isJumping)
                return;

            if (mode == CelebrationMode.PlayerInput)
                UpdatePlayerJump();

            if (mode == CelebrationMode.AiAuto)
                UpdateAiJumpTimer();
        }

        void FixedUpdate()
        {
            if (mode == CelebrationMode.None || rb == null || !anchorSet || isJumping)
                return;

            ApplyDanceMotion();
        }

        void UpdatePlayerJump()
        {
            if (inputReader == null || Time.time - lastJumpTime < jumpCooldown)
                return;

            if (!inputReader.CelebrationJumpPressed)
                return;

            StartJump();
        }

        void UpdateAiJumpTimer()
        {
            if (Time.time < nextAiJumpTime || isJumping)
                return;

            StartJump();
            ScheduleNextAiJump();
        }

        void ScheduleNextAiJump()
        {
            nextAiJumpTime = Time.time + Random.Range(aiJumpIntervalMin, aiJumpIntervalMax);
        }

        void StartJump()
        {
            if (!anchorSet)
                CaptureAnchor();

            StopJumpRoutine();
            jumpRoutine = StartCoroutine(JumpArcRoutine());
            lastJumpTime = Time.time;
        }

        IEnumerator JumpArcRoutine()
        {
            isJumping = true;

            var startPos = transform.position;
            var startRot = transform.rotation;
            var height = mode == CelebrationMode.AiAuto
                ? jumpHeight * Random.Range(0.8f, 1.1f)
                : jumpHeight;
            var duration = mode == CelebrationMode.AiAuto
                ? jumpDuration * Random.Range(0.9f, 1.1f)
                : jumpDuration;
            var spin = mode == CelebrationMode.AiAuto
                ? flipDegrees * Random.Range(0.65f, 1.15f)
                : flipDegrees;
            var spinAxis = transform.right + transform.forward * Random.Range(-0.25f, 0.25f);
            if (spinAxis.sqrMagnitude < 0.01f)
                spinAxis = transform.right;
            spinAxis.Normalize();

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                var heightOffset = 4f * jumpHeight * eased * (1f - eased) * (height / jumpHeight);
                var pos = startPos + Vector3.up * heightOffset;
                var rot = Quaternion.AngleAxis(spin * eased, spinAxis) * startRot;

                rb.MovePosition(pos);
                rb.MoveRotation(rot);
                yield return new WaitForFixedUpdate();
            }

            anchorPosition = startPos;
            anchorRotation = startRot;
            rb.MovePosition(startPos);
            rb.MoveRotation(startRot);

            isJumping = false;
            jumpRoutine = null;
            danceTime = 0f;
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
        }

        void StopJumpRoutine()
        {
            if (jumpRoutine == null)
                return;

            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
            isJumping = false;
        }

        void OnDisable()
        {
            StopJumpRoutine();
        }
    }
}
