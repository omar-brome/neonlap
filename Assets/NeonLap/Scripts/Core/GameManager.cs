using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonLap.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] string raceSceneName = "SampleScene";
        [SerializeField] string mainMenuSceneName = "MainMenu";

        public bool IsPaused { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadRace()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(raceSceneName);
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
    }
}
