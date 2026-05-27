using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Audio
{
    public class RaceAudioController : MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        [SerializeField] AudioSource uiSource;

        bool subscribed;

        public static RaceAudioController Setup(Transform parent, RaceManager manager)
        {
            NeonLapAudioLibrary.Preload();
            var go = new GameObject("RaceAudio");
            go.transform.SetParent(parent, false);
            var controller = go.AddComponent<RaceAudioController>();
            controller.raceManager = manager;
            controller.uiSource = NeonLapAudioSourceFactory.CreateOneShotSource(go.transform, "RaceUiSfx", 0.9f, 0f);
            controller.Subscribe();
            return controller;
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnCountdownTick += HandleCountdownTick;
            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnCountdownTick -= HandleCountdownTick;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void HandleCountdownTick(int value)
        {
            if (uiSource == null)
                return;

            if (value <= 0)
            {
                PlayClip(NeonLapAudioLibrary.CountdownGo, 1f, 1f);
                return;
            }

            PlayClip(NeonLapAudioLibrary.CountdownBeep, 0.85f, value == 1 ? 1.05f : 1f);
        }

        void HandleLapCompleted(int lap)
        {
            if (raceManager != null && lap >= raceManager.TotalLaps)
                return;

            PlayClip(NeonLapAudioLibrary.LapComplete, 0.8f, 1f);
        }

        void HandleRaceFinished(int placement)
        {
            PlayClip(NeonLapAudioLibrary.FinishSting, 1f, 1f);
        }

        void PlayClip(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || uiSource == null)
                return;

            uiSource.pitch = pitch;
            uiSource.PlayOneShot(clip, volume * GameAudioSettings.SfxMix);
        }
    }
}
