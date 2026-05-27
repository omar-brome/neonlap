using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Race
{
    /// <summary>
    /// Invisible pace reference ahead of the player on Easy when leading — softens AI progress rubber-banding.
    /// </summary>
    public class CatchUpGhostController : MonoBehaviour
    {
        const float ProgressLead = 0.055f;
        const float RubberBandDampScale = 0.32f;

        [SerializeField] float moveSmooth = 8f;

        Transform[] waypoints;
        RaceManager raceManager;
        Transform ghostTransform;
        bool ghostActive;

        public static CatchUpGhostController Instance { get; private set; }

        public bool IsActive => ghostActive;
        public float RubberBandDampScaleWhenActive => RubberBandDampScale;

        public static CatchUpGhostController Setup(Transform waypointRoot, RaceManager manager)
        {
            if (manager == null || waypointRoot == null)
                return null;

            var existing = FindAnyObjectByType<CatchUpGhostController>();
            if (existing != null)
            {
                existing.Configure(waypointRoot, manager);
                return existing;
            }

            var go = new GameObject("CatchUpGhost");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<CatchUpGhostController>();
            controller.Configure(waypointRoot, manager);
            return controller;
        }

        void Awake()
        {
            Instance = this;
            ghostTransform = new GameObject("GhostAnchor").transform;
            ghostTransform.SetParent(transform, false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(Transform waypointRoot, RaceManager manager)
        {
            raceManager = manager;
            if (waypointRoot == null)
            {
                waypoints = null;
                return;
            }

            var count = waypointRoot.childCount;
            waypoints = new Transform[count];
            for (var i = 0; i < count; i++)
                waypoints[i] = waypointRoot.GetChild(i);
        }

        void LateUpdate()
        {
            if (raceManager == null || waypoints == null || waypoints.Length < 2)
            {
                ghostActive = false;
                return;
            }

            var shouldRun = GameDifficultySettings.Current == DifficultyLevel.Easy
                            && raceManager.State == RaceState.Racing
                            && raceManager.GetPlayerPosition() == 1;

            ghostActive = shouldRun;
            if (!ghostActive)
                return;

            var playerProgress = raceManager.GetPlayerRaceProgress();
            var ghostProgress = Mathf.Clamp01(playerProgress + ProgressLead);
            var target = SamplePosition(ghostProgress);
            ghostTransform.position = Vector3.Lerp(ghostTransform.position, target,
                1f - Mathf.Exp(-moveSmooth * Time.deltaTime));
        }

        Vector3 SamplePosition(float progress01)
        {
            if (waypoints.Length == 0)
                return Vector3.zero;

            var totalSegments = waypoints.Length;
            var scaled = progress01 * totalSegments;
            var index = Mathf.FloorToInt(scaled) % totalSegments;
            var nextIndex = (index + 1) % totalSegments;
            var t = scaled - Mathf.Floor(scaled);

            var a = waypoints[index] != null ? waypoints[index].position : Vector3.zero;
            var b = waypoints[nextIndex] != null ? waypoints[nextIndex].position : a;
            return Vector3.Lerp(a, b, t) + Vector3.up * 1.2f;
        }
    }
}
