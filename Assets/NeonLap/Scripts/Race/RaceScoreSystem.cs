using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NeonLap.Core;
using NeonLap.Track;
using NeonLap.VFX;
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
        [SerializeField] int barrelKnockScore = 55;
        [SerializeField] int barrelKnockAssistScore = 30;
        [SerializeField] float barrelKnockCooldown = 1.25f;
        [SerializeField] int driftZoneEntryScore = 25;
        [SerializeField] float driftZoneEntryCooldown = 4f;
        [SerializeField] float driftScoreInterval = 0.25f;
        [SerializeField] int driftScorePerTick = 12;
        [SerializeField] int overtakenPenalty = 100;
        [SerializeField] float positionCheckInterval = 0.35f;
        [SerializeField] int lapLeadBonus = 250;
        [SerializeField] int finishBonusFirst = 1500;
        [SerializeField] int finishBonusSecond = 900;
        [SerializeField] int finishBonusThird = 500;
        [SerializeField] int barrelRollScore = 120;
        [SerializeField] int barrelRollChainBonus = 45;
        [SerializeField] float barrelRollChainWindow = 2.75f;
        [SerializeField] int nitroPickupScore = 40;
        [SerializeField] int nitroBoostScorePerTick = 6;
        [SerializeField] float nitroBoostScoreInterval = 0.25f;
        [SerializeField] float policeEscapeScorePerMeter = 2.5f;
        [SerializeField] float policeProximityRange = 42f;
        [SerializeField] int cleanLapBonus = 300;
        [SerializeField] int manualResetPenalty = 150;
        [SerializeField] int offTrackResetPenalty = 90;
        [SerializeField] float comboWindow = 2f;
        [SerializeField] float comboMultiplier = 2f;

        RaceManager raceManager;
        VehicleController playerVehicle;
        VehicleBarrelRoll barrelRoll;
        VehicleNitroBoost nitroBoost;
        Rigidbody playerRigidbody;
        readonly ScoreBreakdown breakdown = new();

        float lastProgressMilestone;
        float nextDriftScoreTime;
        float nextDriftZoneEntryTime;
        float lastDriftZoneEntryMultiplier;
        float nextPositionCheckTime;
        int lastCheckedPosition = 1;
        float nextCollisionScoreTime;
        int lastCollisionRivalId;
        int lastBarrelKnockRivalId;
        float nextBarrelKnockScoreTime;
        bool finishBonusApplied;
        int lastFinishPlacement;
        float lastBarrelRollTime = -999f;
        int barrelRollChainCount;
        float lastDriftActionTime = -999f;
        float lastOvertakeTime = -999f;
        float nextNitroBoostScoreTime;
        float policeChaseDistanceAccum;
        int lapCollisionCount;
        bool cleanLapEligible = true;

        public int Score { get; private set; }

        public ScoreBreakdown Breakdown => breakdown;

        public IReadOnlyList<ScoreLine> GetBreakdownLines()
        {
            return breakdown.BuildLines(lastFinishPlacement);
        }

        public string GetBreakdownText()
        {
            return breakdown.BuildMultilineText(lastFinishPlacement);
        }

        public void Configure(RaceManager manager, GameObject playerCar)
        {
            Unsubscribe();
            raceManager = manager;
            playerVehicle = playerCar != null ? playerCar.GetComponent<VehicleController>() : null;
            barrelRoll = playerCar != null ? playerCar.GetComponent<VehicleBarrelRoll>() : null;
            nitroBoost = playerCar != null ? playerCar.GetComponent<VehicleNitroBoost>() : null;
            playerRigidbody = playerCar != null ? playerCar.GetComponent<Rigidbody>() : null;
            SubscribeBarrelRoll();
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
            breakdown.Reset();
            lastProgressMilestone = 0f;
            nextDriftScoreTime = 0f;
            nextDriftZoneEntryTime = 0f;
            lastDriftZoneEntryMultiplier = 1f;
            nextPositionCheckTime = 0f;
            lastCheckedPosition = 1;
            nextCollisionScoreTime = 0f;
            lastCollisionRivalId = 0;
            lastBarrelKnockRivalId = 0;
            nextBarrelKnockScoreTime = 0f;
            finishBonusApplied = false;
            lastFinishPlacement = 0;
            lastBarrelRollTime = -999f;
            barrelRollChainCount = 0;
            lastDriftActionTime = -999f;
            lastOvertakeTime = -999f;
            nextNitroBoostScoreTime = 0f;
            policeChaseDistanceAccum = 0f;
            ResetLapCollisionTracking();
        }

        void ResetLapCollisionTracking()
        {
            lapCollisionCount = 0;
            cleanLapEligible = true;
        }

        public void ApplyDailyChallengeBonus(int bonus)
        {
            if (bonus <= 0)
                return;

            breakdown.SetDailyBonus(bonus);
            AddScore(bonus);
        }

        public void RegisterShortcutMerge(TrackShortcutDefinition definition, RaceShortcutTracker tracker)
        {
            if (definition == null || raceManager == null || raceManager.State != RaceState.Racing)
                return;

            var bonus = definition.ScoreBonus;
            var beatGhost = TryEvaluateShortcutGhostBeat();
            if (beatGhost)
                bonus += 80;

            tracker?.SetShortcutBeatGhost(beatGhost);
            AddScore(bonus, () => breakdown.AddShortcut(bonus, beatGhost));
        }

        public void RegisterDriftZoneEntry(float zoneMultiplier)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (Time.time < nextDriftZoneEntryTime && Mathf.Approximately(zoneMultiplier, lastDriftZoneEntryMultiplier))
                return;

            nextDriftZoneEntryTime = Time.time + driftZoneEntryCooldown;
            lastDriftZoneEntryMultiplier = zoneMultiplier;
            var bonus = Mathf.RoundToInt(driftZoneEntryScore * Mathf.Max(1f, zoneMultiplier));
            AddScore(bonus, () => breakdown.AddDrift(bonus));
        }

        public void RegisterNitroPickup()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            AddScore(nitroPickupScore, () => breakdown.AddNitroPickup(nitroPickupScore));
        }

        public void RegisterLapCollision()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            lapCollisionCount++;
            cleanLapEligible = false;
        }

        public void RegisterPlayerReset(bool manual)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (manual)
            {
                AddScore(-manualResetPenalty, () => breakdown.AddResetPenalty(-manualResetPenalty));
                return;
            }

            AddScore(-offTrackResetPenalty, () => breakdown.AddOffTrackPenalty(-offTrackResetPenalty));
        }

        bool TryEvaluateShortcutGhostBeat()
        {
            var ghost = FindAnyObjectByType<GhostRacer>();
            if (ghost == null || !ghost.HasGhost || !ghost.IsVisible)
                return false;

            return ghost.TryGetDeltaSeconds(transform.position, out var delta) && delta < -0.05f;
        }

        void SubscribeBarrelRoll()
        {
            if (barrelRoll == null)
                return;

            barrelRoll.RollCompleted -= HandleBarrelRollCompleted;
            barrelRoll.RollCompleted += HandleBarrelRollCompleted;
        }

        void OnDisable()
        {
            Unsubscribe();
            if (barrelRoll != null)
                barrelRoll.RollCompleted -= HandleBarrelRollCompleted;
        }

        void Update()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            UpdateProgressScore();
            UpdateDriftScore();
            UpdateNitroBoostScore();
            UpdatePoliceEscapeScore();
            UpdatePositionPenalty();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (CollisionHazardUtility.IsHazard(collision.collider, this) && !IsSoftVehicleCollision(collision))
                RegisterLapCollision();

            if (collision.collider.gameObject.layer != NeonLapLayers.Vehicle)
                return;

            var rival = collision.collider.GetComponentInParent<AIVehicleController>();
            if (rival == null)
                return;

            var rivalId = RuntimeHelpers.GetHashCode(rival);
            if (Time.time < nextCollisionScoreTime && rivalId == lastCollisionRivalId)
                return;

            lastCollisionRivalId = rivalId;
            nextCollisionScoreTime = Time.time + rivalCollisionCooldown;
            AddScore(rivalCollisionScore, () => breakdown.AddCollision(rivalCollisionScore));
        }

        public void RegisterBarrelKnock(AIVehicleController rival, bool directPlayerHit)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing || rival == null)
                return;

            var rivalId = RuntimeHelpers.GetHashCode(rival);
            if (Time.time < nextBarrelKnockScoreTime && rivalId == lastBarrelKnockRivalId)
                return;

            lastBarrelKnockRivalId = rivalId;
            nextBarrelKnockScoreTime = Time.time + barrelKnockCooldown;
            var points = directPlayerHit ? barrelKnockScore : barrelKnockAssistScore;
            AddScore(points, () => breakdown.AddBarrelKnock(points));
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
                ResetScore();
        }

        void HandleLapCompleted(int lap)
        {
            if (raceManager == null)
                return;

            if (cleanLapEligible && lapCollisionCount == 0)
                AddScore(cleanLapBonus, () => breakdown.AddCleanLap(cleanLapBonus));

            ResetLapCollisionTracking();

            if (raceManager.GetPlayerPosition() != 1)
                return;

            AddScore(lapLeadBonus, () => breakdown.AddLapLead(lapLeadBonus));
        }

        public void ApplyFinishBonus(int placement)
        {
            if (finishBonusApplied)
                return;

            finishBonusApplied = true;
            lastFinishPlacement = placement;
            var bonus = placement switch
            {
                1 => finishBonusFirst,
                2 => finishBonusSecond,
                3 => finishBonusThird,
                _ => 0
            };

            if (bonus > 0)
            {
                breakdown.SetFinishBonus(bonus);
                AddScore(bonus);
            }
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
                AddScore(progressMilestoneScore, () => breakdown.AddProgress(progressMilestoneScore));
            }
        }

        void UpdateDriftScore()
        {
            if (playerVehicle == null || !playerVehicle.IsDrifting)
                return;

            if (Time.time < nextDriftScoreTime)
                return;

            nextDriftScoreTime = Time.time + driftScoreInterval;
            lastDriftActionTime = Time.time;

            var driftPoints = Mathf.RoundToInt(driftScorePerTick * GetDriftScoreMultiplier(playerVehicle));
            var multiplier = GetComboMultiplier(lastOvertakeTime);
            var finalPoints = Mathf.RoundToInt(driftPoints * multiplier);
            var comboExtra = finalPoints - driftPoints;

            AddScore(finalPoints, () =>
            {
                breakdown.AddDrift(driftPoints);
                if (comboExtra > 0)
                    breakdown.AddComboBonus(comboExtra);
            });
        }

        void UpdateNitroBoostScore()
        {
            if (nitroBoost == null || !nitroBoost.IsActive)
                return;

            if (Time.time < nextNitroBoostScoreTime)
                return;

            nextNitroBoostScoreTime = Time.time + nitroBoostScoreInterval;
            AddScore(nitroBoostScorePerTick, () => breakdown.AddNitroBoost(nitroBoostScorePerTick));
        }

        void UpdatePoliceEscapeScore()
        {
            if (!IsPoliceChasing())
            {
                FlushPoliceEscapeScore();
                return;
            }

            var speed = playerRigidbody != null ? playerRigidbody.linearVelocity.magnitude : 0f;
            policeChaseDistanceAccum += speed * Time.deltaTime;

            if (policeChaseDistanceAccum < 8f)
                return;

            var points = Mathf.FloorToInt(policeChaseDistanceAccum * policeEscapeScorePerMeter);
            if (points <= 0)
                return;

            policeChaseDistanceAccum = 0f;
            AddScore(points, () => breakdown.AddPoliceEscape(points));
        }

        void FlushPoliceEscapeScore()
        {
            if (policeChaseDistanceAccum < 1f)
            {
                policeChaseDistanceAccum = 0f;
                return;
            }

            var points = Mathf.FloorToInt(policeChaseDistanceAccum * policeEscapeScorePerMeter);
            policeChaseDistanceAccum = 0f;
            if (points <= 0)
                return;

            AddScore(points, () => breakdown.AddPoliceEscape(points));
        }

        bool IsPoliceChasing()
        {
            if (!GamePoliceSettings.IsActiveForCurrentRace() && !GameRaceModeSettings.IsChase)
                return false;

            var rangeSq = policeProximityRange * policeProximityRange;
            var policeUnits = FindObjectsByType<PoliceChaseVehicle>();
            foreach (var unit in policeUnits)
            {
                if (unit == null)
                    continue;

                var offset = unit.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= rangeSq)
                    return true;
            }

            return false;
        }

        void HandleBarrelRollCompleted(VehicleBarrelRoll roll)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            var chainBonus = 0;
            if (Time.time - lastBarrelRollTime <= barrelRollChainWindow)
            {
                barrelRollChainCount++;
                chainBonus = barrelRollChainBonus * barrelRollChainCount;
            }
            else
            {
                barrelRollChainCount = 0;
            }

            lastBarrelRollTime = Time.time;
            var totalPoints = barrelRollScore + chainBonus;
            AddScore(totalPoints, () => breakdown.AddBarrelRoll(totalPoints));
        }

        static float GetDriftScoreMultiplier(VehicleController vehicle)
        {
            var weather = DynamicWeatherSystem.Instance;
            var baseMultiplier = weather != null ? weather.DriftScoreMultiplier : 1f;

            var presence = vehicle != null ? vehicle.GetComponent<DriftZonePresence>() : null;
            if (presence != null && presence.InDriftZone)
                return baseMultiplier * presence.ActiveMultiplier;

            var registry = TrackGameplayZoneRegistry.Instance;
            if (registry == null || vehicle == null)
                return baseMultiplier;

            var query = new TrackZoneQueryResult();
            registry.Query(vehicle.transform.position, ref query);
            return baseMultiplier * (query.InDriftMultiplier ? Mathf.Max(1f, query.DriftScoreMultiplier) : 1f);
        }

        void UpdatePositionPenalty()
        {
            if (Time.time < nextPositionCheckTime)
                return;

            nextPositionCheckTime = Time.time + positionCheckInterval;
            var position = raceManager.GetPlayerPosition();

            if (position > lastCheckedPosition)
                AddScore(-overtakenPenalty, () => breakdown.AddOvertakenPenalty(-overtakenPenalty));
            else if (position < lastCheckedPosition)
                RegisterOvertake();

            lastCheckedPosition = position;
        }

        void RegisterOvertake()
        {
            lastOvertakeTime = Time.time;

            var bonus = Mathf.RoundToInt(progressMilestoneScore * 0.5f);
            if (bonus <= 0)
                return;

            var multiplier = GetComboMultiplier(lastDriftActionTime);
            var finalPoints = Mathf.RoundToInt(bonus * multiplier);
            var comboExtra = finalPoints - bonus;

            AddScore(finalPoints, () =>
            {
                breakdown.AddProgress(bonus);
                if (comboExtra > 0)
                    breakdown.AddComboBonus(comboExtra);
            });
        }

        float GetComboMultiplier(float otherActionTime)
        {
            return Time.time - otherActionTime <= comboWindow ? comboMultiplier : 1f;
        }

        static bool IsSoftVehicleCollision(Collision collision)
        {
            if (collision.collider.gameObject.layer != NeonLapLayers.Vehicle)
                return false;

            var selfBody = collision.gameObject.GetComponentInParent<Rigidbody>();
            var otherBody = collision.rigidbody ?? collision.collider.attachedRigidbody;
            if (selfBody == null || otherBody == null)
                return false;

            return (selfBody.linearVelocity - otherBody.linearVelocity).magnitude < 9f;
        }

        void AddScore(int delta, System.Action trackBreakdown = null)
        {
            if (delta == 0)
                return;

            trackBreakdown?.Invoke();
            Score = Mathf.Max(0, Score + delta);
        }
    }
}
