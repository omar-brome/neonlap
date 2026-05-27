using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    public class RepairPadPickup : MonoBehaviour
    {
        [SerializeField] float pulseSpeed = 2.4f;

        Transform visualRoot;
        Renderer[] renderers;
        Vector3 visualBaseScale;
        float phaseOffset;

        void Awake()
        {
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
            if (visualRoot == null)
                return;

            var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + phaseOffset) * 0.08f;
            visualRoot.localScale = visualBaseScale * pulse;
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.GetComponentInParent<RacerProgress>();
            if (racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            var raceManager = FindAnyObjectByType<RaceManager>();
            if (raceManager != null && raceManager.State != RaceState.Racing)
                return;

            var tracker = racer.GetComponent<RepairPadLapTracker>();
            if (tracker == null)
                tracker = racer.gameObject.AddComponent<RepairPadLapTracker>();

            if (!tracker.CanUseRepairPad(racer.CurrentLap))
                return;

            var health = racer.GetComponent<VehicleHealthSystem>();
            if (health == null || !health.enabled || health.IsTotalled)
                return;

            health.RestoreHealth();
            racer.GetComponent<VehicleDamageSystem>()?.RestoreVisuals();
            tracker.MarkRepaired(racer.CurrentLap);

            var label = racer.IsPlayer ? "PLAYER" : "RIVAL";
            StadiumIncidentHub.Report($"{label} REPAIRED");
        }
    }
}
