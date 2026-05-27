using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.Race
{
    public class GhostHudController : MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        [SerializeField] Transform playerTransform;
        [SerializeField] Text ghostDeltaText;
        [SerializeField] Button ghostToggleButton;
        [SerializeField] Text ghostToggleLabel;

        readonly List<GhostRacer> ghosts = new();
        GhostRacer primaryGhost;
        public GhostRacer PrimaryGhost => primaryGhost;
        Color aheadColor = new(0.35f, 1f, 0.65f);
        Color behindColor = new(1f, 0.45f, 0.55f);
        Color evenColor = new(0.55f, 0.95f, 1f);

        public static GhostHudController Setup(
            RaceManager manager,
            Transform player,
            Text deltaText,
            Button toggleButton,
            Text toggleLabel)
        {
            if (!GameRaceModeSettings.Rules.UseTimeTrialGhost && !GameRaceModeSettings.Rules.UseGhostDuel)
                return null;

            var go = new GameObject("GhostHud");
            go.transform.SetParent(manager.transform, false);
            var hud = go.AddComponent<GhostHudController>();
            hud.Configure(manager, player, deltaText, toggleButton, toggleLabel);
            return hud;
        }

        public void Configure(
            RaceManager manager,
            Transform player,
            Text deltaText,
            Button toggleButton,
            Text toggleLabel)
        {
            raceManager = manager;
            playerTransform = player;
            ghostDeltaText = deltaText;
            ghostToggleButton = toggleButton;
            ghostToggleLabel = toggleLabel;

            TimeTrialSettings.Load();
            RefreshToggleLabel();

            if (ghostToggleButton != null)
            {
                ghostToggleButton.onClick.RemoveListener(ToggleGhostVisible);
                ghostToggleButton.onClick.AddListener(ToggleGhostVisible);
            }

            if (ghostDeltaText != null)
                ghostDeltaText.gameObject.SetActive(true);
            if (ghostToggleButton != null)
                ghostToggleButton.gameObject.SetActive(true);
        }

        public void RegisterGhost(GhostRacer ghost, bool primary = false)
        {
            if (ghost == null || ghosts.Contains(ghost))
                return;

            ghosts.Add(ghost);
            if (primary || primaryGhost == null)
                primaryGhost = ghost;

            ghost.SetVisible(TimeTrialSettings.GhostVisible);
        }

        void Update()
        {
            if (raceManager == null || raceManager.State != RaceState.Racing)
            {
                if (ghostDeltaText != null)
                    ghostDeltaText.text = string.Empty;
                return;
            }

            UpdateDeltaHud();
        }

        void UpdateDeltaHud()
        {
            if (ghostDeltaText == null || primaryGhost == null || playerTransform == null)
                return;

            if (!primaryGhost.IsVisible || !primaryGhost.HasGhost)
            {
                ghostDeltaText.text = "GHOST OFF";
                ghostDeltaText.color = new Color(0.75f, 0.85f, 1f, 0.85f);
                return;
            }

            if (!primaryGhost.TryGetDeltaSeconds(playerTransform.position, out var delta))
            {
                ghostDeltaText.text = string.Empty;
                return;
            }

            var label = primaryGhost.IsDevGhost ? "DEV" : "PB";
            ghostDeltaText.text = $"{label} {GhostPlaybackDelta.FormatDelta(delta)}";
            ghostDeltaText.color = delta < -0.01f
                ? aheadColor
                : delta > 0.01f
                    ? behindColor
                    : evenColor;
        }

        public void ToggleGhostVisible()
        {
            TimeTrialSettings.ToggleGhostVisible();
            var visible = TimeTrialSettings.GhostVisible;

            foreach (var ghost in ghosts)
            {
                if (ghost != null)
                    ghost.SetVisible(visible);
            }

            RefreshToggleLabel();
        }

        void RefreshToggleLabel()
        {
            if (ghostToggleLabel != null)
                ghostToggleLabel.text = TimeTrialSettings.GhostVisible ? "GHOST ON" : "GHOST OFF";
        }
    }
}
