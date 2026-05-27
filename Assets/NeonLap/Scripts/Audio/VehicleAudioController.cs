using NeonLap.Core;
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
        [SerializeField] AudioSource nitroSource;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] float minEnginePitch = 0.75f;
        [SerializeField] float maxEnginePitch = 1.85f;
        [SerializeField] float maxSpeedReference = 45f;
        [SerializeField] float heavyImpactSpeed = 14f;

        VehicleController vehicle;
        VehicleNitroBoost nitroBoost;
        bool nitroPlaying;
        float engineVolumeMax = 0.72f;
        float engineVolumeScale = 1f;
        bool rivalMix;

        public static VehicleAudioController Setup(GameObject car, bool spatial3D = true, bool useRivalMix = false)
        {
            NeonLapAudioLibrary.Preload();
            var audio = car.GetComponent<VehicleAudioController>();
            if (audio == null)
                audio = car.AddComponent<VehicleAudioController>();

            audio.rivalMix = useRivalMix;
            audio.engineVolumeScale = useRivalMix ? 0.85f : 1f;
            var blend = spatial3D ? 1f : 0f;
            var root = car.transform;
            audio.engineSource = NeonLapAudioSourceFactory.CreateLoopSource(root, "EngineAudio",
                NeonLapAudioLibrary.EngineLoop, 0.55f, false, blend);
            audio.windSource = NeonLapAudioSourceFactory.CreateLoopSource(root, "WindAudio",
                NeonLapAudioLibrary.WindLoop, 0.35f, false, blend);
            audio.driftSource = NeonLapAudioSourceFactory.CreateLoopSource(root, "DriftAudio",
                NeonLapAudioLibrary.DriftScrape, 0.5f, false, blend);
            audio.nitroSource = NeonLapAudioSourceFactory.CreateLoopSource(root, "NitroAudio",
                NeonLapAudioLibrary.NitroWhoosh, 0.65f, false, blend);
            audio.nitroSource.loop = false;
            audio.sfxSource = NeonLapAudioSourceFactory.CreateOneShotSource(root, "VehicleSfx", 0.85f, blend);
            return audio;
        }

        void Awake()
        {
            vehicle = GetComponent<VehicleController>();
            nitroBoost = GetComponent<VehicleNitroBoost>();
        }

        void Update()
        {
            if (vehicle == null)
                return;

            UpdateEngine();
            UpdateWind();
            UpdateDrift();
            UpdateNitro();
        }

        public void PlayImpact(float impactSpeed)
        {
            if (sfxSource == null)
                return;

            var clip = impactSpeed >= heavyImpactSpeed
                ? NeonLapAudioLibrary.ImpactHeavy
                : NeonLapAudioLibrary.ImpactLight;
            if (clip == null)
                return;

            var volume = Mathf.Clamp01(0.35f + impactSpeed / 28f) * GameAudioSettings.SfxMix;
            sfxSource.pitch = Mathf.Lerp(0.9f, 1.15f, Mathf.Clamp01(impactSpeed / 24f));
            sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayNitroActivate()
        {
            if (nitroSource == null || NeonLapAudioLibrary.NitroWhoosh == null)
                return;

            nitroSource.clip = NeonLapAudioLibrary.NitroWhoosh;
            nitroSource.loop = true;
            nitroSource.volume = 0.7f;
            nitroSource.Play();
            nitroPlaying = true;
        }

        void UpdateEngine()
        {
            if (engineSource == null || engineSource.clip == null)
                return;

            var speedRatio = Mathf.Clamp01(vehicle.CurrentSpeed / maxSpeedReference);
            if (!engineSource.isPlaying)
                engineSource.Play();

            engineSource.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedRatio);
            var binding = engineSource.GetComponent<NeonLapAudioSourceBinding>();
            var layer = Mathf.Lerp(0.28f, engineVolumeMax, speedRatio) * engineVolumeScale;
            if (binding != null)
                binding.LayerVolume = layer;
            else
                engineSource.volume = layer * GameAudioSettings.SfxMix;
        }

        void UpdateWind()
        {
            if (windSource == null || windSource.clip == null)
                return;

            if (!windSource.isPlaying)
                windSource.Play();

            var speedRatio = Mathf.Clamp01(vehicle.CurrentSpeed / maxSpeedReference);
            var windVol = speedRatio * (rivalMix ? 0.28f : 0.55f);
            var windBinding = windSource.GetComponent<NeonLapAudioSourceBinding>();
            if (windBinding != null)
                windBinding.LayerVolume = windVol;
            else
                windSource.volume = windVol * GameAudioSettings.SfxMix;
        }

        void UpdateDrift()
        {
            if (driftSource == null || driftSource.clip == null)
                return;

            if (vehicle.IsDrifting)
            {
                if (!driftSource.isPlaying)
                    driftSource.Play();
                driftSource.volume = Mathf.Clamp01(0.35f + vehicle.LateralSpeed / 18f);
                driftSource.pitch = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(vehicle.CurrentSpeed / maxSpeedReference));
            }
            else if (driftSource.isPlaying)
            {
                driftSource.Stop();
            }
        }

        void UpdateNitro()
        {
            if (nitroBoost == null || nitroSource == null)
                return;

            if (nitroBoost.IsActive)
            {
                if (!nitroPlaying)
                    PlayNitroActivate();
                nitroSource.volume = 0.55f + 0.25f * Mathf.Clamp01(nitroBoost.RemainingSeconds / 3.5f);
            }
            else if (nitroPlaying)
            {
                nitroSource.Stop();
                nitroPlaying = false;
            }
        }
    }
}
