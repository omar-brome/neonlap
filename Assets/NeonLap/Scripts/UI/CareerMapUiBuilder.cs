using NeonLap.Race;
using NeonLap.Track;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public static class CareerMapUiBuilder
    {
        static readonly Vector2[] NodePositions =
        {
            new(-480f, -30f),
            new(-320f, 35f),
            new(-160f, -25f),
            new(0f, 40f),
            new(160f, -15f),
            new(320f, 30f),
            new(480f, -5f),
        };

        static readonly Color[] NodeAccents =
        {
            new(0.2f, 1f, 1f),
            new(0.55f, 0.75f, 1f),
            new(1f, 0.72f, 0.35f),
            new(1f, 0.45f, 0.55f),
            new(0.65f, 1f, 0.45f),
            new(0.85f, 0.55f, 1f),
            new(1f, 0.55f, 0.85f),
        };

        public struct CareerMapBuildResult
        {
            public RectTransform MapRoot;
            public Button[] LevelButtons;
        }

        public static CareerMapBuildResult Build(
            Transform panel,
            System.Func<Transform, int, float, Color, Button> createButton)
        {
            var mapRoot = new GameObject("CareerMap").transform;
            mapRoot.SetParent(panel, false);
            var mapRect = mapRoot.gameObject.AddComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.sizeDelta = new Vector2(1200f, 220f);
            mapRect.anchoredPosition = new Vector2(0f, -95f);

            BuildConnectorLines(mapRoot);

            var nodeCount = Mathf.Min(TrackLayoutUtility.LevelCount, NodePositions.Length);
            var buttons = new Button[nodeCount];
            for (var i = 0; i < nodeCount; i++)
            {
                buttons[i] = createButton(mapRoot, i, NodePositions[i].y, NodeAccents[i]);
                var rect = buttons[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = NodePositions[i];
                rect.sizeDelta = new Vector2(300f, 52f);

                var nodeGlow = new GameObject($"NodeGlow_{i + 1}");
                nodeGlow.transform.SetParent(mapRoot, false);
                nodeGlow.transform.SetAsFirstSibling();
                var glowRect = nodeGlow.AddComponent<RectTransform>();
                glowRect.anchorMin = rect.anchorMin;
                glowRect.anchorMax = rect.anchorMax;
                glowRect.pivot = rect.pivot;
                glowRect.anchoredPosition = NodePositions[i];
                glowRect.sizeDelta = new Vector2(308f, 60f);
                var glowImage = nodeGlow.AddComponent<Image>();
                glowImage.color = new Color(NodeAccents[i].r, NodeAccents[i].g, NodeAccents[i].b, 0.18f);
                glowImage.raycastTarget = false;

                if (!CareerScoreStore.IsTrackUnlocked(i))
                {
                    var lockTint = buttons[i].GetComponent<Image>();
                    if (lockTint != null)
                        lockTint.color = new Color(0.12f, 0.1f, 0.16f, 0.95f);

                    var lockIcon = new GameObject($"LockIcon_{i + 1}");
                    lockIcon.transform.SetParent(buttons[i].transform, false);
                    var lockRect = lockIcon.AddComponent<RectTransform>();
                    lockRect.anchorMin = new Vector2(1f, 0.5f);
                    lockRect.anchorMax = new Vector2(1f, 0.5f);
                    lockRect.pivot = new Vector2(1f, 0.5f);
                    lockRect.anchoredPosition = new Vector2(-10f, 0f);
                    lockRect.sizeDelta = new Vector2(28f, 28f);
                    var lockText = lockIcon.AddComponent<Text>();
                    lockText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    lockText.fontSize = 22;
                    lockText.fontStyle = FontStyle.Bold;
                    lockText.alignment = TextAnchor.MiddleCenter;
                    lockText.color = new Color(0.55f, 0.58f, 0.65f, 0.95f);
                    lockText.text = "🔒";
                    lockText.raycastTarget = false;
                }
            }

            return new CareerMapBuildResult
            {
                MapRoot = mapRect,
                LevelButtons = buttons,
            };
        }

        static void BuildConnectorLines(Transform mapRoot)
        {
            var linkCount = Mathf.Min(TrackLayoutUtility.LevelCount, NodePositions.Length);
            for (var i = 0; i < linkCount - 1; i++)
            {
                var start = NodePositions[i];
                var end = NodePositions[i + 1];
                var mid = (start + end) * 0.5f;
                var delta = end - start;
                var length = delta.magnitude;
                var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

                var line = new GameObject($"MapLink_{i + 1}");
                line.transform.SetParent(mapRoot, false);
                line.transform.SetAsFirstSibling();
                var rect = line.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(length, 4f);
                rect.anchoredPosition = mid;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);

                var unlocked = CareerScoreStore.IsTrackUnlocked(i + 1);
                var image = line.AddComponent<Image>();
                image.color = unlocked
                    ? new Color(0.35f, 0.95f, 1f, 0.55f)
                    : new Color(0.35f, 0.4f, 0.5f, 0.35f);
                image.raycastTarget = false;
            }
        }
    }
}
