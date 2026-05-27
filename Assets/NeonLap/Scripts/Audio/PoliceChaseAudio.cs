using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Audio
{
    public class PoliceChaseAudio : MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        [SerializeField] PoliceChaseSystem policeChase;
        [SerializeField] AudioSource sirenSource;
        [SerializeField] float sirenVolume = 0.42f;

        bool subscribed;
        bool sirenActive;

        public static PoliceChaseAudio Instance { get; private set; }

        public static bool IsSirenAudible => Instance != null && Instance.sirenActive;

        public static PoliceChaseAudio Setup(Transform parent, RaceManager manager, PoliceChaseSystem chase)
        {
            if (!GamePoliceSettings.IsActiveForCurrentRace())
                return null;

            NeonLapAudioLibrary.Preload();
            var clip = NeonLapAudioLibrary.PoliceSiren;
            if (clip == null)
                return null;

            var go = new GameObject("PoliceChaseAudio");
            go.transform.SetParent(parent, false);
            var audio = go.AddComponent<PoliceChaseAudio>();
            audio.raceManager = manager;
            audio.policeChase = chase;
            audio.sirenSource = NeonLapAudioSourceFactory.CreateLoopSource(go.transform, "PoliceSiren", clip,
                audio.sirenVolume, false, 0f, NeonLapAudioBus.Sfx);
            audio.sirenSource.loop = true;
            audio.sirenSource.minDistance = 18f;
            audio.sirenSource.maxDistance = 120f;
            audio.sirenSource.spatialBlend = 0.35f;
            Instance = audio;
            audio.Subscribe();
            return audio;
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            Unsubscribe();
            StopSiren();
            if (Instance == this)
                Instance = null;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Subscribe()
        {
            if (subscribed)
                return;

            if (raceManager != null)
                raceManager.OnStateChanged += HandleStateChanged;

            if (policeChase != null)
                policeChase.PoliceUnitsSpawned += HandlePoliceSpawned;

            subscribed = true;
            HandleStateChanged(raceManager != null ? raceManager.State : RaceState.Waiting);
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (raceManager != null)
                raceManager.OnStateChanged -= HandleStateChanged;

            if (policeChase != null)
                policeChase.PoliceUnitsSpawned -= HandlePoliceSpawned;

            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state != RaceState.Racing)
                StopSiren();
        }

        void HandlePoliceSpawned()
        {
            if (!GamePoliceSettings.IsActiveForCurrentRace())
                return;

            StartSiren();
        }

        void StartSiren()
        {
            if (sirenSource == null || sirenActive)
                return;

            sirenSource.volume = sirenVolume;
            sirenSource.Play();
            sirenActive = true;
        }

        void StopSiren()
        {
            if (sirenSource == null || !sirenActive)
                return;

            sirenSource.Stop();
            sirenActive = false;
        }
    }
}
