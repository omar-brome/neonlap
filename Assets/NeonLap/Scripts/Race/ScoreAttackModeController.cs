using NeonLap.Core;
using NeonLap.UI;
using UnityEngine;

namespace NeonLap.Race
{
    public class ScoreAttackModeController : MonoBehaviour
    {
        [SerializeField] float roundDuration = 90f;

        RaceManager raceManager;
        RaceUI raceUi;
        RaceScoreSystem scoreSystem;
        RacePodiumSequence podiumSequence;
        int trackIndex;
        bool subscribed;
        bool finished;

        public static ScoreAttackModeController Setup(
            RaceManager manager,
            RaceUI ui,
            RaceScoreSystem scoring,
            RacePodiumSequence podium)
        {
            if (!GameRaceModeSettings.IsScoreAttack)
                return null;

            var go = new GameObject("ScoreAttackMode");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<ScoreAttackModeController>();
            controller.Configure(manager, ui, scoring, podium);
            return controller;
        }

        void Configure(RaceManager manager, RaceUI ui, RaceScoreSystem scoring, RacePodiumSequence podium)
        {
            raceManager = manager;
            raceUi = ui;
            scoreSystem = scoring;
            podiumSequence = podium;
            trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;

            if (podiumSequence != null)
                podiumSequence.enabled = false;

            manager.SetPlayerLapFinishEnabled(false);
            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Racing)
                finished = false;
        }

        void Update()
        {
            if (finished || raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (raceManager.RaceTime >= roundDuration)
            {
                finished = true;
                raceManager.EndPlayerRace(1);
            }
        }

        void HandleRaceFinished(int placement)
        {
            if (raceUi == null)
                return;

            var score = scoreSystem != null ? scoreSystem.Score : 0;
            var newPb = ScoreAttackRecordStore.TrySaveHighScore(trackIndex, score);
            var breakdown = scoreSystem != null ? scoreSystem.GetBreakdownText() : string.Empty;

            raceUi.ShowScoreAttackFinish(score, newPb, ScoreAttackRecordStore.GetTrackSummary(trackIndex), breakdown);
        }

        public float RemainingTime =>
            raceManager != null && raceManager.State == RaceState.Racing
                ? Mathf.Max(0f, roundDuration - raceManager.RaceTime)
                : 0f;

        public float RoundDuration => roundDuration;
    }
}
