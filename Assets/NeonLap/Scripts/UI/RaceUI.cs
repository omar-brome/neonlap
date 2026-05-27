using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Services.Race;
using NeonLap.VFX;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class RaceUI : MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        [SerializeField] VehicleController playerVehicle;
        [SerializeField] Text lapText;
        [SerializeField] Text lapTimerText;
        [SerializeField] Text pbLapHeaderText;
        [SerializeField] Text pbLapTimerText;
        [SerializeField] Text sectorSplitText;
        [SerializeField] Text raceTimerText;
        [SerializeField] Text bestLapText;
        [SerializeField] Text positionText;
        [SerializeField] Text scoreText;
        [SerializeField] RaceScoreSystem scoreSystem;
        [SerializeField] VehicleDashboardCluster dashboardCluster;
        [SerializeField] Text countdownText;
        [SerializeField] Text countdownSubtitleText;
        [SerializeField] GameObject countdownPanel;
        [SerializeField] GameObject finishPanel;
        [SerializeField] Text finishTitleText;
        [SerializeField] Text finishDetailText;
        [SerializeField] Text finishBreakdownText;
        [SerializeField] RaceFinishScreenView finishScreenView;

        RaceFinishSummary lastFinishSummary;
        bool subscribed;

        readonly System.Collections.Generic.Dictionary<int, float> bestSplitsByCheckpoint = new();
        readonly System.Collections.Generic.Dictionary<int, float> currentSplitsByCheckpoint = new();
        float lastCheckpointTime;
        bool splitsArmed;
        string lastSplitHudText = string.Empty;

        public void Configure(
            RaceManager manager,
            VehicleController player,
            Text lap,
            Text lapTimer,
            Text pbLapHeader,
            Text pbLapTimer,
            Text sectorSplit,
            Text raceTimer,
            Text bestLap,
            Text position,
            Text score,
            RaceScoreSystem scoring,
            VehicleDashboardCluster dashboard,
            Text countdown,
            Text countdownSubtitle,
            GameObject countdownPanelObject,
            GameObject finishPanelObject,
            Text finishTitle,
            Text finishDetail,
            Text finishBreakdown,
            RaceFinishScreenView finishScreen = null)
        {
            Unsubscribe();
            raceManager = manager;
            playerVehicle = player;
            lapText = lap;
            lapTimerText = lapTimer;
            pbLapHeaderText = pbLapHeader;
            pbLapTimerText = pbLapTimer;
            sectorSplitText = sectorSplit;
            raceTimerText = raceTimer;
            bestLapText = bestLap;
            positionText = position;
            scoreText = score;
            scoreSystem = scoring;
            dashboardCluster = dashboard;
            countdownText = countdown;
            countdownSubtitleText = countdownSubtitle;
            countdownPanel = countdownPanelObject;
            finishPanel = finishPanelObject;
            finishTitleText = finishTitle;
            finishDetailText = finishDetail;
            finishBreakdownText = finishBreakdown;
            finishScreenView = finishScreen;
            Subscribe();
            SyncCountdownFromManager();
        }

        void Awake()
        {
            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();
        }

        void Start()
        {
            if (raceManager == null)
                raceManager = FindAnyObjectByType<RaceManager>();
            Subscribe();
            SyncCountdownFromManager();
        }

        void OnEnable()
        {
            Subscribe();
            SyncCountdownFromManager();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (subscribed || raceManager == null)
                return;

            raceManager.OnCountdownTick += HandleCountdownTick;
            raceManager.OnStateChanged += HandleStateChanged;
            raceManager.OnLapCompleted += HandleLapCompleted;
            raceManager.OnCheckpointPassed += HandleCheckpointPassed;
            raceManager.OnRacerPersonalBestLap += HandlePersonalBestLap;
            raceManager.OnRaceFinished += HandleRaceFinished;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
                return;

            raceManager.OnCountdownTick -= HandleCountdownTick;
            raceManager.OnStateChanged -= HandleStateChanged;
            raceManager.OnLapCompleted -= HandleLapCompleted;
            raceManager.OnCheckpointPassed -= HandleCheckpointPassed;
            raceManager.OnRacerPersonalBestLap -= HandlePersonalBestLap;
            raceManager.OnRaceFinished -= HandleRaceFinished;
            subscribed = false;
        }

        void SyncCountdownFromManager()
        {
            if (raceManager == null)
                return;

            if (raceManager.State == RaceState.Countdown)
                ShowCountdown(raceManager.CountdownValue);
        }

        void Update()
        {
            if (raceManager == null)
                return;

            if (lapText != null)
                lapText.text = $"Lap {Mathf.Min(raceManager.CurrentLap, raceManager.TotalLaps)}/{raceManager.TotalLaps}";

            if (lapTimerText != null && raceManager.State == RaceState.Racing)
            {
                var baseText = FormatTime(raceManager.LapTime);
                lapTimerText.text = string.IsNullOrWhiteSpace(lastSplitHudText)
                    ? baseText
                    : $"{baseText}  •  {lastSplitHudText}";
            }

            UpdateTimeTrialPbLapColumn();

            if (raceTimerText != null && (raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished))
                raceTimerText.text = FormatTime(raceManager.RaceTime);

            if (bestLapText != null)
            {
                var best = raceManager.BestLapTime;
                bestLapText.text = best > 0f ? $"Best {FormatTime(best)}" : "Best --:--.--";
            }

            if (positionText != null)
                UpdateModeStatusText();

            UpdateScoreDisplay();
            UpdateDashboardVisibility();
        }

        void UpdateTimeTrialPbLapColumn()
        {
            var show = GameRaceModeSettings.IsTimeTrial && pbLapTimerText != null;
            if (pbLapHeaderText != null)
                pbLapHeaderText.gameObject.SetActive(show);
            if (pbLapTimerText != null)
                pbLapTimerText.gameObject.SetActive(show);

            if (!show)
                return;

            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var pbLap = TimeTrialRecordStore.GetBestLapTime(trackIndex);
            pbLapTimerText.text = pbLap > 0.05f ? FormatTime(pbLap) : "--:--.--";

            if (raceManager.State != RaceState.Racing && raceManager.State != RaceState.Finished)
                return;

            var delta = raceManager.LapTime - pbLap;
            if (pbLap <= 0.05f || raceManager.State != RaceState.Racing)
                return;

            var ahead = delta < -0.01f;
            pbLapTimerText.color = ahead
                ? new Color(0.35f, 1f, 0.65f)
                : delta > 0.01f
                    ? new Color(1f, 0.45f, 0.55f)
                    : new Color(0.55f, 0.95f, 1f);
        }

        void ResetSplitTracking()
        {
            currentSplitsByCheckpoint.Clear();
            lastCheckpointTime = raceManager != null ? raceManager.RaceTime : 0f;
            splitsArmed = true;
            lastSplitHudText = string.Empty;

            if (sectorSplitText != null)
                sectorSplitText.text = string.Empty;
        }

        void UpdateModeStatusText()
        {
            if (positionText == null || raceManager == null)
                return;

            if (raceManager.State != RaceState.Racing && raceManager.State != RaceState.Finished)
            {
                positionText.text = string.Empty;
                return;
            }

            if (GameRaceModeSettings.IsTimeTrial)
            {
                var devLine = TimeTrialRecordStore.IsUsingDevGhost(
                    GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0)
                    ? "  •  DEV GHOST"
                    : string.Empty;
                var rivals = TimeTrialSettings.RivalCount > 0
                    ? $"  •  {TimeTrialSettings.RivalCount} RIVALS"
                    : string.Empty;
                positionText.text = $"TIME TRIAL  •  BEAT THE GHOST{devLine}{rivals}";
                positionText.color = new Color(0.55f, 0.95f, 1f);
                return;
            }

            if (GameRaceModeSettings.IsGhostDuel)
            {
                positionText.text = "GHOST DUEL  •  CYAN = LAP PB  •  ORANGE = RACE PB";
                positionText.color = new Color(0.95f, 0.72f, 1f);
                return;
            }

            if (GameRaceModeSettings.IsChase)
            {
                var heat = playerVehicle != null
                    ? playerVehicle.GetComponent<PlayerHeatSystem>()
                    : null;
                var heatPct = heat != null ? Mathf.RoundToInt(heat.NormalizedHeat * 100f) : 0;
                var chase = FindAnyObjectByType<ChaseModeController>();
                var checkpointTarget = chase != null ? chase.CheckpointEscapeTarget : 8;
                positionText.text =
                    $"OUTRUN  •  HEAT {heatPct}%  •  LAP 3 / {checkpointTarget} CP / 2:00 SURVIVE";
                positionText.color = heatPct >= 75
                    ? new Color(1f, 0.45f, 0.55f)
                    : new Color(1f, 0.72f, 0.35f);
                return;
            }

            if (GameRaceModeSettings.IsScoreAttack)
            {
                var attack = FindAnyObjectByType<ScoreAttackModeController>();
                var remaining = attack != null ? attack.RemainingTime : 0f;
                positionText.text = $"SCORE ATTACK  •  {FormatTime(remaining)} LEFT";
                positionText.color = new Color(1f, 0.92f, 0.35f);
                return;
            }

            if (GameRaceModeSettings.IsElimination)
            {
                var active = raceManager.CountActiveRacers();
                var position = raceManager.GetPlayerPosition();
                positionText.text = $"ELIMINATION  •  {active} RACERS LEFT  •  P{position}";
                positionText.color = position == 1
                    ? new Color(0.45f, 1f, 1f)
                    : Color.white;
                return;
            }

            if (GameRaceModeSettings.IsDemolition)
            {
                var mobile = raceManager.CountMobileRacers();
                var position = raceManager.GetPlayerPosition();
                positionText.text = $"DEMOLITION  •  {mobile} CARS MOVING  •  P{position}";
                positionText.color = mobile <= 2
                    ? new Color(1f, 0.55f, 0.2f)
                    : Color.white;
                return;
            }

            if (GameRaceModeSettings.IsHardcore)
            {
                var position = raceManager.GetPlayerPosition();
                positionText.text = $"HARDCORE  •  ONE HEAVY HIT = OUT  •  P{position}";
                positionText.color = new Color(1f, 0.38f, 0.42f);
                return;
            }

            if (GameRaceModeSettings.IsPractice)
            {
                positionText.text = "PRACTICE  •  FREE RUN  •  NO POLICE";
                positionText.color = new Color(0.65f, 1f, 0.55f);
                return;
            }

            if (GameRaceModeSettings.IsCareer && RaceShortcutTracker.Instance != null
                && RaceShortcutTracker.Instance.UsedShortcutThisLap
                && !RaceShortcutTracker.Instance.ShortcutLapValid)
            {
                positionText.text = "SHORTCUT  •  MERGE CHECKPOINT REQUIRED";
                positionText.color = new Color(1f, 0.55f, 0.2f);
                return;
            }

            if (GameRaceModeSettings.IsTeamRace)
            {
                var teamPlacement = raceManager.GetPlayerTeamPlacement();
                var teammates = Mathf.Max(raceManager.CountTeammates(), 1);
                var team = GameTeamRaceSettings.PlayerTeam;
                var teamRacePos = raceManager.GetPlayerTeamRacePosition();
                positionText.text =
                    $"{GameTeamRaceSettings.GetDisplayName(team)}  •  TEAM P{teamPlacement}/{teammates}  •  VS P{teamRacePos}/2  •  OVERALL P{raceManager.GetPlayerPosition()}";
                positionText.color = GameTeamRaceSettings.GetTeamColor(team);
                return;
            }

            var placement = raceManager.GetPlayerPosition();
            var total = Mathf.Max(raceManager.TotalRacers, 1);
            positionText.text = $"Position {GetPlacementLabel(placement)} / {total}";
            positionText.color = placement == 1
                ? new Color(0.45f, 1f, 1f)
                : Color.white;
        }

        void UpdateScoreDisplay()
        {
            if (scoreText == null || scoreSystem == null)
                return;

            if (!GameRaceModeSettings.Rules.ShowRaceScore)
            {
                scoreText.text = string.Empty;
                return;
            }

            if (raceManager.State != RaceState.Racing && raceManager.State != RaceState.Finished)
            {
                scoreText.text = string.Empty;
                return;
            }

            scoreText.text = $"Score {scoreSystem.Score:N0}";
            scoreText.color = scoreSystem.Score >= 1000
                ? new Color(1f, 0.92f, 0.35f)
                : new Color(0.45f, 1f, 1f);
        }

        void UpdateDashboardVisibility()
        {
            if (dashboardCluster == null || raceManager == null)
                return;

            var visible = raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished;
            dashboardCluster.SetVisible(visible);
        }

        void HandleCountdownTick(int value)
        {
            ShowCountdown(value);
        }

        void ShowCountdown(int value)
        {
            if (finishPanel != null)
                finishPanel.SetActive(false);

            if (countdownPanel != null)
                countdownPanel.SetActive(true);

            if (countdownSubtitleText != null)
            {
                if (value > 0)
                {
                    var lines = new System.Collections.Generic.List<string>();
                    if (GameTrackOptions.ReverseCircuit)
                        lines.Add("↺ REVERSE CIRCUIT — separate PBs & ghosts");

                    var forecast = DynamicWeatherSystem.Instance != null
                        ? DynamicWeatherSystem.Instance.GetCountdownForecastText()
                        : string.Empty;
                    if (!string.IsNullOrWhiteSpace(forecast))
                        lines.Add(forecast);

                    if (lines.Count > 0)
                    {
                        countdownSubtitleText.text = string.Join("  •  ", lines);
                    }
                    else
                    {
                        var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
                        var note = Track.TrackLevelConfig.GetModifierNote(trackIndex);
                        countdownSubtitleText.text = string.IsNullOrWhiteSpace(note) ? "GET READY" : note;
                    }
                }
                else
                {
                    countdownSubtitleText.text = string.Empty;
                }
            }

            if (countdownText == null)
                return;

            countdownText.text = value > 0 ? value.ToString() : "GO!";
            countdownText.color = value > 0 ? new Color(0.45f, 1f, 1f) : new Color(1f, 0.95f, 0.35f);
        }

        void HandleStateChanged(RaceState state)
        {
            if (state == RaceState.Countdown)
            {
                if (finishPanel != null)
                    finishPanel.SetActive(false);
                SyncCountdownFromManager();
                splitsArmed = false;
                currentSplitsByCheckpoint.Clear();
                lastSplitHudText = string.Empty;
                if (sectorSplitText != null)
                    sectorSplitText.text = string.Empty;
                return;
            }

            if (countdownPanel != null && state == RaceState.Racing)
                countdownPanel.SetActive(false);

            if (finishPanel != null && state == RaceState.Countdown)
                finishPanel.SetActive(false);

            if (state == RaceState.Racing)
                ResetSplitTracking();
        }

        void HandleLapCompleted(int lap)
        {
            if (lapTimerText != null)
                lapTimerText.text = FormatTime(raceManager.LastLapTime);

            if (raceManager != null && raceManager.State == RaceState.Racing)
                ResetSplitTracking();
        }

        void HandleCheckpointPassed(RacerProgress racer, TrackCheckpoint checkpoint)
        {
            if (raceManager == null || racer == null || checkpoint == null)
                return;

            if (!racer.IsPlayer)
                return;

            if (!splitsArmed || raceManager.State != RaceState.Racing)
                return;

            var now = raceManager.RaceTime;
            var split = Mathf.Max(0f, now - lastCheckpointTime);
            lastCheckpointTime = now;

            currentSplitsByCheckpoint[checkpoint.Index] = split;

            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            if (TryBuildStoredSectorSplit(trackIndex, checkpoint.Index, split, out var storedText))
            {
                lastSplitHudText = storedText;
                if (sectorSplitText != null)
                    sectorSplitText.text = storedText;
                return;
            }

            var text = $"CP{checkpoint.Index} {split:0.00}s";
            if (bestSplitsByCheckpoint.TryGetValue(checkpoint.Index, out var best) && best > 0.01f)
            {
                var delta = split - best;
                text += $" Δ{delta:+0.00;-0.00}s";
            }

            lastSplitHudText = text;
            if (sectorSplitText != null)
                sectorSplitText.text = text;
        }

        void HandlePersonalBestLap(RacerProgress racer, float lapTime)
        {
            if (racer == null || !racer.IsPlayer)
                return;

            // Personal-best lap snapshot: used to compute checkpoint sector deltas.
            bestSplitsByCheckpoint.Clear();
            foreach (var kvp in currentSplitsByCheckpoint)
                bestSplitsByCheckpoint[kvp.Key] = kvp.Value;
        }

        void HandleRaceFinished(int placement)
        {
            if (GameRaceModeSettings.IsStuntFreestyle)
            {
                FindAnyObjectByType<StuntFreestyleController>()?.EndSession();
                return;
            }

            if (GameRaceModeSettings.IsTimeTrial
                || GameRaceModeSettings.IsGhostDuel
                || GameRaceModeSettings.IsScoreAttack
                || GameRaceModeSettings.IsChase
                || GameRaceModeSettings.IsElimination
                || GameRaceModeSettings.IsDemolition)
                return;

            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            if (GameRaceModeSettings.IsHardcore)
            {
                ShowModeFinish(
                    placement == 1 ? "HARDCORE SURVIVOR" : "HARDCORE ELIMINATED",
                    placement == 1
                        ? "No fatal impacts"
                        : "One heavy hit ended your run",
                    new Color(1f, 0.38f, 0.42f),
                    placement);
                return;
            }

            if (GameRaceModeSettings.IsPractice)
            {
                var practiceScore = scoreSystem != null ? scoreSystem.Score : 0;
                ShowModeFinish(
                    placement == 1 ? "PRACTICE COMPLETE" : "PRACTICE SESSION ENDED",
                    $"Laps finished  •  Score {practiceScore:N0}  •  Time {FormatTime(raceManager.RaceTime)}",
                    new Color(0.65f, 1f, 0.55f),
                    placement,
                    raceManager.BestLapTime,
                    raceManager.RaceTime);
                return;
            }

            if (GameRaceModeSettings.IsTeamRace)
            {
                var teamScore = scoreSystem != null ? scoreSystem.Score : 0;
                var team = GameTeamRaceSettings.PlayerTeam;
                var teamPlacement = raceManager.GetPlayerTeamPlacement();
                var teammates = Mathf.Max(raceManager.CountTeammates(), 1);
                var teamRacePos = raceManager.GetPlayerTeamRacePosition();
                ShowModeFinish(
                    teamRacePos == 1 ? "TEAM VICTORY" : "TEAM RACE FINISHED",
                    $"{GameTeamRaceSettings.GetDisplayName(team)}  •  TEAM P{teamPlacement}/{teammates}  •  VS P{teamRacePos}/2  •  OVERALL P{placement}  •  Score {teamScore:N0}",
                    GameTeamRaceSettings.GetTeamColor(team),
                    placement);
                return;
            }

            if (GameRaceModeSettings.IsCustom)
            {
                var customScore = scoreSystem != null ? scoreSystem.Score : 0;
                var variantLine = GameTrackOptions.GetSummaryLine();
                ShowModeFinish(
                    placement == 1 ? "CUSTOM RACE COMPLETE" : "CUSTOM RACE FINISHED",
                    $"P{placement}  •  Score {customScore:N0}  •  {variantLine}  •  Time {FormatTime(raceManager.RaceTime)}",
                    new Color(0.95f, 0.75f, 0.35f),
                    placement,
                    raceManager.BestLapTime,
                    raceManager.RaceTime);
                return;
            }

            var score = scoreSystem != null ? scoreSystem.Score : 0;
            var trackIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            var result = RaceMetagameBridge.Latest.CareerResult;

            lastFinishSummary = RaceFinishSummaryBuilder.FromCareer(
                placement,
                score,
                result,
                raceManager != null ? raceManager.RaceTime : 0f,
                raceManager != null ? raceManager.BestLapTime : 0f);

            ApplyFinishSummaryToTexts(lastFinishSummary, trackIndex, placement, score, result, raceManager);

            if (!GameRaceModeSettings.Rules.UsePodiumSequence)
            {
                SetGameplayHudVisible(false);
                ShowFinishPanel();
            }
        }

        void ApplyFinishSummaryToTexts(
            RaceFinishSummary summary,
            int trackIndex,
            int placement,
            int score,
            CareerRaceResult result,
            RaceManager manager)
        {
            if (finishTitleText != null)
            {
                finishTitleText.text = summary.Title;
                finishTitleText.color = summary.TitleColor;
            }

            if (finishDetailText != null)
                finishDetailText.text = BuildCareerFinishSummary(manager, placement, score, result, trackIndex);

            if (finishBreakdownText != null)
            {
                var breakdown = scoreSystem != null ? scoreSystem.GetBreakdownText() : string.Empty;
                finishBreakdownText.text = breakdown;
                finishBreakdownText.gameObject.SetActive(!string.IsNullOrEmpty(breakdown));
            }

            finishScreenView?.Prepare(summary);
        }

        static string BuildCareerFinishSummary(
            RaceManager manager,
            int placement,
            int score,
            CareerRaceResult result,
            int trackIndex)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (GameManager.Instance != null)
            {
                var track = GameManager.Instance.GetCurrentTrackDefinition();
                if (track != null)
                    parts.Add(track.trackName);
            }

            if (result.NewHighScore)
                parts.Add("NEW HIGH SCORE");

            if (result.ImprovedMedal && result.Medal != RaceMedal.None)
                parts.Add($"NEW {RaceMedalUtility.GetMedalLabel(result.Medal)} MEDAL");
            else if (result.Medal != RaceMedal.None)
                parts.Add($"Medal {RaceMedalUtility.GetMedalLabel(result.Medal)}");

            if (result.Stars > 0)
                parts.Add(RaceMedalUtility.FormatStars(result.Stars));

            parts.Add($"Score {score:N0}");

            if (result.HighScore > 0)
                parts.Add($"PB {result.HighScore:N0}");

            if (GameRaceModeSettings.IsCareer && NeonLap.Services.Race.RaceMetagameBridge.Latest.CreditsEarned > 0)
                parts.Add($"Credits +{NeonLap.Services.Race.RaceMetagameBridge.Latest.CreditsEarned:N0}");

            if (NeonLap.Services.Race.RaceMetagameBridge.Latest.DailyCompleted)
            {
                if (NeonLap.Services.Race.RaceMetagameBridge.Latest.DailyBonusCredits > 0)
                    parts.Add($"Daily +{NeonLap.Services.Race.RaceMetagameBridge.Latest.DailyBonusCredits:N0}");
                if (NeonLap.Services.Race.RaceMetagameBridge.Latest.DailyBonusStars > 0)
                    parts.Add($"Daily +{NeonLap.Services.Race.RaceMetagameBridge.Latest.DailyBonusStars} ★");
            }

            parts.Add($"{GetPlacementLabel(placement)} Place");

            if (manager != null)
                parts.Add($"Time {FormatTime(manager.RaceTime)}");

            if (Track.TrackLevelConfig.RequiresShortcutForMedal(trackIndex)
                && RaceShortcutTracker.Instance != null
                && !RaceShortcutTracker.Instance.ShortcutRequirementMet)
                parts.Add("NO MEDAL — SHORTCUT REQUIRED");

            if (result.Stars >= 1 && result.PreviousStars < 1 && CareerScoreStore.IsTrackUnlocked(trackIndex + 1))
                parts.Add($"LEVEL {trackIndex + 2} UNLOCKED!");

            if (placement == 1 && GameManager.Instance != null && !GameManager.Instance.HasNextLevel)
                parts.Add("ALL LEVELS COMPLETE!");

            return string.Join("  •  ", parts);
        }

        public void ShowFinishPanel()
        {
            if (finishPanel != null)
                finishPanel.SetActive(true);

            finishScreenView?.Present();
        }

        public void ShowModeFinish(
            string title,
            string detail,
            Color titleColor,
            int placement = 1,
            float bestLapTime = 0f,
            float raceTime = 0f)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            lastFinishSummary = RaceFinishSummaryBuilder.FromMode(title, titleColor, placement, detail);
            lastFinishSummary.BestLapTime = bestLapTime;
            lastFinishSummary.RaceTime = raceTime;
            lastFinishSummary.XpEarned = RaceFinishRewards.GetXpEarned(scoreSystem != null ? scoreSystem.Score : 0, placement);

            if (finishTitleText != null)
            {
                finishTitleText.text = title;
                finishTitleText.color = titleColor;
            }

            if (finishDetailText != null)
                finishDetailText.text = detail;

            if (finishBreakdownText != null)
                finishBreakdownText.gameObject.SetActive(false);

            finishScreenView?.Prepare(lastFinishSummary);
            SetGameplayHudVisible(false);
            ShowFinishPanel();
        }

        public void ShowScoreAttackFinish(int score, bool newPb, string trackSummary, string breakdown)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            var title = newPb ? "NEW SCORE PB!" : "SCORE ATTACK COMPLETE";
            var color = newPb ? new Color(1f, 0.92f, 0.35f) : new Color(0.45f, 1f, 1f);
            var pbFlag = newPb ? "NEW PB  •  " : string.Empty;
            var detail = $"{pbFlag}Score {score:N0}  •  {trackSummary}";

            lastFinishSummary = RaceFinishSummaryBuilder.FromMode(title, color, 1, detail);
            lastFinishSummary.Score = score;
            lastFinishSummary.XpEarned = RaceFinishRewards.GetXpEarned(score, 1);

            if (finishTitleText != null)
            {
                finishTitleText.text = title;
                finishTitleText.color = color;
            }

            if (finishDetailText != null)
                finishDetailText.text = detail;

            if (finishBreakdownText != null)
            {
                finishBreakdownText.text = breakdown;
                finishBreakdownText.gameObject.SetActive(!string.IsNullOrEmpty(breakdown));
            }

            finishScreenView?.Prepare(lastFinishSummary);
            SetGameplayHudVisible(false);
            ShowFinishPanel();
        }

        public void PulseStuntTrick(int points, float airSeconds)
        {
            // Minimal UX: keep using the normal HUD, just ensure score refresh happens quickly.
            UpdateScoreDisplay();
        }

        public void ShowStuntFreestyleFinish(int score, int tricks, float bestAirSeconds, bool improved, string summaryLine)
        {
            var pbFlag = improved ? "NEW PB  •  " : string.Empty;
            var detail = $"{pbFlag}Score {score:N0}  •  Tricks {tricks}  •  Best air {bestAirSeconds:0.0}s  •  {summaryLine}";
            ShowModeFinish("STUNT SESSION COMPLETE", detail, new Color(1f, 0.55f, 0.85f), 1);
        }

        public static string FormatTimePublic(float seconds) => FormatTime(seconds);

        public static string GetPlacementLabelPublic(int placement) => GetPlacementLabel(placement);

        public void ShowTimeTrialFinish(
            float raceTime,
            float bestLapTime,
            bool newRacePb,
            bool newLapPb,
            TimeTrialFinishResult finish,
            string scoreBreakdown,
            bool canAdvanceNext = false)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            TimeTrialSettings.Load();
            var showRanks = TimeTrialSettings.ShowTimeRanks;

            var bestMedal = finish.RaceMedal;
            if (TimeTrialMedalUtility.Rank(finish.LapMedal) > TimeTrialMedalUtility.Rank(bestMedal))
                bestMedal = finish.LapMedal;

            if (finishTitleText != null)
            {
                if (newRacePb || newLapPb)
                    finishTitleText.text = "NEW PERSONAL BEST!";
                else if (showRanks && bestMedal != TimeTrialMedal.None)
                    finishTitleText.text = $"TIME RANK {TimeTrialMedalUtility.GetLabel(bestMedal)}";
                else
                    finishTitleText.text = "TIME TRIAL COMPLETE";

                finishTitleText.color = showRanks && bestMedal != TimeTrialMedal.None
                    ? TimeTrialMedalUtility.GetColor(bestMedal)
                    : new Color(0.45f, 1f, 1f);
            }

            if (finishDetailText != null)
            {
                var pbFlags = string.Empty;
                if (newRacePb)
                    pbFlags += "NEW RACE PB  •  ";
                if (newLapPb)
                    pbFlags += "NEW LAP PB  •  ";
                if (showRanks && (finish.ImprovedRaceMedal || finish.ImprovedLapMedal))
                    pbFlags += "NEW TIME RANK  •  ";

                var rankLine = showRanks
                    ? $"TIME RANK  Race {TimeTrialMedalUtility.GetLabel(finish.RaceMedal)}  •  Lap {TimeTrialMedalUtility.GetLabel(finish.LapMedal)}  •  "
                    : string.Empty;
                finishDetailText.text =
                    $"{pbFlags}{rankLine}Race {FormatTime(raceTime)}  •  Best Lap {FormatTime(bestLapTime)}  •  {finish.TrackSummary}";
            }

            if (finishBreakdownText != null)
            {
                finishBreakdownText.text = string.IsNullOrEmpty(scoreBreakdown)
                    ? string.Empty
                    : "STYLE POINTS (not career)  •  " + scoreBreakdown;
                finishBreakdownText.gameObject.SetActive(!string.IsNullOrEmpty(scoreBreakdown));
            }

            lastFinishSummary = RaceFinishSummaryBuilder.FromMode(
                finishTitleText != null ? finishTitleText.text : "TIME TRIAL COMPLETE",
                finishTitleText != null ? finishTitleText.color : new Color(0.45f, 1f, 1f),
                1,
                finishDetailText != null ? finishDetailText.text : string.Empty);
            lastFinishSummary.BestLapTime = bestLapTime;
            lastFinishSummary.RaceTime = raceTime;
            finishScreenView?.Prepare(lastFinishSummary);

            GetComponent<FinishMenuController>()?.ConfigureTimeTrialFinish(canAdvanceNext);

            SetGameplayHudVisible(false);
            ShowFinishPanel();
        }

        static bool TryBuildStoredSectorSplit(int trackIndex, int checkpointIndex, float split, out string text)
        {
            text = string.Empty;
            if (GameRaceModeSettings.IsPractice)
            {
                var sectorPb = PracticeSectorStore.TrySaveSector(trackIndex, checkpointIndex, split);
                var bestSector = PracticeSectorStore.GetBestSector(trackIndex, checkpointIndex);
                text = $"S{checkpointIndex} {split:0.00}s{PracticeSectorStore.FormatDelta(split, bestSector)}";
                if (sectorPb)
                    text += " PB";
                return true;
            }

            if (!GameRaceModeSettings.IsTimeTrial && !GameRaceModeSettings.IsGhostDuel)
                return false;

            var pb = TimeTrialRecordStore.TrySaveSector(trackIndex, checkpointIndex, split);
            var best = TimeTrialRecordStore.GetBestSector(trackIndex, checkpointIndex);
            text = $"S{checkpointIndex} {split:0.00}s{TimeTrialRecordStore.FormatSectorDelta(split, best)}";
            if (pb)
                text += " PB";
            return true;
        }

        public void SetGameplayHudVisible(bool visible)
        {
            SetTextVisible(lapText, visible);
            SetTextVisible(lapTimerText, visible);
            SetTextVisible(sectorSplitText, visible);
            SetTextVisible(raceTimerText, visible);
            SetTextVisible(bestLapText, visible);
            SetTextVisible(positionText, visible);
            SetTextVisible(scoreText, visible);
            if (dashboardCluster != null)
                dashboardCluster.SetVisible(visible);
        }

        static void SetTextVisible(Text text, bool visible)
        {
            if (text != null)
                text.gameObject.SetActive(visible);
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

        static string FormatTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = seconds % 60f;
            return $"{minutes:00}:{secs:00.00}";
        }
    }
}
