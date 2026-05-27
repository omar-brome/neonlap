using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    public enum GhostPlaybackMode
    {
        Lap = 0,
        Race = 1,
    }

    public class GhostRacer : MonoBehaviour
    {
        RaceManager raceManager;
        List<ReplayFrameSnapshot> frames = new();
        float duration;
        float raceTimeAnchor;
        GhostPlaybackMode playbackMode = GhostPlaybackMode.Lap;
        bool active;
        Renderer[] renderers;
        Collider ghostCollider;
        GhostCollisionTrigger collisionTrigger;
        bool isDevGhost;

        public bool HasGhost => frames.Count >= 2;
        public GhostPlaybackMode PlaybackMode => playbackMode;
        public bool IsVisible { get; private set; } = true;
        public bool IsDevGhost => isDevGhost;
        public IReadOnlyList<ReplayFrameSnapshot> Frames => frames;

        public static GhostRacer Spawn(
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            GhostRecordingData recording,
            RaceManager manager,
            Material bodyTemplate,
            Material accentTemplate,
            GhostPlaybackMode mode = GhostPlaybackMode.Lap,
            Color? bodyColor = null,
            Color? accentColor = null,
            string objectName = "GhostRacer",
            bool isDev = false)
        {
            if (recording == null || !recording.IsValid || manager == null)
                return null;

            var ghostRoot = new GameObject(objectName);
            ghostRoot.transform.SetParent(parent, false);
            ghostRoot.transform.SetPositionAndRotation(position, rotation);

            if (bodyColor.HasValue && accentColor.HasValue)
            {
                GhostVisualBuilder.Build(ghostRoot.transform, bodyTemplate, accentTemplate, bodyColor.Value,
                    accentColor.Value, 1f);
            }
            else
            {
                GhostVisualBuilder.Build(ghostRoot.transform, bodyTemplate, accentTemplate, 0.42f);
            }

            var ghost = ghostRoot.AddComponent<GhostRacer>();
            ghost.Configure(manager, recording.ToFrames(), recording.Duration, mode, isDev, recording.AnchorRaceTime);
            ghost.EnsureCollisionVolume();
            return ghost;
        }

        public void Configure(RaceManager manager, List<ReplayFrameSnapshot> playbackFrames, float playbackDuration,
            GhostPlaybackMode mode = GhostPlaybackMode.Lap, bool devGhost = false, float anchorRaceTime = 0f)
        {
            raceManager = manager;
            frames = playbackFrames ?? new List<ReplayFrameSnapshot>();
            duration = Mathf.Max(playbackDuration, 0.1f);
            playbackMode = mode;
            raceTimeAnchor = Mathf.Max(0f, anchorRaceTime);
            isDevGhost = devGhost;
            active = frames.Count >= 2;
            renderers = GetComponentsInChildren<Renderer>();
            ApplyVisibility(TimeTrialSettings.GhostVisible);
        }

        public void SetVisible(bool visible)
        {
            ApplyVisibility(visible);
        }

        public bool TryGetDeltaSeconds(Vector3 playerPosition, out float deltaSeconds)
        {
            deltaSeconds = 0f;
            if (!active || !IsVisible)
                return false;

            return GhostPlaybackDelta.TryComputeSeconds(frames, playerPosition, GetReferenceTime(), out deltaSeconds);
        }

        public float GetReferenceTime()
        {
            if (raceManager == null)
                return 0f;

            if (playbackMode == GhostPlaybackMode.Race)
                return raceManager.RaceTime;

            var lapTime = raceManager.LapTime;
            return lapTime > 0.05f ? lapTime : raceManager.RaceTime;
        }

        void LateUpdate()
        {
            if (!active || !IsVisible || raceManager == null || raceManager.State != RaceState.Racing)
                return;

            var sampleTime = ResolveSampleTime();
            GhostPlaybackSampler.Sample(frames, sampleTime, out var position, out var rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        float ResolveSampleTime()
        {
            if (duration <= 0.05f)
                return 0f;

            if (playbackMode == GhostPlaybackMode.Race)
            {
                var raceTime = raceManager.RaceTime;
                if (raceTimeAnchor > 0.01f)
                {
                    if (raceTime < raceTimeAnchor)
                        return 0f;

                    return Mathf.Clamp(raceTime - raceTimeAnchor, 0f, duration);
                }

                return Mathf.Clamp(raceTime, 0f, duration);
            }

            var lapTime = raceManager.LapTime;
            if (lapTime > 0.05f)
                return Mathf.Clamp(lapTime, 0f, duration);

            return Mathf.Clamp(raceManager.RaceTime, 0f, duration);
        }

        void ApplyVisibility(bool visible)
        {
            IsVisible = visible;
            if (renderers == null)
                renderers = GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }

            if (ghostCollider != null)
                ghostCollider.enabled = visible && TimeTrialSettings.GhostCollisionPenalty;
        }

        void EnsureCollisionVolume()
        {
            var triggerGo = new GameObject("GhostCollision");
            triggerGo.transform.SetParent(transform, false);
            triggerGo.transform.localPosition = Vector3.zero;

            ghostCollider = triggerGo.AddComponent<BoxCollider>();
            ghostCollider.isTrigger = true;
            if (ghostCollider is BoxCollider box)
                box.size = new Vector3(2.6f, 1.4f, 4.8f);

            collisionTrigger = triggerGo.AddComponent<GhostCollisionTrigger>();
            collisionTrigger.Configure(this);
            ghostCollider.enabled = TimeTrialSettings.GhostCollisionPenalty;
        }
    }
}
