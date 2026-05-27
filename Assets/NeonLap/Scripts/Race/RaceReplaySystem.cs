using System.Collections;
using System.Collections.Generic;
using NeonLap.Camera;
using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeonLap.Race
{
    public class RaceReplaySystem : MonoBehaviour
    {
        struct ReplayFrame
        {
            public float Time;
            public Vector3 Position;
            public Quaternion Rotation;

            public ReplayFrameSnapshot ToSnapshot()
            {
                return ReplayFrameSnapshot.FromTransform(Time, Position, Rotation);
            }
        }

        sealed class RacerTrack
        {
            public Transform Transform;
            public Rigidbody Rigidbody;
            public bool IsPlayer;
            public readonly List<ReplayFrame> Frames = new();
        }

        enum ReplayCameraShot
        {
            Chase,
            Side,
            LowHero,
            Wide,
            FinishDrama,
            OvertakeDrama
        }

        struct OvertakeCandidate
        {
            public float Time;
            public float Score;
        }

        [SerializeField] float recordInterval = 0.1f;
        [SerializeField] float highlightDuration = 18f;
        [SerializeField] float overtakeWindowHalf = 2.75f;
        [SerializeField] float shotDuration = 4.2f;
        [SerializeField] float cameraSmooth = 6f;
        [SerializeField] float playbackSpeed = 1f;

        RaceManager raceManager;
        FollowCamera followCamera;
        Transform playerTransform;
        UnityEngine.Camera replayCamera;
        GameObject replayOverlay;
        Text replayTitleText;
        Text replayHintText;

        readonly List<RacerTrack> tracks = new();
        float recordAccumulator;
        float recordedDuration;
        float lastPlaybackStartTime;
        float lastPlaybackEndTime;
        float saveGhostFeedbackEndTime;
        bool recording;
        bool subscribed;

        public bool HasRecording => tracks.Count > 0 && recordedDuration > 0.5f;

        public float LastPlaybackStartTime => lastPlaybackStartTime;

        public float LastPlaybackEndTime => lastPlaybackEndTime;

        public List<ReplayFrameSnapshot> ExportPlayerFrames(float startTime, float endTime)
        {
            var result = new List<ReplayFrameSnapshot>();
            var player = tracks.Find(track => track.IsPlayer);
            if (player == null || player.Frames.Count == 0)
                return result;

            var minTime = Mathf.Max(0f, startTime);
            var maxTime = endTime > 0f ? endTime : recordedDuration;

            foreach (var frame in player.Frames)
            {
                if (frame.Time < minTime)
                    continue;

                if (frame.Time > maxTime)
                    break;

                result.Add(frame.ToSnapshot());
            }

            if (result.Count > 0)
            {
                var offset = result[0].Time;
                for (var i = 0; i < result.Count; i++)
                {
                    var frame = result[i];
                    frame.Time -= offset;
                    result[i] = frame;
                }
            }

            return result;
        }

        public GhostRecordingData ExportPlaybackHighlightGhost(float startTime, float endTime, float anchorRaceTime = 0f)
        {
            return GhostReplayBridge.BuildRecording(ExportPlayerFrames(startTime, endTime), anchorRaceTime);
        }

        public GhostRecordingData ExportLastPlaybackHighlightGhost()
        {
            return ExportPlaybackHighlightGhost(lastPlaybackStartTime, lastPlaybackEndTime);
        }

        public bool TrySaveLastPlaybackAsLapGhost(out string message)
        {
            message = string.Empty;
            if (!GhostReplayBridge.CanSaveToPbStore)
            {
                message = "GHOST SAVE — TIME TRIAL / GHOST DUEL ONLY";
                return false;
            }

            var recording = ExportLastPlaybackHighlightGhost();
            if (recording == null || !recording.IsValid)
            {
                message = "GHOST SAVE FAILED";
                return false;
            }

            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            if (!GhostReplayBridge.SaveHighlightAsLapGhost(trackIndex, recording))
            {
                message = "GHOST SAVE FAILED";
                return false;
            }

            message = $"GHOST SAVED — LAP {TimeTrialRecordStore.FormatTime(recording.Duration)}";
            return true;
        }

        public void Configure(RaceManager manager, FollowCamera camera, GameObject playerCar, Transform uiRoot)
        {
            Unsubscribe();
            raceManager = manager;
            followCamera = camera;
            playerTransform = playerCar != null ? playerCar.transform : null;
            replayCamera = camera != null ? camera.GetComponent<UnityEngine.Camera>() : UnityEngine.Camera.main;
            BuildOverlay(uiRoot);
            Subscribe();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            HideOverlay();
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Racing)
                BeginRecording();
            else if (state == RaceState.Countdown)
                ClearRecording();
        }

        void Update()
        {
            if (!recording || raceManager == null || raceManager.State != RaceState.Racing)
                return;

            recordAccumulator += Time.deltaTime;
            while (recordAccumulator >= recordInterval)
            {
                recordAccumulator -= recordInterval;
                recordedDuration += recordInterval;
                CaptureFrame(recordedDuration);
            }
        }

        void BeginRecording()
        {
            ClearRecording();
            recording = true;
            recordAccumulator = 0f;

            tracks.Clear();
            foreach (var racer in raceManager.Racers)
            {
                if (racer == null || racer.IsEliminated)
                    continue;

                tracks.Add(new RacerTrack
                {
                    Transform = racer.transform,
                    Rigidbody = racer.GetComponent<Rigidbody>(),
                    IsPlayer = racer.IsPlayer
                });
            }
        }

        void ClearRecording()
        {
            recording = false;
            recordAccumulator = 0f;
            recordedDuration = 0f;
            tracks.Clear();
        }

        void CaptureFrame(float time)
        {
            for (var i = tracks.Count - 1; i >= 0; i--)
            {
                var track = tracks[i];
                if (track.Transform == null || !track.Transform.gameObject.activeInHierarchy)
                {
                    tracks.RemoveAt(i);
                    continue;
                }

                track.Frames.Add(new ReplayFrame
                {
                    Time = time,
                    Position = track.Transform.position,
                    Rotation = track.Transform.rotation
                });
            }
        }

        public bool TryFindBestOvertake(out float centerTime)
        {
            centerTime = 0f;
            var player = tracks.Find(track => track.IsPlayer);
            if (player == null || player.Frames.Count < 4)
                return false;

            OvertakeCandidate? best = null;
            foreach (var rival in tracks)
            {
                if (rival.IsPlayer || rival.Frames.Count < 4)
                    continue;

                DetectOvertakes(player, rival, ref best);
            }

            if (!best.HasValue)
                return false;

            centerTime = best.Value.Time;
            return true;
        }

        public IEnumerator PlayBestOvertakeReplay()
        {
            if (!HasRecording || playerTransform == null || replayCamera == null)
                yield break;

            if (!TryFindBestOvertake(out var centerTime))
            {
                yield return PlayHighlightReplay();
                yield break;
            }

            var startTime = Mathf.Max(0f, centerTime - overtakeWindowHalf);
            var endTime = Mathf.Min(recordedDuration, centerTime + overtakeWindowHalf);
            if (endTime - startTime < 1.5f)
            {
                yield return PlayHighlightReplay();
                yield break;
            }

            yield return PlayReplayWindow(startTime, endTime, "OVERTAKE", true);
        }

        public IEnumerator PlayHighlightReplay()
        {
            if (!HasRecording || playerTransform == null || replayCamera == null)
                yield break;

            var endTime = recordedDuration;
            var startTime = Mathf.Max(0f, endTime - highlightDuration);
            yield return PlayReplayWindow(startTime, endTime, "RACE REPLAY", false);
        }

        IEnumerator PlayReplayWindow(float startTime, float endTime, string title, bool overtakeFocus)
        {
            lastPlaybackStartTime = startTime;
            lastPlaybackEndTime = endTime;
            saveGhostFeedbackEndTime = 0f;

            PrepareRacersForPlayback();
            FreezeDriving();

            if (followCamera != null)
                followCamera.enabled = false;

            Camera.CameraSpectacleDirector.Instance?.SetReplayActive(true);

            if (replayTitleText != null)
                replayTitleText.text = title;

            ShowOverlay();

            var duration = endTime - startTime;
            var playbackTime = startTime;
            var previousShot = overtakeFocus
                ? ReplayCameraShot.OvertakeDrama
                : GetShotForPlaybackTime(0f, duration);

            ApplyTracksAtTime(startTime);

            UpdateReplayHint();

            while (playbackTime < endTime)
            {
                if (WasSkipPressed())
                    break;

                if (WasSaveGhostPressed() && TrySaveLastPlaybackAsLapGhost(out var saveMessage))
                {
                    saveGhostFeedbackEndTime = Time.time + 2.5f;
                    if (replayHintText != null)
                        replayHintText.text = saveMessage;
                }

                playbackTime += Time.deltaTime * playbackSpeed;
                var clampedTime = Mathf.Min(playbackTime, endTime);
                ApplyTracksAtTime(clampedTime);

                var shotTime = clampedTime - startTime;
                var shot = overtakeFocus
                    ? ReplayCameraShot.OvertakeDrama
                    : GetShotForPlaybackTime(shotTime, duration);
                UpdateCinematicCamera(shot, previousShot, shotTime, duration);
                previousShot = shot;

                yield return null;
            }

            ApplyTracksAtTime(endTime);
            HideOverlay();
            Camera.CameraSpectacleDirector.Instance?.SetReplayActive(false);

            if (followCamera != null)
                followCamera.enabled = true;
        }

        static void DetectOvertakes(RacerTrack player, RacerTrack rival, ref OvertakeCandidate? best)
        {
            var rivalWasAhead = false;
            for (var i = 1; i < player.Frames.Count; i++)
            {
                var frame = player.Frames[i];
                if (!TrySampleTrackAtTime(rival.Frames, frame.Time, out var rivalPosition, out _))
                    continue;

                var offset = rivalPosition - frame.Position;
                offset.y = 0f;
                if (offset.sqrMagnitude < 4f)
                    continue;

                var forward = frame.Rotation * Vector3.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f)
                    continue;

                forward.Normalize();
                var rivalAhead = Vector3.Dot(forward, offset) > 0f;
                if (rivalWasAhead && !rivalAhead)
                {
                    var score = 1f / Mathf.Max(offset.magnitude, 5f);
                    if (!best.HasValue || score > best.Value.Score)
                        best = new OvertakeCandidate { Time = frame.Time, Score = score };
                }

                rivalWasAhead = rivalAhead;
            }
        }

        static bool TrySampleTrackAtTime(List<ReplayFrame> frames, float time, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (frames == null || frames.Count == 0)
                return false;

            SampleTrack(frames, time, out position, out rotation);
            return true;
        }

        static bool WasSkipPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }

        static bool WasSaveGhostPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.gKey.wasPressedThisFrame;
        }

        void UpdateReplayHint()
        {
            if (replayHintText == null)
                return;

            if (Time.time < saveGhostFeedbackEndTime)
                return;

            replayHintText.text = GhostReplayBridge.CanSaveToPbStore
                ? "SPACE SKIP  •  G SAVE HIGHLIGHT AS LAP GHOST"
                : "SPACE TO SKIP";
        }

        void PrepareRacersForPlayback()
        {
            foreach (var track in tracks)
            {
                if (track.Transform == null)
                    continue;

                if (!track.Transform.gameObject.activeSelf)
                    track.Transform.gameObject.SetActive(true);
            }
        }

        void FreezeDriving()
        {
            foreach (var track in tracks)
            {
                if (track.Transform == null)
                    continue;

                var vehicle = track.Transform.GetComponent<VehicleController>();
                if (vehicle != null)
                    vehicle.enabled = false;

                var ai = track.Transform.GetComponent<AIVehicleController>();
                if (ai != null)
                    ai.enabled = false;

                if (track.Rigidbody == null)
                    track.Rigidbody = track.Transform.GetComponent<Rigidbody>();

                if (track.Rigidbody == null)
                    continue;

                track.Rigidbody.linearVelocity = Vector3.zero;
                track.Rigidbody.angularVelocity = Vector3.zero;
                track.Rigidbody.isKinematic = true;
            }
        }

        void ApplyTracksAtTime(float time)
        {
            foreach (var track in tracks)
            {
                if (track.Transform == null || track.Frames.Count == 0)
                    continue;

                SampleTrack(track.Frames, time, out var position, out var rotation);

                if (track.Rigidbody != null)
                {
                    track.Rigidbody.MovePosition(position);
                    track.Rigidbody.MoveRotation(rotation);
                }
                else
                {
                    track.Transform.SetPositionAndRotation(position, rotation);
                }
            }
        }

        static void SampleTrack(List<ReplayFrame> frames, float time, out Vector3 position, out Quaternion rotation)
        {
            var snapshots = new List<ReplayFrameSnapshot>(frames.Count);
            for (var i = 0; i < frames.Count; i++)
                snapshots.Add(frames[i].ToSnapshot());

            GhostPlaybackSampler.Sample(snapshots, time, out position, out rotation);
        }

        ReplayCameraShot GetShotForPlaybackTime(float shotTime, float totalDuration)
        {
            if (totalDuration <= shotDuration * 1.5f)
                return ReplayCameraShot.Chase;

            if (shotTime >= totalDuration - shotDuration * 0.85f)
                return ReplayCameraShot.FinishDrama;

            var index = Mathf.FloorToInt(shotTime / shotDuration);
            return (ReplayCameraShot)(index % 4);
        }

        void UpdateCinematicCamera(ReplayCameraShot shot, ReplayCameraShot previousShot, float shotTime,
            float totalDuration)
        {
            var focus = playerTransform;
            if (focus == null)
                return;

            var localOffset = GetShotOffset(shot);
            var previousOffset = GetShotOffset(previousShot);

            var shotProgress = (shotTime % shotDuration) / shotDuration;
            var transition = shot == previousShot
                ? 1f
                : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shotProgress / 0.28f));

            var blendedLocalOffset = Vector3.Lerp(previousOffset, localOffset, transition);
            var desiredPosition = focus.TransformPoint(blendedLocalOffset);
            var lookTarget = focus.position + Vector3.up * 1.1f + focus.forward * 2f;
            var desiredRotation = Quaternion.LookRotation(lookTarget - desiredPosition, Vector3.up);
            var desiredFov = GetShotFov(shot);

            var camTransform = replayCamera.transform;
            camTransform.position = Vector3.Lerp(camTransform.position, desiredPosition, cameraSmooth * Time.deltaTime);
            camTransform.rotation = Quaternion.Slerp(camTransform.rotation, desiredRotation,
                cameraSmooth * Time.deltaTime);
            replayCamera.fieldOfView = Mathf.Lerp(replayCamera.fieldOfView, desiredFov, cameraSmooth * Time.deltaTime);
        }

        static Vector3 GetShotOffset(ReplayCameraShot shot)
        {
            return shot switch
            {
                ReplayCameraShot.Chase => new Vector3(0f, 4.2f, -13f),
                ReplayCameraShot.Side => new Vector3(9f, 2.4f, -3f),
                ReplayCameraShot.LowHero => new Vector3(-3.5f, 1.4f, 7f),
                ReplayCameraShot.Wide => new Vector3(0f, 10f, -22f),
                ReplayCameraShot.FinishDrama => new Vector3(2f, 3.2f, -16f),
                ReplayCameraShot.OvertakeDrama => new Vector3(7.5f, 2.1f, -2.5f),
                _ => new Vector3(0f, 4f, -12f)
            };
        }

        static float GetShotFov(ReplayCameraShot shot)
        {
            return shot switch
            {
                ReplayCameraShot.Wide => 68f,
                ReplayCameraShot.LowHero => 58f,
                ReplayCameraShot.FinishDrama => 52f,
                ReplayCameraShot.OvertakeDrama => 56f,
                _ => 62f
            };
        }

        void BuildOverlay(Transform uiRoot)
        {
            if (uiRoot == null || replayOverlay != null)
                return;

            replayOverlay = new GameObject("ReplayOverlay");
            replayOverlay.transform.SetParent(uiRoot, false);

            var rect = replayOverlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var backdrop = replayOverlay.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.12f);
            backdrop.raycastTarget = false;

            replayTitleText = CreateOverlayText(replayOverlay.transform, "ReplayTitle", "RACE REPLAY",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), 42,
                new Color(0.45f, 1f, 1f), FontStyle.Bold);

            replayHintText = CreateOverlayText(replayOverlay.transform, "ReplayHint", "SPACE TO SKIP",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), 20,
                new Color(0.85f, 0.9f, 1f, 0.85f), FontStyle.Normal);
            var hintRect = replayHintText.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(900f, 56f);

            replayOverlay.SetActive(false);
        }

        static Text CreateOverlayText(Transform parent, string name, string content, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, int fontSize, Color color, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(640f, 56f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        void ShowOverlay()
        {
            if (replayOverlay != null)
                replayOverlay.SetActive(true);
        }

        void HideOverlay()
        {
            if (replayOverlay != null)
                replayOverlay.SetActive(false);
        }
    }
}
