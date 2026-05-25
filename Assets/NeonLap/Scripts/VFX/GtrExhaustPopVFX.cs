using NeonLap.Input;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.VFX
{
    [RequireComponent(typeof(Rigidbody))]
    public class GtrExhaustPopVFX : MonoBehaviour
    {
        static readonly Vector3 LeftExhaustLocal = new(-0.38f, 0.18f, -1.42f);
        static readonly Vector3 RightExhaustLocal = new(0.38f, 0.18f, -1.42f);

        [SerializeField] float minPopSpeed = 11f;
        [SerializeField] float popCooldownMin = 0.45f;
        [SerializeField] float popCooldownMax = 1.1f;
        [SerializeField] float flashPeakIntensity = 4.5f;
        [SerializeField] float flashDuration = 0.14f;

        Rigidbody rb;
        IVehicleInputProvider inputProvider;
        VehicleNitroBoost nitroBoost;

        ParticleSystem leftPop;
        ParticleSystem rightPop;
        Light leftFlash;
        Light rightFlash;

        float lastForwardSpeed;
        float lastAccelerate;
        float nextPopTime;
        bool wasNitroActive;
        float leftFlashEndTime;
        float rightFlashEndTime;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            inputProvider = GetComponent<IVehicleInputProvider>();
            nitroBoost = GetComponent<VehicleNitroBoost>();

            leftPop = CreatePopSystem("GtrPopLeft", LeftExhaustLocal);
            rightPop = CreatePopSystem("GtrPopRight", RightExhaustLocal);
            leftFlash = CreateFlashLight("GtrFlashLeft", LeftExhaustLocal);
            rightFlash = CreateFlashLight("GtrFlashRight", RightExhaustLocal);
        }

        void Update()
        {
            if (rb == null || rb.isKinematic)
                return;

            UpdateFlashLights();

            if (inputProvider == null)
                inputProvider = GetComponent<IVehicleInputProvider>();
            if (inputProvider == null)
                return;

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            var accelerate = inputProvider.Accelerate;
            var deceleration = lastForwardSpeed - forwardSpeed;
            var liftOff = lastAccelerate > 0.45f && accelerate < 0.18f;
            var overrun = forwardSpeed > minPopSpeed && deceleration > 0.75f && accelerate < 0.3f;
            var hardLift = forwardSpeed > minPopSpeed * 1.35f && deceleration > 1.8f;

            if (nitroBoost != null && nitroBoost.IsActive && !wasNitroActive)
                TryEmitPop(8, 0.85f, true);

            wasNitroActive = nitroBoost != null && nitroBoost.IsActive;

            if (Time.time < nextPopTime)
            {
                lastForwardSpeed = forwardSpeed;
                lastAccelerate = accelerate;
                return;
            }

            if (hardLift && Random.value < 0.42f)
            {
                TryEmitPop(Random.Range(4, 7), 0.75f, true);
            }
            else if (overrun && Random.value < 0.28f)
            {
                TryEmitPop(Random.Range(3, 5), 0.65f, false);
            }
            else if (liftOff && forwardSpeed > minPopSpeed * 0.85f && Random.value < 0.22f)
            {
                TryEmitPop(Random.Range(2, 4), 0.6f, false);
            }
            else if (forwardSpeed > minPopSpeed * 1.2f && accelerate > 0.92f && Random.value < 0.004f)
            {
                TryEmitPop(Random.Range(2, 3), 0.55f, false);
            }

            lastForwardSpeed = forwardSpeed;
            lastAccelerate = accelerate;
        }

        void TryEmitPop(int burstCount, float sizeScale, bool strongPop)
        {
            EmitPop(leftPop, burstCount, sizeScale);
            EmitPop(rightPop, burstCount, sizeScale);

            TriggerFlash(leftFlash, ref leftFlashEndTime, strongPop);
            TriggerFlash(rightFlash, ref rightFlashEndTime, strongPop);

            nextPopTime = Time.time + Random.Range(popCooldownMin, popCooldownMax);
        }

        static void EmitPop(ParticleSystem pop, int burstCount, float sizeScale)
        {
            if (pop == null)
                return;

            var emitParams = new ParticleSystem.EmitParams();
            emitParams.startSize = Random.Range(0.32f, 0.52f) * sizeScale;
            pop.Emit(emitParams, burstCount);
        }

        void TriggerFlash(Light flash, ref float flashEndTime, bool strongPop)
        {
            if (flash == null)
                return;

            flashEndTime = Time.time + flashDuration;
            flash.enabled = true;
            flash.intensity = flashPeakIntensity * (strongPop ? 1.1f : 0.85f);
        }

        void UpdateFlashLights()
        {
            UpdateFlashLight(leftFlash, leftFlashEndTime);
            UpdateFlashLight(rightFlash, rightFlashEndTime);
        }

        void UpdateFlashLight(Light flash, float flashEndTime)
        {
            if (flash == null)
                return;

            var remaining = flashEndTime - Time.time;
            if (remaining <= 0f)
            {
                flash.enabled = false;
                return;
            }

            var t = remaining / flashDuration;
            flash.enabled = true;
            flash.intensity = flashPeakIntensity * t * t;
        }

        ParticleSystem CreatePopSystem(string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.gravityModifier = 0.05f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.72f, 0.12f, 0.95f),
                new Color(1f, 0.38f, 0.05f, 0.85f));

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.05f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.18f, 0.75f),
                new Keyframe(0.55f, 0.95f),
                new Keyframe(1f, 1.1f)));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.55f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.35f),
                    new GradientColorKey(new Color(0.55f, 0.18f, 0.06f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.7f, 0.08f),
                    new GradientAlphaKey(0.45f, 0.35f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            ParticleVelocityUtility.ConfigureRandomAxes(velocityOverLifetime, 0f, 0f, 0f, 0f, 1.2f, 2.4f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetPopMaterial();

            ps.Play();
            return ps;
        }

        Light CreateFlashLight(string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition + new Vector3(0f, 0f, -0.08f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.52f, 0.12f);
            light.range = 3.2f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        static Material sharedPopMaterial;

        static Material GetPopMaterial()
        {
            if (sharedPopMaterial != null)
                return sharedPopMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            sharedPopMaterial = new Material(shader);
            sharedPopMaterial.SetColor("_BaseColor", new Color(1f, 0.55f, 0.1f, 1f));
            return sharedPopMaterial;
        }
    }
}
