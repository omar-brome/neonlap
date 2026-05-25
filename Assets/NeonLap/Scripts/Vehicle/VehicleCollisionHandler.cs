using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleCollisionHandler : MonoBehaviour
    {
        [SerializeField] float separationBoost = 1.6f;
        [SerializeField] float impactDamping = 0.93f;
        [SerializeField] float vehicleSeparationBoost = 0.45f;
        [SerializeField] float vehicleImpactDamping = 0.98f;
        [SerializeField] float vehicleSoftCollisionSpeed = 9f;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!CollisionHazardUtility.IsHazard(collision.collider, this))
                return;

            if (IsSoftVehicleCollision(collision))
                return;

            ResolveImpact(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            if (!CollisionHazardUtility.IsHazard(collision.collider, this))
                return;

            if (collision.contactCount == 0)
                return;

            if (IsSoftVehicleCollision(collision))
                return;

            var contact = collision.GetContact(0);
            var intoSurface = Vector3.Dot(rb.linearVelocity, contact.normal);
            if (intoSurface >= -0.35f)
                return;

            var isVehicle = collision.collider.gameObject.layer == NeonLapLayers.Vehicle;
            var pushStrength = isVehicle ? vehicleSeparationBoost : separationBoost;
            rb.linearVelocity -= contact.normal * intoSurface * (isVehicle ? 0.12f : 0.22f);
            rb.AddForce(contact.normal * pushStrength, ForceMode.Acceleration);
        }

        bool IsSoftVehicleCollision(Collision collision)
        {
            if (collision.collider.gameObject.layer != NeonLapLayers.Vehicle)
                return false;

            var otherBody = collision.rigidbody ?? collision.collider.attachedRigidbody;
            if (otherBody == null)
                return false;

            return (rb.linearVelocity - otherBody.linearVelocity).magnitude < vehicleSoftCollisionSpeed;
        }

        void ResolveImpact(Collision collision)
        {
            if (collision.contactCount == 0)
                return;

            var isVehicle = collision.collider.gameObject.layer == NeonLapLayers.Vehicle;
            var damping = isVehicle ? vehicleImpactDamping : impactDamping;
            var boost = isVehicle ? vehicleSeparationBoost : separationBoost;

            var contact = collision.GetContact(0);
            var normal = contact.normal;
            var velocity = rb.linearVelocity;
            var intoSurface = Vector3.Dot(velocity, normal);

            if (intoSurface < 0f)
                rb.linearVelocity -= normal * (intoSurface * (isVehicle ? 0.45f : 0.85f) + boost * Time.fixedDeltaTime);

            rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, normal) * damping;
        }
    }
}
