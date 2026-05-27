using NeonLap.Core;
using NeonLap.Services.Race;
using NeonLap.Track;
using NeonLap.UI;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public class GhostDuelController : MonoBehaviour
    {
        static readonly Color LapGhostBody = new(0.35f, 0.95f, 1f, 0.42f);
        static readonly Color LapGhostAccent = new(0.2f, 1.8f, 2.2f, 0.55f);
        static readonly Color RaceGhostBody = new(1f, 0.55f, 0.15f, 0.42f);
        static readonly Color RaceGhostAccent = new(2.2f, 0.9f, 0.2f, 0.55f);

        [SerializeField] RaceManager raceManager;
        [SerializeField] RaceReplaySystem replaySystem;
        [SerializeField] RaceUI raceUi;
        [SerializeField] OvalTrackBuilder trackBuilder;
        [SerializeField] GameObject playerCar;
        [SerializeField] Material bodyTemplate;
        [SerializeField] Material accentTemplate;
        [SerializeField] GhostHudController ghostHud;

        GhostRacer lapGhost;
        GhostRacer raceGhost;
        int trackIndex;
        float lapWindowStart;
        bool subscribed;
        bool newLapPb;
        bool newRacePb;

        public static GhostDuelController Setup(
            RaceManager manager,
            RaceReplaySystem replay,
            RaceUI ui,
            OvalTrackBuilder track,
            GameObject player,
            Material bodyMat,
            Material accentMat,
            GhostHudController hud)
        {
            if (!GameRaceModeSettings.IsGhostDuel)
                return null;

            var go = new GameObject("GhostDuel");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<GhostDuelController>();
            controller.Configure(manager, replay, ui, track, player, bodyMat, accentMat, hud);
            return controller;
        }

        void Configure(
            RaceManager manager,
            RaceReplaySystem replay,
            RaceUI ui,
            OvalTrackBuilder track,
            GameObject player,
            Material bodyMat,
            Material accentMat,
            GhostHudController hud)
        {
            raceManager = manager;
            replaySystem = replay;
            raceUi = ui;
            trackBuilder = track;
            playerCar = player;
            bodyTemplate = bodyMat;
            accentTemplate = accentMat;
            ghostHud = hud;
            trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;

            TimeTrialSettings.Load();
            SpawnGhosts();
            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Racing)
                lapWindowStart = 0f;
        }

        void HandleLapCompleted(int lap)
        {
            if (replaySystem == null || raceManager == null)
                return;

            var lapTime = raceManager.LastLapTime;
            var lapFrames = replaySystem.ExportPlayerFrames(lapWindowStart, raceManager.RaceTime);
            var recording = GhostReplayBridge.BuildRecording(lapFrames);
            newLapPb = TimeTrialRecordStore.TrySaveBestLap(trackIndex, lapTime, recording);
            if (newLapPb)
            {
                RaceEventHub.PublishLapPersonalBest(new LapPersonalBestEvent
                {
                    TrackIndex = trackIndex,
                    LapTime = lapTime,
                    Mode = RaceMode.GhostDuel,
                });
                RefreshLapGhost(recording);
            }

            lapWindowStart = raceManager.RaceTime;
        }

        void HandleRaceFinished(int placement)
        {
            if (replaySystem == null || raceManager == null)
                return;

            var raceTime = raceManager.RaceTime;
            var finalLapFrames = replaySystem.ExportPlayerFrames(lapWindowStart, raceTime);
            var raceRecording = GhostReplayBridge.BuildRecording(finalLapFrames, lapWindowStart);
            newRacePb = GhostReplayBridge.SaveFinalLapRacePb(trackIndex, raceTime, lapWindowStart, raceRecording);
            if (newRacePb)
                RefreshRaceGhost(raceRecording);

            var extra = lapGhost == null && raceGhost == null ? "Drive a lap to record ghosts" : null;
            var finish = TimeTrialFinishEvaluator.Evaluate(
                trackIndex,
                raceTime,
                raceManager.BestLapTime,
                newRacePb,
                newLapPb,
                extra);

            var canAdvance = GameManager.Instance != null && GameManager.Instance.HasNextLevel;
            var scoreSystem = playerCar != null ? playerCar.GetComponent<RaceScoreSystem>() : null;
            var breakdown = scoreSystem != null ? scoreSystem.GetBreakdownText() : string.Empty;
            raceUi?.ShowTimeTrialFinish(
                raceTime,
                raceManager.BestLapTime,
                newRacePb,
                newLapPb,
                finish,
                breakdown,
                canAdvance);
        }

        void SpawnGhosts()
        {
            if (trackBuilder == null || playerCar == null)
                return;

            var lapRecording = TimeTrialRecordStore.LoadBestLapGhost(trackIndex);
            if (lapRecording != null && lapRecording.IsValid)
            {
                lapGhost = SpawnGhost("LapGhost", lapRecording, GhostPlaybackMode.Lap, LapGhostBody, LapGhostAccent);
                ghostHud?.RegisterGhost(lapGhost, primary: true);
            }

            var raceRecording = TimeTrialRecordStore.LoadBestRaceGhost(trackIndex);
            if (raceRecording != null && raceRecording.IsValid)
            {
                raceGhost = SpawnGhost("RaceGhost", raceRecording, GhostPlaybackMode.Race, RaceGhostBody, RaceGhostAccent);
                ghostHud?.RegisterGhost(raceGhost);
            }
        }

        void RefreshLapGhost(GhostRecordingData recording)
        {
            if (lapGhost != null)
                Destroy(lapGhost.gameObject);

            lapGhost = SpawnGhost("LapGhost", recording, GhostPlaybackMode.Lap, LapGhostBody, LapGhostAccent);
            ghostHud?.RegisterGhost(lapGhost, primary: true);
        }

        void RefreshRaceGhost(GhostRecordingData recording)
        {
            if (raceGhost != null)
                Destroy(raceGhost.gameObject);

            raceGhost = SpawnGhost("RaceGhost", recording, GhostPlaybackMode.Race, RaceGhostBody, RaceGhostAccent);
            ghostHud?.RegisterGhost(raceGhost);
        }

        GhostRacer SpawnGhost(string name, GhostRecordingData recording, GhostPlaybackMode mode, Color bodyColor,
            Color accentColor)
        {
            var ghost = GhostRacer.Spawn(
                trackBuilder.transform,
                trackBuilder.StartPosition,
                trackBuilder.StartRotation,
                recording,
                raceManager,
                bodyTemplate,
                accentTemplate,
                mode,
                bodyColor,
                accentColor,
                name);
            return ghost;
        }
    }
}
