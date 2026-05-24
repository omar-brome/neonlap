using UnityEngine;

namespace NeonLap.Vehicle
{
    [CreateAssetMenu(fileName = "VehicleProfile", menuName = "NeonLap/Vehicle Profile")]
    public class VehicleProfile : ScriptableObject
    {
        [Header("Speed")]
        public float maxSpeed = 45f;
        public float acceleration = 35f;
        public float brakeForce = 50f;
        public float reverseForce = 20f;

        [Header("Steering")]
        public float turnSpeedLow = 120f;
        public float turnSpeedHigh = 45f;

        [Header("Grip")]
        public float grip = 12f;
        [Range(0f, 1f)] public float driftGripMultiplier = 0.4f;
        public float driftRecovery = 6f;

        [Header("Hover")]
        public float hoverHeight = 1.2f;
        public float hoverForce = 80f;
        public float hoverDamping = 8f;
        public float downforce = 25f;
    }
}
