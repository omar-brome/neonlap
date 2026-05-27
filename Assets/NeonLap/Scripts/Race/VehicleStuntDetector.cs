using System;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public class VehicleStuntDetector : MonoBehaviour
    {
        [SerializeField] float minAirSeconds = 0.35f;
        [SerializeField] float minAirSpeed = 8f;

        VehicleGroundProbe probe;
        Rigidbody rb;
        float airStartTime;
        float sessionBestAir;
        int trickCount;
        int sessionScore;
        bool wasGrounded = true;

        public int SessionScore => sessionScore;
        public int TrickCount => trickCount;
        public float SessionBestAir => sessionBestAir;
        public bool IsAirborne
        {
            get
            {
                if (probe == null)
                    return false;
                return !probe.Probe().IsGrounded;
            }
        }

        public event Action<int, float> OnTrickLanded;

        void Awake()
        {
            probe = GetComponent<VehicleGroundProbe>();
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (probe == null)
                return;

            var grounded = probe.Probe().IsGrounded;
            if (!wasGrounded && grounded)
                CompleteTrick();

            if (wasGrounded && !grounded)
                airStartTime = Time.time;

            wasGrounded = grounded;
        }

        void CompleteTrick()
        {
            var airTime = Time.time - airStartTime;
            if (airTime < minAirSeconds)
                return;

            var speed = rb != null ? rb.linearVelocity.magnitude : 0f;
            if (speed < minAirSpeed)
                return;

            sessionBestAir = Mathf.Max(sessionBestAir, airTime);
            var points = Mathf.RoundToInt(120f * airTime + speed * 4f);
            sessionScore += points;
            trickCount++;
            OnTrickLanded?.Invoke(points, airTime);
        }
    }
}
