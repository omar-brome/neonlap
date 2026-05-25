using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class VehicleFuelSystem : MonoBehaviour
    {
        [SerializeField] float tankDuration = 420f;

        RaceManager raceManager;
        float fuelRemaining;
        bool depleting;

        public float NormalizedFuel =>
            tankDuration <= 0.01f ? 0f : Mathf.Clamp01(fuelRemaining / tankDuration);

        public bool IsEmpty => fuelRemaining <= 0.01f;

        public void Configure(float duration, RaceManager manager)
        {
            if (duration > 0.01f)
                tankDuration = duration;

            raceManager = manager;
            ResetTank();
        }

        public void ResetTank()
        {
            fuelRemaining = tankDuration;
            depleting = false;
        }

        public bool TryRefill()
        {
            if (!IsEmpty)
                return false;

            fuelRemaining = tankDuration;
            return true;
        }

        void Awake()
        {
            raceManager ??= FindAnyObjectByType<RaceManager>();
            ResetTank();
        }

        void Update()
        {
            if (raceManager == null)
                return;

            switch (raceManager.State)
            {
                case RaceState.Waiting:
                case RaceState.Countdown:
                    ResetTank();
                    break;
                case RaceState.Racing:
                    if (!depleting)
                    {
                        depleting = true;
                        fuelRemaining = tankDuration;
                    }

                    fuelRemaining = Mathf.Max(0f, fuelRemaining - Time.deltaTime);
                    break;
            }
        }
    }
}
