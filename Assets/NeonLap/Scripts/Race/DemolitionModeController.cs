using NeonLap.Core;
using NeonLap.UI;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public class DemolitionModeController : MonoBehaviour
    {
        RaceManager raceManager;
        RaceUI raceUi;
        bool subscribed;

        public static DemolitionModeController Setup(RaceManager manager, RaceUI ui)
        {
            if (!GameRaceModeSettings.IsDemolition)
                return null;

            var go = new GameObject("DemolitionMode");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<DemolitionModeController>();
            controller.Configure(manager, ui);
            return controller;
        }

        void Configure(RaceManager manager, RaceUI ui)
        {
            raceManager = manager;
            raceUi = ui;
            manager.SetPlayerLapFinishEnabled(false);
            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnRacerEliminated += HandleRacerEliminated;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnRacerEliminated -= HandleRacerEliminated;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Racing)
                CheckForWinner();
        }

        void HandleRacerEliminated(RacerProgress racer)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            CheckForWinner();
        }

        void CheckForWinner()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            var mobileCount = CountMobileRacers();
            if (mobileCount > 1)
                return;

            foreach (var racer in raceManager.Racers)
            {
                if (racer == null || !VehicleMobility.IsRacerMobile(racer))
                    continue;

                if (racer.IsPlayer)
                    raceManager.EndPlayerRace(1);
                return;
            }
        }

        int CountMobileRacers()
        {
            var count = 0;
            foreach (var racer in raceManager.Racers)
            {
                if (VehicleMobility.IsRacerMobile(racer))
                    count++;
            }

            return count;
        }

        void HandleRaceFinished(int placement)
        {
            if (raceUi == null || raceManager == null)
                return;

            var won = placement == 1;
            raceUi.ShowModeFinish(
                won ? "DEMOLITION WIN!" : "WRECKED OUT",
                won
                    ? $"Last car moving  •  Time {RaceUI.FormatTimePublic(raceManager.RaceTime)}"
                    : $"{RaceUI.GetPlacementLabelPublic(placement)}  •  Hull destroyed",
                won ? new Color(1f, 0.55f, 0.2f) : Color.white);
        }
    }
}
