using UnityEngine;

namespace NeonLap.VFX
{
    public class RainEffect : MonoBehaviour
    {
        [SerializeField] Vector3 emitterLocalOffset = new(0f, 16f, 12f);
        [SerializeField] Vector3 emitterSize = new(70f, 1f, 55f);

        Transform followTarget;
        float intensityMultiplier = 1f;
        ParticleSystem rainStreaks;
        ParticleSystem rainMist;

        public void Configure(Transform target, float intensity = 1f)
        {
            followTarget = target;
            intensityMultiplier = Mathf.Clamp(intensity, 0f, 1f);
            ApplyIntensity();
        }

        public void SetWeatherIntensity(float intensity)
        {
            intensityMultiplier = Mathf.Clamp(intensity, 0f, 1f);
            ApplyIntensity();
        }

        void Awake()
        {
            rainStreaks = CreateRainStreakSystem(transform);
            rainMist = CreateRainMistSystem(transform);
            ApplyIntensity();
        }

        void ApplyIntensity()
        {
            var intensity = Mathf.Clamp(intensityMultiplier, 0f, 1f);
            var active = intensity > 0.01f;

            if (rainStreaks != null)
            {
                var main = rainStreaks.main;
                main.maxParticles = Mathf.RoundToInt(4500 * intensity);

                var emission = rainStreaks.emission;
                emission.rateOverTime = active ? 1400f * intensity : 0f;
            }

            if (rainMist != null)
            {
                var main = rainMist.main;
                main.maxParticles = active ? Mathf.Max(40, Mathf.RoundToInt(320 * intensity)) : 0;

                var emission = rainMist.emission;
                emission.rateOverTime = active ? 28f * intensity : 0f;
            }
        }

        void LateUpdate()
        {
            if (followTarget == null)
                return;

            transform.position = followTarget.position + followTarget.TransformDirection(emitterLocalOffset);
            transform.rotation = Quaternion.identity;
        }

        ParticleSystem CreateRainStreakSystem(Transform parent)
        {
            var go = new GameObject("RainStreaks");
            go.transform.SetParent(parent, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4500;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 30f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
            main.gravityModifier = 1.15f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.72f, 0.82f, 0.95f, 0.35f),
                new Color(0.55f, 0.72f, 0.92f, 0.55f));

            var emission = ps.emission;
            emission.rateOverTime = 1400f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = emitterSize;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ParticleVelocityUtility.ConfigureRandomAxes(velocityOverLifetime, -1.8f, 1.8f, 0f, 0f, -0.8f, 0.8f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.75f, 0.85f, 1f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.68f, 0.9f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.55f, 0.08f),
                    new GradientAlphaKey(0.45f, 0.75f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.6f),
                new Keyframe(0.15f, 1f),
                new Keyframe(1f, 0.85f)));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3.2f;
            renderer.velocityScale = 0.08f;
            renderer.material = CreateRainMaterial(new Color(0.75f, 0.88f, 1f, 0.55f));

            ps.Play();
            return ps;
        }

        ParticleSystem CreateRainMistSystem(Transform parent)
        {
            var go = new GameObject("RainMist");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -10f, 4f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 320;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.4f, 4.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.gravityModifier = -0.02f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.35f, 0.45f, 0.62f, 0.08f),
                new Color(0.28f, 0.38f, 0.55f, 0.16f));

            var emission = ps.emission;
            emission.rateOverTime = 28f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(emitterSize.x * 0.85f, 2f, emitterSize.z * 0.85f);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ParticleVelocityUtility.ConfigureRandomAxes(velocityOverLifetime, -0.4f, 0.4f, -0.35f, 0.25f, 0f, 0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.25f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateRainMaterial(new Color(0.55f, 0.68f, 0.85f, 0.12f));

            ps.Play();
            return ps;
        }

        static Material CreateRainMaterial(Color tint)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            var material = new Material(shader);
            material.SetColor("_BaseColor", tint);
            return material;
        }
    }
}
