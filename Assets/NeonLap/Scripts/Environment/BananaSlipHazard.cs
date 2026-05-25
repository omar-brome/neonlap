using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    public class BananaSlipHazard : MonoBehaviour
    {
        [SerializeField] float minImpactSpeed = 2.5f;

        void OnTriggerEnter(Collider other)
        {
            TrySlip(other);
        }

        void OnTriggerStay(Collider other)
        {
            TrySlip(other);
        }

        void TrySlip(Collider other)
        {
            var racer = other.GetComponentInParent<RacerProgress>();
            if (racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            var raceManager = FindAnyObjectByType<RaceManager>();
            if (raceManager != null && raceManager.State != RaceState.Racing)
                return;

            var slip = racer.GetComponent<VehicleSlipEffect>();
            if (slip == null)
                return;

            var rb = racer.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic)
                return;

            if (rb.linearVelocity.magnitude < minImpactSpeed)
                return;

            slip.ApplyBananaSlip(transform.position, rb.linearVelocity.magnitude);
        }
    }
}
