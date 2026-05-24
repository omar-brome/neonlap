using NeonLap.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button quitButton;
        [SerializeField] GameObject controlsPanel;
        [SerializeField] Button controlsBackButton;

        public void Configure(Button start, Button controls, Button quit, GameObject controlsPanelObject, Button back)
        {
            startButton = start;
            controlsButton = controls;
            quitButton = quit;
            controlsPanel = controlsPanelObject;
            controlsBackButton = back;

            if (controlsPanel != null)
                controlsPanel.SetActive(false);

            WireButtons();
        }

        void Start()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
            WireButtons();
        }

        void WireButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (controlsButton != null)
            {
                controlsButton.onClick.RemoveListener(OnControlsClicked);
                controlsButton.onClick.AddListener(OnControlsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (controlsBackButton != null)
            {
                controlsBackButton.onClick.RemoveListener(OnControlsBackClicked);
                controlsBackButton.onClick.AddListener(OnControlsBackClicked);
            }
        }

        void OnStartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.LoadRace();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }

        void OnControlsClicked()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(true);
        }

        void OnControlsBackClicked()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
        }

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
