using System.Collections;
using System.Collections.Generic;
using NeonLap.Race;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class RaceMinimap : MonoBehaviour
    {
        const float MapPadding = 8f;
        const float TrackLineThickness = 4f;
        const float StartLineThickness = 5f;

        static readonly Color PanelColor = new(0.03f, 0.05f, 0.1f, 0.94f);
        static readonly Color TrackColor = new(0.2f, 0.82f, 0.95f, 1f);
        static readonly Color StartLineColor = new(1f, 0.92f, 0.35f, 1f);
        static readonly Color PlayerBlipColor = new(0.45f, 1f, 1f, 1f);
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

        [SerializeField] RaceManager raceManager;

        RectTransform mapArea;
        RectTransform blipRoot;
        CanvasGroup canvasGroup;
        Vector2 boundsMin;
        Vector2 boundsMax;
        IReadOnlyList<Vector3> centerlinePoints;
        readonly List<Image> blips = new();
        readonly List<GameObject> trackLineObjects = new();
        int rivalColorIndex;
        bool trackBuilt;
        bool layoutBuilt;
        bool pendingInit;
        static Sprite WhiteSprite => UiSpriteUtility.White;

        public void Configure(RaceManager manager, IReadOnlyList<Vector3> centerline)
        {
            raceManager = manager;
            centerlinePoints = centerline;
            trackBuilt = false;
            pendingInit = true;
            ClearTrackLines();
            BuildLayout();
            ComputeBounds(centerline);
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
            var titleText = titleGo.gameObject.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.85f, 1f, 1f, 1f);
            titleText.text = "TRACK MAP";
            titleText.raycastTarget = false;
            var titleOutline = titleText.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleOutline.effectDistance = new Vector2(1f, -1f);

            var trackRoot = CreateChildRect("TrackLines", inner);
            Stretch(trackRoot, MapPadding + 4f);

            mapArea = trackRoot;
            blipRoot = CreateChildRect("Blips", inner);
            Stretch(blipRoot, MapPadding + 4f);

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
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

            foreach (var point in centerline)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
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
                var a = WorldToMapLocal(centerline[i]);
                var b = WorldToMapLocal(centerline[(i + 1) % centerline.Count]);
                trackLineObjects.Add(CreateLineSegment(mapArea, a, b, TrackLineThickness, TrackColor));
            }

            if (centerline.Count > 1)
            {
                var start = WorldToMapLocal(centerline[0]);
                var next = WorldToMapLocal(centerline[1]);
                trackLineObjects.Add(CreateLineSegment(mapArea, start, next, StartLineThickness, StartLineColor));
            }
        }

        void TryBuildTrackLine()
        {
            if (trackBuilt || centerlinePoints == null)
                return;

            if (mapArea == null || mapArea.rect.width < 1f || mapArea.rect.height < 1f)
                return;

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

        void EnsureBlips()
        {
            if (raceManager == null)
                return;

            var racerCount = raceManager.Racers.Count;
            while (blips.Count < racerCount)
                blips.Add(CreateBlip());

            rivalColorIndex = 0;
            for (var i = 0; i < blips.Count; i++)
            {
                var visible = i < racerCount;
                blips[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                var racer = raceManager.Racers[i];
                var color = racer.IsPlayer
                    ? PlayerBlipColor
                    : RivalBlipColors[rivalColorIndex++ % RivalBlipColors.Length];
                blips[i].color = color;
            }
        }

        void LateUpdate()
        {
            if (raceManager == null || mapArea == null || blipRoot == null || canvasGroup == null)
                return;

            if (pendingInit || !trackBuilt)
            {
                Canvas.ForceUpdateCanvases();
                TryBuildTrackLine();
                EnsureBlips();
            }

            var visible = raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;

            if (!visible)
                return;

            if (raceManager.Racers.Count != blips.Count)
                EnsureBlips();

            RefreshBlips();
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
                var mapPos = WorldToMapLocal(racer.transform.position);
                var rect = blip.rectTransform;
                rect.anchoredPosition = mapPos;
                rect.sizeDelta = racer.IsPlayer ? new Vector2(12f, 12f) : new Vector2(8f, 8f);

                var color = blip.color;
                color.a = racer.IsFinished ? 0.45f : 1f;
                blip.color = color;
            }
        }

        Vector2 WorldToMapLocal(Vector3 world)
        {
            var rect = mapArea.rect;
            var nx = Mathf.InverseLerp(boundsMin.x, boundsMax.x, world.x);
            var ny = Mathf.InverseLerp(boundsMin.y, boundsMax.y, world.z);
            return new Vector2(nx * rect.width, ny * rect.height);
        }

        Image CreateBlip()
        {
            var go = new GameObject("Blip");
            go.transform.SetParent(blipRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(8f, 8f);
            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.type = Image.Type.Simple;
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
