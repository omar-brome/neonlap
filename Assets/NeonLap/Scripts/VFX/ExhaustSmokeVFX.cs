using UnityEngine;

namespace NeonLap.VFX
{
    [RequireComponent(typeof(Rigidbody))]
    public class ExhaustSmokeVFX : MonoBehaviour
    {
        [SerializeField] Vector3 localEmitPosition = new(0f, 0.22f, -1.32f);
        [SerializeField] float minForwardSpeed = 1.5f;
        [SerializeField] float maxForwardSpeed = 28f;

        static Material sharedSmokeMaterial;

        Rigidbody rb;
        ParticleSystem smoke;
        float lastForwardSpeed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            smoke = CreateSmokeSystem(transform, localEmitPosition);
            SetEmissionRate(0f);
        }

        void OnDisable()
        {
            SetEmissionRate(0f);
        }

        void Update()
        {
            if (rb == null || smoke == null)
                return;

            if (rb.isKinematic)
            {
                SetEmissionRate(0f);
                return;
            }

            var forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            var speedRatio = Mathf.Clamp01((forwardSpeed - minForwardSpeed) / (maxForwardSpeed - minForwardSpeed));
            var accelerating = forwardSpeed > lastForwardSpeed + 0.05f;
            lastForwardSpeed = forwardSpeed;

            if (forwardSpeed < minForwardSpeed * 0.5f)
            {
                SetEmissionRate(0f);
                return;
            }

            var baseRate = Mathf.Lerp(3f, 24f, speedRatio);
            if (accelerating)
                baseRate *= 1.45f;

            SetEmissionRate(baseRate);

            var mainModule = smoke.main;
            mainModule.startSpeed = Mathf.Lerp(0.6f, 2.4f, speedRatio);
        }

        void SetEmissionRate(float rate)
        {
            if (smoke == null)
                return;

            var emission = smoke.emission;
            emission.rateOverTime = rate;
        }

        static ParticleSystem CreateSmokeSystem(Transform carRoot, Vector3 localPosition)
        {
            var go = new GameObject("ExhaustSmoke");
            go.transform.SetParent(carRoot, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.gravityModifier = -0.08f;
            main.maxParticles = 80;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.55f, 0.55f, 0.58f, 0.35f),
                new Color(0.75f, 0.75f, 0.78f, 0.55f));

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.07f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ParticleVelocityUtility.ConfigureRandomAxes(velocityOverLifetime, -0.35f, 0.35f, 0.4f, 1.2f, -0.25f, 0.15f);

            var limitVelocity = ps.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.dampen = 0.18f;
            limitVelocity.drag = 0.6f;

            var forceOverLifetime = ps.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.y = 0.35f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 0.75f),
                new Keyframe(1f, 1.6f)));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.65f, 0.65f, 0.68f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.45f, 0.48f), 0.55f),
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.32f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.45f, 0f),
                    new GradientAlphaKey(0.28f, 0.45f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.55f;
            noise.scrollSpeed = 0.35f;
            noise.damping = true;

            var inheritVelocity = ps.inheritVelocity;
            inheritVelocity.enabled = true;
            inheritVelocity.mode = ParticleSystemInheritVelocityMode.Current;
            inheritVelocity.curve = new ParticleSystem.MinMaxCurve(0.25f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetSmokeMaterial();

            ps.Play();
            return ps;
        }

        static Material GetSmokeMaterial()
        {
            if (sharedSmokeMaterial != null)
                return sharedSmokeMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            sharedSmokeMaterial = new Material(shader);
            sharedSmokeMaterial.SetColor("_BaseColor", Color.white);
            return sharedSmokeMaterial;
        }
    }
}
