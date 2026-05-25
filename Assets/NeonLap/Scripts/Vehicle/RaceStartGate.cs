using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class RaceStartGate : MonoBehaviour
    {
        Rigidbody rb;
        RaceManager raceManager;
        bool subscribed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            Freeze();
        }

        void OnEnable()
        {
            TrySubscribe();
        }

        void Update()
        {
            if (!subscribed)
                TrySubscribe();
        }

        void OnDestroy()
        {
            if (raceManager != null)
                raceManager.OnStateChanged -= HandleRaceStateChanged;
        }

        void TrySubscribe()
        {
            if (subscribed)
                return;

            raceManager = FindAnyObjectByType<RaceManager>();
            if (raceManager == null)
            {
                Freeze();
                return;
            }

            raceManager.OnStateChanged += HandleRaceStateChanged;
            subscribed = true;
            ApplyGate(raceManager.State);
        }

        void HandleRaceStateChanged(RaceState state)
        {
            ApplyGate(state);
        }

        void ApplyGate(RaceState state)
        {
            if (state == RaceState.Racing || state == RaceState.Finished)
                Release();
            else
                Freeze();
        }

        void Freeze()
        {
            if (rb == null)
                return;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
        }

        void Release()
        {
            if (rb != null)
                rb.isKinematic = false;
        }
    }
}
