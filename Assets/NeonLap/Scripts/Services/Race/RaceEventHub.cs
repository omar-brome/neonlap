using System;
using NeonLap.Core;

namespace NeonLap.Services.Race
{
    public readonly struct RaceFinishEvent
    {
        public int Placement { get; init; }
        public int TrackIndex { get; init; }
        public RaceMode Mode { get; init; }
        public float RaceTime { get; init; }
        public float BestLapTime { get; init; }
        public int Score { get; init; }
        public bool Won { get; init; }
    }

    public readonly struct LapPersonalBestEvent
    {
        public int TrackIndex { get; init; }
        public float LapTime { get; init; }
        public RaceMode Mode { get; init; }
    }

    public readonly struct PoliceEscapeEvent
    {
        public int CheckpointsPassed { get; init; }
        public float SurvivalTime { get; init; }
        public bool CheckpointEscape { get; init; }
    }

    /// <summary>
    /// Cross-cutting race events for achievements, leaderboards, Steam, and mobile SDK hooks.
    /// </summary>
    public static class RaceEventHub
    {
        public static event Action<RaceFinishEvent> RaceFinished;
        public static event Action<LapPersonalBestEvent> LapPersonalBest;
        public static event Action<PoliceEscapeEvent> PoliceEscaped;

        public static void PublishRaceFinished(RaceFinishEvent finish)
        {
            RaceFinished?.Invoke(finish);
        }

        public static void PublishLapPersonalBest(LapPersonalBestEvent lapPb)
        {
            LapPersonalBest?.Invoke(lapPb);
        }

        public static void PublishPoliceEscaped(PoliceEscapeEvent escape)
        {
            PoliceEscaped?.Invoke(escape);
        }
    }
}
