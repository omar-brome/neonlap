using System;
using System.Collections;
using System.Collections.Generic;
using NeonLap.Track;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Race
{
    public enum RaceState
    {
        Waiting,
        Countdown,
        Racing,
        Finished
    }

    public class RaceManager : MonoBehaviour
    {
        [SerializeField] int totalLaps = 1;
        [SerializeField] float countdownInterval = 1f;
        [SerializeField] OvalTrackBuilder trackBuilder;
        [SerializeField] VehicleReset playerReset;

        readonly List<TrackCheckpoint> checkpoints = new();
        readonly List<RacerProgress> racers = new();

        RacerProgress playerRacer;
        float raceStartTime;
        float lapStartTime;
        float bestLapTime = float.MaxValue;
        float lastLapTime;
        int countdownValue = 3;
        int playerFinishPosition;

        public RaceState State { get; private set; } = RaceState.Waiting;
        public int CurrentLap => playerRacer != null ? playerRacer.CurrentLap : 1;
        public int TotalLaps => totalLaps;
        public int PlayerFinishPosition => playerFinishPosition;
        public int TotalRacers => racers.Count;
        public IReadOnlyList<RacerProgress> Racers => racers;
        public float RaceTime => State == RaceState.Racing || State == RaceState.Finished
            ? Time.time - raceStartTime
            : 0f;
        public float LapTime => State == RaceState.Racing ? Time.time - lapStartTime : 0f;
        public float BestLapTime => bestLapTime == float.MaxValue ? 0f : bestLapTime;
        public float LastLapTime => lastLapTime;
        public int CountdownValue => countdownValue;

        public event Action<RaceState> OnStateChanged;
        public event Action<int> OnLapCompleted;
        public event Action<int> OnRaceFinished;
        public event Action<int> OnCountdownTick;

        void Start()
        {
            RegisterRacers();
            RegisterCheckpoints();
            StartCoroutine(BeginRaceCountdown());
        }

        void RegisterRacers()
        {
            racers.Clear();
            var found = FindObjectsByType<RacerProgress>();
            racers.AddRange(found);
            playerRacer = racers.Find(r => r.IsPlayer);
        }

        void RegisterCheckpoints()
        {
            checkpoints.Clear();
            var found = FindObjectsByType<TrackCheckpoint>(FindObjectsInactive.Exclude);
            Array.Sort(found, (a, b) => a.Index.CompareTo(b.Index));
            checkpoints.AddRange(found);

            foreach (var cp in checkpoints)
                cp.OnPassed += HandleCheckpointPassed;
        }

        IEnumerator BeginRaceCountdown()
        {
            FreezeAllRacersAtGrid();
            SetState(RaceState.Countdown);
            countdownValue = 3;
            playerFinishPosition = 0;
            OnCountdownTick?.Invoke(countdownValue);

            var firstCheckpoint = checkpoints.Count > 1 ? 1 : 0;
            foreach (var racer in racers)
            {
                if (!racer.IsPlayer)
                {
                    var reset = racer.GetComponent<VehicleReset>();
                    reset?.RestoreForRaceRestart();
                }

                racer.ResetProgress(firstCheckpoint);
            }

            while (countdownValue > 0)
            {
                yield return new WaitForSeconds(countdownInterval);
                countdownValue--;
                if (countdownValue > 0)
                    OnCountdownTick?.Invoke(countdownValue);
            }

            OnCountdownTick?.Invoke(0);
            yield return new WaitForSeconds(0.75f);
            raceStartTime = Time.time;
            lapStartTime = raceStartTime;
            SetState(RaceState.Racing);
        }

        void HandleCheckpointPassed(TrackCheckpoint checkpoint, RacerProgress racer)
        {
            if (State != RaceState.Racing || racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            if (checkpoint.Index != racer.NextCheckpointIndex)
                return;

            if (checkpoint.IsFinishLine && racer.CurrentLap >= totalLaps)
            {
                CompleteRacer(racer);
                return;
            }

            if (racer.IsPlayer && playerReset != null)
                playerReset.SetRespawnPoint(checkpoint.transform);

            racer.NextCheckpointIndex = (checkpoint.Index + 1) % Mathf.Max(checkpoints.Count, 1);

            if (!checkpoint.IsFinishLine)
                return;

            if (racer.IsPlayer)
            {
                lastLapTime = Time.time - lapStartTime;
                if (lastLapTime < bestLapTime)
                    bestLapTime = lastLapTime;
                OnLapCompleted?.Invoke(racer.CurrentLap);
            }

            racer.CanTriggerFinish = false;
            racer.AdvanceLap(checkpoints.Count > 1 ? 1 : 0);
            if (racer.IsPlayer)
                lapStartTime = Time.time;
        }

        void CompleteRacer(RacerProgress racer)
        {
            if (racer.IsFinished)
                return;

            if (racer.IsPlayer)
            {
                lastLapTime = Time.time - lapStartTime;
                if (lastLapTime < bestLapTime)
                    bestLapTime = lastLapTime;
                OnLapCompleted?.Invoke(racer.CurrentLap);
            }

            racer.MarkFinished(RaceTime);
            racer.CanTriggerFinish = false;

            if (racer.IsPlayer)
                FinishPlayerRace();
        }

        void FinishPlayerRace()
        {
            playerFinishPosition = GetPlayerPosition();
            if (playerRacer != null)
                playerRacer.GetComponent<RaceScoreSystem>()?.ApplyFinishBonus(playerFinishPosition);

            SetState(RaceState.Finished);
            OnRaceFinished?.Invoke(playerFinishPosition);
        }

        void FreezeAllRacersAtGrid()
        {
            foreach (var racer in racers)
            {
                if (racer == null)
                    continue;

                var rb = racer.GetComponent<Rigidbody>();
                if (rb == null)
                    continue;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        public float GetPlayerRaceProgress()
        {
            if (playerRacer == null || checkpoints.Count == 0)
                return 0f;

            if (playerRacer.IsFinished)
                return 1f;

            var checkpointCount = Mathf.Max(checkpoints.Count, 1);
            var firstCheckpoint = checkpointCount > 1 ? 1 : 0;
            var completed = (playerRacer.CurrentLap - 1) * checkpointCount +
                            Mathf.Max(playerRacer.NextCheckpointIndex - firstCheckpoint, 0);
            var total = Mathf.Max(totalLaps * checkpointCount, 1);
            return Mathf.Clamp01((float)completed / total);
        }

        public int GetPlayerPosition()
        {
            if (playerRacer == null)
                return 1;

            return CalculatePlacement(playerRacer);
        }

        public List<RacerProgress> GetRankedRacers()
        {
            var ranked = new List<RacerProgress>(racers);
            ranked.Sort(CompareRacerStandings);
            return ranked;
        }

        int CompareRacerStandings(RacerProgress a, RacerProgress b)
        {
            if (IsRacerAhead(a, b))
                return -1;
            if (IsRacerAhead(b, a))
                return 1;
            return 0;
        }

        int CalculatePlacement(RacerProgress player)
        {
            var rank = 1;
            foreach (var rival in racers)
            {
                if (rival == player)
                    continue;

                if (IsRacerAhead(rival, player))
                    rank++;
            }

            return rank;
        }

        static bool IsRacerAhead(RacerProgress a, RacerProgress b)
        {
            if (a.IsEliminated && !b.IsEliminated)
                return false;
            if (!a.IsEliminated && b.IsEliminated)
                return true;

            if (a.IsFinished && b.IsFinished)
                return a.FinishTime < b.FinishTime;
            if (a.IsFinished)
                return true;
            if (b.IsFinished)
                return false;

            if (a.CurrentLap != b.CurrentLap)
                return a.CurrentLap > b.CurrentLap;

            return a.NextCheckpointIndex > b.NextCheckpointIndex;
        }

        void SetState(RaceState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void RestartRace()
        {
            StopAllCoroutines();
            bestLapTime = float.MaxValue;
            lastLapTime = 0f;
            playerFinishPosition = 0;
            StartCoroutine(BeginRaceCountdown());
        }

        void OnDestroy()
        {
            foreach (var cp in checkpoints)
                cp.OnPassed -= HandleCheckpointPassed;
        }
    }
}
