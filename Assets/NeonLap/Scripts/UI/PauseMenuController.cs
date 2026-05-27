using NeonLap.Core;
using NeonLap.Input;
using NeonLap.Race;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject controlsPanel;
        [SerializeField] RaceManager raceManager;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button controlsBackButton;
        [SerializeField] Button quitButton;
        [SerializeField] Text statusText;

        NeonLap.Input.IVehicleInputProvider inputProvider;

        public void Configure(
            GameObject panel,
            RaceManager manager,
            Button resume,
            Button restart,
            Button controls,
            Button quit,
            GameObject controlsPanelObject = null,
            Button controlsBack = null,
            Text status = null)
        {
            pausePanel = panel;
            statusText = status;
            raceManager = manager;
            resumeButton = resume;
            restartButton = restart;
            controlsButton = controls;
            quitButton = quit;
            controlsPanel = controlsPanelObject;
            controlsBackButton = controlsBack;

            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (controlsPanel != null)
                controlsPanel.SetActive(false);

            WireButtons();
        }

        void Start()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (controlsPanel != null)
                controlsPanel.SetActive(false);

            WireButtons();
            inputProvider = FindAnyObjectByType<NeonLap.Input.CompositeVehicleInputProvider>()
                            ?? (IVehicleInputProvider)FindAnyObjectByType<NeonLap.Input.PlayerInputReader>();
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

            if (controlsButton != null)
            {
                controlsButton.onClick.RemoveListener(ShowControls);
                controlsButton.onClick.AddListener(ShowControls);
            }

            if (controlsBackButton != null)
            {
                controlsBackButton.onClick.RemoveListener(HideControls);
                controlsBackButton.onClick.AddListener(HideControls);
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
            {
                if (controlsPanel != null && controlsPanel.activeSelf)
                    HideControls();
                else
                    TogglePause();
            }
        }

        void TogglePause()
        {
            var paused = pausePanel != null && !pausePanel.activeSelf;
            if (pausePanel != null)
                pausePanel.SetActive(paused);

            if (controlsPanel != null)
                controlsPanel.SetActive(false);

            if (paused)
                RefreshStatusText();

            if (GameManager.Instance != null)
                GameManager.Instance.SetPaused(paused);
            else
                Time.timeScale = paused ? 0f : 1f;
        }

        void ShowControls()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(true);
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        void HideControls()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
            if (pausePanel != null)
                pausePanel.SetActive(true);

            RefreshStatusText();
        }

        void RefreshStatusText()
        {
            if (statusText == null)
                return;

            statusText.text = PauseMenuStatusText.Build();
        }

        void Resume()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
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
