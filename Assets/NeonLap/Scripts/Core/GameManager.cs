using NeonLap.Services;
using NeonLap.Services.Platform;
using NeonLap.Track;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonLap.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] string raceSceneName = "SampleScene";
        [SerializeField] string mainMenuSceneName = "MainMenu";
        [SerializeField] TrackDefinition fallbackTrack;

        const string CurrentLevelPrefsKey = "NeonLap_CurrentLevelIndex";

        TrackDefinition[] levelTracks;
        TrackRegistry trackRegistry;
        int currentLevelIndex;

        public bool IsPaused { get; private set; }
        public int CurrentLevelIndex => currentLevelIndex;
        public int TotalLevels => EnsureLevelTracks().Length;
        public bool HasNextLevel => currentLevelIndex < TotalLevels - 1;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentLevelIndex = PlayerPrefs.GetInt(CurrentLevelPrefsKey, 0);
            GameQualitySettings.Load();
            GameDifficultySettings.Load();
            GameTrackOptions.Load();
            TimeTrialSettings.Load();
            GameLapSettings.Load();
            GamePoliceSettings.Load();
            GameHapticsSettings.Load();
            GameMinimapSettings.Load();
            GameRaceModeSettings.Load();
            GameTeamRaceSettings.Load();
            GameAudioSettings.Load();
            trackRegistry = TrackCatalog.LoadRegistry();
            NeonLapServicesBootstrap.EnsureInitialized();

            if (GetComponent<NeonLapCloudSaveAutoBackup>() == null)
                gameObject.AddComponent<NeonLapCloudSaveAutoBackup>();
        }

        public void SetFallbackTrack(TrackDefinition track)
        {
            if (track != null)
                fallbackTrack = track;

            levelTracks = null;
        }

        public TrackDefinition GetCurrentTrackDefinition()
        {
            var tracks = EnsureLevelTracks();
            var index = Mathf.Clamp(currentLevelIndex, 0, tracks.Length - 1);
            var track = tracks[index];
            ApplyLayoutForLevel(track, index);
            return track;
        }

        public TrackDefinition GetTrackDefinition(int levelIndex)
        {
            var tracks = EnsureLevelTracks();
            if (levelIndex < 0 || levelIndex >= tracks.Length)
                return null;

            return tracks[levelIndex];
        }

        public void StartNewCareer()
        {
            StartLevel(0);
        }

        public void StartLevel(int levelIndex)
        {
            StartRace(levelIndex, RaceMode.Career);
        }

        public void StartTimeTrial(int levelIndex)
        {
            StartRace(levelIndex, RaceMode.TimeTrial);
        }

        public void StartRace(int levelIndex, RaceMode mode)
        {
            GameRaceModeSettings.SetMode(mode);
            StartLevelInternal(levelIndex);
        }

        void StartLevelInternal(int levelIndex)
        {
            levelTracks = null;
            var tracks = EnsureLevelTracks();
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, tracks.Length - 1);
            PlayerPrefs.SetInt(CurrentLevelPrefsKey, currentLevelIndex);
            PlayerPrefs.Save();
            LoadRace();
        }

        public void LoadRace()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(raceSceneName);
        }

        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(raceSceneName);
        }

        public void LoadNextLevel()
        {
            if (!HasNextLevel)
                return;

            currentLevelIndex++;
            LoadRace();
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        TrackDefinition[] EnsureLevelTracks()
        {
            if (levelTracks != null && levelTracks.Length > 0)
                return levelTracks;

            if (trackRegistry == null)
                trackRegistry = TrackCatalog.LoadRegistry();

            if (trackRegistry != null && trackRegistry.Count > 0)
            {
                levelTracks = trackRegistry.tracks;
                for (var i = 0; i < levelTracks.Length; i++)
                {
                    if (fallbackTrack != null && levelTracks[i] != null)
                        levelTracks[i].sceneName = fallbackTrack.sceneName;

                    ApplyLayoutForLevel(levelTracks[i], i);
                }

                return levelTracks;
            }

            var level1 = CreateTrackDefinition("Neon Circuit", TrackLayout.Level1NeonCircuit, 1, 88f, 22f, 26f, 10);
            var level2 = CreateTrackDefinition("Turbo Sprint", TrackLayout.Level2TurboSprint, 1, 102f, 20f, 27f, 12);
            var level3 = CreateTrackDefinition("Metro Gauntlet", TrackLayout.Level3MetroGauntlet, 1, 96f, 21f, 27f, 12);
            var level4 = CreateTrackDefinition("Zigzag Thunder", TrackLayout.Level4ZigZagThunder, 1, 110f, 18f, 26f, 12);
            var level5 = CreateTrackDefinition("Square Circuit", TrackLayout.Level5SquareCircuit, 1, 100f, 18f, 27f, 12);
            var level6 = CreateTrackDefinition("Ridge Run", TrackLayout.Level6RidgeRun, 1, 92f, 22f, 27f, 12);
            var level7 = CreateTrackDefinition("Neon Crossover", TrackLayout.Level7NeonCrossover, 1, 98f, 20f, 27f, 12);

            if (fallbackTrack != null)
            {
                level1.sceneName = fallbackTrack.sceneName;
                level2.sceneName = fallbackTrack.sceneName;
                level3.sceneName = fallbackTrack.sceneName;
                level4.sceneName = fallbackTrack.sceneName;
                level5.sceneName = fallbackTrack.sceneName;
                level6.sceneName = fallbackTrack.sceneName;
                level7.sceneName = fallbackTrack.sceneName;
            }

            level3.hasShortcuts = true;
            level7.hasShortcuts = true;

            levelTracks = new[] { level1, level2, level3, level4, level5, level6, level7 };
            for (var i = 0; i < levelTracks.Length; i++)
                ApplyLayoutForLevel(levelTracks[i], i);

            return levelTracks;
        }

        static void ApplyLayoutForLevel(TrackDefinition track, int levelIndex)
        {
            if (track == null)
                return;

            track.layout = TrackLayoutUtility.LayoutForLevelIndex(levelIndex);
        }

        static TrackDefinition CreateTrackDefinition(string trackName, TrackLayout layout, int laps,
            float straightLength, float turnRadius, float trackWidth, int checkpoints)
        {
            var track = ScriptableObject.CreateInstance<TrackDefinition>();
            track.trackName = trackName;
            track.sceneName = "SampleScene";
            track.layout = layout;
            track.lapCount = laps;
            track.checkpointCount = checkpoints;
            track.straightLength = straightLength;
            track.turnRadius = turnRadius;
            track.trackWidth = trackWidth;
            return track;
        }
    }
}
