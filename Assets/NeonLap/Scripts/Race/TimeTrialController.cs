using NeonLap.Core;
using NeonLap.Services.Race;
using NeonLap.Track;
using NeonLap.UI;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public class TimeTrialController : MonoBehaviour
    {
        static readonly Color DevGhostBody = new(0.75f, 0.55f, 1f, 0.38f);
        static readonly Color DevGhostAccent = new(1.2f, 0.65f, 2f, 0.5f);

        [SerializeField] RaceManager raceManager;
        [SerializeField] RaceReplaySystem replaySystem;
        [SerializeField] RaceUI raceUi;
        [SerializeField] RacePodiumSequence podiumSequence;
        [SerializeField] OvalTrackBuilder trackBuilder;
        [SerializeField] GameObject playerCar;
        [SerializeField] Material bodyTemplate;
        [SerializeField] Material accentTemplate;
        [SerializeField] GhostHudController ghostHud;

        GhostRacer ghostRacer;
        int trackIndex;
        float lapWindowStart;
        bool subscribed;
        bool newLapPb;
        bool newRacePb;
        bool beatDevGhost;
        bool startedWithDevGhost;

        public static TimeTrialController Setup(
            RaceManager manager,
            RaceReplaySystem replay,
            RaceUI ui,
            RacePodiumSequence podium,
            OvalTrackBuilder track,
            GameObject player,
            Material bodyMat,
            Material accentMat,
            GhostHudController hud)
        {
            if (!GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel)
                return null;

            var go = new GameObject("TimeTrial");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<TimeTrialController>();
            controller.Configure(manager, replay, ui, podium, track, player, bodyMat, accentMat, hud);
            return controller;
        }

        void Configure(
            RaceManager manager,
            RaceReplaySystem replay,
            RaceUI ui,
            RacePodiumSequence podium,
            OvalTrackBuilder track,
            GameObject player,
            Material bodyMat,
            Material accentMat,
            GhostHudController hud)
        {
            raceManager = manager;
            replaySystem = replay;
            raceUi = ui;
            podiumSequence = podium;
            trackBuilder = track;
            playerCar = player;
            bodyTemplate = bodyMat;
            accentTemplate = accentMat;
            ghostHud = hud;
            trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            startedWithDevGhost = !TimeTrialRecordStore.HasPlayerPb(trackIndex);

            TimeTrialSettings.Load();

            if (podiumSequence != null)
                podiumSequence.enabled = false;

            SpawnGhost();
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
                    Mode = RaceMode.TimeTrial,
                });
                RefreshGhost(recording, playerPb: true);
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

            beatDevGhost = false;
            if (startedWithDevGhost)
            {
                var devTarget = DevGhostLibrary.GetReferenceLapTime(trackIndex);
                if (newLapPb || newRacePb)
                    beatDevGhost = true;
                else if (devTarget > 0f && raceManager.BestLapTime > 0.05f && raceManager.BestLapTime <= devTarget)
                    beatDevGhost = true;
            }

            var extra = beatDevGhost ? "BEAT DEV GHOST!" : null;
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

        void SpawnGhost()
        {
            if (trackBuilder == null || playerCar == null)
                return;

            var isDev = !TimeTrialRecordStore.HasPlayerPb(trackIndex);
            var recording = TimeTrialRecordStore.LoadPlaybackGhost(trackIndex);
            if (recording == null || !recording.IsValid)
                return;

            var mode = recording.AnchorRaceTime > 0.01f ? GhostPlaybackMode.Race : GhostPlaybackMode.Lap;
            ghostRacer = SpawnGhostInstance(recording, isDev, mode);
            ghostHud?.RegisterGhost(ghostRacer, primary: true);
        }

        void RefreshGhost(GhostRecordingData recording, bool playerPb)
        {
            if (ghostRacer != null)
                Destroy(ghostRacer.gameObject);

            var mode = recording.AnchorRaceTime > 0.01f ? GhostPlaybackMode.Race : GhostPlaybackMode.Lap;
            ghostRacer = SpawnGhostInstance(recording, devGhost: !playerPb, mode);
            ghostHud?.RegisterGhost(ghostRacer, primary: true);
        }

        GhostRacer SpawnGhostInstance(GhostRecordingData recording, bool devGhost,
            GhostPlaybackMode mode = GhostPlaybackMode.Lap)
        {
            Color? body = devGhost ? DevGhostBody : null;
            Color? accent = devGhost ? DevGhostAccent : null;

            return GhostRacer.Spawn(
                trackBuilder.transform,
                trackBuilder.StartPosition,
                trackBuilder.StartRotation,
                recording,
                raceManager,
                bodyTemplate,
                accentTemplate,
                mode,
                body,
                accent,
                devGhost ? "DevGhost" : mode == GhostPlaybackMode.Race ? "PbRaceGhost" : "PbGhost",
                devGhost);
        }
    }
}
