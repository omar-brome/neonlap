namespace NeonLap.Core
{
    public readonly struct RaceModeRules
    {
        public readonly bool SpawnAiRivals;
        public readonly int AiRivalCount;
        public readonly bool AllowPolice;
        public readonly bool ForcePolice;
        public readonly bool SpawnHelicopter;
        public readonly bool InfiniteFuel;
        public readonly bool UseLapFinish;
        public readonly bool ShowRaceScore;
        public readonly bool CountsTowardCareer;
        public readonly bool UseCareerUnlock;
        public readonly bool UseTimeTrialGhost;
        public readonly bool UseGhostDuel;
        public readonly bool SpawnTrackObstacles;
        public readonly bool UsePodiumSequence;
        public readonly bool RequiresCareerUnlock;

        public RaceModeRules(
            bool spawnAiRivals,
            int aiRivalCount,
            bool allowPolice,
            bool forcePolice,
            bool spawnHelicopter,
            bool infiniteFuel,
            bool useLapFinish,
            bool showRaceScore,
            bool countsTowardCareer,
            bool useCareerUnlock,
            bool useTimeTrialGhost,
            bool useGhostDuel,
            bool spawnTrackObstacles,
            bool usePodiumSequence,
            bool requiresCareerUnlock)
        {
            SpawnAiRivals = spawnAiRivals;
            AiRivalCount = aiRivalCount;
            AllowPolice = allowPolice;
            ForcePolice = forcePolice;
            SpawnHelicopter = spawnHelicopter;
            InfiniteFuel = infiniteFuel;
            UseLapFinish = useLapFinish;
            ShowRaceScore = showRaceScore;
            CountsTowardCareer = countsTowardCareer;
            UseCareerUnlock = useCareerUnlock;
            UseTimeTrialGhost = useTimeTrialGhost;
            UseGhostDuel = useGhostDuel;
            SpawnTrackObstacles = spawnTrackObstacles;
            UsePodiumSequence = usePodiumSequence;
            RequiresCareerUnlock = requiresCareerUnlock;
        }

        public static RaceModeRules For(RaceMode mode)
        {
            return mode switch
            {
                RaceMode.Career => new RaceModeRules(
                    spawnAiRivals: true,
                    aiRivalCount: 9,
                    allowPolice: true,
                    forcePolice: false,
                    spawnHelicopter: true,
                    infiniteFuel: false,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: true,
                    useCareerUnlock: true,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: true,
                    usePodiumSequence: true,
                    requiresCareerUnlock: true),

                RaceMode.TimeTrial => new RaceModeRules(
                    spawnAiRivals: false,
                    aiRivalCount: 0,
                    allowPolice: false,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: true,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: true,
                    useGhostDuel: false,
                    spawnTrackObstacles: false,
                    usePodiumSequence: false,
                    requiresCareerUnlock: false),

                RaceMode.GhostDuel => new RaceModeRules(
                    spawnAiRivals: false,
                    aiRivalCount: 0,
                    allowPolice: false,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: true,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: true,
                    spawnTrackObstacles: false,
                    usePodiumSequence: false,
                    requiresCareerUnlock: false),

                RaceMode.Elimination => new RaceModeRules(
                    spawnAiRivals: true,
                    aiRivalCount: 9,
                    allowPolice: true,
                    forcePolice: false,
                    spawnHelicopter: true,
                    infiniteFuel: false,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: true,
                    usePodiumSequence: true,
                    requiresCareerUnlock: false),

                RaceMode.Chase => new RaceModeRules(
                    spawnAiRivals: false,
                    aiRivalCount: 0,
                    allowPolice: false,
                    forcePolice: true,
                    spawnHelicopter: false,
                    infiniteFuel: false,
                    useLapFinish: false,
                    showRaceScore: false,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: false,
                    usePodiumSequence: false,
                    requiresCareerUnlock: false),

                RaceMode.ScoreAttack => new RaceModeRules(
                    spawnAiRivals: true,
                    aiRivalCount: 5,
                    allowPolice: false,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: false,
                    useLapFinish: false,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: true,
                    usePodiumSequence: false,
                    requiresCareerUnlock: false),

                RaceMode.StuntFreestyle => new RaceModeRules(
                    spawnAiRivals: false,
                    aiRivalCount: 0,
                    allowPolice: false,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: true,
                    useLapFinish: false,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: false,
                    usePodiumSequence: false,
                    requiresCareerUnlock: false),

                RaceMode.Practice => new RaceModeRules(
                    spawnAiRivals: false,
                    aiRivalCount: 0,
                    allowPolice: false,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: true,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: false,
                    usePodiumSequence: false,
                    requiresCareerUnlock: false),

                RaceMode.Custom => new RaceModeRules(
                    spawnAiRivals: true,
                    aiRivalCount: 9,
                    allowPolice: true,
                    forcePolice: false,
                    spawnHelicopter: true,
                    infiniteFuel: false,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: true,
                    usePodiumSequence: true,
                    requiresCareerUnlock: false),

                RaceMode.TeamRace => new RaceModeRules(
                    spawnAiRivals: true,
                    aiRivalCount: 9,
                    allowPolice: false,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: false,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: true,
                    usePodiumSequence: true,
                    requiresCareerUnlock: false),

                RaceMode.Demolition => new RaceModeRules(
                    spawnAiRivals: true,
                    aiRivalCount: 9,
                    allowPolice: false,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: false,
                    useLapFinish: false,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: true,
                    usePodiumSequence: true,
                    requiresCareerUnlock: false),

                RaceMode.Hardcore => new RaceModeRules(
                    spawnAiRivals: true,
                    aiRivalCount: 9,
                    allowPolice: true,
                    forcePolice: false,
                    spawnHelicopter: false,
                    infiniteFuel: false,
                    useLapFinish: true,
                    showRaceScore: true,
                    countsTowardCareer: false,
                    useCareerUnlock: false,
                    useTimeTrialGhost: false,
                    useGhostDuel: false,
                    spawnTrackObstacles: true,
                    usePodiumSequence: true,
                    requiresCareerUnlock: false),

                _ => For(RaceMode.Career)
            };
        }
    }
}
