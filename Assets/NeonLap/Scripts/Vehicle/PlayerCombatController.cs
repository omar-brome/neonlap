using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Input;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(RacerProgress))]
    [RequireComponent(typeof(VehicleCombatShield))]
    public class PlayerCombatController : MonoBehaviour
    {
        const float BananaDropCooldown = 0.85f;
        const float EmpCooldown = 12f;
        const float EmpRange = 38f;
        const float EmpDisableDuration = 4.5f;
        const float MinRaceSpeedToDeploy = 4f;

        [SerializeField] int startingBananaCharges = 3;
        [SerializeField] int maxBananaCharges = 3;

        PlayerInputReader inputReader;
        RacerProgress racer;
        VehicleCombatShield shield;
        RaceManager raceManager;
        Transform droppedHazardRoot;

        int bananaCharges;
        float nextBananaTime;
        float nextEmpTime;

        public int BananaCharges => bananaCharges;
        public int MaxBananaCharges => maxBananaCharges;
        public bool ShieldActive => shield != null && shield.IsActive;
        public float EmpCooldownRemaining => Mathf.Max(0f, nextEmpTime - Time.time);

        public void Configure(PlayerInputReader reader, RaceManager manager = null)
        {
            UnsubscribeRace();
            inputReader = reader;
            if (manager != null)
                raceManager = manager;
            SubscribeRace();
            ResetCombat();
        }

        void Awake()
        {
            racer = GetComponent<RacerProgress>();
            shield = GetComponent<VehicleCombatShield>();
        }

        void OnDisable() => UnsubscribeRace();

        void SubscribeRace()
        {
            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();

            if (raceManager != null)
                raceManager.OnStateChanged += HandleRaceStateChanged;
        }

        void UnsubscribeRace()
        {
            if (raceManager != null)
                raceManager.OnStateChanged -= HandleRaceStateChanged;
        }

        void HandleRaceStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
                ResetCombat();
        }

        void Update()
        {
            if (inputReader == null || racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();

            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (inputReader.DropBananaPressed)
                TryDropBanana();

            if (inputReader.EmpPulsePressed)
                TryEmpPulse();
        }

        public void ResetCombat()
        {
            bananaCharges = startingBananaCharges;
            nextBananaTime = 0f;
            nextEmpTime = 0f;
            shield?.ResetShield();
        }

        void TryDropBanana()
        {
            if (bananaCharges <= 0 || Time.time < nextBananaTime)
                return;

            var rb = GetComponent<Rigidbody>();
            if (rb == null || rb.linearVelocity.magnitude < MinRaceSpeedToDeploy)
                return;

            if (droppedHazardRoot == null)
            {
                var root = new GameObject("PlayerCombatDrops");
                droppedHazardRoot = root.transform;
            }

            var dropPosition = transform.position - transform.forward * 5.5f;
            var rotation = transform.rotation * Quaternion.Euler(0f, Random.Range(-18f, 18f), 0f);
            BananaHazardFactory.Spawn(dropPosition, rotation, droppedHazardRoot, "PlayerBanana");

            bananaCharges--;
            nextBananaTime = Time.time + BananaDropCooldown;
            StadiumIncidentHub.Report("BANANA DROPPED");
        }

        void TryEmpPulse()
        {
            if (Time.time < nextEmpTime)
                return;

            var target = FindNearestEmpTarget();
            if (target == null)
                return;

            var nitro = target.GetComponent<VehicleNitroBoost>();
            if (nitro != null)
                nitro.DisableBoost(EmpDisableDuration);

            nextEmpTime = Time.time + EmpCooldown;
            var placement = raceManager != null ? raceManager.GetRacerPlacement(target.GetComponent<RacerProgress>()) : 0;
            StadiumIncidentHub.Report(placement > 0 ? $"EMP HIT P{placement}" : "EMP HIT");
        }

        AIVehicleController FindNearestEmpTarget()
        {
            var rivals = FindObjectsByType<AIVehicleController>(FindObjectsInactive.Exclude);
            AIVehicleController nearest = null;
            var nearestDistance = EmpRange;

            for (var i = 0; i < rivals.Length; i++)
            {
                var rival = rivals[i];
                if (rival == null)
                    continue;

                var rivalRacer = rival.GetComponent<RacerProgress>();
                if (rivalRacer == null || rivalRacer.IsFinished || rivalRacer.IsEliminated)
                    continue;

                var distance = Vector3.Distance(transform.position, rival.transform.position);
                if (distance >= nearestDistance)
                    continue;

                nearest = rival;
                nearestDistance = distance;
            }

            return nearest;
        }
    }
}
