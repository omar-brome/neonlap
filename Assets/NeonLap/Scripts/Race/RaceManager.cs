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
        [SerializeField] int totalLaps = 3;
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
            SetState(RaceState.Countdown);
            countdownValue = 3;
            playerFinishPosition = 0;
            OnCountdownTick?.Invoke(countdownValue);

            var firstCheckpoint = checkpoints.Count > 1 ? 1 : 0;
            foreach (var racer in racers)
                racer.ResetProgress(firstCheckpoint);

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
            if (State != RaceState.Racing || racer == null)
                return;

            if (checkpoint.Index != racer.NextCheckpointIndex)
                return;

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

            if (racer.CurrentLap >= totalLaps)
            {
                racer.MarkFinished(RaceTime);
                if (racer.IsPlayer)
                    FinishPlayerRace();
                return;
            }

            racer.AdvanceLap(checkpoints.Count > 1 ? 1 : 0);
            if (racer.IsPlayer)
                lapStartTime = Time.time;
        }

        void FinishPlayerRace()
        {
            playerFinishPosition = CalculatePlacement(playerRacer);
            SetState(RaceState.Finished);
            OnRaceFinished?.Invoke(playerFinishPosition);
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
