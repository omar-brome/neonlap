using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(AIVehicleController))]
    [RequireComponent(typeof(VehicleNitroBoost))]
    public class AICombatController : MonoBehaviour
    {
        const float NitroCooldownBase = 11f;
        const float BananaCooldownBase = 16f;
        const float ProgressNearThreshold = 0.045f;
        const float StraightTurnAngleMax = 28f;

        [SerializeField] int startingNitroCharges = 1;
        [SerializeField] int maxNitroCharges = 2;
        [SerializeField] int startingBananaCharges = 1;
        AIVehicleController ai;
        VehicleNitroBoost nitro;
        RacerProgress racer;
        Transform playerTarget;
        RaceManager raceManager;
        AIPersonalityProfile personality;
        Transform droppedHazardRoot;

        int nitroCharges;
        int bananaCharges;
        float nextNitroTime;
        float nextBananaTime;
        float nextDecisionTime;
        int rivalSeed;
        bool collectsNitroPickups = true;
        bool usesNitroZones = true;

        public bool CanCollectPickups => collectsNitroPickups && nitroCharges < maxNitroCharges;

        public void Configure(Transform player, int rivalIndex, AIPersonalityProfile profile)
        {
            playerTarget = player;
            personality = profile;
            rivalSeed = rivalIndex * 17 + 3;
            nitroCharges = startingNitroCharges;
            bananaCharges = startingBananaCharges;
            ApplyDifficulty(GameDifficultySettings.Current);
        }

        public void ApplyDifficulty(DifficultyLevel level)
        {
            var preset = GameDifficultySettings.GetPreset(level);
            collectsNitroPickups = preset.AiCollectsNitroPickups;
            usesNitroZones = preset.AiUsesNitroZones;
        }

        void Awake()
        {
            ai = GetComponent<AIVehicleController>();
            nitro = GetComponent<VehicleNitroBoost>();
            racer = GetComponent<RacerProgress>();
        }

        void Update()
        {
            if (ai == null || racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();

            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (Time.time < nextDecisionTime)
                return;

            nextDecisionTime = Time.time + Random.Range(0.35f, 0.7f);
            TryUseNitro();
            TryDropBanana();
        }

        void TryUseNitro()
        {
            if (nitro == null || nitro.IsDisabled || nitro.IsActive || nitroCharges <= 0 || Time.time < nextNitroTime)
                return;

            if (!IsNearPlayerOnTrack(out var progressDelta, out var distance))
                return;

            if (progressDelta > -0.01f || progressDelta < -ProgressNearThreshold * 2.2f)
                return;

            if (distance > 42f || distance < 8f)
                return;

            if (!IsOnStraight())
                return;

            var chance = 0.22f * personality.NitroChanceMultiplier * GameDifficultySettings.Preset.AiCombatAggression;
            if (!RollChance(chance))
                return;

            nitro.ActivateBoost(usePickupImpulse: true);
            nitroCharges--;
            nextNitroTime = Time.time + NitroCooldownBase * Random.Range(0.85f, 1.15f);
        }

        void TryDropBanana()
        {
            if (bananaCharges <= 0 || Time.time < nextBananaTime)
                return;

            if (!IsNearPlayerOnTrack(out var progressDelta, out var distance))
                return;

            if (progressDelta < 0.008f || progressDelta > ProgressNearThreshold * 1.6f)
                return;

            if (distance < 12f || distance > 36f)
                return;

            var chance = 0.16f * personality.BananaChanceMultiplier * GameDifficultySettings.Preset.AiCombatAggression;
            if (!RollChance(chance))
                return;

            DropBananaBehind();
            bananaCharges--;
            nextBananaTime = Time.time + BananaCooldownBase * Random.Range(0.9f, 1.2f);
        }

        bool IsNearPlayerOnTrack(out float progressDelta, out float distance)
        {
            progressDelta = 0f;
            distance = float.MaxValue;

            if (playerTarget == null || raceManager == null)
                return false;

            var playerRacer = raceManager.PlayerRacer;
            if (playerRacer == null)
                return false;

            distance = Vector3.Distance(transform.position, playerTarget.position);
            if (distance > 48f)
                return false;

            var aiProgress = raceManager.GetRaceProgress(racer);
            var playerProgress = raceManager.GetRaceProgress(playerRacer);
            progressDelta = aiProgress - playerProgress;
            return Mathf.Abs(progressDelta) < ProgressNearThreshold * 2.5f;
        }

        bool IsOnStraight()
        {
            return ai != null && ai.EstimatedUpcomingTurnAngle < StraightTurnAngleMax;
        }

        void DropBananaBehind()
        {
            if (droppedHazardRoot == null)
            {
                var root = new GameObject("AIRivalDrops");
                droppedHazardRoot = root.transform;
            }

            var dropPosition = transform.position - transform.forward * 5f;
            var rotation = transform.rotation * Quaternion.Euler(0f, Random.Range(-24f, 24f), 0f);
            BananaHazardFactory.Spawn(dropPosition, rotation, droppedHazardRoot,
                "AIRivalBanana_" + gameObject.name);
        }

        bool RollChance(float chance)
        {
            var roll = Mathf.PerlinNoise(Time.time * 0.17f + rivalSeed, rivalSeed * 0.31f);
            return roll < Mathf.Clamp01(chance);
        }

        public void OnNitroPickupCollected()
        {
            nitroCharges = Mathf.Min(nitroCharges + 1, maxNitroCharges);
            if (nitro != null && !nitro.IsDisabled && !nitro.IsActive && nitroCharges > 0 && IsOnStraight() &&
                RollChance(0.55f))
            {
                nitro.ActivateBoost(usePickupImpulse: true);
                nitroCharges--;
            }
        }

        public void OnNitroZoneEntered()
        {
            if (!usesNitroZones || nitro == null || nitro.IsDisabled || racer == null || racer.IsFinished ||
                racer.IsEliminated)
                return;

            if (!IsOnStraight())
                return;

            nitroCharges = Mathf.Min(nitroCharges + 1, maxNitroCharges);

            if (!IsNearPlayerOnTrack(out var progressDelta, out _))
                return;

            if (progressDelta > -0.005f)
                return;

            nitro.ActivateBoost(usePickupImpulse: true);
            nitroCharges = Mathf.Max(0, nitroCharges - 1);
            nextNitroTime = Time.time + NitroCooldownBase * 0.65f;
        }
    }
}
