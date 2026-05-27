using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Input;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonLap.Core.Content
{
    public readonly struct NeonLapCarSpawnRequest
    {
        public string CarName { get; init; }
        public VehicleProfile Profile { get; init; }
        public bool IsPlayer { get; init; }
        public Color BodyColor { get; init; }
        public Color AccentColor { get; init; }
        public Material BodyMaterial { get; init; }
        public Material AccentMaterial { get; init; }
        public InputActionAsset InputActions { get; init; }
        public VehicleDamageMode DamageMode { get; init; }
    }

    /// <summary>
    /// Instantiates optional prefabs or falls back to runtime assembly via delegate.
    /// </summary>
    public static class NeonLapCarSpawner
    {
        public delegate GameObject RuntimeBuildDelegate(in NeonLapCarSpawnRequest request);

        public static GameObject Spawn(
            in NeonLapCarSpawnRequest request,
            NeonLapContentCatalog catalog,
            RuntimeBuildDelegate runtimeBuild)
        {
            if (catalog != null && catalog.UsePrefabsWhenAssigned)
            {
                var prefab = request.IsPlayer ? catalog.PlayerCarPrefab : catalog.AiRivalCarPrefab;
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab);
                    ConfigureSpawnedCar(instance, in request);
                    return instance;
                }
            }

            return runtimeBuild != null ? runtimeBuild(request) : null;
        }

        public static void ConfigureSpawnedCar(GameObject car, in NeonLapCarSpawnRequest request)
        {
            if (car == null)
                return;

            car.name = request.CarName;
            car.SetActive(false);
            car.tag = request.IsPlayer ? "Player" : "Untagged";
            car.layer = NeonLapLayers.Vehicle;

            var appearance = car.GetComponent<VehicleAppearance>();
            if (appearance != null && request.BodyMaterial != null && request.AccentMaterial != null)
            {
                var buildArgs = new HoverCarVisualBuilder.BuildArgs(request.BodyMaterial, request.AccentMaterial,
                    request.BodyColor, request.AccentColor, request.IsPlayer);
                appearance.Configure(buildArgs);
            }

            var damage = car.GetComponent<VehicleDamageSystem>();
            if (damage != null)
                damage.Configure(RaceModeDamageRules.GetDamageProfile());

            if (car.GetComponent<VehicleHealthSystem>() == null)
                car.AddComponent<VehicleHealthSystem>();

            if (car.GetComponent<RepairPadLapTracker>() == null)
                car.AddComponent<RepairPadLapTracker>();

            var progress = car.GetComponent<RacerProgress>();
            if (progress != null)
                progress.Configure(request.IsPlayer);

            if (request.IsPlayer)
            {
                var reader = car.GetComponent<PlayerInputReader>();
                if (reader != null && request.InputActions != null)
                    reader.Configure(request.InputActions);

                var controller = car.GetComponent<VehicleController>();
                if (controller != null && request.Profile != null)
                    controller.Configure(request.Profile, reader);

                var barrelRoll = car.GetComponent<VehicleBarrelRoll>();
                if (barrelRoll != null && request.Profile != null)
                    barrelRoll.Configure(request.Profile);
            }
            else
            {
                var ai = car.GetComponent<AIVehicleController>();
                if (ai != null && request.Profile != null)
                {
                    var field = typeof(AIVehicleController).GetField("profile",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    field?.SetValue(ai, request.Profile);
                }
            }

            var prefabRoot = car.GetComponent<NeonLapCarPrefabRoot>();
            if (prefabRoot == null)
                prefabRoot = car.AddComponent<NeonLapCarPrefabRoot>();
            prefabRoot.SetTemplate(request.IsPlayer);
        }
    }
}
