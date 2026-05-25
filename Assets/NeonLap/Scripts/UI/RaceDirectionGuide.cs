using System.Collections.Generic;
using NeonLap.Race;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class RaceDirectionGuide : MonoBehaviour
    {
        static readonly Color ArrowColor = new(0.35f, 1f, 1f, 0.95f);
        static readonly Color EdgeArrowColor = new(1f, 0.82f, 0.2f, 0.92f);
        static Sprite WhiteSprite => UiSpriteUtility.White;

        RaceManager raceManager;
        Transform playerTransform;
        RacerProgress playerRacer;
        IReadOnlyList<Transform> checkpoints;

        RectTransform hudRoot;
        RectTransform compassArrow;
        Text distanceLabel;
        Image edgeArrow;
        GameObject worldArrowRoot;

        public void Configure(RaceManager manager, Transform player, IReadOnlyList<Transform> checkpointTransforms,
            Transform canvasRoot)
        {
            raceManager = manager;
            playerTransform = player;
            playerRacer = player != null ? player.GetComponent<RacerProgress>() : null;
            checkpoints = checkpointTransforms;
            BuildHud(canvasRoot);
            BuildWorldArrow();
        }

        void BuildHud(Transform canvasRoot)
        {
            var rootGo = new GameObject("DirectionGuideHud");
            rootGo.transform.SetParent(canvasRoot, false);
            var rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            hudRoot = CreateChildRect("CompassCluster", rootGo.transform);
            hudRoot.anchorMin = new Vector2(0.5f, 1f);
            hudRoot.anchorMax = new Vector2(0.5f, 1f);
            hudRoot.pivot = new Vector2(0.5f, 1f);
            hudRoot.anchoredPosition = new Vector2(0f, -52f);
            hudRoot.sizeDelta = new Vector2(120f, 120f);

            CreateImage("CompassRing", hudRoot, new Vector2(0f, -8f), new Vector2(92f, 92f),
                new Color(0.04f, 0.08f, 0.14f, 0.72f));

            var ringBorder = CreateImage("CompassBorder", hudRoot, new Vector2(0f, -8f), new Vector2(98f, 98f),
                new Color(0.35f, 1f, 1f, 0.35f));
            ringBorder.transform.SetAsFirstSibling();

            compassArrow = CreateChildRect("CompassArrow", hudRoot);
            compassArrow.anchorMin = new Vector2(0.5f, 0.5f);
            compassArrow.anchorMax = new Vector2(0.5f, 0.5f);
            compassArrow.pivot = new Vector2(0.5f, 0.5f);
            compassArrow.anchoredPosition = new Vector2(0f, -8f);
            compassArrow.sizeDelta = new Vector2(34f, 52f);

            var arrowBody = CreateImage("ArrowBody", compassArrow, new Vector2(0f, -4f), new Vector2(10f, 28f), ArrowColor);
            var arrowHead = CreateImage("ArrowHead", compassArrow, new Vector2(0f, 16f), new Vector2(28f, 28f), ArrowColor);
            arrowHead.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var labelGo = CreateChildRect("GuideLabel", hudRoot);
            labelGo.anchorMin = new Vector2(0.5f, 1f);
            labelGo.anchorMax = new Vector2(0.5f, 1f);
            labelGo.pivot = new Vector2(0.5f, 1f);
            labelGo.anchoredPosition = new Vector2(0f, 2f);
            labelGo.sizeDelta = new Vector2(120f, 20f);
            var label = labelGo.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.75f, 0.95f, 1f, 0.9f);
            label.text = "ROUTE";
            label.raycastTarget = false;

            var distanceGo = CreateChildRect("DistanceLabel", hudRoot);
            distanceGo.anchorMin = new Vector2(0.5f, 0f);
            distanceGo.anchorMax = new Vector2(0.5f, 0f);
            distanceGo.pivot = new Vector2(0.5f, 1f);
            distanceGo.anchoredPosition = new Vector2(0f, -96f);
            distanceGo.sizeDelta = new Vector2(160f, 24f);
            distanceLabel = distanceGo.gameObject.AddComponent<Text>();
            distanceLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            distanceLabel.fontSize = 18;
            distanceLabel.fontStyle = FontStyle.Bold;
            distanceLabel.alignment = TextAnchor.MiddleCenter;
            distanceLabel.color = ArrowColor;
            distanceLabel.raycastTarget = false;

            edgeArrow = CreateImage("EdgeArrow", rootRect, Vector2.zero, new Vector2(34f, 34f), EdgeArrowColor);
            edgeArrow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            edgeArrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            edgeArrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            edgeArrow.enabled = false;

            hudRoot.gameObject.SetActive(false);
            edgeArrow.gameObject.SetActive(false);
        }

        void BuildWorldArrow()
        {
            worldArrowRoot = new GameObject("DirectionGuideWorld");
            var material = CreateGuideMaterial();

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "GuideShaft";
            shaft.transform.SetParent(worldArrowRoot.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            shaft.transform.localScale = new Vector3(0.35f, 0.12f, 1.1f);
            ApplyGuideMaterial(shaft, material);
            Destroy(shaft.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "GuideHead";
            head.transform.SetParent(worldArrowRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            head.transform.localScale = new Vector3(0.95f, 0.12f, 0.95f);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            ApplyGuideMaterial(head, material);
            Destroy(head.GetComponent<Collider>());

            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "GuideGlow";
            glow.transform.SetParent(worldArrowRoot.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            glow.transform.localScale = Vector3.one * 0.55f;
            ApplyGuideMaterial(glow, material, 0.35f);
            Destroy(glow.GetComponent<Collider>());

            worldArrowRoot.SetActive(false);
        }

        void LateUpdate()
        {
            if (playerTransform == null || raceManager == null)
                return;

            var visible = raceManager.State == RaceState.Racing
                            && playerRacer != null
                            && !playerRacer.IsFinished
                            && !playerRacer.IsEliminated;

            if (!visible)
            {
                SetHudVisible(false);
                if (worldArrowRoot != null)
                    worldArrowRoot.SetActive(false);
                return;
            }

            var target = GetTargetPosition();
            var toTarget = target - playerTransform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            if (distance < 0.5f)
            {
                SetHudVisible(false);
                if (worldArrowRoot != null)
                    worldArrowRoot.SetActive(false);
                return;
            }

            SetHudVisible(true);
            UpdateHudGuide(toTarget, distance);
            UpdateWorldArrow(toTarget, distance);
        }

        void UpdateHudGuide(Vector3 toTarget, float distance)
        {
            if (compassArrow != null)
            {
                var forward = playerTransform.forward;
                forward.y = 0f;
                var angle = Vector3.SignedAngle(forward.normalized, toTarget.normalized, Vector3.up);
                compassArrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }

            if (distanceLabel != null)
                distanceLabel.text = $"{Mathf.RoundToInt(distance)}m";

            UpdateEdgeArrow(toTarget);
        }

        void UpdateEdgeArrow(Vector3 toTarget)
        {
            if (edgeArrow == null)
                return;

            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                edgeArrow.enabled = false;
                return;
            }

            var target = playerTransform.position + toTarget;
            var screen = cam.WorldToScreenPoint(target);
            var canvasRect = edgeArrow.rectTransform.parent as RectTransform;
            if (canvasRect == null)
                return;

            var onScreen = screen.z > 0f
                             && screen.x > 80f && screen.x < Screen.width - 80f
                             && screen.y > 120f && screen.y < Screen.height - 120f;

            var forward = playerTransform.forward;
            forward.y = 0f;
            var targetVisible = onScreen && Vector3.Dot(forward.normalized, toTarget.normalized) > 0.25f;

            if (targetVisible)
            {
                edgeArrow.enabled = false;
                return;
            }

            edgeArrow.enabled = true;
            Vector2 edgePoint;
            if (screen.z > 0f)
            {
                edgePoint = new Vector2(screen.x, screen.y);
            }
            else
            {
                var camSpace = cam.transform.InverseTransformDirection(toTarget.normalized);
                edgePoint = ScreenCenter + new Vector2(camSpace.x, camSpace.y).normalized * 800f;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, edgePoint, null, out var localPoint);
            var clamped = ClampToEdge(localPoint, canvasRect.rect, 72f);
            edgeArrow.rectTransform.anchoredPosition = clamped;
            edgeArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, AngleFromCenter(clamped));
        }

        static Vector2 ScreenCenter => new(Screen.width * 0.5f, Screen.height * 0.5f);

        void UpdateWorldArrow(Vector3 toTarget, float distance)
        {
            if (worldArrowRoot == null)
                return;

            if (distance < 12f)
            {
                worldArrowRoot.SetActive(false);
                return;
            }

            worldArrowRoot.SetActive(true);
            var leadDistance = Mathf.Clamp(distance * 0.3f, 10f, 26f);
            var position = playerTransform.position + toTarget.normalized * leadDistance + Vector3.up * 3.2f;
            worldArrowRoot.transform.position = position;
            worldArrowRoot.transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        Vector3 GetTargetPosition()
        {
            if (checkpoints == null || checkpoints.Count == 0 || playerRacer == null)
                return playerTransform.position + playerTransform.forward * 30f;

            var index = Mathf.Clamp(playerRacer.NextCheckpointIndex, 0, checkpoints.Count - 1);
            var checkpoint = checkpoints[index];
            if (checkpoint == null)
                return playerTransform.position + playerTransform.forward * 30f;

            return checkpoint.position;
        }

        void SetHudVisible(bool visible)
        {
            if (hudRoot != null)
                hudRoot.gameObject.SetActive(visible);
            if (edgeArrow != null && !visible)
                edgeArrow.enabled = false;
        }

        static RectTransform CreateChildRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        static Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var rect = CreateChildRect(name, parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Vector2 ClampToEdge(Vector2 localPoint, Rect rect, float inset)
        {
            var half = rect.size * 0.5f;
            var maxX = half.x - inset;
            var maxY = half.y - inset;
            if (Mathf.Abs(localPoint.x) <= maxX && Mathf.Abs(localPoint.y) <= maxY)
            {
                if (Mathf.Abs(localPoint.x) > Mathf.Abs(localPoint.y))
                    localPoint.x = Mathf.Sign(localPoint.x) * maxX;
                else
                    localPoint.y = Mathf.Sign(localPoint.y) * maxY;
            }

            localPoint.x = Mathf.Clamp(localPoint.x, -maxX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, -maxY, maxY);
            return localPoint;
        }

        static float AngleFromCenter(Vector2 localPoint)
        {
            return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg - 90f;
        }

        static Material CreateGuideMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.SetColor("_BaseColor", new Color(0.2f, 1f, 1f, 0.9f));
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.35f, 3.5f, 4f));
            material.SetFloat("_Smoothness", 0.85f);
            return material;
        }

        static void ApplyGuideMaterial(GameObject go, Material template, float alphaScale = 1f)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var material = new Material(template);
            if (alphaScale < 0.99f)
            {
                var color = material.GetColor("_BaseColor");
                color.a *= alphaScale;
                material.SetColor("_BaseColor", color);
            }

            renderer.sharedMaterial = material;
        }
    }
}
