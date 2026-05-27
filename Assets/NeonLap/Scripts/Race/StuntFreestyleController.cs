using NeonLap.Core;
using NeonLap.UI;
using UnityEngine;

namespace NeonLap.Race
{
    public class StuntFreestyleController : MonoBehaviour
    {
        RaceManager raceManager;
        RaceUI raceUi;
        VehicleStuntDetector stuntDetector;
        bool ending;

        public static StuntFreestyleController Setup(RaceManager manager, RaceUI ui, GameObject playerCar)
        {
            if (!GameRaceModeSettings.IsStuntFreestyle)
                return null;

            var go = new GameObject("StuntFreestyleMode");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<StuntFreestyleController>();
            controller.Configure(manager, ui, playerCar);
            return controller;
        }

        void Configure(RaceManager manager, RaceUI ui, GameObject playerCar)
        {
            raceManager = manager;
            raceUi = ui;
            stuntDetector = playerCar != null ? playerCar.GetComponent<VehicleStuntDetector>() : null;
            if (stuntDetector == null && playerCar != null)
                stuntDetector = playerCar.AddComponent<VehicleStuntDetector>();

            if (stuntDetector != null)
                stuntDetector.OnTrickLanded += HandleTrickLanded;

            manager.SetPlayerLapFinishEnabled(false);
        }

        void Update()
        {
            if (ending || raceManager == null)
                return;

            if (raceManager.State != RaceState.Racing)
                return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            {
                ending = true;
                raceManager.EndPlayerRace(1);
            }
        }

        void OnDestroy()
        {
            if (stuntDetector != null)
                stuntDetector.OnTrickLanded -= HandleTrickLanded;
        }

        void HandleTrickLanded(int points, float airSeconds)
        {
            raceUi?.PulseStuntTrick(points, airSeconds);
        }

        public void EndSession()
        {
            if (stuntDetector == null || raceUi == null)
                return;

            var improved = StuntFreestyleRecordStore.TrySaveSession(
                stuntDetector.SessionScore,
                stuntDetector.SessionBestAir,
                stuntDetector.TrickCount);

            raceUi.ShowStuntFreestyleFinish(
                stuntDetector.SessionScore,
                stuntDetector.TrickCount,
                stuntDetector.SessionBestAir,
                improved,
                StuntFreestyleRecordStore.GetSummaryLine());
        }

        public float SessionScore => stuntDetector != null ? stuntDetector.SessionScore : 0f;
        public int TrickCount => stuntDetector != null ? stuntDetector.TrickCount : 0;
        public float BestAir => stuntDetector != null ? stuntDetector.SessionBestAir : 0f;
        public bool IsAirborne => stuntDetector != null && stuntDetector.IsAirborne;
    }
}
