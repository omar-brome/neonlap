using NeonLap.Core;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public class RaceScoreSystem : MonoBehaviour
    {
        [SerializeField] float progressMilestoneStep = 0.05f;
        [SerializeField] int progressMilestoneScore = 50;
        [SerializeField] int rivalCollisionScore = 75;
        [SerializeField] float rivalCollisionCooldown = 1.5f;
        [SerializeField] int driftScorePerTick = 8;
        [SerializeField] float driftScoreInterval = 0.25f;
        [SerializeField] int overtakenPenalty = 100;
        [SerializeField] float positionCheckInterval = 0.35f;
        [SerializeField] int lapLeadBonus = 250;
        [SerializeField] int finishBonusFirst = 1500;
        [SerializeField] int finishBonusSecond = 900;
        [SerializeField] int finishBonusThird = 500;

        RaceManager raceManager;
        VehicleController playerVehicle;

        float lastProgressMilestone;
        float nextDriftScoreTime;
        float nextPositionCheckTime;
        int lastCheckedPosition = 1;
        float nextCollisionScoreTime;
        int lastCollisionRivalId;
        bool finishBonusApplied;

        public int Score { get; private set; }

        public void Configure(RaceManager manager, GameObject playerCar)
        {
            Unsubscribe();
            raceManager = manager;
            playerVehicle = playerCar != null ? playerCar.GetComponent<VehicleController>() : null;
            Subscribe();
            ResetScore();
        }

        void Subscribe()
        {
            if (raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnRaceFinished += HandleRaceFinished;
        }

        void Unsubscribe()
        {
            if (raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager.OnRaceFinished -= HandleRaceFinished;
        }

        public void ResetScore()
        {
            Score = 0;
            lastProgressMilestone = 0f;
            nextDriftScoreTime = 0f;
            nextPositionCheckTime = 0f;
            lastCheckedPosition = 1;
            nextCollisionScoreTime = 0f;
            lastCollisionRivalId = 0;
            finishBonusApplied = false;
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Update()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            UpdateProgressScore();
            UpdateDriftScore();
            UpdatePositionPenalty();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (collision.collider.gameObject.layer != NeonLapLayers.Vehicle)
                return;

            var rival = collision.collider.GetComponentInParent<AIVehicleController>();
            if (rival == null)
                return;

            var rivalId = rival.GetInstanceID();
            if (Time.time < nextCollisionScoreTime && rivalId == lastCollisionRivalId)
                return;

            lastCollisionRivalId = rivalId;
            nextCollisionScoreTime = Time.time + rivalCollisionCooldown;
            AddScore(rivalCollisionScore);
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
                ResetScore();
        }

        void HandleLapCompleted(int lap)
        {
            if (raceManager == null || raceManager.GetPlayerPosition() != 1)
                return;

            AddScore(lapLeadBonus);
        }

        public void ApplyFinishBonus(int placement)
        {
            if (finishBonusApplied)
                return;

            finishBonusApplied = true;
            var bonus = placement switch
            {
                1 => finishBonusFirst,
                2 => finishBonusSecond,
                3 => finishBonusThird,
                _ => 0
            };

            if (bonus > 0)
                AddScore(bonus);
        }

        void HandleRaceFinished(int placement)
        {
            ApplyFinishBonus(placement);
        }

        void UpdateProgressScore()
        {
            var progress = raceManager.GetPlayerRaceProgress();
            while (progress >= lastProgressMilestone + progressMilestoneStep)
            {
                lastProgressMilestone += progressMilestoneStep;
                AddScore(progressMilestoneScore);
            }
        }

        void UpdateDriftScore()
        {
            if (playerVehicle == null || !playerVehicle.IsDrifting)
                return;

            if (Time.time < nextDriftScoreTime)
                return;

            nextDriftScoreTime = Time.time + driftScoreInterval;
            AddScore(driftScorePerTick);
        }

        void UpdatePositionPenalty()
        {
            if (Time.time < nextPositionCheckTime)
                return;

            nextPositionCheckTime = Time.time + positionCheckInterval;
            var position = raceManager.GetPlayerPosition();

            if (position > lastCheckedPosition)
                AddScore(-overtakenPenalty);

            lastCheckedPosition = position;
        }

        void AddScore(int delta)
        {
            if (delta == 0)
                return;

            Score = Mathf.Max(0, Score + delta);
        }
    }
}
