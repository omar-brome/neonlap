using NeonLap.Input;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class FollowCamera : MonoBehaviour
    {
        const string ModePrefKey = "NeonLap.CameraMode";

        struct ModeProfile
        {
            public Vector3 Offset;
            public bool UseCarRotation;
            public float LookAhead;
            public float LookHeight;
            public float Pitch;
            public float MaxRoll;
            public float BaseFov;
            public float MaxFov;
            public float PositionSmooth;
            public float RotationSmooth;
        }

        [SerializeField] Transform target;
        [SerializeField] FollowCameraMode mode = FollowCameraMode.ThirdPerson;

        UnityEngine.Camera cam;
        VehicleController vehicle;
        PlayerInputReader inputReader;
        Vector3 smoothedOffset;
        float modeSwitchCooldown;
        float lookBackBlend;

        public bool IsLookingBack => lookBackBlend > 0.05f;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                vehicle = target != null ? target.GetComponent<VehicleController>() : null;
                inputReader = target != null ? target.GetComponent<PlayerInputReader>() : null;
            }
        }

        public FollowCameraMode Mode => mode;

        public string ModeLabel => mode switch
        {
            FollowCameraMode.FirstPerson => "HOOD CAM",
            FollowCameraMode.CloseThirdPerson => "CLOSE CAM",
            _ => "FOLLOW CAM",
        };

        void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            mode = (FollowCameraMode)Mathf.Clamp(PlayerPrefs.GetInt(ModePrefKey, (int)FollowCameraMode.ThirdPerson), 0, 2);
            smoothedOffset = GetProfile(mode).Offset;
        }

        void LateUpdate()
        {
            if (target == null || !enabled)
                return;

            if (CameraSpectacleDirector.Instance != null && CameraSpectacleDirector.Instance.IsOverridingFollow)
                return;

            if (vehicle == null)
                vehicle = target.GetComponent<VehicleController>();
            if (inputReader == null)
                inputReader = target.GetComponent<PlayerInputReader>();

            if (modeSwitchCooldown > 0f)
                modeSwitchCooldown -= Time.deltaTime;

            var switchCamera = inputReader != null && inputReader.SwitchCameraPressed;
            var composite = target.GetComponent<CompositeVehicleInputProvider>();
            if (composite != null && composite.SwitchCameraPressed)
                switchCamera = true;

            if (switchCamera && modeSwitchCooldown <= 0f)
            {
                CycleMode();
                modeSwitchCooldown = 0.2f;
            }

            var lookBackHeld = inputReader != null && inputReader.LookBackHeld;
            var targetLookBack = lookBackHeld ? 1f : 0f;
            lookBackBlend = Mathf.MoveTowards(lookBackBlend, targetLookBack, 10f * Time.deltaTime);

            var profile = GetProfile(mode);
            var baseOffset = profile.Offset;
            var lookBackOffset = new Vector3(baseOffset.x, baseOffset.y, Mathf.Abs(baseOffset.z));
            var targetOffset = Vector3.Lerp(baseOffset, lookBackOffset, lookBackBlend);
            smoothedOffset = Vector3.Lerp(smoothedOffset, targetOffset, profile.PositionSmooth * Time.deltaTime);

            var speed = vehicle != null ? vehicle.CurrentSpeed : 0f;
            var steer = vehicle != null ? vehicle.SteerInput : 0f;
            var maxSpeed = vehicle != null && vehicle.Profile != null ? vehicle.Profile.maxSpeed : 45f;
            var speedRatio = Mathf.Clamp01(speed / Mathf.Max(maxSpeed, 1f));
            var rollScale = Mathf.Lerp(1f, 0.25f, lookBackBlend);

            var desiredPosition = target.TransformPoint(smoothedOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, profile.PositionSmooth * Time.deltaTime);

            Quaternion desiredRotation;
            if (profile.UseCarRotation)
            {
                var pitch = profile.Pitch - speedRatio * 1.8f;
                var roll = -steer * profile.MaxRoll * rollScale;
                var yaw = Mathf.Lerp(0f, 180f, lookBackBlend);
                desiredRotation = target.rotation * Quaternion.Euler(pitch, yaw, roll);
            }
            else
            {
                var forwardLookTarget = target.position
                                        + target.forward * profile.LookAhead
                                        + Vector3.up * profile.LookHeight;
                var rearLookTarget = target.position
                                     - target.forward * (profile.LookAhead + 8f)
                                     + Vector3.up * profile.LookHeight;
                var lookTarget = Vector3.Lerp(forwardLookTarget, rearLookTarget, lookBackBlend);
                var lookRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
                var roll = Quaternion.Euler(0f, 0f, -steer * profile.MaxRoll * rollScale);
                desiredRotation = lookRotation * roll;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, profile.RotationSmooth * Time.deltaTime);

            var fovT = Mathf.Clamp01(speed / Mathf.Max(maxSpeed, 1f));
            var baseFov = Mathf.Lerp(profile.BaseFov, profile.BaseFov + 6f, lookBackBlend);
            cam.fieldOfView = Mathf.Lerp(baseFov, profile.MaxFov, fovT);
        }

        public void CycleMode()
        {
            var next = (int)mode + 1;
            if (next > (int)FollowCameraMode.CloseThirdPerson)
                next = 0;
            SetMode((FollowCameraMode)next);
        }

        public void SetMode(FollowCameraMode newMode)
        {
            mode = newMode;
            PlayerPrefs.SetInt(ModePrefKey, (int)mode);
            PlayerPrefs.Save();
            smoothedOffset = GetProfile(mode).Offset;
        }

        static ModeProfile GetProfile(FollowCameraMode cameraMode)
        {
            return cameraMode switch
            {
                FollowCameraMode.FirstPerson => new ModeProfile
                {
                    Offset = new Vector3(0f, 1.05f, 0.72f),
                    UseCarRotation = true,
                    Pitch = -2.5f,
                    MaxRoll = 2.5f,
                    BaseFov = 74f,
                    MaxFov = 82f,
                    PositionSmooth = 14f,
                    RotationSmooth = 14f,
                },
                FollowCameraMode.CloseThirdPerson => new ModeProfile
                {
                    Offset = new Vector3(0f, 2.35f, -4.8f),
                    UseCarRotation = false,
                    LookAhead = 3.5f,
                    LookHeight = 1.1f,
                    MaxRoll = 4f,
                    BaseFov = 62f,
                    MaxFov = 76f,
                    PositionSmooth = 11f,
                    RotationSmooth = 12f,
                },
                _ => new ModeProfile
                {
                    Offset = new Vector3(0f, 4f, -10f),
                    UseCarRotation = false,
                    LookAhead = 1.5f,
                    LookHeight = 1.4f,
                    MaxRoll = 5f,
                    BaseFov = 60f,
                    MaxFov = 75f,
                    PositionSmooth = 8f,
                    RotationSmooth = 10f,
                },
            };
        }
    }
}
