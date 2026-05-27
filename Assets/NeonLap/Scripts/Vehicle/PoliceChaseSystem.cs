using System;
using System.Collections;
using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using NeonLap.Track;
using NeonLap.VFX;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class PoliceChaseSystem : MonoBehaviour
    {
        static readonly Color PoliceBodyColor = new(0.12f, 0.14f, 0.18f);
        static readonly Color PoliceAccentColor = new(0.15f, 0.45f, 1.8f);

        [SerializeField] float spawnChance = 0.58f;
        [SerializeField] int minUnits = 1;
        [SerializeField] int maxUnits = 2;
        [SerializeField] float spawnDelayMin = 7f;
        [SerializeField] float spawnDelayMax = 16f;
        [SerializeField] float spawnDistanceBehind = 38f;

        RaceManager raceManager;
        GameObject playerCar;
        OvalTrackBuilder trackBuilder;
        VehicleProfile vehicleProfile;
        Material bodyTemplate;
        Material accentTemplate;

        readonly List<GameObject> activeUnits = new();
        bool spawnAttempted;
        bool subscribed;
        bool outrunMode;

        public event Action PoliceUnitsSpawned;
        public bool HasActiveUnits => activeUnits.Count > 0;
        public IReadOnlyList<GameObject> ActiveUnits => activeUnits;

        public void SetOutrunMode(bool enabled)
        {
            outrunMode = enabled;
            if (!enabled)
                return;

            spawnChance = 1f;
            spawnDelayMin = 2f;
            spawnDelayMax = 5f;
            minUnits = 2;
            maxUnits = 3;
            spawnAttempted = false;

            if (raceManager != null && raceManager.State == RaceState.Racing && isActiveAndEnabled)
                StartCoroutine(TrySpawnPoliceDelayed());
        }

        public void Configure(
            RaceManager manager,
            GameObject player,
            OvalTrackBuilder track,
            VehicleProfile profile,
            Material bodyMat,
            Material accentMat)
        {
            Unsubscribe();
            raceManager = manager;
            playerCar = player;
            trackBuilder = track;
            vehicleProfile = profile;
            bodyTemplate = bodyMat;
            accentTemplate = accentMat;
            Subscribe();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            subscribed = true;

            if (raceManager.State == RaceState.Racing && !spawnAttempted)
                StartCoroutine(TrySpawnPoliceDelayed());
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
                spawnAttempted = false;

            if (state == RaceState.Racing && !spawnAttempted)
                StartCoroutine(TrySpawnPoliceDelayed());
        }

        IEnumerator TrySpawnPoliceDelayed()
        {
            if (spawnAttempted)
                yield break;

            spawnAttempted = true;

            if (!outrunMode)
            {
                GamePoliceSettings.Load();
                if (!GamePoliceSettings.IsActiveForCurrentRace())
                    yield break;
            }

            if (UnityEngine.Random.value > spawnChance)
                yield break;

            yield return new WaitForSeconds(UnityEngine.Random.Range(spawnDelayMin, spawnDelayMax));

            if (raceManager == null || raceManager.State != RaceState.Racing)
                yield break;

            var count = UnityEngine.Random.Range(minUnits, maxUnits + 1);
            for (var i = 0; i < count; i++)
            {
                SpawnPoliceUnit(i);
                yield return new WaitForSeconds(1.2f);
            }

            if (activeUnits.Count > 0)
                PoliceUnitsSpawned?.Invoke();
        }

        void SpawnPoliceUnit(int index)
        {
            if (vehicleProfile == null || playerCar == null || trackBuilder == null)
                return;

            var car = new GameObject("PoliceUnit_" + (index + 1));
            car.layer = NeonLapLayers.Vehicle;
            car.transform.SetParent(transform, false);

            var rb = car.AddComponent<Rigidbody>();
            rb.mass = 920f;
            rb.linearDamping = 0.45f;
            rb.angularDamping = 2.2f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            ObstaclePhysics.ConfigureVehicle(rb);

            HoverCarVisualBuilder.Build(car.transform,
                new HoverCarVisualBuilder.BuildArgs(bodyTemplate, accentTemplate, PoliceBodyColor, PoliceAccentColor));
            BuildPoliceDetails(car.transform);

            car.AddComponent<VehicleGroundProbe>();
            VehicleCollisionBody.Build(car);

            var chase = car.AddComponent<PoliceChaseVehicle>();
            chase.Configure(vehicleProfile, raceManager);

            var lights = car.AddComponent<PoliceLightBarVFX>();
            lights.Configure(car.transform.Find("Visual"));

            var roofSiren = car.AddComponent<PoliceRoofSirenVFX>();
            roofSiren.Build(car.transform.Find("Visual"));

            var spawn = GetSpawnPosition(index);
            car.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            rb.linearVelocity = spawn.rotation * Vector3.forward * 8f;
            car.SetActive(true);

            var oldMarker = car.transform.Find("HazardMarker");
            if (oldMarker != null)
                Destroy(oldMarker.gameObject);

            activeUnits.Add(car);
        }

        (Vector3 position, Quaternion rotation) GetSpawnPosition(int index)
        {
            var anchor = playerCar.transform;
            var forward = anchor.forward;
            var right = anchor.right;
            var lateral = (index % 2 == 0 ? -1f : 1f) * 5.5f;
            var position = anchor.position - forward * (spawnDistanceBehind + index * 7f) + right * lateral;
            position.y = trackBuilder.StartPosition.y + 0.6f;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            return (position, rotation);
        }

        static void BuildPoliceDetails(Transform carRoot)
        {
            var visual = carRoot.Find("Visual");
            if (visual == null)
                return;

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var barMat = new Material(lit);
            barMat.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.1f));
            barMat.SetFloat("_Metallic", 0.55f);
            barMat.SetFloat("_Smoothness", 0.72f);

            var redMat = new Material(lit);
            redMat.SetColor("_BaseColor", new Color(0.75f, 0.08f, 0.08f));
            redMat.EnableKeyword("_EMISSION");
            redMat.SetColor("_EmissionColor", new Color(2.5f, 0.15f, 0.15f));

            var blueMat = new Material(lit);
            blueMat.SetColor("_BaseColor", new Color(0.08f, 0.18f, 0.75f));
            blueMat.EnableKeyword("_EMISSION");
            blueMat.SetColor("_EmissionColor", new Color(0.15f, 0.35f, 2.5f));

            CreatePolicePart(visual, "PoliceLightBar", new Vector3(0f, 0.48f, -0.08f), new Vector3(0.72f, 0.08f, 0.24f),
                barMat);
            CreatePolicePart(visual, "PoliceLightL", new Vector3(-0.18f, 0.48f, -0.08f), new Vector3(0.16f, 0.07f, 0.16f),
                redMat);
            CreatePolicePart(visual, "PoliceLightR", new Vector3(0.18f, 0.48f, -0.08f), new Vector3(0.16f, 0.07f, 0.16f),
                blueMat);
            CreatePolicePart(visual, "PoliceBadge", new Vector3(0f, 0.28f, 1.05f), new Vector3(0.28f, 0.08f, 0.04f),
                barMat);
        }

        public bool TryGetClosestUnit(Vector3 worldPosition, out Transform unitTransform, out float distance)
        {
            unitTransform = null;
            distance = float.MaxValue;

            for (var i = activeUnits.Count - 1; i >= 0; i--)
            {
                var unit = activeUnits[i];
                if (unit == null)
                {
                    activeUnits.RemoveAt(i);
                    continue;
                }

                var d = Vector3.Distance(worldPosition, unit.transform.position);
                if (d >= distance)
                    continue;

                distance = d;
                unitTransform = unit.transform;
            }

            return unitTransform != null;
        }

        static void CreatePolicePart(Transform parent, string name, Vector3 localPosition, Vector3 localScale,
            Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            UnityEngine.Object.Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
