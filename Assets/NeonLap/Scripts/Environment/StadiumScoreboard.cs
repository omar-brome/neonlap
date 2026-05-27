using System.Collections.Generic;
using System.Text;
using NeonLap.Audio;
using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using NeonLap.VFX;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.Environment
{
    public class StadiumScoreboard : MonoBehaviour
    {
        const int MaxIncidentLines = 4;
        const float IncidentDisplaySeconds = 5.5f;
        const float FastestLapTickerSeconds = 6f;
        const float LeaderboardRefreshInterval = 0.4f;
        const float PositionPollInterval = 0.35f;

        [SerializeField] Text titleText;
        [SerializeField] Text positionText;
        [SerializeField] Text lapText;
        [SerializeField] Text timerText;
        [SerializeField] Text statusText;
        [SerializeField] Text leaderboardHeaderText;
        [SerializeField] Text leaderLine1Text;
        [SerializeField] Text leaderLine2Text;
        [SerializeField] Text leaderLine3Text;
        [SerializeField] Text fastestLapTickerText;
        [SerializeField] Text incidentLine1Text;
        [SerializeField] Text incidentLine2Text;
        [SerializeField] Text incidentLine3Text;
        [SerializeField] Text incidentLine4Text;

        RaceManager raceManager;
        PoliceChaseSystem policeChase;
        StadiumPaSpeaker paSpeaker;

        readonly Queue<string> incidentQueue = new();
        readonly List<Text> incidentLines = new();

        string fastestLapTickerMessage = string.Empty;
        float fastestLapTickerEndTime;
        float nextLeaderboardRefresh;
        float nextPositionPoll;
        int trackedPlayerPosition;
        bool subscribed;

        public void Configure(
            RaceManager manager,
            Text title,
            Text position,
            Text lap,
            Text timer,
            Text status,
            Text leaderboardHeader,
            Text leader1,
            Text leader2,
            Text leader3,
            Text fastestLapTicker,
            Text incident1,
            Text incident2,
            Text incident3,
            Text incident4,
            PoliceChaseSystem police = null,
            StadiumPaSpeaker speaker = null)
        {
            Unsubscribe();
            raceManager = manager;
            policeChase = police;
            paSpeaker = speaker;

            titleText = title;
            positionText = position;
            lapText = lap;
            timerText = timer;
            statusText = status;
            leaderboardHeaderText = leaderboardHeader;
            leaderLine1Text = leader1;
            leaderLine2Text = leader2;
            leaderLine3Text = leader3;
            fastestLapTickerText = fastestLapTicker;

            incidentLines.Clear();
            if (incident1 != null) incidentLines.Add(incident1);
            if (incident2 != null) incidentLines.Add(incident2);
            if (incident3 != null) incidentLines.Add(incident3);
            if (incident4 != null) incidentLines.Add(incident4);

            incidentQueue.Clear();
            fastestLapTickerMessage = string.Empty;
            trackedPlayerPosition = 0;
            Subscribe();
            RefreshIncidentDisplay();
            ClearFastestLapTicker();
        }

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnRacerEliminated += HandleRacerEliminated;
            raceManager.OnRacerPersonalBestLap += HandlePersonalBestLap;
            StadiumIncidentHub.IncidentReported += HandleIncident;
            if (policeChase != null)
                policeChase.PoliceUnitsSpawned += HandlePoliceDeployed;

            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (raceManager != null)
            {
                raceManager.OnStateChanged -= HandleStateChanged;
                raceManager.OnRacerEliminated -= HandleRacerEliminated;
                raceManager.OnRacerPersonalBestLap -= HandlePersonalBestLap;
            }

            StadiumIncidentHub.IncidentReported -= HandleIncident;
            if (policeChase != null)
                policeChase.PoliceUnitsSpawned -= HandlePoliceDeployed;

            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
            {
                incidentQueue.Clear();
                fastestLapTickerMessage = string.Empty;
                trackedPlayerPosition = 0;
                RefreshIncidentDisplay();
                ClearFastestLapTicker();
            }
        }

        void HandleRacerEliminated(RacerProgress racer)
        {
            if (racer == null || raceManager == null)
                return;

            var placement = raceManager.GetRacerPlacement(racer);
            if (racer.IsPlayer)
                QueueIncident("YOU ELIMINATED");
            else
                QueueIncident($"P{placement} ELIMINATED");
        }

        void HandlePoliceDeployed() => QueueIncident("POLICE DEPLOYED");

        void HandleIncident(string message) => QueueIncident(message);

        void HandlePersonalBestLap(RacerProgress racer, float lapTime)
        {
            if (racer == null || lapTime <= 0.05f)
                return;

            var label = GetRacerBroadcastLabel(racer, raceManager != null ? raceManager.GetRacerPlacement(racer) : 1);
            ShowFastestLapTicker($"FASTEST LAP — {label}  {FormatLapTime(lapTime)}");
        }

        void Update()
        {
            if (raceManager == null)
                return;

            if (titleText != null)
                titleText.text = "NEON LAP LIVE";

            if (positionText != null)
            {
                var position = raceManager.GetPlayerPosition();
                var total = Mathf.Max(raceManager.TotalRacers, 1);
                positionText.text = $"P{position}/{total}";
            }

            if (lapText != null)
            {
                var lap = Mathf.Min(raceManager.CurrentLap, raceManager.TotalLaps);
                lapText.text = $"LAP {lap}/{raceManager.TotalLaps}";
            }

            if (timerText != null)
                timerText.text = FormatRaceTime(raceManager.RaceTime);

            if (statusText != null)
            {
                statusText.text = raceManager.State switch
                {
                    RaceState.Countdown => raceManager.CountdownValue > 0
                        ? $"STARTS IN {raceManager.CountdownValue}"
                        : "GO!",
                    RaceState.Racing => "RACE LIVE",
                    RaceState.Finished => "FINISH",
                    _ => "NEON GRAND PRIX"
                };
            }

            if (Time.time >= nextLeaderboardRefresh)
            {
                nextLeaderboardRefresh = Time.time + LeaderboardRefreshInterval;
                RefreshLeaderboard();
            }

            if (raceManager.State == RaceState.Racing && Time.time >= nextPositionPoll)
            {
                nextPositionPoll = Time.time + PositionPollInterval;
                PollPlayerPosition();
            }

            UpdateFastestLapTicker();
            RefreshSecondaryTicker();
        }

        void PollPlayerPosition()
        {
            var position = raceManager.GetPlayerPosition();
            if (trackedPlayerPosition <= 0)
            {
                trackedPlayerPosition = position;
                return;
            }

            if (position == trackedPlayerPosition)
                return;

            if (position == 1 && trackedPlayerPosition > 1)
                CrowdReactionHub.Emit(CrowdReactionKind.Celebration);
            else if (position < trackedPlayerPosition)
                CrowdReactionHub.Emit(CrowdReactionKind.Cheer);
            else if (position > trackedPlayerPosition)
                CrowdReactionHub.Emit(CrowdReactionKind.Mild);

            trackedPlayerPosition = position;
        }

        void RefreshLeaderboard()
        {
            if (leaderboardHeaderText != null)
                leaderboardHeaderText.text = "TOP 3";

            var ranked = raceManager.GetRankedRacers();
            SetLeaderLine(leaderLine1Text, ranked, 0);
            SetLeaderLine(leaderLine2Text, ranked, 1);
            SetLeaderLine(leaderLine3Text, ranked, 2);
        }

        void SetLeaderLine(Text line, List<RacerProgress> ranked, int index)
        {
            if (line == null)
                return;

            if (ranked == null || index >= ranked.Count || ranked[index] == null)
            {
                line.text = index switch
                {
                    0 => "P1  —",
                    1 => "P2  —",
                    _ => "P3  —",
                };
                return;
            }

            var racer = ranked[index];
            var placement = raceManager != null ? raceManager.GetRacerPlacement(racer) : index + 1;
            var name = GetRacerBroadcastLabel(racer, placement);
            if (racer.HasPersonalBestLap)
                line.text = $"P{placement}  {name}  {FormatLapTime(racer.PersonalBestLapTime)}";
            else
                line.text = $"P{placement}  {name}";
        }

        static string GetRacerBroadcastLabel(RacerProgress racer, int placement)
        {
            if (racer == null)
                return "—";

            if (racer.IsPlayer)
                return "YOU";

            if (racer.IsEliminated)
                return "OUT";

            var identity = racer.GetComponent<RivalIdentity>();
            if (identity != null && !string.IsNullOrWhiteSpace(identity.ShortName))
                return identity.ShortName.ToUpperInvariant();

            return $"R{placement}";
        }

        void RefreshSecondaryTicker()
        {
            if (fastestLapTickerText == null)
                return;

            if (!string.IsNullOrEmpty(fastestLapTickerMessage) && Time.time < fastestLapTickerEndTime)
                return;

            var message = BuildSecondaryTickerMessage();
            fastestLapTickerText.text = message;
            fastestLapTickerText.color = new Color(0.45f, 1f, 1f, 0.72f);
        }

        string BuildSecondaryTickerMessage()
        {
            if (raceManager == null)
                return "NEON GRAND PRIX";

            if (TryBuildGhostDeltaTicker(out var ghostLine))
                return ghostLine;

            if (GameRaceModeSettings.IsCareer && TryBuildCareerMedalTicker(out var careerLine))
                return careerLine;

            if (TryBuildWeatherGameplayTicker(out var weatherLine))
                return weatherLine;

            return BuildRivalStandingsTicker();
        }

        bool TryBuildCareerMedalTicker(out string line)
        {
            line = string.Empty;
            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var table = CareerMedalTables.Get(trackIndex);
            var player = raceManager.PlayerRacer;
            var score = player != null ? player.GetComponent<RaceScoreSystem>()?.Score ?? 0 : 0;
            var placement = raceManager.GetPlayerPosition();
            line = $"GOLD {table.GoldScore:N0} PTS — YOU {score:N0} (P{placement})";
            return true;
        }

        bool TryBuildGhostDeltaTicker(out string line)
        {
            line = string.Empty;
            if (!GameRaceModeSettings.IsTimeTrial && !GameRaceModeSettings.IsGhostDuel)
                return false;

            var player = raceManager.PlayerRacer;
            if (player == null)
                return false;

            var ghost = Object.FindAnyObjectByType<GhostRacer>();
            if (ghost == null || !ghost.IsVisible || !ghost.HasGhost)
                return false;

            if (!ghost.TryGetDeltaSeconds(player.transform.position, out var delta))
                return false;

            var label = ghost.IsDevGhost ? "DEV GHOST" : "PB GHOST";
            line = $"{label} {GhostPlaybackDelta.FormatDelta(delta)}";
            return true;
        }

        bool TryBuildWeatherGameplayTicker(out string line)
        {
            line = string.Empty;
            var weather = DynamicWeatherSystem.Instance;
            if (weather == null || raceManager.State != RaceState.Racing)
                return false;

            if (weather.SunnyBlend < 0.38f)
            {
                line = $"RAIN — GRIP {weather.GripMultiplier:0.00}×";
                return true;
            }

            if (weather.SunnyBlend > 0.82f)
            {
                line = $"SUN — TOP SPEED {weather.TopSpeedMultiplier:0.00}×";
                return true;
            }

            return false;
        }

        string BuildRivalStandingsTicker()
        {
            var ranked = raceManager.GetRankedRacers();
            if (ranked == null || ranked.Count == 0)
                return "FIELD STANDINGS";

            var builder = new StringBuilder("RIVALS ");
            var added = 0;
            for (var i = 0; i < ranked.Count && added < 3; i++)
            {
                var racer = ranked[i];
                if (racer == null || racer.IsPlayer)
                    continue;

                if (added > 0)
                    builder.Append(" · ");

                builder.Append(GetRacerBroadcastLabel(racer, raceManager.GetRacerPlacement(racer)));
                added++;
            }

            return added > 0 ? builder.ToString() : "FIELD STANDINGS";
        }

        void QueueIncident(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var upper = message.ToUpperInvariant();
            incidentQueue.Enqueue(upper);
            while (incidentQueue.Count > MaxIncidentLines)
                incidentQueue.Dequeue();

            RefreshIncidentDisplay();
            paSpeaker?.PlayIncident(upper);
        }

        void RefreshIncidentDisplay()
        {
            var messages = incidentQueue.ToArray();
            for (var i = 0; i < incidentLines.Count; i++)
            {
                if (incidentLines[i] == null)
                    continue;

                var index = messages.Length - incidentLines.Count + i;
                incidentLines[i].text = index >= 0 && index < messages.Length
                    ? $"> {messages[index]}"
                    : string.Empty;
            }
        }

        void ShowFastestLapTicker(string message)
        {
            fastestLapTickerMessage = message;
            fastestLapTickerEndTime = Time.time + FastestLapTickerSeconds;
            UpdateFastestLapTicker();
        }

        void UpdateFastestLapTicker()
        {
            if (fastestLapTickerText == null)
                return;

            if (string.IsNullOrEmpty(fastestLapTickerMessage) || Time.time >= fastestLapTickerEndTime)
            {
                ClearFastestLapTicker();
                return;
            }

            fastestLapTickerText.text = fastestLapTickerMessage;
            var pulse = 0.85f + Mathf.Sin(Time.time * 8f) * 0.15f;
            fastestLapTickerText.color = new Color(1f, 0.92f, 0.35f, pulse);
        }

        void ClearFastestLapTicker()
        {
            fastestLapTickerMessage = string.Empty;
            if (fastestLapTickerText != null)
            {
                fastestLapTickerText.text = "FASTEST LAP —";
                fastestLapTickerText.color = new Color(0.45f, 1f, 1f, 0.55f);
            }
        }

        static string FormatRaceTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = seconds % 60f;
            return $"{minutes:00}:{secs:00.0}";
        }

        static string FormatLapTime(float seconds)
        {
            if (seconds >= 60f)
            {
                var minutes = Mathf.FloorToInt(seconds / 60f);
                var secs = seconds % 60f;
                return $"{minutes}:{secs:00.00}";
            }

            return $"{seconds:0.00}s";
        }
    }
}
