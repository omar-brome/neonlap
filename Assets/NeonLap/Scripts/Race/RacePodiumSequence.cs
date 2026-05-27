using System.Collections;
using System.Collections.Generic;
using NeonLap.Audio;
using NeonLap.Camera;
using NeonLap.Track;
using NeonLap.UI;
using NeonLap.Vehicle;
using NeonLap.VFX;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeonLap.Race
{
    public class RacePodiumSequence : MonoBehaviour
    {
        const float PodiumAutoContinueDelay = 12f;

        [SerializeField] RaceManager raceManager;
        [SerializeField] OvalTrackBuilder trackBuilder;
        [SerializeField] GameObject playerCar;
        [SerializeField] FollowCamera followCamera;
        [SerializeField] RaceUI raceUi;
        [SerializeField] GameObject finishPanel;
        [SerializeField] Text jumpHintText;
        [SerializeField] RaceReplaySystem replaySystem;

        readonly List<RacerProgress> podiumRacers = new();
        Transform podiumRoot;
        PodiumFireworksVFX fireworks;
        bool subscribed;
        bool sequenceActive;
        bool awaitingPodiumDismiss;
        int pendingPlayerPlacement;
        float podiumCelebrationStartTime;

        public bool IsAwaitingDismiss => awaitingPodiumDismiss;

        public void Configure(
            RaceManager manager,
            OvalTrackBuilder track,
            GameObject player,
            FollowCamera camera,
            RaceUI ui,
            GameObject finishPanelObject,
            Text jumpHint,
            RaceReplaySystem replay)
        {
            Unsubscribe();
            raceManager = manager;
            trackBuilder = track;
            playerCar = player;
            followCamera = camera;
            raceUi = ui;
            finishPanel = finishPanelObject;
            jumpHintText = jumpHint;
            replaySystem = replay;
            Subscribe();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            awaitingPodiumDismiss = false;
        }

        void Update()
        {
            if (!awaitingPodiumDismiss)
                return;

            var photoMode = FindAnyObjectByType<PhotoModeController>();
            if (photoMode != null && photoMode.IsActive)
            {
                RefreshPodiumHintWithCountdown();
                return;
            }

            // Use unscaled time so photo mode (timeScale=0) doesn't stall auto-continue.
            if (Time.unscaledTime - podiumCelebrationStartTime >= PodiumAutoContinueDelay)
            {
                ShowFinishMenu();
                return;
            }

            RefreshPodiumHintWithCountdown();

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.enterKey.wasPressedThisFrame)
                ShowFinishMenu();
        }

        void RefreshPodiumHintWithCountdown()
        {
            var remaining = Mathf.CeilToInt(
                Mathf.Max(0f, PodiumAutoContinueDelay - (Time.unscaledTime - podiumCelebrationStartTime)));
            var playerOnPodium = pendingPlayerPlacement <= 3;
            ShowPodiumHint(playerOnPodium
                ? $"P PHOTO  •  SPACE JUMP  •  ENTER ({remaining}s)"
                : $"P PHOTO  •  ENTER ({remaining}s)");
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void HandleRaceFinished(int playerPlacement)
        {
            if (sequenceActive)
                return;

            awaitingPodiumDismiss = false;
            HideFinishMenu();
            HidePodiumHint();

            StartCoroutine(PlayFinishSequence(playerPlacement));
        }

        IEnumerator PlayFinishSequence(int playerPlacement)
        {
            sequenceActive = true;
            pendingPlayerPlacement = playerPlacement;

            raceUi?.SetGameplayHudVisible(false);

            if (playerPlacement == 1 && replaySystem != null && replaySystem.HasRecording)
                yield return PlayP1SlowmoThenReplay();
            else if (replaySystem != null && replaySystem.HasRecording)
                yield return replaySystem.PlayBestOvertakeReplay();

            yield return PlayPodiumSequence(playerPlacement);
        }

        IEnumerator PlayP1SlowmoThenReplay()
        {
            var previousScale = Time.timeScale;
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = previousScale > 0.01f ? previousScale : 1f;
            yield return replaySystem.PlayBestOvertakeReplay();
        }

        IEnumerator PlayPodiumSequence(int playerPlacement)
        {
            yield return new WaitForSeconds(0.45f);

            var ranked = raceManager.GetRankedRacers();
            if (ranked.Count == 0)
            {
                ShowFinishMenu();
                sequenceActive = false;
                yield break;
            }

            DisableDrivingForAll();
            HideNonPodiumRacers(ranked);

            var podium = BuildPodiumScene();
            var topCount = Mathf.Min(3, ranked.Count);
            var moveDuration = 1.35f;

            for (var displayIndex = 0; displayIndex < topCount; displayIndex++)
            {
                var racer = GetRacerForDisplaySlot(ranked, displayIndex);
                if (racer == null)
                    continue;

                var slot = podium.Slots[displayIndex];
                var worldPos = podium.Root.TransformPoint(slot.LocalPosition + new Vector3(0f, 0.55f, -0.15f));
                var worldRot = podium.Root.rotation * slot.LocalRotation;
                yield return MoveRacerToSlot(racer.transform, worldPos, worldRot, moveDuration);
            }

            SetupPodiumCamera(podium);
            BeginPodiumCelebration(playerPlacement, ranked);

            sequenceActive = false;
        }

        static RacerProgress GetRacerForDisplaySlot(IReadOnlyList<RacerProgress> ranked, int displayIndex)
        {
            var rankIndex = displayIndex switch
            {
                0 => 1,
                1 => 0,
                2 => 2,
                _ => displayIndex
            };

            return rankIndex < ranked.Count ? ranked[rankIndex] : null;
        }

        void BeginPodiumCelebration(int playerPlacement, IReadOnlyList<RacerProgress> ranked)
        {
            HideFinishMenu();
            awaitingPodiumDismiss = true;
            podiumCelebrationStartTime = Time.unscaledTime;
            DynamicRaceMusicController.Instance?.EnterPodium();

            var playerOnPodium = playerPlacement <= 3;
            RefreshPodiumHintWithCountdown();

            StartFireworks();

            var topCount = Mathf.Min(3, ranked.Count);
            for (var i = 0; i < topCount; i++)
            {
                var racer = ranked[i];
                if (racer == null)
                    continue;

                var jump = racer.GetComponent<PodiumJumpController>();
                if (jump == null)
                    continue;

                if (racer.IsPlayer)
                    jump.EnableCelebration();
                else
                    jump.EnableAiCelebration();
            }
        }

        void StartFireworks()
        {
            StopFireworks();

            if (podiumRoot == null)
                return;

            var fireworksGo = new GameObject("PodiumFireworks");
            fireworksGo.transform.SetParent(podiumRoot, false);
            fireworks = fireworksGo.AddComponent<PodiumFireworksVFX>();
            fireworks.Configure(podiumRoot.position + Vector3.up * 1.5f, 11f);
        }

        void StopFireworks()
        {
            if (fireworks == null)
                return;

            Destroy(fireworks.gameObject);
            fireworks = null;
        }

        void ShowFinishMenu()
        {
            awaitingPodiumDismiss = false;
            HidePodiumHint();
            DisablePodiumCelebration();
            StopFireworks();
            DynamicRaceMusicController.Instance?.ExitPodium();

            raceUi?.SetGameplayHudVisible(false);
            raceUi?.ShowFinishPanel();

            if (finishPanel != null)
                finishPanel.SetActive(true);
        }

        void HideFinishMenu()
        {
            if (finishPanel != null)
                finishPanel.SetActive(false);
        }

        void ShowPodiumHint(string message)
        {
            if (jumpHintText == null)
                return;

            jumpHintText.gameObject.SetActive(true);
            jumpHintText.text = message;
        }

        void HidePodiumHint()
        {
            if (jumpHintText != null)
                jumpHintText.gameObject.SetActive(false);
        }

        void DisablePodiumCelebration()
        {
            foreach (var racer in podiumRacers)
            {
                if (racer == null)
                    continue;

                var jump = racer.GetComponent<PodiumJumpController>();
                jump?.SetJumpEnabled(false);

                var rb = racer.GetComponent<Rigidbody>();
                if (rb == null)
                    continue;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        PodiumBuilder.BuiltPodium BuildPodiumScene()
        {
            if (podiumRoot != null)
                Destroy(podiumRoot.gameObject);

            var forward = trackBuilder.StartRotation * Vector3.forward;
            var right = trackBuilder.StartRotation * Vector3.right;
            var trackWidth = trackBuilder.TrackWidth;
            var center = trackBuilder.StartPosition
                         - forward * (trackWidth * 0.65f + 16f)
                         + right * (trackWidth * 0.35f)
                         + Vector3.up * 0.05f;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var baseMat = new Material(lit);
            baseMat.SetColor("_BaseColor", new Color(0.06f, 0.07f, 0.12f));
            baseMat.SetFloat("_Smoothness", 0.55f);

            var accentMat = new Material(lit);
            accentMat.SetColor("_BaseColor", new Color(0.08f, 0.25f, 0.32f));
            accentMat.EnableKeyword("_EMISSION");
            accentMat.SetColor("_EmissionColor", new Color(0.15f, 1.2f, 1.5f));

            var built = PodiumBuilder.Build(center, rotation, baseMat, accentMat);
            podiumRoot = built.Root;
            return built;
        }

        IEnumerator MoveRacerToSlot(Transform racerTransform, Vector3 targetPosition, Quaternion targetRotation,
            float duration)
        {
            var rb = racerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            var startPosition = racerTransform.position;
            var startRotation = racerTransform.rotation;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                var pos = Vector3.Lerp(startPosition, targetPosition, t);
                var rot = Quaternion.Slerp(startRotation, targetRotation, t);

                if (rb != null)
                {
                    rb.MovePosition(pos);
                    rb.MoveRotation(rot);
                }
                else
                {
                    racerTransform.SetPositionAndRotation(pos, rot);
                }

                yield return null;
            }

            if (rb != null)
            {
                rb.MovePosition(targetPosition);
                rb.MoveRotation(targetRotation);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                racerTransform.SetPositionAndRotation(targetPosition, targetRotation);
            }
        }

        void DisableDrivingForAll()
        {
            foreach (var racer in raceManager.Racers)
            {
                if (racer == null)
                    continue;

                var vehicleController = racer.GetComponent<VehicleController>();
                if (vehicleController != null)
                    vehicleController.enabled = false;

                var aiController = racer.GetComponent<AIVehicleController>();
                if (aiController != null)
                    aiController.enabled = false;

                var jump = racer.GetComponent<PodiumJumpController>();
                jump?.SetJumpEnabled(false);

                var rb = racer.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }
        }

        void HideNonPodiumRacers(IReadOnlyList<RacerProgress> ranked)
        {
            podiumRacers.Clear();
            for (var i = 0; i < Mathf.Min(3, ranked.Count); i++)
                podiumRacers.Add(ranked[i]);

            foreach (var racer in raceManager.Racers)
            {
                if (podiumRacers.Contains(racer))
                    continue;

                racer.gameObject.SetActive(false);
            }
        }

        void SetupPodiumCamera(PodiumBuilder.BuiltPodium podium)
        {
            if (followCamera == null)
                return;

            followCamera.enabled = false;

            var cam = followCamera.GetComponent<UnityEngine.Camera>();
            if (cam == null)
                return;

            cam.transform.position = podium.CameraPosition;
            cam.transform.rotation = Quaternion.LookRotation(podium.LookTarget - podium.CameraPosition, Vector3.up);
            cam.fieldOfView = 52f;
        }
    }
}
