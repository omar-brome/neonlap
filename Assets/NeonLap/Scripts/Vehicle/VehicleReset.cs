using NeonLap.Input;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleReset : MonoBehaviour
    {
        [SerializeField] MonoBehaviour inputProviderBehaviour;
        [SerializeField] float fallYThreshold = -5f;
        [SerializeField] float offTrackResetDelay = 3f;
        [SerializeField] float aiOffTrackResetDelay = 0.8f;
        [SerializeField] float startupGracePeriod = 2f;
        [SerializeField] float aiEliminationDepth = 250f;

        Rigidbody rb;
        IVehicleInputProvider inputProvider;
        AIVehicleController aiController;
        RacerProgress racerProgress;
        RaceManager raceManager;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Transform respawnPoint;
        float offTrackTimer;
        float spawnTime;

        public void Configure(IVehicleInputProvider provider)
        {
            inputProvider = provider;
            inputProviderBehaviour = provider as MonoBehaviour;
        }

        public void ConfigureForAi(AIVehicleController ai)
        {
            aiController = ai;
        }

        public void SetSpawnPoint(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        public void SetRespawnPoint(Transform point)
        {
            respawnPoint = point;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnTime = Time.time;
            racerProgress = GetComponent<RacerProgress>();
            raceManager = FindAnyObjectByType<RaceManager>();

            inputProvider = inputProviderBehaviour as IVehicleInputProvider;
            if (inputProvider == null && inputProviderBehaviour != null)
                inputProvider = inputProviderBehaviour.GetComponent<IVehicleInputProvider>();
            if (inputProvider == null)
                inputProvider = GetComponent<IVehicleInputProvider>();
        }

        void Update()
        {
            if (inputProvider != null && inputProvider.ResetPressed)
            {
                var fuel = GetComponent<VehicleFuelSystem>();
                if (fuel != null && fuel.TryRefill())
                    return;

                ResetVehicle();
            }

            if (Time.time - spawnTime < startupGracePeriod)
                return;

            if (IsAiEliminated())
                return;

            if (transform.position.y < fallYThreshold)
                ResetVehicle();
        }

        void FixedUpdate()
        {
            if (Time.time - spawnTime < startupGracePeriod)
                return;

            if (rb.isKinematic || IsAiEliminated())
                return;

            var probe = GetComponent<VehicleGroundProbe>();
            if (probe == null)
                return;

            if (probe.Probe().IsGrounded && (aiController == null || !aiController.IsOffTrack()))
            {
                offTrackTimer = 0f;
                return;
            }

            offTrackTimer += Time.fixedDeltaTime;
            var resetDelay = aiController != null ? aiOffTrackResetDelay : offTrackResetDelay;
            if (offTrackTimer >= resetDelay)
            {
                if (aiController != null)
                {
                    var health = GetComponent<AIVehicleHealthSystem>();
                    if (health != null && !health.IsTotalled)
                    {
                        health.ApplyOffTrackDamage();
                        if (health.IsTotalled)
                            return;

                        offTrackTimer = 0f;
                        aiController.RecoverToTrack();
                        return;
                    }
                }

                ResetVehicle();
            }
        }

        public void ResetVehicle()
        {
            if (aiController != null)
            {
                if (IsAiEliminated() || !IsRaceActive())
                    return;

                var health = GetComponent<AIVehicleHealthSystem>();
                if (health != null && !health.IsTotalled)
                {
                    health.ApplyFallDamage();
                    if (health.IsTotalled)
                        return;

                    offTrackTimer = 0f;
                    aiController.RecoverToTrack();
                    return;
                }

                EliminateAi();
                return;
            }

            offTrackTimer = 0f;
            spawnTime = Time.time;

            var position = respawnPoint != null ? respawnPoint.position : spawnPosition;
            var rotation = respawnPoint != null ? respawnPoint.rotation : spawnRotation;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.MovePosition(position + Vector3.up * 0.5f);
            rb.MoveRotation(rotation);

            GetComponent<VehicleDamageSystem>()?.RestoreVisuals();
        }

        public void RestoreForRaceRestart()
        {
            if (aiController == null)
                return;

            gameObject.SetActive(true);
            aiController.enabled = true;
            GetComponent<AIVehicleHealthSystem>()?.Restore();
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(spawnPosition + Vector3.up * 0.5f);
            rb.MoveRotation(spawnRotation);
            offTrackTimer = 0f;
            spawnTime = Time.time;
            GetComponent<VehicleDamageSystem>()?.RestoreVisuals();
        }

        void EliminateAi()
        {
            offTrackTimer = 0f;

            var eliminationTime = raceManager != null ? raceManager.RaceTime : Time.time;
            racerProgress?.MarkEliminated(eliminationTime);

            aiController.enabled = false;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;

            var awayPosition = spawnPosition
                               + Vector3.down * aiEliminationDepth
                               + spawnRotation * Vector3.back * 120f;
            rb.MovePosition(awayPosition);

            GetComponent<VehicleDamageSystem>()?.RestoreVisuals();
            gameObject.SetActive(false);
        }

        bool IsAiEliminated()
        {
            return aiController != null && racerProgress != null && racerProgress.IsEliminated;
        }

        bool IsRaceActive()
        {
            return raceManager != null && raceManager.State == RaceState.Racing;
        }
    }
}
