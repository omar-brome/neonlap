using System;
using System.Collections;
using System.Collections.Generic;
using NeonLap.Audio;
using NeonLap.Race;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class RaceCommentarySystem : MonoBehaviour
    {
        static readonly string[] TakeLeadLines =
        {
            "Player takes the lead!",
            "Player is out in front!",
            "Into P1 — Player leads the race!",
            "Player snatches the lead!",
        };

        static readonly string[] OvertakeLines =
        {
            "Player moves up to {0}!",
            "Player climbs into {0}!",
            "Up a spot — Player is {0}!",
            "Player gains ground — now {0}!",
        };

        static readonly string[] BigGainLines =
        {
            "Huge move! Player rockets up {0} places!",
            "What a charge — Player gains {0} spots!",
            "Player is flying through the field!",
        };

        static readonly string[] DropLines =
        {
            "Player drops to {0}.",
            "Falls back to {0} for Player.",
            "Player loses a place — now {0}.",
            "Slipping to {0}.",
        };

        static readonly string[] LastPlaceLines =
        {
            "Player is at the back — time to push!",
            "Last place for now — Player needs a comeback!",
            "Player has work to do from the rear!",
        };

        static readonly string[] RaceStartLines =
        {
            "And they're away!",
            "Green flag — the race is on!",
            "Lights out — let's go!",
        };

        static readonly string[] FinalLapLines =
        {
            "Final lap!",
            "White flag — one lap to go!",
            "Last lap — everything on the line!",
        };

        static readonly string[] WinLines =
        {
            "Player wins it!",
            "Checkered flag — Player takes the win!",
            "Victory for Player!",
        };

        static readonly string[] FinishLines =
        {
            "Player crosses the line in {0}!",
            "Race done — Player finishes {0}!",
            "Player brings it home in {0}!",
        };

        [SerializeField] RaceManager raceManager;
        [SerializeField] Text subtitleText;
        [SerializeField] GameObject subtitlePanel;
        [SerializeField] float pollInterval = 0.35f;
        [SerializeField] float displayDuration = 3.6f;
        [SerializeField] float minTimeBetweenLines = 2.2f;

        int trackedPosition;
        int totalRacers;
        bool monitoring;
        bool subscribed;
        float lastLineTime;
        Coroutine displayRoutine;
        Coroutine monitorRoutine;
        CommentaryVoiceover voiceover;
        readonly System.Random lineRandom = new(90210);

        public event Action<CommentaryCategory> OnCommentaryLine;

        public void Configure(RaceManager manager, Text subtitle, GameObject panel, CommentaryVoiceover voice = null)
        {
            Unsubscribe();
            raceManager = manager;
            subtitleText = subtitle;
            subtitlePanel = panel;
            voiceover = voice;
            Subscribe();
            ResetTracking();
            SetPanelVisible(false);
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            StopMonitoring();
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
            {
                ResetTracking();
                StopMonitoring();
                ClearSubtitle();
                return;
            }

            if (state == RaceState.Racing)
            {
                ResetTracking();
                BeginMonitoring();
                QueueLine(PickRandom(RaceStartLines), CommentaryCategory.RaceStart, true);
                return;
            }

            if (state == RaceState.Finished)
                StopMonitoring();
        }

        void HandleLapCompleted(int completedLap)
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
                return;

            if (completedLap == raceManager.TotalLaps - 1)
                QueueLine(PickRandom(FinalLapLines), CommentaryCategory.FinalLap);
        }

        void HandleRaceFinished(int placement)
        {
            StopMonitoring();
            if (placement == 1)
                QueueLine(PickRandom(WinLines), CommentaryCategory.Win, true);
            else
                QueueLine(string.Format(PickRandom(FinishLines), GetPlacementLabel(placement)),
                    CommentaryCategory.Finish, true);
        }

        void ResetTracking()
        {
            trackedPosition = 0;
            totalRacers = raceManager != null ? Mathf.Max(raceManager.TotalRacers, 1) : 1;
        }

        void BeginMonitoring()
        {
            if (monitoring || raceManager == null)
                return;

            monitorRoutine = StartCoroutine(MonitorPositionRoutine());
            monitoring = true;
        }

        void StopMonitoring()
        {
            if (monitorRoutine != null)
            {
                StopCoroutine(monitorRoutine);
                monitorRoutine = null;
            }

            monitoring = false;
        }

        IEnumerator MonitorPositionRoutine()
        {
            var wait = new WaitForSeconds(pollInterval);
            while (raceManager != null && raceManager.State == RaceState.Racing)
            {
                yield return wait;

                var position = raceManager.GetPlayerPosition();
                totalRacers = Mathf.Max(raceManager.TotalRacers, 1);

                if (trackedPosition <= 0)
                {
                    trackedPosition = position;
                    continue;
                }

                if (position == trackedPosition)
                    continue;

                AnnouncePositionChange(trackedPosition, position);
                trackedPosition = position;
            }

            monitoring = false;
            monitorRoutine = null;
        }

        void AnnouncePositionChange(int oldPosition, int newPosition)
        {
            if (newPosition < oldPosition)
            {
                var gain = oldPosition - newPosition;
                if (newPosition == 1)
                    QueueLine(PickRandom(TakeLeadLines), CommentaryCategory.TakeLead);
                else if (gain >= 2)
                    QueueLine(string.Format(PickRandom(BigGainLines), gain), CommentaryCategory.BigGain);
                else
                    QueueLine(string.Format(PickRandom(OvertakeLines), GetPlacementLabel(newPosition)),
                        CommentaryCategory.Overtake);
                return;
            }

            if (newPosition >= totalRacers)
                QueueLine(PickRandom(LastPlaceLines), CommentaryCategory.LastPlace);
            else
                QueueLine(string.Format(PickRandom(DropLines), GetPlacementLabel(newPosition)),
                    CommentaryCategory.Drop);
        }

        void QueueLine(string line, CommentaryCategory category, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            if (!force && Time.time - lastLineTime < minTimeBetweenLines)
                return;

            lastLineTime = Time.time;
            voiceover?.Play(category, force);
            OnCommentaryLine?.Invoke(category);
            if (displayRoutine != null)
                StopCoroutine(displayRoutine);
            displayRoutine = StartCoroutine(DisplayLineRoutine(line));
        }

        IEnumerator DisplayLineRoutine(string line)
        {
            if (subtitleText != null)
                subtitleText.text = line;
            SetPanelVisible(true);

            yield return new WaitForSeconds(displayDuration);

            ClearSubtitle();
            displayRoutine = null;
        }

        void ClearSubtitle()
        {
            if (subtitleText != null)
                subtitleText.text = string.Empty;
            SetPanelVisible(false);
        }

        void SetPanelVisible(bool visible)
        {
            if (subtitlePanel != null)
                subtitlePanel.SetActive(visible);
        }

        string PickRandom(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            return lines[lineRandom.Next(lines.Count)];
        }

        static string GetPlacementLabel(int placement)
        {
            var suffix = (placement % 100) switch
            {
                11 or 12 or 13 => "th",
                _ when placement % 10 == 1 => "st",
                _ when placement % 10 == 2 => "nd",
                _ when placement % 10 == 3 => "rd",
                _ => "th"
            };

            return $"{placement}{suffix}";
        }
    }
}
