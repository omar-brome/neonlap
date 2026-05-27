using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Input;
using NeonLap.Race;
using NeonLap.UI;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeonLap.Camera
{
    public class PhotoModeController : MonoBehaviour
    {
        [SerializeField] FollowCamera followCamera;
        [SerializeField] RaceManager raceManager;
        [SerializeField] Transform photoTarget;
        [SerializeField] GameObject hintPanel;
        [SerializeField] Text hintText;

        readonly List<GameObject> hiddenRoots = new();
        GameObject canvasRoot;

        float orbitYaw;
        float orbitPitch = 12f;
        float orbitDistance = 12f;
        float savedTimeScale = 1f;
        bool active;
        bool followWasEnabled;

        public bool IsActive => active;

        public void Configure(
            FollowCamera camera,
            RaceManager manager,
            GameObject playerCar,
            Text hintLabel,
            GameObject hintPanelObject,
            GameObject canvas)
        {
            followCamera = camera;
            raceManager = manager;
            photoTarget = playerCar != null ? playerCar.transform : null;
            hintText = hintLabel;
            hintPanel = hintPanelObject;
            canvasRoot = canvas;

            hiddenRoots.Clear();

            if (hintPanel != null)
                hintPanel.SetActive(false);
        }

        void Update()
        {
            if (!CanTogglePhotoMode())
                return;

            if (WasPhotoTogglePressed())
            {
                if (active)
                    ExitPhotoMode();
                else
                    EnterPhotoMode();
            }

            if (!active)
                return;

            if (WasExitPressed())
            {
                ExitPhotoMode();
                return;
            }

            UpdateOrbitInput();
            ApplyOrbitCamera();

            if (followCamera != null && WasCameraCyclePressed())
                followCamera.CycleMode();
        }

        bool CanTogglePhotoMode()
        {
            if (raceManager == null)
                return false;

            if (raceManager.State == RaceState.Finished)
                return true;

            var podium = FindAnyObjectByType<RacePodiumSequence>();
            return podium != null && podium.IsAwaitingDismiss;
        }

        public void EnterPhotoMode()
        {
            if (active || photoTarget == null)
                return;

            active = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (GameManager.Instance != null)
                GameManager.Instance.SetPaused(true);

            HideRaceUi();

            var raceUi = FindAnyObjectByType<RaceUI>();
            raceUi?.SetGameplayHudVisible(false);

            if (followCamera != null)
            {
                followWasEnabled = followCamera.enabled;
                followCamera.enabled = false;
                var offset = followCamera.transform.position - photoTarget.position;
                orbitDistance = Mathf.Clamp(offset.magnitude, 4f, 24f);
                orbitYaw = photoTarget.eulerAngles.y;
            }

            var vehicle = photoTarget.GetComponent<VehicleController>();
            if (vehicle != null)
                vehicle.enabled = false;

            if (hintPanel != null)
                hintPanel.SetActive(true);

            if (hintText != null)
            {
                hintText.text =
                    "PHOTO MODE  •  Mouse / Arrows orbit  •  Scroll zoom  •  C camera  •  P exit";
            }
        }

        public void ExitPhotoMode()
        {
            if (!active)
                return;

            active = false;
            Time.timeScale = savedTimeScale > 0.01f ? savedTimeScale : 1f;

            if (GameManager.Instance != null)
                GameManager.Instance.SetPaused(false);

            ShowRaceUi();

            if (followCamera != null)
                followCamera.enabled = followWasEnabled;

            var vehicle = photoTarget != null ? photoTarget.GetComponent<VehicleController>() : null;
            if (vehicle != null)
                vehicle.enabled = true;

            if (hintPanel != null)
                hintPanel.SetActive(false);
        }

        void UpdateOrbitInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                    orbitYaw -= 45f * Time.unscaledDeltaTime;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                    orbitYaw += 45f * Time.unscaledDeltaTime;
                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
                    orbitPitch = Mathf.Min(orbitPitch + 30f * Time.unscaledDeltaTime, 55f);
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
                    orbitPitch = Mathf.Max(orbitPitch - 30f * Time.unscaledDeltaTime, -15f);
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                var delta = mouse.delta.ReadValue();
                orbitYaw += delta.x * 0.12f;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.08f, -15f, 55f);
                var scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    orbitDistance = Mathf.Clamp(orbitDistance - scroll * 0.02f, 4f, 26f);
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var stick = gamepad.rightStick.ReadValue();
                orbitYaw += stick.x * 70f * Time.unscaledDeltaTime;
                orbitPitch = Mathf.Clamp(orbitPitch - stick.y * 40f * Time.unscaledDeltaTime, -15f, 55f);
            }
        }

        void ApplyOrbitCamera()
        {
            if (followCamera == null || photoTarget == null)
                return;

            var cam = followCamera.GetComponent<UnityEngine.Camera>();
            if (cam == null)
                return;

            var rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            var position = photoTarget.position + rotation * Vector3.back * orbitDistance + Vector3.up * 1.2f;
            cam.transform.position = position;
            cam.transform.rotation = Quaternion.LookRotation(photoTarget.position + Vector3.up * 0.8f - position, Vector3.up);
        }

        static bool WasPhotoTogglePressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
                return true;

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.selectButton.wasPressedThisFrame;
        }

        static bool WasExitPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null
                   && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
        }

        static bool WasCameraCyclePressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
                return true;

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;
        }

        void HideRaceUi()
        {
            hiddenRoots.Clear();
            if (canvasRoot == null)
                return;

            for (var i = 0; i < canvasRoot.transform.childCount; i++)
            {
                var child = canvasRoot.transform.GetChild(i).gameObject;
                if (hintPanel != null && child == hintPanel)
                    continue;

                if (child.name == "PodiumJumpHint")
                    continue;

                if (child.activeSelf)
                {
                    hiddenRoots.Add(child);
                    child.SetActive(false);
                }
            }

            var pauseUi = GameObject.Find("PauseMenuUI");
            if (pauseUi != null && pauseUi.activeSelf)
            {
                hiddenRoots.Add(pauseUi);
                pauseUi.SetActive(false);
            }
        }

        void ShowRaceUi()
        {
            foreach (var root in hiddenRoots)
            {
                if (root != null)
                    root.SetActive(true);
            }

            hiddenRoots.Clear();
        }
    }
}
