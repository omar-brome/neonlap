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
        bool playerLapFinishEnabled = true;

        public RaceState State { get; private set; } = RaceState.Waiting;
        public bool PlayerLapFinishEnabled => playerLapFinishEnabled;
        public int CurrentLap => playerRacer != null ? playerRacer.CurrentLap : 1;
        public int TotalLaps => totalLaps;
        public int PlayerFinishPosition => playerFinishPosition;
        public int TotalRacers => racers.Count;
        public IReadOnlyList<RacerProgress> Racers => racers;
        public RacerProgress PlayerRacer => playerRacer;
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
        public event Action<RacerProgress, TrackCheckpoint> OnCheckpointPassed;
        public event Action<RacerProgress> OnRacerEliminated;
        public event Action<RacerProgress, float> OnRacerPersonalBestLap;

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
            foreach (var racer in racers)
                racer?.BeginLapSegment(raceStartTime);
            SetState(RaceState.Racing);
        }

        void HandleCheckpointPassed(TrackCheckpoint checkpoint, RacerProgress racer)
        {
            if (State != RaceState.Racing || racer == null || racer.IsFinished || racer.IsEliminated)
                return;

            if (racer.IsPlayer && RaceShortcutTracker.Instance != null)
                RaceShortcutTracker.Instance.TryAuthorizeMergeCheckpoint(checkpoint.Index, racer);

            if (checkpoint.Index != racer.NextCheckpointIndex)
                return;

            OnCheckpointPassed?.Invoke(racer, checkpoint);

            if (checkpoint.IsFinishLine && racer.CurrentLap >= totalLaps)
            {
                if (racer.IsPlayer && !playerLapFinishEnabled)
                {
                    AdvanceRacerLap(racer);
                    return;
                }

                CompleteRacer(racer);
                return;
            }

            if (racer.IsPlayer && playerReset != null)
                playerReset.SetRespawnPoint(checkpoint.transform);

            racer.NextCheckpointIndex = (checkpoint.Index + 1) % Mathf.Max(checkpoints.Count, 1);

            if (!checkpoint.IsFinishLine)
                return;

            if (!TryAcceptShortcutLap(racer))
                return;

            RecordCompletedLap(racer);

            racer.CanTriggerFinish = false;
            racer.AdvanceLap(checkpoints.Count > 1 ? 1 : 0);
        }

        bool TryAcceptShortcutLap(RacerProgress racer)
        {
            if (racer == null || !racer.IsPlayer || RaceShortcutTracker.Instance == null)
                return true;

            if (RaceShortcutTracker.Instance.ValidateLapAfterShortcut())
            {
                RaceShortcutTracker.Instance.ResetLapShortcutState();
                return true;
            }

            RaceShortcutTracker.Instance.ResetLapShortcutState();
            racer.BeginLapSegment(RaceTime);
            return false;
        }

        void CompleteRacer(RacerProgress racer)
        {
            if (racer.IsFinished)
                return;

            RecordCompletedLap(racer);

            racer.MarkFinished(RaceTime);
            racer.CanTriggerFinish = false;

            if (racer.IsPlayer)
                FinishPlayerRace();
        }

        public void SetPlayerLapFinishEnabled(bool enabled)
        {
            playerLapFinishEnabled = enabled;
        }

        public void EliminateRacer(RacerProgress racer)
        {
            if (racer == null || racer.IsEliminated || racer.IsFinished)
                return;

            racer.MarkEliminated(RaceTime);
            OnRacerEliminated?.Invoke(racer);

            if (racer.IsPlayer)
                EndPlayerRace(UsesTeamPlacement() ? GetPlayerTeamRacePosition() : GetPlayerPosition());
        }

        public void EndPlayerRace(int placement)
        {
            if (State == RaceState.Finished)
                return;

            if (playerRacer != null && !playerRacer.IsFinished)
                playerRacer.MarkFinished(RaceTime);

            playerFinishPosition = Mathf.Max(1, placement);
            if (playerRacer != null)
                playerRacer.GetComponent<RaceScoreSystem>()?.ApplyFinishBonus(playerFinishPosition);

            SetState(RaceState.Finished);
            OnRaceFinished?.Invoke(playerFinishPosition);
        }

        public int CountMobileRacers()
        {
            var count = 0;
            foreach (var racer in racers)
            {
                if (Vehicle.VehicleMobility.IsRacerMobile(racer))
                    count++;
            }

            return count;
        }

        public int CountActiveRacers()
        {
            var count = 0;
            foreach (var racer in racers)
            {
                if (racer != null && !racer.IsEliminated && !racer.IsFinished)
                    count++;
            }

            return count;
        }

        public RacerProgress GetLastPlaceActiveRacer()
        {
            var ranked = GetRankedRacers();
            for (var i = ranked.Count - 1; i >= 0; i--)
            {
                var racer = ranked[i];
                if (racer != null && !racer.IsEliminated && !racer.IsFinished)
                    return racer;
            }

            return null;
        }

        void FinishPlayerRace()
        {
            EndPlayerRace(UsesTeamPlacement() ? GetPlayerTeamRacePosition() : GetPlayerPosition());
        }

        public bool UsesTeamPlacement() => Core.GameRaceModeSettings.IsTeamRace;

        public int GetPlayerTeamPlacement()
        {
            if (playerRacer == null)
                return 1;

            var playerTeam = playerRacer.GetComponent<RacerTeamMarker>()?.Team ?? RaceTeam.Blue;
            if (playerTeam == RaceTeam.None)
                return GetPlayerPosition();

            var rank = 1;
            foreach (var rival in racers)
            {
                if (rival == null || rival == playerRacer)
                    continue;

                var marker = rival.GetComponent<RacerTeamMarker>();
                if (marker == null || marker.Team != playerTeam)
                    continue;

                if (IsRacerAhead(rival, playerRacer))
                    rank++;
            }

            return rank;
        }

        public int CountTeammates()
        {
            if (playerRacer == null)
                return 1;

            var playerTeam = playerRacer.GetComponent<RacerTeamMarker>()?.Team ?? RaceTeam.None;
            if (playerTeam == RaceTeam.None)
                return TotalRacers;

            var count = 1;
            foreach (var rival in racers)
            {
                if (rival == null || rival == playerRacer)
                    continue;

                var marker = rival.GetComponent<RacerTeamMarker>();
                if (marker != null && marker.Team == playerTeam)
                    count++;
            }

            return count;
        }

        public int GetRacerTeamPlacement(RacerProgress racer)
        {
            if (racer == null)
                return 1;

            var team = racer.GetComponent<RacerTeamMarker>()?.Team ?? RaceTeam.None;
            if (team == RaceTeam.None)
                return GetRacerPlacement(racer);

            var rank = 1;
            foreach (var other in racers)
            {
                if (other == null || other == racer)
                    continue;

                var otherTeam = other.GetComponent<RacerTeamMarker>()?.Team ?? RaceTeam.None;
                if (otherTeam != team)
                    continue;

                if (IsRacerAhead(other, racer))
                    rank++;
            }

            return rank;
        }

        public float GetTeamBestProgress(RaceTeam team)
        {
            if (team == RaceTeam.None)
                return 0f;

            var best = 0f;
            foreach (var racer in racers)
            {
                if (racer == null)
                    continue;

                var marker = racer.GetComponent<RacerTeamMarker>();
                if (marker == null || marker.Team != team)
                    continue;

                best = Mathf.Max(best, GetRaceProgress(racer));
            }

            return best;
        }

        public int GetTeamRacePosition(RaceTeam team)
        {
            if (team == RaceTeam.None)
                return 1;

            var rank = 1;
            foreach (var otherTeam in new[] { RaceTeam.Blue, RaceTeam.Red })
            {
                if (otherTeam == team || otherTeam == RaceTeam.None)
                    continue;

                if (GetTeamBestProgress(otherTeam) > GetTeamBestProgress(team))
                    rank++;
            }

            return rank;
        }

        public int GetPlayerTeamRacePosition()
        {
            if (playerRacer == null)
                return 1;

            var playerTeam = playerRacer.GetComponent<RacerTeamMarker>()?.Team ?? RaceTeam.None;
            return GetTeamRacePosition(playerTeam);
        }

        void AdvanceRacerLap(RacerProgress racer)
        {
            if (!TryAcceptShortcutLap(racer))
                return;

            RecordCompletedLap(racer);

            var firstCheckpoint = checkpoints.Count > 1 ? 1 : 0;
            racer.CanTriggerFinish = false;
            racer.AdvanceLap(firstCheckpoint);
        }

        void RecordCompletedLap(RacerProgress racer)
        {
            if (racer == null)
                return;

            if (racer.IsPlayer && RaceShortcutTracker.Instance != null)
                RaceShortcutTracker.Instance.ResetLapShortcutState();

            var isPersonalBest = racer.TryCompleteLapSegment(RaceTime);
            if (isPersonalBest)
                OnRacerPersonalBestLap?.Invoke(racer, racer.LastLapTime);

            if (racer.IsPlayer)
            {
                lastLapTime = racer.LastLapTime;
                if (lastLapTime > 0.05f && lastLapTime < bestLapTime)
                    bestLapTime = lastLapTime;
                OnLapCompleted?.Invoke(racer.CurrentLap);
            }
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

                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                rb.isKinematic = true;
            }
        }

        public float GetPlayerRaceProgress()
        {
            return GetRaceProgress(playerRacer);
        }

        public float GetRaceProgress(RacerProgress racer)
        {
            if (racer == null || checkpoints.Count == 0)
                return 0f;

            if (racer.IsFinished)
                return 1f;

            var checkpointCount = Mathf.Max(checkpoints.Count, 1);
            var firstCheckpoint = checkpointCount > 1 ? 1 : 0;
            var completed = (racer.CurrentLap - 1) * checkpointCount +
                            Mathf.Max(racer.NextCheckpointIndex - firstCheckpoint, 0);
            var total = Mathf.Max(totalLaps * checkpointCount, 1);
            return Mathf.Clamp01((float)completed / total);
        }

        public int GetPlayerPosition()
        {
            if (playerRacer == null)
                return 1;

            return GetRacerPlacement(playerRacer);
        }

        public int GetRacerPlacement(RacerProgress racer)
        {
            if (racer == null)
                return 1;

            var rank = 1;
            foreach (var rival in racers)
            {
                if (rival == racer)
                    continue;

                if (IsRacerAhead(rival, racer))
                    rank++;
            }

            return rank;
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

        int CalculatePlacement(RacerProgress player) => GetRacerPlacement(player);

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
