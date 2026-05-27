using System.Collections;
using NeonLap.Core;
using NeonLap.UI;
using UnityEngine;

namespace NeonLap.Race
{
    public class EliminationModeController : MonoBehaviour
    {
        [SerializeField] float firstEliminationDelay = 40f;
        [SerializeField] float eliminationInterval = 28f;

        RaceManager raceManager;
        RaceUI raceUi;
        RacePodiumSequence podiumSequence;
        Coroutine eliminationRoutine;
        int eliminationRound;
        bool subscribed;

        public static EliminationModeController Setup(
            RaceManager manager,
            RaceUI ui,
            RacePodiumSequence podium)
        {
            if (!GameRaceModeSettings.IsElimination)
                return null;

            var go = new GameObject("EliminationMode");
            go.transform.SetParent(manager.transform, false);
            var controller = go.AddComponent<EliminationModeController>();
            controller.Configure(manager, ui, podium);
            return controller;
        }

        void Configure(RaceManager manager, RaceUI ui, RacePodiumSequence podium)
        {
            raceManager = manager;
            raceUi = ui;
            podiumSequence = podium;
            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            Unsubscribe();
            if (eliminationRoutine != null)
            {
                StopCoroutine(eliminationRoutine);
                eliminationRoutine = null;
            }
        }

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
            if (state != RaceState.Racing)
                return;

            eliminationRound = 0;
            if (eliminationRoutine != null)
                StopCoroutine(eliminationRoutine);
            eliminationRoutine = StartCoroutine(RunEliminationLoop());
        }

        void HandleRacerEliminated(RacerProgress racer)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            CheckForWinner();
        }

        IEnumerator RunEliminationLoop()
        {
            yield return new WaitForSeconds(firstEliminationDelay);

            while (raceManager != null && raceManager.State == RaceState.Racing)
            {
                if (raceManager.CountActiveRacers() <= 1)
                {
                    CheckForWinner();
                    yield break;
                }

                EliminateLastPlace();
                yield return new WaitForSeconds(eliminationInterval);
            }
        }

        void EliminateLastPlace()
        {
            var target = raceManager.GetLastPlaceActiveRacer();
            if (target == null)
                return;

            eliminationRound++;
            raceManager.EliminateRacer(target);
            CheckForWinner();
        }

        void CheckForWinner()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (raceManager.CountActiveRacers() > 1)
                return;

            foreach (var racer in raceManager.Racers)
            {
                if (racer == null || racer.IsEliminated || racer.IsFinished)
                    continue;

                if (racer.IsPlayer)
                    raceManager.EndPlayerRace(1);
                return;
            }
        }

        void HandleRaceFinished(int placement)
        {
            if (raceUi == null || raceManager == null)
                return;

            var won = placement == 1;
            raceUi.ShowModeFinish(
                won ? "ELIMINATION WIN!" : "ELIMINATED",
                won
                    ? $"Last racer standing  •  Time {RaceUI.FormatTimePublic(raceManager.RaceTime)}"
                    : $"{RaceUI.GetPlacementLabelPublic(placement)} Place  •  {eliminationRound} elimination rounds survived",
                won ? new Color(0.45f, 1f, 1f) : Color.white);
        }
    }
}
