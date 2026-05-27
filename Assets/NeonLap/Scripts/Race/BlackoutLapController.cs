using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Rendering;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.Race
{
    public class BlackoutLapController : MonoBehaviour
    {
        [SerializeField] float ambientDimScale = 0.22f;
        [SerializeField] float fillLightDimScale = 0.18f;

        RaceManager raceManager;
        Material trackEdgeMaterial;
        int blackoutLap = 1;
        bool blackoutActive;

        AmbientMode savedAmbientMode;
        Color savedSky;
        Color savedEquator;
        Color savedGround;
        float savedFogDensity;
        readonly List<(Light light, float intensity)> dimmedLights = new();
        readonly List<VehicleTaillightController> taillights = new();

        public bool IsBlackoutActive => blackoutActive;
        public int BlackoutLap => blackoutLap;

        public static BlackoutLapController Setup(Transform parent, RaceManager manager, Material edgeMaterial)
        {
            var go = new GameObject("BlackoutLap");
            go.transform.SetParent(parent, false);
            var controller = go.AddComponent<BlackoutLapController>();
            controller.Configure(manager, edgeMaterial);
            return controller;
        }

        void Configure(RaceManager manager, Material edgeMaterial)
        {
            raceManager = manager;
            trackEdgeMaterial = edgeMaterial;
            CacheTaillights();
            PickBlackoutLap();
            Subscribe();
        }

        void OnDestroy() => Unsubscribe();

        void CacheTaillights()
        {
            taillights.Clear();
            taillights.AddRange(FindObjectsByType<VehicleTaillightController>(FindObjectsInactive.Exclude));
        }

        void PickBlackoutLap()
        {
            var totalLaps = GameLapSettings.CurrentLaps;
            if (totalLaps <= 1)
            {
                blackoutLap = 1;
                return;
            }

            blackoutLap = Random.Range(2, totalLaps + 1);
        }

        void Subscribe()
        {
            if (raceManager == null)
                return;

            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnStateChanged += HandleStateChanged;
        }

        void Unsubscribe()
        {
            if (raceManager == null)
                return;

            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager.OnStateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Racing && blackoutLap == 1)
                EnableBlackout();
            else if (state != RaceState.Racing)
                DisableBlackout();
        }

        void HandleLapCompleted(int lap)
        {
            if (lap == blackoutLap)
                EnableBlackout();
            else if (lap == blackoutLap + 1 || (blackoutActive && lap > blackoutLap))
                DisableBlackout();
        }

        void EnableBlackout()
        {
            if (blackoutActive)
                return;

            blackoutActive = true;
            NeonTrackEdgePulseDriver.SetBlackoutActive(true);
            SaveAmbient();
            DimEnvironmentLights();
            ApplyTaillightBlackout(true);
            StadiumIncidentHub.Report("BLACKOUT LAP");
        }

        void DisableBlackout()
        {
            if (!blackoutActive)
                return;

            blackoutActive = false;
            NeonTrackEdgePulseDriver.SetBlackoutActive(false);
            RestoreAmbient();
            RestoreEnvironmentLights();
            ApplyTaillightBlackout(false);
        }

        void SaveAmbient()
        {
            savedAmbientMode = RenderSettings.ambientMode;
            savedSky = RenderSettings.ambientSkyColor;
            savedEquator = RenderSettings.ambientEquatorColor;
            savedGround = RenderSettings.ambientGroundColor;
            savedFogDensity = RenderSettings.fogDensity;

            RenderSettings.ambientSkyColor = savedSky * ambientDimScale;
            RenderSettings.ambientEquatorColor = savedEquator * ambientDimScale;
            RenderSettings.ambientGroundColor = savedGround * ambientDimScale;
            RenderSettings.fogDensity = savedFogDensity * 1.35f;
        }

        void RestoreAmbient()
        {
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientSkyColor = savedSky;
            RenderSettings.ambientEquatorColor = savedEquator;
            RenderSettings.ambientGroundColor = savedGround;
            RenderSettings.fogDensity = savedFogDensity;
        }

        void DimEnvironmentLights()
        {
            dimmedLights.Clear();
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light == null || light.type == LightType.Directional)
                    continue;

                dimmedLights.Add((light, light.intensity));
                light.intensity *= fillLightDimScale;
            }
        }

        void RestoreEnvironmentLights()
        {
            foreach (var entry in dimmedLights)
            {
                if (entry.light != null)
                    entry.light.intensity = entry.intensity;
            }

            dimmedLights.Clear();
        }

        void ApplyTaillightBlackout(bool enabled)
        {
            if (taillights.Count == 0)
                CacheTaillights();

            foreach (var taillight in taillights)
            {
                if (taillight != null)
                    taillight.SetBlackoutMode(enabled);
            }
        }
    }
}
