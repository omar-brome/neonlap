using System.Collections;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    public class NitroPickup : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 14f;
        [SerializeField] float bobAmplitude = 0.12f;
        [SerializeField] float bobSpeed = 2.4f;
        [SerializeField] float spinSpeed = 90f;

        Collider pickupCollider;
        Transform visualRoot;
        Renderer[] renderers;
        Vector3 visualBaseLocalPosition;
        bool collected;
        float phaseOffset;

        public bool IsAvailable => isActiveAndEnabled && !collected;

        public Vector3 WorldPosition => transform.position;

        void OnEnable() => NitroPickupRegistry.Register(this);

        void OnDisable() => NitroPickupRegistry.Unregister(this);

        void Awake()
        {
            pickupCollider = GetComponent<Collider>();
            visualRoot = transform.Find("Visual");
            if (visualRoot != null)
            {
                visualBaseLocalPosition = visualRoot.localPosition;
                renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            }

            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            if (collected || visualRoot == null)
                return;

            var bob = Mathf.Sin(Time.time * bobSpeed + phaseOffset) * bobAmplitude;
            visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * bob;
            visualRoot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
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
            if (racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            var raceManager = FindAnyObjectByType<RaceManager>();
            if (raceManager != null && raceManager.State != RaceState.Racing)
                return;

            var nitro = racer.GetComponent<VehicleNitroBoost>();
            if (nitro == null)
                return;

            var aiCombat = racer.GetComponent<AICombatController>();
            if (racer.IsPlayer)
            {
                nitro.AddCharge(1);
                racer.GetComponent<RaceScoreSystem>()?.RegisterNitroPickup();
                StadiumIncidentHub.Report("NITRO COLLECTED");
            }
            else if (aiCombat != null && aiCombat.CanCollectPickups)
            {
                aiCombat.OnNitroPickupCollected();
            }
            else
            {
                return;
            }

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
