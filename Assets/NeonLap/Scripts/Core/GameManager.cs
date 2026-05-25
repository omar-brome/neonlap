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

        TrackDefinition[] levelTracks;
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
            GameQualitySettings.Load();
            GameDifficultySettings.Load();
            GameLapSettings.Load();
            GamePoliceSettings.Load();
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
            return tracks[Mathf.Clamp(currentLevelIndex, 0, tracks.Length - 1)];
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
            var tracks = EnsureLevelTracks();
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, tracks.Length - 1);
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

            var level1 = fallbackTrack != null
                ? fallbackTrack
                : CreateTrackDefinition("Oval Circuit", TrackLayout.Oval, 1, 80f, 25f, 26f, 10);

            levelTracks = new[]
            {
                level1,
                CreateTrackDefinition("Neon Speedway", TrackLayout.TriOvalSpeedway, 1, 120f, 30f, 30f, 14),
                CreateTrackDefinition("Championship Ring", TrackLayout.TechnicalRing, 1, 130f, 28f, 30f, 16),
            };

            return levelTracks;
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
