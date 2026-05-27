using System;
using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class PlayerHeatSystem : MonoBehaviour
    {
        [SerializeField] float maxHeat = 100f;
        [SerializeField] float policeRamHeat = 18f;
        [SerializeField] float decayPerSecond = 10f;
        [SerializeField] float safeDecayBoost = 1.35f;
        [SerializeField] float policeProximityRange = 42f;

        RaceManager raceManager;
        float currentHeat;
        bool subscribed;

        public float NormalizedHeat => maxHeat <= 0.01f ? 0f : Mathf.Clamp01(currentHeat / maxHeat);
        public bool IsBusted => currentHeat >= maxHeat - 0.01f;

        public event Action HeatMaxed;

        public static PlayerHeatSystem Setup(GameObject playerCar, RaceManager manager)
        {
            if (!GameRaceModeSettings.IsChase || playerCar == null)
                return null;

            var heat = playerCar.GetComponent<PlayerHeatSystem>();
            if (heat == null)
                heat = playerCar.AddComponent<PlayerHeatSystem>();
            heat.Configure(manager);
            return heat;
        }

        public void Configure(RaceManager manager)
        {
            raceManager = manager;
            ResetHeat();
            Subscribe();
        }

        public void ResetHeat()
        {
            currentHeat = 0f;
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
                ResetHeat();
        }

        void Update()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing || IsBusted)
                return;

            var decay = decayPerSecond;
            if (!IsPoliceNearby())
                decay *= safeDecayBoost;

            currentHeat = Mathf.Max(0f, currentHeat - decay * Time.deltaTime);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing || IsBusted)
                return;

            if (collision.collider.GetComponentInParent<PoliceChaseVehicle>() == null)
                return;

            AddHeat(policeRamHeat);
        }

        public void AddHeat(float amount)
        {
            if (amount <= 0f || IsBusted)
                return;

            currentHeat = Mathf.Min(maxHeat, currentHeat + amount);
            if (IsBusted)
                HeatMaxed?.Invoke();
        }

        bool IsPoliceNearby()
        {
            var policeUnits = FindObjectsByType<PoliceChaseVehicle>();
            var rangeSq = policeProximityRange * policeProximityRange;
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
    }
}
