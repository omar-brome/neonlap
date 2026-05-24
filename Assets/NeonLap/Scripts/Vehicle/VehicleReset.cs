using NeonLap.Input;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleReset : MonoBehaviour
    {
        [SerializeField] MonoBehaviour inputProviderBehaviour;
        [SerializeField] float fallYThreshold = -5f;
        [SerializeField] float offTrackResetDelay = 3f;
        [SerializeField] float startupGracePeriod = 2f;

        Rigidbody rb;
        IVehicleInputProvider inputProvider;
        AIVehicleController aiController;
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

            inputProvider = inputProviderBehaviour as IVehicleInputProvider;
            if (inputProvider == null && inputProviderBehaviour != null)
                inputProvider = inputProviderBehaviour.GetComponent<IVehicleInputProvider>();
            if (inputProvider == null)
                inputProvider = GetComponent<IVehicleInputProvider>();
        }

        void Update()
        {
            if (inputProvider != null && inputProvider.ResetPressed)
                ResetVehicle();

            if (Time.time - spawnTime < startupGracePeriod)
                return;

            if (transform.position.y < fallYThreshold)
                ResetVehicle();
        }

        void FixedUpdate()
        {
            if (Time.time - spawnTime < startupGracePeriod)
                return;

            if (rb.isKinematic)
                return;
            var probe = GetComponent<VehicleGroundProbe>();
            if (probe == null)
                return;

            if (probe.Probe().IsGrounded)
            {
                offTrackTimer = 0f;
                return;
            }

            offTrackTimer += Time.fixedDeltaTime;
            if (offTrackTimer >= offTrackResetDelay)
                ResetVehicle();
        }

        public void ResetVehicle()
        {
            offTrackTimer = 0f;
            spawnTime = Time.time;

            if (aiController != null)
            {
                aiController.RecoverToTrack();
                return;
            }

            var position = respawnPoint != null ? respawnPoint.position : spawnPosition;
            var rotation = respawnPoint != null ? respawnPoint.rotation : spawnRotation;

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.MovePosition(position + Vector3.up * 0.5f);
            rb.MoveRotation(rotation);
        }
    }
}
