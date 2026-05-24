using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class RaceStartGate : MonoBehaviour
    {
        Rigidbody rb;
        RaceManager raceManager;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        void Start()
        {
            raceManager = FindAnyObjectByType<RaceManager>();
            if (raceManager == null)
            {
                rb.isKinematic = false;
                return;
            }

            raceManager.OnStateChanged += HandleRaceStateChanged;
            ApplyGate(raceManager.State);

            if (raceManager.State == RaceState.Countdown)
                raceManager.OnCountdownTick += HandleCountdownTick;
        }

        void OnDestroy()
        {
            if (raceManager != null)
            {
                raceManager.OnStateChanged -= HandleRaceStateChanged;
                raceManager.OnCountdownTick -= HandleCountdownTick;
            }
        }

        void HandleCountdownTick(int value)
        {
            if (value != 0)
                return;

            rb.isKinematic = false;
            raceManager.OnCountdownTick -= HandleCountdownTick;
        }

        void HandleRaceStateChanged(RaceState state)
        {
            ApplyGate(state);
        }

        void ApplyGate(RaceState state)
        {
            var allowDriving = state == RaceState.Racing || state == RaceState.Finished;

            if (!allowDriving)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                rb.isKinematic = true;
                return;
            }

            rb.isKinematic = false;
        }
    }
}
