using System.Collections;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    public class BananaSlipHazard : MonoBehaviour
    {
        [SerializeField] float minImpactSpeed = 2.5f;
        [SerializeField] float perRacerCooldown = 1.1f;

        bool respawnAfterSlip;
        float respawnDelay;
        float lastSlipTime;
        Coroutine respawnRoutine;

        public void ResetForSpawn(bool respawn, float delay)
        {
            respawnAfterSlip = respawn;
            respawnDelay = Mathf.Max(4f, delay);
            lastSlipTime = 0f;
            CancelRespawn();
        }

        public void CancelRespawn()
        {
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
        }

        void OnTriggerEnter(Collider other) => TrySlip(other);

        void OnTriggerStay(Collider other) => TrySlip(other);

        void TrySlip(Collider other)
        {
            var racer = other.GetComponentInParent<RacerProgress>();
            if (racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            if (Time.time - lastSlipTime < perRacerCooldown)
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
            lastSlipTime = Time.time;

            if (respawnAfterSlip)
                respawnRoutine = StartCoroutine(RespawnAfterDelay());
        }

        IEnumerator RespawnAfterDelay()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            yield return new WaitForSeconds(respawnDelay);

            if (collider != null)
                collider.enabled = true;

            respawnRoutine = null;
        }
    }
}
