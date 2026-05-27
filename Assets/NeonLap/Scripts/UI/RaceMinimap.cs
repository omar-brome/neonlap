using System.Collections;
using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class RaceMinimap : MonoBehaviour
    {
        const float MapPadding = 8f;
        const float TrackLineThickness = 4f;
        const float ShortcutLineThickness = 3f;
        const float StartLineThickness = 5f;
        const float RotateViewRadius = 72f;
        const float DashLength = 5f;
        const float DashGap = 4f;

        static readonly Color PanelColor = new(0.03f, 0.05f, 0.1f, 0.94f);
        static readonly Color TrackColor = new(0.2f, 0.82f, 0.95f, 1f);
        static readonly Color ShortcutColor = new(0.2f, 1f, 0.95f, 0.92f);
        static readonly Color StartLineColor = new(1f, 0.92f, 0.35f, 1f);
        static readonly Color PlayerBlipColor = new(0.45f, 1f, 1f, 1f);
        static readonly Color GhostBlipColor = new(0.55f, 0.95f, 1f, 0.42f);
        static readonly Color PoliceBlipColor = new(0.45f, 0.65f, 1f, 1f);
        static readonly Color[] RivalBlipColors =
        {
            new(1f, 0.45f, 0.45f),
            new(1f, 0.72f, 0.35f),
            new(0.95f, 0.92f, 0.35f),
            new(0.45f, 1f, 0.55f),
            new(0.55f, 0.72f, 1f),
            new(0.82f, 0.5f, 1f),
            new(1f, 0.55f, 0.85f),
            new(1f, 0.85f, 0.45f),
            new(0.82f, 0.82f, 0.88f),
        };

        static readonly Color BarrelIconColor = new(1f, 0.55f, 0.12f);
        static readonly Color CrateIconColor = new(0.55f, 0.32f, 0.16f);
        static readonly Color ConeIconColor = new(1f, 0.42f, 0.1f);
        static readonly Color DebrisIconColor = new(0.35f, 0.85f, 0.95f);

        [SerializeField] RaceManager raceManager;
        [SerializeField] PoliceChaseSystem policeChase;

        RectTransform mapArea;
        RectTransform blipRoot;
        RectTransform overlayRoot;
        CanvasGroup canvasGroup;
        Text titleText;
        Text modeHintText;
        Button modeToggleButton;
        Transform playerTransform;

        Vector2 boundsMin;
        Vector2 boundsMax;
        IReadOnlyList<Vector3> centerlinePoints;
        IReadOnlyList<IReadOnlyList<Vector3>> shortcutPaths;

        readonly List<Image> blips = new();
        readonly List<Text> blipNameLabels = new();
        readonly List<Image> policeBlips = new();
        readonly List<Text> policeLabels = new();
        readonly List<Image> hazardIcons = new();
        Image ghostBlip;
        GhostHudController ghostHud;
        readonly List<GameObject> trackLineObjects = new();

        int rivalColorIndex;
        bool trackBuilt;
        bool layoutBuilt;
        bool pendingInit;
        bool rotateWithCar;

        static Sprite WhiteSprite => UiSpriteUtility.White;

        public void Configure(
            RaceManager manager,
            IReadOnlyList<Vector3> centerline,
            IReadOnlyList<IReadOnlyList<Vector3>> shortcuts,
            PoliceChaseSystem police)
        {
            raceManager = manager;
            centerlinePoints = centerline;
            shortcutPaths = shortcuts;
            policeChase = police;
            trackBuilt = false;
            pendingInit = true;

            GameMinimapSettings.Load();
            rotateWithCar = GameMinimapSettings.RotateWithCar;

            ClearTrackLines();
            ClearHazardIcons();
            BuildLayout();
            ComputeBounds(centerline);
            BuildStaticOverlays();
            UpdateModeLabel();
        }

        public void BindGhostHud(GhostHudController controller)
        {
            ghostHud = controller;
            EnsureGhostBlip();
        }

        void EnsureGhostBlip()
        {
            if (ghostBlip != null || blipRoot == null)
                return;

            ghostBlip = CreateIcon("GhostBlip", GhostBlipColor, new Vector2(9f, 9f));
            ghostBlip.gameObject.SetActive(false);
        }

        void Start()
        {
            StartCoroutine(InitializeWhenReady());
        }

        IEnumerator InitializeWhenReady()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            TryBuildTrackLine();
            EnsureBlips();
            pendingInit = false;
        }

        void BuildLayout()
        {
            if (layoutBuilt)
                return;

            layoutBuilt = true;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var panelImage = GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = gameObject.AddComponent<Image>();
                panelImage.color = PanelColor;
            }

            panelImage.sprite = WhiteSprite;
            panelImage.type = Image.Type.Simple;

            var border = CreateChildRect("Border", transform);
            Stretch(border, 0f);
            var borderImage = border.gameObject.AddComponent<Image>();
            borderImage.sprite = WhiteSprite;
            borderImage.type = Image.Type.Simple;
            borderImage.color = new Color(0.35f, 0.95f, 1f, 1f);
            borderImage.raycastTarget = false;
            border.SetAsFirstSibling();

            var inner = CreateChildRect("InnerPanel", transform);
            Stretch(inner, 4f);
            var innerImage = inner.gameObject.AddComponent<Image>();
            innerImage.sprite = WhiteSprite;
            innerImage.type = Image.Type.Simple;
            innerImage.color = PanelColor;
            innerImage.raycastTarget = false;

            var titleGo = CreateChildRect("MapTitle", transform);
            titleGo.anchorMin = new Vector2(0f, 1f);
            titleGo.anchorMax = new Vector2(1f, 1f);
            titleGo.pivot = new Vector2(0.5f, 1f);
            titleGo.anchoredPosition = new Vector2(0f, -4f);
            titleGo.sizeDelta = new Vector2(0f, 22f);
            titleText = titleGo.gameObject.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.85f, 1f, 1f, 1f);
            titleText.text = "TRACK MAP";
            titleText.raycastTarget = false;

            var trackRoot = CreateChildRect("TrackLines", inner);
            Stretch(trackRoot, MapPadding + 4f);
            mapArea = trackRoot;

            overlayRoot = CreateChildRect("Overlays", inner);
            Stretch(overlayRoot, MapPadding + 4f);

            blipRoot = CreateChildRect("Blips", inner);
            Stretch(blipRoot, MapPadding + 4f);

            var toggleGo = CreateChildRect("ModeToggle", transform);
            toggleGo.anchorMin = new Vector2(1f, 0f);
            toggleGo.anchorMax = new Vector2(1f, 0f);
            toggleGo.pivot = new Vector2(1f, 0f);
            toggleGo.anchoredPosition = new Vector2(-6f, 6f);
            toggleGo.sizeDelta = new Vector2(54f, 22f);
            modeToggleButton = toggleGo.gameObject.AddComponent<Button>();
            var toggleImage = toggleGo.gameObject.AddComponent<Image>();
            toggleImage.sprite = WhiteSprite;
            toggleImage.color = new Color(0.08f, 0.16f, 0.22f, 0.92f);
            modeToggleButton.targetGraphic = toggleImage;
            modeToggleButton.onClick.AddListener(ToggleMapRotationMode);

            var toggleLabelGo = CreateChildRect("ModeLabel", toggleGo);
            Stretch(toggleLabelGo, 2f);
            modeHintText = toggleLabelGo.gameObject.AddComponent<Text>();
            modeHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            modeHintText.fontSize = 11;
            modeHintText.fontStyle = FontStyle.Bold;
            modeHintText.alignment = TextAnchor.MiddleCenter;
            modeHintText.color = new Color(0.75f, 0.95f, 1f, 1f);
            modeHintText.raycastTarget = false;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        void BuildStaticOverlays()
        {
            ClearHazardIcons();
            var hazards = MinimapHazardRegistry.Markers;
            for (var i = 0; i < hazards.Count; i++)
            {
                var marker = hazards[i];
                var icon = CreateIcon("Hazard_" + i, GetHazardColor(marker.Kind), new Vector2(7f, 7f));
                hazardIcons.Add(icon);
            }
        }

        static Color GetHazardColor(MinimapHazardKind kind)
        {
            return kind switch
            {
                MinimapHazardKind.Barrel => BarrelIconColor,
                MinimapHazardKind.Crate => CrateIconColor,
                MinimapHazardKind.Cone => ConeIconColor,
                _ => DebrisIconColor,
            };
        }

        void ComputeBounds(IReadOnlyList<Vector3> centerline)
        {
            if (centerline == null || centerline.Count == 0)
            {
                boundsMin = new Vector2(-50f, -30f);
                boundsMax = new Vector2(50f, 30f);
                return;
            }

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            void IncludePoint(Vector3 point)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            foreach (var point in centerline)
                IncludePoint(point);

            if (shortcutPaths != null)
            {
                foreach (var path in shortcutPaths)
                {
                    if (path == null)
                        continue;

                    foreach (var point in path)
                        IncludePoint(point);
                }
            }

            var width = maxX - minX;
            var depth = maxZ - minZ;
            var size = Mathf.Max(width, depth) * 1.12f;
            var centerX = (minX + maxX) * 0.5f;
            var centerZ = (minZ + maxZ) * 0.5f;

            boundsMin = new Vector2(centerX - size * 0.5f, centerZ - size * 0.5f);
            boundsMax = new Vector2(centerX + size * 0.5f, centerZ + size * 0.5f);
        }

        void BuildTrackLine(IReadOnlyList<Vector3> centerline)
        {
            if (mapArea == null || centerline == null || centerline.Count < 2)
                return;

            if (mapArea.rect.width < 1f || mapArea.rect.height < 1f)
                return;

            for (var i = 0; i < centerline.Count; i++)
            {
                var a = WorldToMapLocal(centerline[i], null);
                var b = WorldToMapLocal(centerline[(i + 1) % centerline.Count], null);
                trackLineObjects.Add(CreateLineSegment(mapArea, a, b, TrackLineThickness, TrackColor));
            }

            if (centerline.Count > 1)
            {
                var start = WorldToMapLocal(centerline[0], null);
                var next = WorldToMapLocal(centerline[1], null);
                trackLineObjects.Add(CreateLineSegment(mapArea, start, next, StartLineThickness, StartLineColor));
            }

            BuildShortcutLines();
        }

        void BuildShortcutLines()
        {
            if (shortcutPaths == null)
                return;

            for (var p = 0; p < shortcutPaths.Count; p++)
            {
                var path = shortcutPaths[p];
                if (path == null || path.Count < 2)
                    continue;

                for (var i = 0; i < path.Count - 1; i++)
                    BuildDashedMapSegment(path[i], path[i + 1]);
            }
        }

        void BuildDashedMapSegment(Vector3 worldA, Vector3 worldB)
        {
            var a = WorldToMapLocal(worldA, null);
            var b = WorldToMapLocal(worldB, null);
            var delta = b - a;
            var length = delta.magnitude;
            if (length < 0.01f)
                return;

            var direction = delta / length;
            var cursor = 0f;
            while (cursor < length)
            {
                var dashEnd = Mathf.Min(cursor + DashLength, length);
                var segA = a + direction * cursor;
                var segB = a + direction * dashEnd;
                trackLineObjects.Add(CreateLineSegment(mapArea, segA, segB, ShortcutLineThickness, ShortcutColor));
                cursor += DashLength + DashGap;
            }
        }

        void TryBuildTrackLine()
        {
            if (trackBuilt || centerlinePoints == null)
                return;

            if (mapArea == null || mapArea.rect.width < 1f || mapArea.rect.height < 1f)
                return;

            if (rotateWithCar)
            {
                trackBuilt = centerlinePoints.Count >= 2;
                return;
            }

            ClearTrackLines();
            BuildTrackLine(centerlinePoints);
            trackBuilt = centerlinePoints.Count >= 2 && trackLineObjects.Count > 0;
        }

        void ClearTrackLines()
        {
            for (var i = 0; i < trackLineObjects.Count; i++)
            {
                if (trackLineObjects[i] != null)
                    Destroy(trackLineObjects[i]);
            }

            trackLineObjects.Clear();
            trackBuilt = false;
        }

        void ClearHazardIcons()
        {
            for (var i = 0; i < hazardIcons.Count; i++)
            {
                if (hazardIcons[i] != null)
                    Destroy(hazardIcons[i].gameObject);
            }

            hazardIcons.Clear();
        }

        void EnsureBlips()
        {
            if (raceManager == null)
                return;

            var racerCount = raceManager.Racers.Count;
            while (blips.Count < racerCount)
            {
                blips.Add(CreateBlip());
                blipNameLabels.Add(CreateBlipNameLabel(blips[blips.Count - 1].rectTransform));
            }

            rivalColorIndex = 0;
            for (var i = 0; i < blips.Count; i++)
            {
                var visible = i < racerCount;
                blips[i].gameObject.SetActive(visible);
                if (i < blipNameLabels.Count)
                    blipNameLabels[i].gameObject.SetActive(visible);

                if (!visible)
                    continue;

                var racer = raceManager.Racers[i];
                Color color;
                string blipName = string.Empty;
                if (racer.IsPlayer)
                {
                    color = PlayerBlipColor;
                    blipName = "YOU";
                    playerTransform = racer.transform;
                }
                else
                {
                    var identity = racer.GetComponent<RivalIdentity>();
                    if (identity != null)
                    {
                        color = RivalRoster.GetBlipColor(identity.RivalIndex);
                        blipName = identity.ShortName;
                        if (GameRaceModeSettings.IsTeamRace)
                        {
                            var teamMarker = racer.GetComponent<RacerTeamMarker>();
                            if (teamMarker != null && teamMarker.Team != RaceTeam.None)
                                blipName += teamMarker.Team == RaceTeam.Blue ? "·B" : "·R";
                        }
                    }
                    else
                    {
                        color = RivalBlipColors[rivalColorIndex % RivalBlipColors.Length];
                        rivalColorIndex++;
                    }
                }

                blips[i].color = color;
                if (i < blipNameLabels.Count)
                {
                    blipNameLabels[i].text = blipName;
                    blipNameLabels[i].color = color;
                }
            }
        }

        void LateUpdate()
        {
            if (raceManager == null || mapArea == null || blipRoot == null || canvasGroup == null)
                return;

            HandleToggleInput();

            if (pendingInit || (!trackBuilt && !rotateWithCar))
            {
                Canvas.ForceUpdateCanvases();
                TryBuildTrackLine();
                EnsureBlips();
            }

            var visible = raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;

            if (!visible)
                return;

            if (raceManager.Racers.Count != blips.Count)
                EnsureBlips();

            if (rotateWithCar)
                RefreshRotatingMap();
            else
                RefreshNorthUpOverlays();

            RefreshBlips();
            RefreshGhostBlip();
            RefreshPoliceBlips();
        }

        void HandleToggleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.mKey.wasPressedThisFrame)
                return;

            if (raceManager.State != RaceState.Racing && raceManager.State != RaceState.Finished)
                return;

            ToggleMapRotationMode();
        }

        void ToggleMapRotationMode()
        {
            rotateWithCar = !rotateWithCar;
            GameMinimapSettings.SetRotateWithCar(rotateWithCar);
            UpdateModeLabel();
            ClearTrackLines();
            TryBuildTrackLine();
        }

        void UpdateModeLabel()
        {
            if (titleText != null)
                titleText.text = rotateWithCar ? "MAP • HEADING UP" : "TRACK MAP";

            if (modeHintText != null)
                modeHintText.text = rotateWithCar ? "N-UP" : "HEAD";
        }

        void RefreshNorthUpOverlays()
        {
            var hazards = MinimapHazardRegistry.Markers;
            for (var i = 0; i < hazardIcons.Count && i < hazards.Count; i++)
            {
                var icon = hazardIcons[i];
                icon.gameObject.SetActive(true);
                icon.rectTransform.anchoredPosition = WorldToMapLocal(hazards[i].WorldPosition, null);
            }
        }

        void RefreshRotatingMap()
        {
            ClearTrackLines();
            if (centerlinePoints == null || playerTransform == null)
                return;

            for (var i = 0; i < centerlinePoints.Count; i++)
            {
                var a = WorldToMapLocal(centerlinePoints[i], playerTransform);
                var b = WorldToMapLocal(centerlinePoints[(i + 1) % centerlinePoints.Count], playerTransform);
                if (!IsInsideMap(a) && !IsInsideMap(b))
                    continue;

                trackLineObjects.Add(CreateLineSegment(mapArea, a, b, TrackLineThickness, TrackColor));
            }

            if (shortcutPaths != null)
            {
                foreach (var path in shortcutPaths)
                {
                    if (path == null || path.Count < 2)
                        continue;

                    for (var i = 0; i < path.Count - 1; i++)
                    {
                        var a = WorldToMapLocal(path[i], playerTransform);
                        var b = WorldToMapLocal(path[i + 1], playerTransform);
                        if (!IsInsideMap(a) && !IsInsideMap(b))
                            continue;

                        BuildDashedMapSegmentRotating(path[i], path[i + 1]);
                    }
                }
            }

            var hazards = MinimapHazardRegistry.Markers;
            for (var i = 0; i < hazardIcons.Count && i < hazards.Count; i++)
            {
                var mapPos = WorldToMapLocal(hazards[i].WorldPosition, playerTransform);
                var icon = hazardIcons[i];
                icon.gameObject.SetActive(IsInsideMap(mapPos));
                icon.rectTransform.anchoredPosition = mapPos;
            }
        }

        void BuildDashedMapSegmentRotating(Vector3 worldA, Vector3 worldB)
        {
            var a = WorldToMapLocal(worldA, playerTransform);
            var b = WorldToMapLocal(worldB, playerTransform);
            var delta = b - a;
            var length = delta.magnitude;
            if (length < 0.01f)
                return;

            var direction = delta / length;
            var cursor = 0f;
            while (cursor < length)
            {
                var dashEnd = Mathf.Min(cursor + DashLength, length);
                var segA = a + direction * cursor;
                var segB = a + direction * dashEnd;
                trackLineObjects.Add(CreateLineSegment(mapArea, segA, segB, ShortcutLineThickness, ShortcutColor));
                cursor += DashLength + DashGap;
            }
        }

        void RefreshBlips()
        {
            var racers = raceManager.Racers;
            for (var i = 0; i < racers.Count && i < blips.Count; i++)
            {
                var racer = racers[i];
                var blip = blips[i];
                if (racer == null)
                {
                    blip.gameObject.SetActive(false);
                    continue;
                }

                blip.gameObject.SetActive(true);
                var mapPos = WorldToMapLocal(racer.transform.position, rotateWithCar ? playerTransform : null);
                if (rotateWithCar && !IsInsideMap(mapPos) && !racer.IsPlayer)
                {
                    blip.gameObject.SetActive(false);
                    continue;
                }

                var rect = blip.rectTransform;
                rect.anchoredPosition = mapPos;
                rect.sizeDelta = racer.IsPlayer ? new Vector2(12f, 12f) : new Vector2(8f, 8f);

                var color = blip.color;
                color.a = racer.IsFinished ? 0.45f : 1f;
                blip.color = color;

                if (i < blipNameLabels.Count)
                {
                    var label = blipNameLabels[i];
                    label.rectTransform.anchoredPosition = new Vector2(0f, -10f);
                    var labelColor = color;
                    labelColor.a = racer.IsFinished ? 0.45f : 0.95f;
                    label.color = labelColor;
                    label.gameObject.SetActive(true);
                }
            }
        }

        void RefreshGhostBlip()
        {
            EnsureGhostBlip();
            if (ghostBlip == null)
                return;

            var ghost = ghostHud != null ? ghostHud.PrimaryGhost : null;
            if (ghost == null || !ghost.IsVisible || !ghost.HasGhost)
            {
                ghostBlip.gameObject.SetActive(false);
                return;
            }

            var mapPos = WorldToMapLocal(ghost.transform.position, rotateWithCar ? playerTransform : null);
            if (rotateWithCar && !IsInsideMap(mapPos))
            {
                ghostBlip.gameObject.SetActive(false);
                return;
            }

            ghostBlip.gameObject.SetActive(true);
            ghostBlip.color = GhostBlipColor;
            ghostBlip.rectTransform.anchoredPosition = mapPos;
            ghostBlip.rectTransform.sizeDelta = new Vector2(9f, 9f);
            ghostBlip.transform.SetAsLastSibling();
        }

        void RefreshPoliceBlips()
        {
            if (policeChase == null || !policeChase.HasActiveUnits)
            {
                for (var i = 0; i < policeBlips.Count; i++)
                    policeBlips[i].gameObject.SetActive(false);
                for (var i = 0; i < policeLabels.Count; i++)
                    policeLabels[i].gameObject.SetActive(false);
                return;
            }

            var units = policeChase.ActiveUnits;
            while (policeBlips.Count < units.Count)
            {
                policeBlips.Add(CreateBlip());
                policeLabels.Add(CreatePoliceLabel(policeBlips[policeBlips.Count - 1].rectTransform));
            }

            for (var i = 0; i < policeBlips.Count; i++)
            {
                var blip = policeBlips[i];
                var label = i < policeLabels.Count ? policeLabels[i] : null;
                if (i >= units.Count || units[i] == null)
                {
                    blip.gameObject.SetActive(false);
                    if (label != null)
                        label.gameObject.SetActive(false);
                    continue;
                }

                var mapPos = WorldToMapLocal(units[i].transform.position,
                    rotateWithCar ? playerTransform : null);
                if (rotateWithCar && !IsInsideMap(mapPos))
                {
                    blip.gameObject.SetActive(false);
                    if (label != null)
                        label.gameObject.SetActive(false);
                    continue;
                }

                blip.gameObject.SetActive(true);
                blip.color = PoliceBlipColor;
                blip.rectTransform.anchoredPosition = mapPos;
                blip.rectTransform.sizeDelta = new Vector2(10f, 10f);

                if (label != null)
                {
                    label.gameObject.SetActive(true);
                    label.rectTransform.anchoredPosition = new Vector2(0f, -9f);
                    label.color = PoliceBlipColor;
                }
            }
        }

        static Text CreatePoliceLabel(RectTransform blipRect)
        {
            var go = new GameObject("PoliceLabel");
            go.transform.SetParent(blipRect, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(36f, 10f);

            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 8;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.UpperCenter;
            label.color = PoliceBlipColor;
            label.text = "PD";
            label.raycastTarget = false;
            return label;
        }

        Vector2 WorldToMapLocal(Vector3 world, Transform rotateOrigin)
        {
            var rect = mapArea.rect;
            if (rotateWithCar && rotateOrigin != null)
            {
                var relative = world - rotateOrigin.position;
                relative.y = 0f;
                relative = Quaternion.Euler(0f, -rotateOrigin.eulerAngles.y, 0f) * relative;
                var nx = relative.x / RotateViewRadius * 0.5f + 0.5f;
                var ny = relative.z / RotateViewRadius * 0.5f + 0.5f;
                return new Vector2(nx * rect.width, ny * rect.height);
            }

            var northX = Mathf.InverseLerp(boundsMin.x, boundsMax.x, world.x);
            var northY = Mathf.InverseLerp(boundsMin.y, boundsMax.y, world.z);
            return new Vector2(northX * rect.width, northY * rect.height);
        }

        bool IsInsideMap(Vector2 mapPos)
        {
            var rect = mapArea.rect;
            return mapPos.x >= -8f && mapPos.y >= -8f && mapPos.x <= rect.width + 8f && mapPos.y <= rect.height + 8f;
        }

        Image CreateBlip()
        {
            return CreateIcon("Blip", Color.white, new Vector2(8f, 8f));
        }

        static Text CreateBlipNameLabel(RectTransform blipRect)
        {
            var go = new GameObject("BlipName");
            go.transform.SetParent(blipRect, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(48f, 12f);

            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 9;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.UpperCenter;
            label.raycastTarget = false;
            return label;
        }

        Image CreateIcon(string name, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(blipRoot != null ? blipRoot : transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static RectTransform CreateChildRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        static GameObject CreateLineSegment(RectTransform parent, Vector2 a, Vector2 b, float thickness, Color color)
        {
            var go = new GameObject("TrackSegment");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var delta = b - a;
            var length = delta.magnitude;
            if (length < 0.01f)
                return go;

            rect.sizeDelta = new Vector2(length, thickness);
            rect.anchoredPosition = (a + b) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return go;
        }
    }
}
