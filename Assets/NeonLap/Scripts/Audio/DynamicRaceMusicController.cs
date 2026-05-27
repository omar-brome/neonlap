using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Audio
{
    public enum RaceMusicLayer
    {
        Off = 0,
        Calm = 1,
        Racing = 2,
        Chase = 3,
        FinalLap = 4,
        Podium = 5,
    }

    public class DynamicRaceMusicController : MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        [SerializeField] PoliceChaseSystem policeChase;
        [SerializeField] float crossfadeSpeed = 1.8f;
        [SerializeField] float musicBpm = 128f;
        [SerializeField] float beatPulseSharpness = 2.4f;
        [SerializeField] float policeDuckAmount = 0.42f;
        [SerializeField] float calmUnderRacingMix = 0.12f;

        AudioSource calmSource;
        AudioSource racingSource;
        AudioSource chaseSource;
        AudioSource finalLapSource;
        AudioSource podiumSource;
        NeonLapAudioSourceBinding calmBinding;
        NeonLapAudioSourceBinding racingBinding;
        NeonLapAudioSourceBinding chaseBinding;
        NeonLapAudioSourceBinding finalBinding;
        NeonLapAudioSourceBinding podiumBinding;
        RaceMusicLayer targetLayer = RaceMusicLayer.Calm;
        bool subscribed;
        bool podiumLocked;

        public static DynamicRaceMusicController Instance { get; private set; }

        public float MusicBpm => musicBpm;

        public float BeatPulse
        {
            get
            {
                var active = GetActiveMusicSource();
                var sampleTime = active != null && active.isPlaying ? active.time : Time.time;
                var beatPhase = sampleTime * musicBpm / 60f;
                var wave = Mathf.Abs(Mathf.Sin(beatPhase * Mathf.PI * 2f));
                return Mathf.Pow(wave, beatPulseSharpness);
            }
        }

        AudioSource GetActiveMusicSource()
        {
            if (podiumBinding != null && podiumBinding.LayerVolume > 0.05f)
                return podiumSource;
            if (finalBinding != null && finalBinding.LayerVolume > 0.05f)
                return finalLapSource;
            if (chaseBinding != null && chaseBinding.LayerVolume > 0.05f)
                return chaseSource;
            if (racingBinding != null && racingBinding.LayerVolume > 0.05f)
                return racingSource;
            return calmSource;
        }

        public static DynamicRaceMusicController Setup(Transform parent, RaceManager manager, PoliceChaseSystem police)
        {
            NeonLapAudioLibrary.Preload();
            var go = new GameObject("DynamicRaceMusic");
            go.transform.SetParent(parent, false);
            var controller = go.AddComponent<DynamicRaceMusicController>();
            controller.Configure(manager, police);
            return controller;
        }

        void Configure(RaceManager manager, PoliceChaseSystem police)
        {
            raceManager = manager;
            policeChase = police;
            Instance = this;
            BuildSources();
            Subscribe();
            SetLayerImmediate(RaceMusicLayer.Calm);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void BuildSources()
        {
            calmSource = CreateLayerSource(transform, "MusicCalm", NeonLapAudioLibrary.MusicCalm, out calmBinding, 0.42f);
            racingSource = CreateLayerSource(transform, "MusicRacing", NeonLapAudioLibrary.MusicRacing, out racingBinding,
                0f);
            chaseSource = CreateLayerSource(transform, "MusicChase", NeonLapAudioLibrary.MusicChase, out chaseBinding, 0f);
            finalLapSource = CreateLayerSource(transform, "MusicFinalLap", NeonLapAudioLibrary.MusicFinalLap,
                out finalBinding, 0f);
            podiumSource = CreateLayerSource(transform, "MusicPodium", NeonLapAudioLibrary.MusicPodium, out podiumBinding,
                0f);
        }

        static AudioSource CreateLayerSource(Transform parent, string name, AudioClip clip,
            out NeonLapAudioSourceBinding binding, float initialLayer)
        {
            var source = NeonLapAudioSourceFactory.CreateLoopSource(parent, name, clip, 1f, false, 0f,
                NeonLapAudioBus.Music);
            binding = source.GetComponent<NeonLapAudioSourceBinding>();
            binding.LayerVolume = initialLayer;
            if (clip != null)
                source.Play();
            return source;
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnLapCompleted += HandleLapCompleted;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            subscribed = false;
        }

        void Update()
        {
            if (raceManager == null)
                return;

            if (!podiumLocked)
                targetLayer = EvaluateTargetLayer();

            CrossfadeLayers();
        }

        RaceMusicLayer EvaluateTargetLayer()
        {
            if (raceManager.State == RaceState.Waiting || raceManager.State == RaceState.Countdown)
                return RaceMusicLayer.Calm;

            if (raceManager.State != RaceState.Racing)
                return RaceMusicLayer.Off;

            if (raceManager.CurrentLap >= raceManager.TotalLaps)
                return RaceMusicLayer.FinalLap;

            if (GamePoliceSettings.IsActiveForCurrentRace() && policeChase != null && policeChase.HasActiveUnits)
                return RaceMusicLayer.Chase;

            return RaceMusicLayer.Racing;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Finished && !podiumLocked)
                targetLayer = RaceMusicLayer.Off;
        }

        void HandleLapCompleted(int lap)
        {
            if (podiumLocked || raceManager == null)
                return;

            if (lap >= raceManager.TotalLaps - 1)
                targetLayer = RaceMusicLayer.FinalLap;
        }

        public void EnterPodium()
        {
            podiumLocked = true;
            targetLayer = RaceMusicLayer.Podium;
            SetLayerImmediate(RaceMusicLayer.Podium);
        }

        public void ExitPodium()
        {
            podiumLocked = false;
            targetLayer = RaceMusicLayer.Off;
        }

        void CrossfadeLayers()
        {
            var duck = PoliceChaseAudio.IsSirenAudible ? policeDuckAmount : 0f;
            var calm = GetLayerTarget(RaceMusicLayer.Calm, 0.42f, duck);
            var racing = GetLayerTarget(RaceMusicLayer.Racing, 0.5f, duck);
            var chase = GetLayerTarget(RaceMusicLayer.Chase, 0.58f, duck);
            var finalLap = GetLayerTarget(RaceMusicLayer.FinalLap, 0.62f, duck);
            var podium = GetLayerTarget(RaceMusicLayer.Podium, 0.55f, duck);

            if (targetLayer == RaceMusicLayer.Racing)
                calm = Mathf.Max(calm, calmUnderRacingMix * (1f - duck));

            FadeBinding(calmBinding, calm);
            FadeBinding(racingBinding, racing);
            FadeBinding(chaseBinding, chase);
            FadeBinding(finalBinding, finalLap);
            FadeBinding(podiumBinding, podium);
        }

        float GetLayerTarget(RaceMusicLayer layer, float volume, float duck)
        {
            if (targetLayer != layer)
                return 0f;

            return volume * (1f - duck);
        }

        void FadeBinding(NeonLapAudioSourceBinding binding, float targetVolume)
        {
            if (binding == null)
                return;

            binding.LayerVolume = Mathf.MoveTowards(binding.LayerVolume, targetVolume,
                crossfadeSpeed * Time.deltaTime);
        }

        void SetLayerImmediate(RaceMusicLayer layer)
        {
            targetLayer = layer;
            var duck = PoliceChaseAudio.IsSirenAudible ? policeDuckAmount : 0f;
            if (calmBinding != null)
                calmBinding.LayerVolume = layer == RaceMusicLayer.Calm ? 0.42f * (1f - duck) : 0f;
            if (racingBinding != null)
                racingBinding.LayerVolume = layer == RaceMusicLayer.Racing ? 0.5f * (1f - duck) : 0f;
            if (chaseBinding != null)
                chaseBinding.LayerVolume = layer == RaceMusicLayer.Chase ? 0.58f * (1f - duck) : 0f;
            if (finalBinding != null)
                finalBinding.LayerVolume = layer == RaceMusicLayer.FinalLap ? 0.62f * (1f - duck) : 0f;
            if (podiumBinding != null)
                podiumBinding.LayerVolume = layer == RaceMusicLayer.Podium ? 0.55f * (1f - duck) : 0f;
        }
    }
}
