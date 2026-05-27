using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Audio
{
    /// <summary>
    /// Positional stadium PA tied to the jumbotron / <see cref="Environment.StadiumScoreboard"/>.
    /// </summary>
    public class StadiumPaSpeaker : MonoBehaviour
    {
        [SerializeField] float minDistance = 12f;
        [SerializeField] float maxDistance = 140f;
        [SerializeField] float volume = 0.82f;
        [SerializeField] float retriggerGap = 1.4f;

        AudioSource paSource;
        RaceManager raceManager;
        float lastPlayTime;
        bool subscribed;

        public static StadiumPaSpeaker Setup(Transform jumbotronRoot, RaceManager manager)
        {
            if (jumbotronRoot == null || manager == null)
                return null;

            NeonLapAudioLibrary.Preload();
            var speaker = jumbotronRoot.GetComponent<StadiumPaSpeaker>();
            if (speaker == null)
                speaker = jumbotronRoot.gameObject.AddComponent<StadiumPaSpeaker>();

            speaker.Configure(manager);
            return speaker;
        }

        void Configure(RaceManager manager)
        {
            Unsubscribe();
            raceManager = manager;
            EnsureSource();
            Subscribe();
        }

        void EnsureSource()
        {
            if (paSource != null)
                return;

            paSource = NeonLapAudioSourceFactory.CreateOneShotSource(transform, "StadiumPa", volume, 1f);
            paSource.minDistance = minDistance;
            paSource.maxDistance = maxDistance;
            paSource.rolloffMode = AudioRolloffMode.Logarithmic;
            paSource.spread = 18f;
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

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Racing)
                PlayLapCall(1, false, true);
        }

        void HandleLapCompleted(int completedLap)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            var nextLap = completedLap + 1;
            if (nextLap > raceManager.TotalLaps)
                return;

            var finalLap = nextLap >= raceManager.TotalLaps;
            PlayLapCall(nextLap, finalLap, false);
        }

        public void PlayIncident(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || !CanPlay())
                return;

            PlayClip(NeonLapAudioLibrary.GetPaIncidentClip(message));
        }

        void PlayLapCall(int lapNumber, bool finalLap, bool force)
        {
            if (!CanPlay(force))
                return;

            PlayClip(NeonLapAudioLibrary.GetPaLapClip(lapNumber, finalLap));
        }

        bool CanPlay(bool force = false)
        {
            if (paSource == null)
                return false;

            if (!force && Time.time - lastPlayTime < retriggerGap)
                return false;

            return true;
        }

        void PlayClip(AudioClip clip)
        {
            if (clip == null || paSource == null)
                return;

            paSource.pitch = Random.Range(0.98f, 1.02f);
            paSource.PlayOneShot(clip, volume);
            lastPlayTime = Time.time;
        }
    }
}
