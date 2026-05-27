using NeonLap.Input;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.VFX
{
    public class DriftTrailVFX : MonoBehaviour
    {
        [SerializeField] VehicleController vehicle;
        [SerializeField] TrailRenderer leftTrail;
        [SerializeField] TrailRenderer rightTrail;
        [SerializeField] float lateralSlipThreshold = 2.4f;
        [SerializeField] float longDriftThreshold = 0.85f;
        [SerializeField] float maxDriftBoostDuration = 2.4f;

        static readonly Color BaseTrailColor = new(0.35f, 0.95f, 1f, 0.55f);
        static readonly Color LongDriftColor = new(1f, 0.72f, 0.2f, 0.95f);

        IVehicleInputProvider inputProvider;
        DriftCameraShake cameraShake;
        float handbrakeDriftDuration;

        public float DriftIntensity { get; private set; }

        void Awake()
        {
            if (vehicle == null)
                vehicle = GetComponentInParent<VehicleController>();

            if (vehicle != null)
                inputProvider = vehicle.GetComponent<IVehicleInputProvider>();

            cameraShake = FindAnyObjectByType<DriftCameraShake>();
        }

        void Update()
        {
            if (vehicle == null)
                return;

            var slipping = vehicle.GetComponent<VehicleSlipEffect>()?.IsSlipping ?? false;
            var handbrake = inputProvider != null && inputProvider.DriftHeld;
            var drifting = vehicle.IsDrifting || vehicle.LateralSpeed >= lateralSlipThreshold || slipping;
            var longDrift = handbrake && drifting;

            if (longDrift)
                handbrakeDriftDuration = Mathf.Min(handbrakeDriftDuration + Time.deltaTime, maxDriftBoostDuration);
            else
                handbrakeDriftDuration = Mathf.Max(0f, handbrakeDriftDuration - Time.deltaTime * 2.5f);

            var intensity = longDrift
                ? Mathf.Clamp01(handbrakeDriftDuration / longDriftThreshold)
                : drifting ? 0.35f : 0f;
            DriftIntensity = intensity;

            ApplyTrail(leftTrail, drifting, intensity);
            ApplyTrail(rightTrail, drifting, intensity);

            if (cameraShake != null && handbrake && intensity > 0.2f)
                cameraShake.ReportDriftIntensity(intensity);
        }

        static void ApplyTrail(TrailRenderer trail, bool emit, float intensity)
        {
            if (trail == null)
                return;

            trail.emitting = emit;
            if (!emit)
                return;

            var boost = Mathf.Clamp01(intensity);
            trail.startWidth = Mathf.Lerp(0.35f, 1.05f, boost);
            trail.endWidth = Mathf.Lerp(0.05f, 0.18f, boost);
            trail.time = Mathf.Lerp(0.35f, 0.95f, boost);
            var color = Color.Lerp(BaseTrailColor, LongDriftColor, boost);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0.02f);
        }
    }
}
