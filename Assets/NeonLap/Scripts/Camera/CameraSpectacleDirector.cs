using NeonLap.Environment;
using NeonLap.Input;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Camera
{
    /// <summary>
    /// Layered camera overrides: drone helicopter view, police pursuit cam, then FollowCamera.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class CameraSpectacleDirector : MonoBehaviour
    {
        const float PoliceEngageDistance = 38f;
        const float PoliceFullBlendDistance = 16f;
        const float DroneViewDuration = 2f;

        [SerializeField] float droneHeightOffset = 5f;
        [SerializeField] float droneLookAhead = 6f;
        [SerializeField] float policeHeight = 3.2f;
        [SerializeField] float policeBehindDistance = 7f;
        [SerializeField] float policeSideBias = 4.5f;
        [SerializeField] float blendSmooth = 9f;

        FollowCamera followCamera;
        UnityEngine.Camera unityCamera;
        Transform playerTarget;
        PlayerInputReader inputReader;
        PoliceChaseSystem policeChase;
        RaceManager raceManager;

        float droneEndTime;
        bool replayActive;

        public static CameraSpectacleDirector Instance { get; private set; }

        public bool IsOverridingFollow => replayActive || IsDroneActive || GetPoliceBlend() > 0.05f;

        public bool IsDroneActive => Time.time < droneEndTime;

        public float PoliceBlend => GetPoliceBlend();

        public string GetSpectacleLabel()
        {
            if (replayActive)
                return null;

            if (IsDroneActive)
                return "DRONE CAM";

            if (GetPoliceBlend() > 0.05f)
                return "PURSUIT CAM";

            return null;
        }

        public static CameraSpectacleDirector Setup(
            UnityEngine.Camera camera,
            FollowCamera follow,
            Transform player,
            PoliceChaseSystem police,
            RaceManager manager)
        {
            if (camera == null)
                return null;

            var director = camera.GetComponent<CameraSpectacleDirector>();
            if (director == null)
                director = camera.gameObject.AddComponent<CameraSpectacleDirector>();

            director.Configure(follow, player, police, manager);
            return director;
        }

        public void Configure(FollowCamera follow, Transform player, PoliceChaseSystem police, RaceManager manager)
        {
            followCamera = follow;
            unityCamera = follow != null ? follow.GetComponent<UnityEngine.Camera>() : GetComponent<UnityEngine.Camera>();
            playerTarget = player;
            policeChase = police;
            raceManager = manager;
            inputReader = player != null ? player.GetComponent<PlayerInputReader>() : null;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetReplayActive(bool active)
        {
            replayActive = active;
            if (followCamera != null)
                followCamera.enabled = !active && !IsOverridingFollow;
        }

        void LateUpdate()
        {
            if (replayActive || playerTarget == null || unityCamera == null)
                return;

            if (raceManager != null && raceManager.State != RaceState.Racing)
            {
                if (followCamera != null && !followCamera.enabled && Time.time >= droneEndTime)
                    followCamera.enabled = true;
                return;
            }

            if (inputReader == null)
                inputReader = playerTarget.GetComponent<PlayerInputReader>();

            var composite = playerTarget.GetComponent<CompositeVehicleInputProvider>();
            var dronePressed = inputReader != null && inputReader.DroneCamPressed;
            if (composite != null && composite.DroneCamPressed)
                dronePressed = true;

            if (dronePressed && PatrolHelicopter.Active != null)
                BeginDroneView();

            var policeBlend = GetPoliceBlend();
            var droneActive = Time.time < droneEndTime;

            if (droneActive)
            {
                ApplyDroneCamera();
                if (followCamera != null)
                    followCamera.enabled = false;
                return;
            }

            if (policeBlend > 0.01f && policeChase != null &&
                policeChase.TryGetClosestUnit(playerTarget.position, out var policeUnit, out _))
            {
                ApplyPoliceChaseCamera(policeUnit, policeBlend);
                if (followCamera != null)
                    followCamera.enabled = false;
                return;
            }

            if (followCamera != null && !followCamera.enabled)
                followCamera.enabled = true;
        }

        void BeginDroneView()
        {
            if (unityCamera == null)
                return;

            droneEndTime = Time.time + DroneViewDuration;
        }

        void ApplyDroneCamera()
        {
            var helicopter = PatrolHelicopter.Active;
            if (helicopter == null || playerTarget == null)
                return;

            var heliTransform = helicopter.transform;
            var heliForward = heliTransform.forward;
            heliForward.y = 0f;
            if (heliForward.sqrMagnitude < 0.01f)
                heliForward = Vector3.forward;
            heliForward.Normalize();

            var desiredPosition = heliTransform.position
                                  - heliForward * 4f
                                  + Vector3.up * droneHeightOffset;
            var lookTarget = playerTarget.position + playerTarget.forward * droneLookAhead + Vector3.up * 1.2f;

            var camTransform = unityCamera.transform;
            camTransform.position = Vector3.Lerp(camTransform.position, desiredPosition, blendSmooth * Time.deltaTime);
            var desiredRotation = Quaternion.LookRotation(lookTarget - camTransform.position, Vector3.up);
            camTransform.rotation = Quaternion.Slerp(camTransform.rotation, desiredRotation, blendSmooth * Time.deltaTime);
            unityCamera.fieldOfView = Mathf.Lerp(unityCamera.fieldOfView, 58f, blendSmooth * Time.deltaTime);
        }

        void ApplyPoliceChaseCamera(Transform policeUnit, float blend)
        {
            var forward = playerTarget.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var toPolice = policeUnit.position - playerTarget.position;
            toPolice.y = 0f;
            var side = Vector3.Cross(Vector3.up, forward).normalized;
            if (Vector3.Dot(side, toPolice) < 0f)
                side = -side;

            var desiredPosition = playerTarget.position
                                  - forward * policeBehindDistance
                                  + side * policeSideBias
                                  + Vector3.up * policeHeight;
            var lookTarget = playerTarget.position + forward * 2f + Vector3.up * 1f;

            var camTransform = unityCamera.transform;
            camTransform.position = Vector3.Lerp(camTransform.position, desiredPosition, blendSmooth * blend * Time.deltaTime);
            var desiredRotation = Quaternion.LookRotation(lookTarget - camTransform.position, Vector3.up);
            camTransform.rotation = Quaternion.Slerp(camTransform.rotation, desiredRotation,
                blendSmooth * blend * Time.deltaTime);
            unityCamera.fieldOfView = Mathf.Lerp(unityCamera.fieldOfView, 54f, blendSmooth * blend * Time.deltaTime);
        }

        float GetPoliceBlend()
        {
            if (policeChase == null || !policeChase.HasActiveUnits || playerTarget == null)
                return 0f;

            if (!policeChase.TryGetClosestUnit(playerTarget.position, out _, out var distance))
                return 0f;

            if (distance > PoliceEngageDistance)
                return 0f;

            return Mathf.Clamp01(1f - Mathf.InverseLerp(PoliceFullBlendDistance, PoliceEngageDistance, distance));
        }
    }
}
