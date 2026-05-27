using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleTrackZoneResponder : MonoBehaviour
    {
        const float OverdrivePassiveSpeed = 1.1f;
        const float OverdrivePassiveAccel = 1.14f;
        const float OverdriveNitroStackSpeed = 1.28f;
        const float OverdriveNitroStackAccel = 1.22f;
        const float GravityHoverMultiplier = 0.38f;
        const float GravityBarrelRollMinMph = 11f;
        const float AirCrestGroundRollMinMph = 12f;
        const float WindForceBase = 11f;

        Rigidbody rb;
        VehicleGroundProbe groundProbe;
        VehicleNitroBoost nitroBoost;
        VehicleController playerController;
        AIVehicleController aiController;
        VehicleBarrelRoll barrelRoll;
        TrackZoneQueryResult zoneState;
        float windPhase;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            groundProbe = GetComponent<VehicleGroundProbe>();
            nitroBoost = GetComponent<VehicleNitroBoost>();
            playerController = GetComponent<VehicleController>();
            aiController = GetComponent<AIVehicleController>();
            barrelRoll = GetComponent<VehicleBarrelRoll>();
        }

        void FixedUpdate()
        {
            if (rb == null || rb.isKinematic)
                return;

            zoneState.Reset();
            var registry = TrackGameplayZoneRegistry.Instance;
            if (registry == null)
            {
                ClearZoneModifiers();
                return;
            }

            registry.Query(transform.position, ref zoneState);

            ApplyNitroZone();
            ApplyGravityWell();
            ApplyAirCrest();
            ApplyWind();
            ApplyTunnelAi();
        }

        void ApplyNitroZone()
        {
            if (nitroBoost == null)
                return;

            if (!zoneState.InOverdrive)
            {
                nitroBoost.ClearZoneBonuses();
                return;
            }

            nitroBoost.SetZoneBonuses(
                OverdrivePassiveSpeed,
                OverdrivePassiveAccel,
                OverdriveNitroStackSpeed,
                OverdriveNitroStackAccel);
        }

        void ApplyGravityWell()
        {
            var hoverMultiplier = zoneState.InGravityWell ? GravityHoverMultiplier : 1f;
            playerController?.SetZoneHoverForceMultiplier(hoverMultiplier);
            aiController?.SetZoneHoverForceMultiplier(hoverMultiplier);

            if (barrelRoll == null || zoneState.InAirCrest)
                return;

            barrelRoll.SetAllowAirRoll(false);
            barrelRoll.SetMinSpeedMphOverride(zoneState.InGravityWell ? GravityBarrelRollMinMph : -1f);
            barrelRoll.SetZoneHoverForceMultiplier(hoverMultiplier);
        }

        void ApplyAirCrest()
        {
            if (barrelRoll == null)
                return;

            if (!zoneState.InAirCrest)
            {
                if (!zoneState.InGravityWell)
                {
                    barrelRoll.SetAllowAirRoll(false);
                    barrelRoll.SetMinSpeedMphOverride(-1f);
                }

                return;
            }

            var airborne = groundProbe != null && !groundProbe.Probe().IsGrounded;
            barrelRoll.SetAllowAirRoll(true);
            barrelRoll.SetMinSpeedMphOverride(airborne ? 8f : AirCrestGroundRollMinMph);
            barrelRoll.SetAirRollMinSpeedMph(8f);
        }

        void ApplyWind()
        {
            if (!zoneState.InWindGust || rb == null)
                return;

            windPhase += Time.fixedDeltaTime * (2.4f + zoneState.WindStrength);
            var gust = Mathf.Sin(windPhase * 3.1f) * 0.65f + Mathf.Sin(windPhase * 1.7f) * 0.35f;
            var direction = zoneState.WindDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = transform.right;

            direction.Normalize();
            rb.AddForce(direction * (WindForceBase * zoneState.WindStrength * gust), ForceMode.Acceleration);
        }

        void ApplyTunnelAi()
        {
            if (aiController == null)
                return;

            aiController.SetZoneTrackHalfWidthMultiplier(zoneState.InMetroTunnel
                ? zoneState.AiTrackHalfWidthScale
                : 1f);
        }

        void ClearZoneModifiers()
        {
            nitroBoost?.ClearZoneBonuses();
            playerController?.SetZoneHoverForceMultiplier(1f);
            aiController?.SetZoneHoverForceMultiplier(1f);
            aiController?.SetZoneTrackHalfWidthMultiplier(1f);
            barrelRoll?.SetAllowAirRoll(false);
            barrelRoll?.SetMinSpeedMphOverride(-1f);
            barrelRoll?.SetZoneHoverForceMultiplier(1f);
        }
    }
}
