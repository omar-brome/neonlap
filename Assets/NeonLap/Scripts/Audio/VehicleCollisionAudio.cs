using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Audio
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleCollisionAudio : MonoBehaviour
    {
        [SerializeField] float minImpactSpeed = 4f;
        [SerializeField] float minCrowdGroanSpeed = 6.5f;

        Rigidbody rb;
        VehicleAudioController vehicleAudio;
        bool isPlayer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            vehicleAudio = GetComponent<VehicleAudioController>();
            isPlayer = CompareTag("Player");
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!CollisionHazardUtility.IsHazard(collision.collider, this))
                return;

            if (vehicleAudio == null)
                return;

            var relativeSpeed = collision.relativeVelocity.magnitude;
            if (relativeSpeed < minImpactSpeed)
                return;

            vehicleAudio.PlayImpact(relativeSpeed);

            if (isPlayer && relativeSpeed >= minCrowdGroanSpeed)
                CrowdReactionHub.Emit(CrowdReactionKind.Groan);
        }
    }
}
