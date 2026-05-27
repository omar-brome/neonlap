using System.Collections;
using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    public class FuelPadPickup : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 12f;
        [SerializeField] float pulseSpeed = 2.1f;

        Collider pickupCollider;
        Transform visualRoot;
        Renderer[] renderers;
        Vector3 visualBaseScale;
        bool collected;
        float phaseOffset;

        void Awake()
        {
            pickupCollider = GetComponent<Collider>();
            visualRoot = transform.Find("Visual");
            if (visualRoot != null)
            {
                visualBaseScale = visualRoot.localScale;
                renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            }

            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            if (collected || visualRoot == null)
                return;

            var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + phaseOffset) * 0.06f;
            visualRoot.localScale = visualBaseScale * pulse;
        }

        void OnTriggerEnter(Collider other)
        {
            TryCollect(other);
        }

        void TryCollect(Collider other)
        {
            if (collected)
                return;

            var racer = other.GetComponentInParent<RacerProgress>();
            if (racer == null || !racer.IsPlayer || racer.IsFinished || racer.IsEliminated)
                return;

            var raceManager = FindAnyObjectByType<RaceManager>();
            if (raceManager != null && raceManager.State != RaceState.Racing)
                return;

            var fuel = racer.GetComponent<VehicleFuelSystem>();
            if (fuel == null || fuel.IsInfinite)
                return;

            if (!fuel.RefillFromPad())
                return;

            StartCoroutine(CollectAndRespawn());
        }

        IEnumerator CollectAndRespawn()
        {
            collected = true;

            if (pickupCollider != null)
                pickupCollider.enabled = false;

            SetVisualsVisible(false);
            yield return new WaitForSeconds(respawnDelay);

            collected = false;

            if (pickupCollider != null)
                pickupCollider.enabled = true;

            SetVisualsVisible(true);
        }

        void SetVisualsVisible(bool visible)
        {
            if (renderers == null)
                return;

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }
        }
    }
}
