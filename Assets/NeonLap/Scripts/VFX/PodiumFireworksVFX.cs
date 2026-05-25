using System.Collections;
using UnityEngine;

namespace NeonLap.VFX
{
    public class PodiumFireworksVFX : MonoBehaviour
    {
        static readonly Color[] BurstPalette =
        {
            new(0.2f, 1f, 1f),
            new(1f, 0.35f, 0.85f),
            new(1f, 0.88f, 0.25f),
            new(0.45f, 1f, 0.55f),
            new(1f, 0.55f, 0.2f),
            new(0.65f, 0.45f, 1f),
        };

        [SerializeField] float spawnRadius = 10f;
        [SerializeField] float minLaunchInterval = 0.28f;
        [SerializeField] float maxLaunchInterval = 0.95f;
        [SerializeField] float minApexHeight = 11f;
        [SerializeField] float maxApexHeight = 20f;

        Vector3 center;
        Material burstMaterial;
        Coroutine launchRoutine;

        public void Configure(Vector3 worldCenter, float radius = 10f)
        {
            center = worldCenter;
            spawnRadius = radius;
        }

        void OnEnable()
        {
            if (launchRoutine != null)
                StopCoroutine(launchRoutine);

            launchRoutine = StartCoroutine(LaunchLoop());
        }

        void OnDisable()
        {
            if (launchRoutine != null)
            {
                StopCoroutine(launchRoutine);
                launchRoutine = null;
            }
        }

        IEnumerator LaunchLoop()
        {
            yield return new WaitForSeconds(0.35f);

            while (enabled)
            {
                yield return StartCoroutine(LaunchSingleFirework());
                yield return new WaitForSeconds(Random.Range(minLaunchInterval, maxLaunchInterval));
            }
        }

        IEnumerator LaunchSingleFirework()
        {
            var offset = Random.insideUnitCircle * spawnRadius;
            var launchPos = center + new Vector3(offset.x, 0.6f, offset.y);
            var apexHeight = Random.Range(minApexHeight, maxApexHeight);
            var apex = center + new Vector3(offset.x * 0.35f, apexHeight, offset.y * 0.35f);
            var burstColor = BurstPalette[Random.Range(0, BurstPalette.Length)];

            var rocketLight = CreateFlashLight(launchPos, burstColor * 0.35f, 2.5f);
            var trail = CreateTrailSystem(launchPos, burstColor);

            var duration = Random.Range(0.55f, 0.95f);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                var pos = Vector3.Lerp(launchPos, apex, t);
                rocketLight.transform.position = pos;
                rocketLight.intensity = Mathf.Lerp(2.5f, 5.5f, t);

                var emitParams = new ParticleSystem.EmitParams { position = pos };
                trail.Emit(emitParams, Random.Range(2, 5));

                yield return null;
            }

            Destroy(rocketLight.gameObject);
            SpawnBurst(apex, burstColor);
        }

        void SpawnBurst(Vector3 position, Color color)
        {
            var burstGo = new GameObject("FireworkBurst");
            burstGo.transform.position = position;

            var ps = burstGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.gravityModifier = 0.55f;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.45f));

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.5f), 0.25f),
                    new GradientColorKey(color * 0.6f, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(0.75f, 0.45f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var renderer = burstGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetBurstMaterial();

            ps.Emit(Random.Range(36, 58));

            var spark = CreateSparkSystem(burstGo.transform, color);
            spark.Emit(Random.Range(14, 24));

            var flash = CreateFlashLight(position, color, 14f);
            StartCoroutine(FadeFlash(flash, 0.35f));

            Destroy(burstGo, 2.5f);
        }

        ParticleSystem CreateTrailSystem(Vector3 position, Color color)
        {
            var go = new GameObject("FireworkTrail");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(color * 1.2f, Color.white);

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetBurstMaterial();

            Destroy(go, 2f);
            return ps;
        }

        ParticleSystem CreateSparkSystem(Transform parent, Color color)
        {
            var go = new GameObject("FireworkSparks");
            go.transform.SetParent(parent, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.gravityModifier = 0.85f;
            main.startColor = color;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetBurstMaterial();

            return ps;
        }

        static Light CreateFlashLight(Vector3 position, Color color, float intensity)
        {
            var go = new GameObject("FireworkFlash");
            go.transform.position = position;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = 18f;
            light.shadows = LightShadows.None;
            return light;
        }

        static IEnumerator FadeFlash(Light flash, float duration)
        {
            if (flash == null)
                yield break;

            var start = flash.intensity;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = 1f - elapsed / duration;
                flash.intensity = start * t * t;
                yield return null;
            }

            Destroy(flash.gameObject);
        }

        Material GetBurstMaterial()
        {
            if (burstMaterial != null)
                return burstMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            burstMaterial = new Material(shader);
            burstMaterial.SetColor("_BaseColor", Color.white);
            burstMaterial.EnableKeyword("_EMISSION");
            burstMaterial.SetColor("_EmissionColor", Color.white * 2f);
            return burstMaterial;
        }
    }
}
