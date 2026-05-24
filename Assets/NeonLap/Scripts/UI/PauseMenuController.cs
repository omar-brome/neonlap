using NeonLap.Core;
using NeonLap.Race;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] GameObject pausePanel;
        [SerializeField] RaceManager raceManager;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;

        NeonLap.Input.IVehicleInputProvider inputProvider;

        public void Configure(GameObject panel, RaceManager manager, Button resume, Button restart, Button quit)
        {
            pausePanel = panel;
            raceManager = manager;
            resumeButton = resume;
            restartButton = restart;
            quitButton = quit;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            WireButtons();
        }

        void Start()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);

            WireButtons();
            inputProvider = FindAnyObjectByType<NeonLap.Input.PlayerInputReader>();
        }

        void WireButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
                resumeButton.onClick.AddListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
                restartButton.onClick.AddListener(Restart);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitToMenu);
                quitButton.onClick.AddListener(QuitToMenu);
            }
        }

        void Update()
        {
            if (inputProvider != null && inputProvider.PausePressed)
                TogglePause();
        }

        void TogglePause()
        {
            var paused = pausePanel != null && !pausePanel.activeSelf;
            if (pausePanel != null)
                pausePanel.SetActive(paused);

            if (GameManager.Instance != null)
                GameManager.Instance.SetPaused(paused);
            else
                Time.timeScale = paused ? 0f : 1f;
        }

        void Resume()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.SetPaused(false);
            else
                Time.timeScale = 1f;
        }

        void Restart()
        {
            Resume();
            if (raceManager != null)
                raceManager.RestartRace();
        }

        void QuitToMenu()
        {
            Resume();
            if (GameManager.Instance != null)
                GameManager.Instance.LoadMainMenu();
        }
    }
}
