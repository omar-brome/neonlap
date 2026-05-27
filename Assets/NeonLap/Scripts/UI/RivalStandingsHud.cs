using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class RivalStandingsHud : MonoBehaviour
    {
        const int MaxRows = 6;

        RaceManager raceManager;
        readonly List<Text> rowLabels = new();
        CanvasGroup canvasGroup;

        public static RivalStandingsHud Setup(Transform canvasRoot, RaceManager manager)
        {
            if (canvasRoot == null || manager == null)
                return null;

            var existing = canvasRoot.GetComponentInChildren<RivalStandingsHud>(true);
            if (existing != null)
            {
                existing.Configure(manager);
                return existing;
            }

            var panel = new GameObject("RivalStandingsHud");
            panel.transform.SetParent(canvasRoot, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -120f);
            rect.sizeDelta = new Vector2(200f, 168f);

            var hud = panel.AddComponent<RivalStandingsHud>();
            hud.BuildRows(panel.transform);
            hud.Configure(manager);
            return hud;
        }

        void BuildRows(Transform parent)
        {
            canvasGroup = parent.gameObject.AddComponent<CanvasGroup>();
            for (var i = 0; i < MaxRows; i++)
            {
                var row = new GameObject("RivalRow_" + i);
                row.transform.SetParent(parent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * 26f);
                rect.sizeDelta = new Vector2(0f, 22f);

                var label = row.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 13;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleLeft;
                label.raycastTarget = false;
                rowLabels.Add(label);
            }
        }

        public void Configure(RaceManager manager)
        {
            raceManager = manager;
        }

        void LateUpdate()
        {
            if (raceManager == null || canvasGroup == null)
                return;

            var visible = raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished;
            canvasGroup.alpha = visible ? 1f : 0f;

            if (!visible)
                return;

            var ranked = raceManager.GetRankedRacers();
            var row = 0;
            if (GameRaceModeSettings.IsTeamRace)
            {
                var playerTeam = GameTeamRaceSettings.PlayerTeam;
                var teamPos = raceManager.GetPlayerTeamRacePosition();
                if (row < MaxRows)
                {
                    rowLabels[row].text =
                        $"{GameTeamRaceSettings.GetDisplayName(playerTeam)}  TEAM P{teamPos}/2";
                    rowLabels[row].color = GameTeamRaceSettings.GetTeamColor(playerTeam);
                    row++;
                }
            }

            for (var i = 0; i < ranked.Count && row < MaxRows; i++)
            {
                var racer = ranked[i];
                if (racer == null || racer.IsPlayer)
                    continue;

                var identity = racer.GetComponent<RivalIdentity>();
                if (identity == null)
                    continue;

                var teamMarker = racer.GetComponent<RacerTeamMarker>();
                var teamTag = string.Empty;
                var placementLabel = $"P{raceManager.GetRacerPlacement(racer)}";
                if (GameRaceModeSettings.IsTeamRace && teamMarker != null)
                {
                    teamTag = teamMarker.Team == RaceTeam.Blue ? " [B]" : teamMarker.Team == RaceTeam.Red ? " [R]" : string.Empty;
                    placementLabel = $"T-P{raceManager.GetRacerTeamPlacement(racer)}";
                }

                var status = racer.IsEliminated ? " OUT" : racer.IsFinished ? " FIN" : string.Empty;
                rowLabels[row].text = $"{placementLabel} {identity.ShortName}{teamTag}{status}";
                rowLabels[row].color = identity.HudColor;
                row++;
            }

            for (var i = row; i < MaxRows; i++)
                rowLabels[i].text = string.Empty;
        }
    }
}
