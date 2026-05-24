using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Audio
{
    [RequireComponent(typeof(VehicleController))]
    public class VehicleAudioController : MonoBehaviour
    {
        [SerializeField] AudioSource engineSource;
        [SerializeField] AudioSource driftSource;
        [SerializeField] AudioSource windSource;
        [SerializeField] float minEnginePitch = 0.8f;
        [SerializeField] float maxEnginePitch = 2f;
        [SerializeField] float maxSpeedReference = 45f;

        VehicleController vehicle;

        void Awake()
        {
            vehicle = GetComponent<VehicleController>();
        }

        void Update()
        {
            if (vehicle == null)
                return;

            var speedRatio = Mathf.Clamp01(vehicle.CurrentSpeed / maxSpeedReference);

            if (engineSource != null && engineSource.clip != null)
            {
                if (!engineSource.isPlaying)
                    engineSource.Play();
                engineSource.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedRatio);
                engineSource.volume = Mathf.Lerp(0.3f, 1f, speedRatio);
            }

            if (windSource != null && windSource.clip != null)
            {
                if (!windSource.isPlaying)
                    windSource.Play();
                windSource.volume = speedRatio * 0.6f;
            }

            if (driftSource != null)
            {
                if (vehicle.IsDrifting && driftSource.clip != null && !driftSource.isPlaying)
                    driftSource.Play();
                else if (!vehicle.IsDrifting && driftSource.isPlaying)
                    driftSource.Stop();
            }
        }
    }
}
