using UnityEngine;

namespace NeonLap.VFX
{
    public class SandstormEffect : MonoBehaviour
    {
        [SerializeField] Vector3 emitterLocalOffset = new(0f, 8f, 10f);
        [SerializeField] Vector3 emitterSize = new(85f, 14f, 70f);

        Transform followTarget;
        float intensityMultiplier;
        ParticleSystem dustClouds;
        ParticleSystem sandStreaks;

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
            dustClouds = CreateDustSystem(transform);
            sandStreaks = CreateStreakSystem(transform);
            ApplyIntensity();
        }

        void ApplyIntensity()
        {
            var intensity = Mathf.Clamp(intensityMultiplier, 0f, 1f);
            var active = intensity > 0.01f;

            if (dustClouds != null)
            {
                var main = dustClouds.main;
                main.maxParticles = Mathf.RoundToInt(900 * intensity);
                var emission = dustClouds.emission;
                emission.rateOverTime = active ? 220f * intensity : 0f;
            }

            if (sandStreaks != null)
            {
                var main = sandStreaks.main;
                main.maxParticles = Mathf.RoundToInt(1800 * intensity);
                var emission = sandStreaks.emission;
                emission.rateOverTime = active ? 520f * intensity : 0f;
            }
        }

        void LateUpdate()
        {
            if (followTarget == null)
                return;

            transform.position = followTarget.position + followTarget.TransformDirection(emitterLocalOffset);
            transform.rotation = Quaternion.Euler(0f, followTarget.eulerAngles.y, 0f);
        }

        ParticleSystem CreateDustSystem(Transform parent)
        {
            var go = new GameObject("SandDust");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 11f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.82f, 0.55f, 0.28f, 0.12f),
                new Color(0.65f, 0.38f, 0.18f, 0.22f));

            var emission = ps.emission;
            emission.rateOverTime = 220f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = emitterSize;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ParticleVelocityUtility.ConfigureRandomAxes(velocityOverLifetime, -4f, 4f, -0.5f, 1.2f, -3f, 3f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.65f;
            noise.frequency = 0.22f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateSandMaterial(new Color(0.78f, 0.5f, 0.22f, 0.18f));
            ps.Play();
            return ps;
        }

        ParticleSystem CreateStreakSystem(Transform parent)
        {
            var go = new GameObject("SandStreaks");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 2f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 24f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.gravityModifier = 0.05f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.95f, 0.68f, 0.32f, 0.35f),
                new Color(0.82f, 0.48f, 0.2f, 0.55f));

            var emission = ps.emission;
            emission.rateOverTime = 520f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(emitterSize.x, 4f, emitterSize.z * 0.6f);
            shape.rotation = new Vector3(-8f, 0f, 0f);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            ParticleVelocityUtility.ConfigureRandomAxes(velocityOverLifetime, -6f, 6f, 0f, 2f, 8f, 16f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.4f;
            renderer.velocityScale = 0.12f;
            renderer.material = CreateSandMaterial(new Color(0.92f, 0.62f, 0.25f, 0.45f));
            ps.Play();
            return ps;
        }

        static Material CreateSandMaterial(Color tint)
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
