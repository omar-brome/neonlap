using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(AIVehicleController))]
    public class RivalGrudgeController : MonoBehaviour
    {
        const float BumpResetWindow = 12f;
        const int BumpsToBlock = 2;
        const float BlockDuration = 3f;
        const float MinBumpSpeed = 5f;

        AIVehicleController ai;
        Transform playerTransform;
        int playerBumpCount;
        float lastBumpTime;

        public void Configure(AIVehicleController controller, Transform player)
        {
            ai = controller;
            playerTransform = player;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (ai == null || playerTransform == null)
                return;

            var racer = collision.collider.GetComponentInParent<RacerProgress>();
            if (racer == null || !racer.IsPlayer)
                return;

            if (collision.relativeVelocity.magnitude < MinBumpSpeed)
                return;

            if (Time.time - lastBumpTime > BumpResetWindow)
                playerBumpCount = 0;

            lastBumpTime = Time.time;
            playerBumpCount++;

            if (playerBumpCount >= BumpsToBlock)
            {
                ai.ActivatePlayerBlock(BlockDuration);
                playerBumpCount = 0;
                Environment.StadiumIncidentHub.Report($"{GetBroadcastName()} BLOCKING");
            }
        }

        string GetBroadcastName()
        {
            var identity = GetComponent<RivalIdentity>();
            return identity != null ? identity.ShortName : "RIVAL";
        }
    }
}
