using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Environment
{
    [RequireComponent(typeof(Rigidbody))]
    public class RollingBarrelImpact : MonoBehaviour
    {
        [SerializeField] float minKnockSpeed = 5.5f;
        [SerializeField] float knockStrength = 5.5f;
        [SerializeField] float playerInfluenceWindow = 3f;
        [SerializeField] float playerInfluenceRadius = 22f;

        Rigidbody rb;
        float lastPlayerInfluenceTime = -999f;
        float nextKnockScoreTime;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void OnCollisionEnter(Collision collision)
        {
            var vehicleBody = collision.rigidbody ?? collision.collider.attachedRigidbody;
            if (vehicleBody == null)
                return;

            var racer = vehicleBody.GetComponent<RacerProgress>();
            if (racer == null)
                return;

            if (racer.IsPlayer)
            {
                lastPlayerInfluenceTime = Time.time;
                return;
            }

            var rival = vehicleBody.GetComponent<AIVehicleController>();
            if (rival == null)
                return;

            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minKnockSpeed)
                return;

            ApplyKnock(vehicleBody, collision);
            TryAwardKnockScore(rival, impactSpeed);
        }

        void ApplyKnock(Rigidbody vehicleBody, Collision collision)
        {
            if (vehicleBody == null || collision.contactCount == 0)
                return;

            var contact = collision.GetContact(0);
            var pushDir = contact.normal;
            if (pushDir.sqrMagnitude < 0.01f)
                pushDir = (vehicleBody.position - transform.position).normalized;

            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.01f)
                return;

            pushDir.Normalize();
            var strength = Mathf.Lerp(knockStrength * 0.65f, knockStrength * 1.35f,
                Mathf.InverseLerp(minKnockSpeed, 18f, collision.relativeVelocity.magnitude));
            vehicleBody.AddForce(pushDir * strength, ForceMode.VelocityChange);
        }

        void TryAwardKnockScore(AIVehicleController rival, float impactSpeed)
        {
            if (Time.time < nextKnockScoreTime)
                return;

            var player = FindAnyObjectByType<RaceManager>()?.PlayerRacer;
            if (player == null)
                return;

            var playerInvolved = Time.time - lastPlayerInfluenceTime <= playerInfluenceWindow;
            if (!playerInvolved)
            {
                var distance = Vector3.Distance(player.transform.position, transform.position);
                playerInvolved = distance <= playerInfluenceRadius && impactSpeed >= minKnockSpeed + 2f;
            }

            if (!playerInvolved)
                return;

            var scoreSystem = player.GetComponent<RaceScoreSystem>();
            scoreSystem?.RegisterBarrelKnock(rival, playerInvolved && Time.time - lastPlayerInfluenceTime <= playerInfluenceWindow);
            nextKnockScoreTime = Time.time + 1.25f;
        }
    }
}
