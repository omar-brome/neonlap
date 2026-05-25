using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Race;
using NeonLap.Track;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.Environment
{
    public class RaceEnvironmentBuilder : MonoBehaviour
    {
        struct EnvMaterials
        {
            public Material Ground;
            public Material Stadium;
            public Material Building;
            public Material BuildingAccent;
            public Material Foliage;
            public Material Trunk;
            public Material Screen;
            public Material ScreenGlow;
            public Material SignBoard;
            public Material LightTower;
            public Material LampHead;
            public Material[] Crowd;
        }

        static readonly string[] MotivationalSigns =
        {
            "GO GO GO",
            "LET'S RACE",
            "YOU GOT THIS",
            "FULL SPEED",
            "NEON LAP",
            "BELIEVE",
            "FASTER",
            "WINNER",
            "PUSH IT",
            "NEVER QUIT",
            "RACE DAY",
            "CHAMPION",
        };

        Transform environmentRoot;
        StadiumScoreboard scoreboard;
        Text jumbotronTitle;
        Text jumbotronPosition;
        Text jumbotronLap;
        Text jumbotronTimer;
        Text jumbotronStatus;
        EnvMaterials materials;

        float crowdDensity = 1f;
        float environmentDensity = 1f;

        public void Build(TrackDefinition definition, Vector3 startPosition, Quaternion startRotation,
            Vector2 environmentHalfExtents, QualityPreset qualityPreset)
        {
            if (environmentRoot != null)
                Destroy(environmentRoot.gameObject);

            environmentRoot = new GameObject("EnvironmentRoot").transform;
            environmentRoot.SetParent(transform, false);

            crowdDensity = Mathf.Clamp(qualityPreset.CrowdDensity, 0.2f, 1f);
            environmentDensity = Mathf.Clamp(qualityPreset.EnvironmentDensity, 0.2f, 1f);

            var trackWidth = definition != null ? definition.trackWidth : 26f;
            var half = environmentHalfExtents.x;
            var turnRadius = environmentHalfExtents.y;

            materials = CreateMaterials();
            BuildGroundPlane(half, turnRadius);
            BuildCitySkyline(half, turnRadius);
            BuildTrees(half, turnRadius, trackWidth);
            BuildStadium(half, turnRadius, trackWidth);
            BuildJumbotron(startPosition, startRotation, trackWidth);
            BuildStadiumLightingSystem(half, turnRadius, trackWidth);
        }

        public void ConfigureScoreboard(RaceManager raceManager)
        {
            scoreboard?.Configure(raceManager, jumbotronTitle, jumbotronPosition, jumbotronLap, jumbotronTimer,
                jumbotronStatus);
        }

        EnvMaterials CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material Make(Color baseColor, Color emission, float smoothness = 0.4f, float emissionIntensity = 0f)
            {
                var mat = new Material(lit);
                mat.SetColor("_BaseColor", baseColor);
                if (emissionIntensity > 0f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emission * emissionIntensity);
                }

                mat.SetFloat("_Smoothness", smoothness);
                return mat;
            }

            var crowdColors = new[]
            {
                Make(new Color(0.95f, 0.25f, 0.35f), Color.red, 0.2f),
                Make(new Color(0.25f, 0.55f, 1f), Color.blue, 0.2f),
                Make(new Color(0.35f, 0.95f, 0.45f), Color.green, 0.2f),
                Make(new Color(1f, 0.85f, 0.25f), Color.yellow, 0.2f),
                Make(new Color(0.85f, 0.45f, 1f), Color.magenta, 0.2f),
                Make(new Color(0.95f, 0.55f, 0.2f), new Color(1f, 0.5f, 0f), 0.2f),
                Make(new Color(0.2f, 0.9f, 0.9f), Color.cyan, 0.2f),
                Make(new Color(0.92f, 0.92f, 0.95f), Color.white, 0.2f),
            };

            return new EnvMaterials
            {
                Ground = Make(new Color(0.03f, 0.05f, 0.08f), Color.black, 0.15f),
                Stadium = Make(new Color(0.08f, 0.1f, 0.16f), new Color(0.1f, 0.3f, 0.45f), 0.35f, 0.15f),
                Building = Make(new Color(0.06f, 0.07f, 0.12f), new Color(0.15f, 0.2f, 0.35f), 0.55f, 0.25f),
                BuildingAccent = Make(new Color(0.05f, 0.12f, 0.18f), new Color(0.2f, 0.8f, 1f), 0.75f, 1.8f),
                Foliage = Make(new Color(0.05f, 0.22f, 0.14f), new Color(0.1f, 0.8f, 0.35f), 0.25f, 0.6f),
                Trunk = Make(new Color(0.12f, 0.08f, 0.06f), Color.black, 0.1f),
                Screen = Make(new Color(0.02f, 0.04f, 0.08f), new Color(0.1f, 0.4f, 0.55f), 0.85f, 0.35f),
                ScreenGlow = Make(new Color(0.05f, 0.15f, 0.2f), new Color(0.3f, 1f, 1f), 0.9f, 2.5f),
                SignBoard = Make(new Color(0.95f, 0.92f, 0.2f), new Color(1f, 0.85f, 0.15f), 0.35f, 0.8f),
                LightTower = Make(new Color(0.12f, 0.14f, 0.2f), new Color(0.15f, 0.35f, 0.5f), 0.55f, 0.35f),
                LampHead = Make(new Color(0.75f, 0.88f, 1f), new Color(0.6f, 0.95f, 1f), 0.92f, 3.5f),
                Crowd = crowdColors
            };
        }

        void BuildGroundPlane(float half, float turnRadius)
        {
            var ground = CreatePrimitive(PrimitiveType.Cube, "CityGround", environmentRoot,
                Vector3.zero, new Vector3(half * 2f + 220f, 0.2f, turnRadius * 2f + 220f), materials.Ground);
            ground.transform.position = new Vector3(0f, -0.15f, 0f);
        }

        void BuildStadium(float half, float turnRadius, float trackWidth)
        {
            var stadiumRoot = CreateEmpty("Stadium", environmentRoot);
            var outerOffset = trackWidth * 0.5f + 10f;
            var standDepth = 18f;
            var standHeight = 14f;

            BuildGrandstand(stadiumRoot, new Vector3(0f, standHeight * 0.5f, turnRadius + outerOffset + standDepth * 0.5f),
                new Vector3(half * 2f + 28f, standHeight, standDepth), Quaternion.identity, 6, Vector3.back);
            BuildGrandstand(stadiumRoot, new Vector3(0f, standHeight * 0.5f, -(turnRadius + outerOffset + standDepth * 0.5f)),
                new Vector3(half * 2f + 28f, standHeight, standDepth), Quaternion.identity, 6, Vector3.forward);
            BuildGrandstand(stadiumRoot, new Vector3(half + outerOffset + standDepth * 0.5f, standHeight * 0.5f, 0f),
                new Vector3(standDepth, standHeight, turnRadius * 2f + 20f), Quaternion.identity, 5, Vector3.left);
            BuildGrandstand(stadiumRoot, new Vector3(-(half + outerOffset + standDepth * 0.5f), standHeight * 0.5f, 0f),
                new Vector3(standDepth, standHeight, turnRadius * 2f + 20f), Quaternion.identity, 5, Vector3.right);

            BuildCornerCrowd(stadiumRoot, new Vector3(half + outerOffset * 0.35f, standHeight * 0.45f, turnRadius + outerOffset * 0.35f),
                new Vector3(12f, standHeight * 0.9f, 12f), Vector3.back);
            BuildCornerCrowd(stadiumRoot, new Vector3(-half - outerOffset * 0.35f, standHeight * 0.45f, turnRadius + outerOffset * 0.35f),
                new Vector3(12f, standHeight * 0.9f, 12f), Vector3.back);
            BuildCornerCrowd(stadiumRoot, new Vector3(half + outerOffset * 0.35f, standHeight * 0.45f, -(turnRadius + outerOffset * 0.35f)),
                new Vector3(12f, standHeight * 0.9f, 12f), Vector3.forward);
            BuildCornerCrowd(stadiumRoot, new Vector3(-half - outerOffset * 0.35f, standHeight * 0.45f, -(turnRadius + outerOffset * 0.35f)),
                new Vector3(12f, standHeight * 0.9f, 12f), Vector3.forward);

            if (environmentDensity >= 0.55f)
                BuildOuterBleachers(stadiumRoot, half, turnRadius, outerOffset + standDepth + 1f);
            BuildStadiumRing(stadiumRoot, half, turnRadius, outerOffset + standDepth + 2f);
            BuildEntranceArc(stadiumRoot, new Vector3(0f, 6f, turnRadius + outerOffset + standDepth + 3f), Vector3.back);
            BuildTracksideRetainingWalls(stadiumRoot, half, turnRadius, trackWidth);
        }

        void BuildGrandstand(Transform parent, Vector3 center, Vector3 size, Quaternion rotation, int tiers,
            Vector3 faceDirection)
        {
            var stand = CreateEmpty("Grandstand", parent);
            stand.transform.SetPositionAndRotation(center, rotation);

            var tierHeight = size.y / tiers;
            for (var tier = 0; tier < tiers; tier++)
            {
                var y = tier * tierHeight + tierHeight * 0.5f;
                var inset = tier * 1.1f;
                var tierSize = new Vector3(size.x - inset * 2f, tierHeight * 0.85f, size.z - inset * 0.35f);
                if (tierSize.x < 4f || tierSize.z < 2f)
                    continue;

                CreatePrimitive(PrimitiveType.Cube, "Tier_" + tier, stand.transform,
                    new Vector3(0f, y, 0f), tierSize, materials.Stadium);

                var crowdRoot = CreateEmpty("Crowd_T" + tier, stand.transform);
                crowdRoot.localPosition = new Vector3(0f, y + tierHeight * 0.35f, 0f);
                FillCrowd(crowdRoot, tierSize, faceDirection, tier);
            }
        }

        void BuildCornerCrowd(Transform parent, Vector3 center, Vector3 size, Vector3 faceDirection)
        {
            var corner = CreateEmpty("CornerCrowd", parent);
            corner.transform.position = center;

            for (var tier = 0; tier < 2; tier++)
            {
                var y = tier * (size.y / 4f) + size.y / 8f;
                var crowdRoot = CreateEmpty("CornerCrowd_T" + tier, corner.transform);
                crowdRoot.localPosition = new Vector3(0f, y, 0f);
                var tierSize = new Vector3(size.x - tier * 0.8f, size.y / 4f, size.z - tier * 0.8f);
                FillCrowd(crowdRoot, tierSize, faceDirection, tier + 200);
            }
        }

        void BuildOuterBleachers(Transform parent, float half, float turnRadius, float radius)
        {
            var bleacherRoot = CreateEmpty("OuterBleachers", parent);

            BuildOuterBleacherSection(bleacherRoot,
                new Vector3(0f, 18f, turnRadius + radius + 6f),
                new Vector3(half * 1.6f, 8f, 10f), Vector3.back, 300);
            BuildOuterBleacherSection(bleacherRoot,
                new Vector3(0f, 18f, -(turnRadius + radius + 6f)),
                new Vector3(half * 1.6f, 8f, 10f), Vector3.forward, 301);
            BuildOuterBleacherSection(bleacherRoot,
                new Vector3(half + radius + 6f, 18f, 0f),
                new Vector3(10f, 8f, turnRadius * 1.6f), Vector3.left, 302);
            BuildOuterBleacherSection(bleacherRoot,
                new Vector3(-(half + radius + 6f), 18f, 0f),
                new Vector3(10f, 8f, turnRadius * 1.6f), Vector3.right, 303);
        }

        void BuildOuterBleacherSection(Transform parent, Vector3 center, Vector3 size, Vector3 faceDirection, int seed)
        {
            var section = CreateEmpty("BleacherSection", parent);
            section.transform.position = center;
            CreatePrimitive(PrimitiveType.Cube, "BleacherBase", section.transform,
                Vector3.zero, size, materials.Stadium);

            for (var tier = 0; tier < 2; tier++)
            {
                var crowdRoot = CreateEmpty("BleacherCrowd_T" + tier, section.transform);
                crowdRoot.localPosition = new Vector3(0f, size.y * 0.15f + tier * (size.y * 0.22f), 0f);
                var tierSize = new Vector3(size.x - tier * 1.2f, size.y * 0.2f, size.z - tier * 0.5f);
                var maxCrowd = Mathf.Max(28, Mathf.RoundToInt(260 * crowdDensity));
                FillCrowd(crowdRoot, tierSize, faceDirection, tier + seed, maxCrowd, 0.68f);
            }
        }

        void FillCrowd(Transform parent, Vector3 areaSize, Vector3 faceDirection, int tier)
        {
            var maxCrowd = Mathf.Max(24, Mathf.RoundToInt(240 * crowdDensity));
            FillCrowd(parent, areaSize, faceDirection, tier, maxCrowd, tier >= 3 ? 0.72f : 0.64f);
        }

        void FillCrowd(Transform parent, Vector3 areaSize, Vector3 faceDirection, int tier, int maxCrowdPerTier,
            float spacing)
        {
            var cols = Mathf.Max(1, Mathf.FloorToInt(areaSize.x / spacing));
            var rows = Mathf.Max(1, Mathf.FloorToInt(areaSize.z / spacing * 0.82f));
            var random = new System.Random(1000 + tier * 131 + parent.name.GetHashCode());
            var spawned = 0;

            for (var row = 0; row < rows && spawned < maxCrowdPerTier; row++)
            {
                for (var col = 0; col < cols && spawned < maxCrowdPerTier; col++)
                {
                    if (random.NextDouble() < 0.006)
                        continue;

                    var x = (col - (cols - 1) * 0.5f) * spacing;
                    var z = (row - rows * 0.5f) * spacing * 0.68f;
                    if (faceDirection.z < 0f)
                        z = -z;

                    var height = 0.72f + (float)random.NextDouble() * 0.38f;
                    var width = 0.34f + (float)random.NextDouble() * 0.14f;
                    var crowdMat = materials.Crowd[random.Next(materials.Crowd.Length)];

                    var fanRoot = CreateEmpty("Fan", parent);
                    fanRoot.localPosition = new Vector3(x, 0f, z);
                    if (faceDirection.sqrMagnitude > 0.001f)
                        fanRoot.localRotation = Quaternion.LookRotation(faceDirection, Vector3.up);

                    CreatePrimitive(PrimitiveType.Capsule, "Body", fanRoot,
                        new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, width), crowdMat);

                    var animateFan = random.NextDouble() < 0.22;
                    if (animateFan)
                    {
                        var isJumping = random.NextDouble() < 0.3;
                        var phase = (float)random.NextDouble() * Mathf.PI * 2f;
                        var animator = fanRoot.gameObject.AddComponent<CrowdFanAnimator>();
                        animator.Configure(isJumping, phase);
                    }

                    if (random.NextDouble() < 0.07)
                    {
                        var message = MotivationalSigns[random.Next(MotivationalSigns.Length)];
                        AddMotivationalSign(fanRoot, height, message, random);
                    }

                    spawned++;
                }
            }
        }

        void AddMotivationalSign(Transform fanRoot, float fanHeight, string message, System.Random random)
        {
            var signRoot = CreateEmpty("Sign", fanRoot);
            signRoot.localPosition = new Vector3(0f, fanHeight * 0.55f, 0.32f);
            signRoot.localRotation = Quaternion.Euler(
                (float)(random.NextDouble() * 16.0 - 8.0),
                (float)(random.NextDouble() * 20.0 - 10.0),
                (float)(random.NextDouble() * 12.0 - 6.0));

            CreatePrimitive(PrimitiveType.Cube, "Pole", signRoot,
                new Vector3(0f, -0.12f, 0f), new Vector3(0.06f, 0.28f, 0.06f), materials.Stadium);

            var boardWidth = message.Length > 8 ? 0.72f : 0.58f;
            CreatePrimitive(PrimitiveType.Cube, "Board", signRoot,
                new Vector3(0f, 0.1f, 0f), new Vector3(boardWidth, 0.34f, 0.05f), materials.SignBoard);

            var textGo = new GameObject("SignText");
            textGo.transform.SetParent(signRoot, false);
            textGo.transform.localPosition = new Vector3(0f, 0.1f, -0.03f);
            textGo.transform.localRotation = Quaternion.identity;

            var textMesh = textGo.AddComponent<TextMesh>();
            textMesh.text = message;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.fontSize = message.Length > 9 ? 28 : 34;
            textMesh.characterSize = 0.038f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.08f, 0.08f, 0.12f);
            textMesh.fontStyle = FontStyle.Bold;
        }

        void BuildStadiumRing(Transform parent, float half, float turnRadius, float radius)
        {
            const int segments = 20;
            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * (half + radius * 0.55f);
                var z = Mathf.Sin(angle) * (turnRadius + radius * 0.55f);
                if (Mathf.Abs(x) > half + 4f && Mathf.Abs(z) > turnRadius + 4f)
                    continue;

                var pos = new Vector3(Mathf.Clamp(x, -half - 8f, half + 8f), 16f, Mathf.Clamp(z, -turnRadius - 8f, turnRadius + 8f));
                CreatePrimitive(PrimitiveType.Cylinder, "OuterPillar_" + i, parent,
                    pos, new Vector3(1.1f, 16f, 1.1f), materials.LightTower);
            }
        }

        void BuildEntranceArc(Transform parent, Vector3 position, Vector3 faceDirection)
        {
            var arc = CreateEmpty("MainEntrance", parent);
            arc.transform.position = position;
            arc.transform.rotation = Quaternion.LookRotation(faceDirection, Vector3.up);

            CreatePrimitive(PrimitiveType.Cube, "ArchL", arc.transform, new Vector3(-8f, 4f, 0f),
                new Vector3(1.2f, 8f, 1.2f), materials.BuildingAccent);
            CreatePrimitive(PrimitiveType.Cube, "ArchR", arc.transform, new Vector3(8f, 4f, 0f),
                new Vector3(1.2f, 8f, 1.2f), materials.BuildingAccent);
            CreatePrimitive(PrimitiveType.Cube, "ArchTop", arc.transform, new Vector3(0f, 8.2f, 0f),
                new Vector3(17f, 1f, 1.2f), materials.BuildingAccent);
            CreatePrimitive(PrimitiveType.Cube, "Sign", arc.transform, new Vector3(0f, 6.5f, -0.4f),
                new Vector3(12f, 2f, 0.2f), materials.ScreenGlow);
        }

        void BuildJumbotron(Vector3 startPosition, Quaternion startRotation, float trackWidth)
        {
            var screenRoot = CreateEmpty("Jumbotron", environmentRoot);
            var forward = startRotation * Vector3.forward;
            var outside = Vector3.Cross(forward, Vector3.up).normalized;
            var offset = outside * (trackWidth * 0.5f + 24f) - forward * 10f;
            screenRoot.transform.position = startPosition + Vector3.up * 11f + offset;
            screenRoot.transform.rotation = startRotation * Quaternion.Euler(0f, 180f, 0f);

            CreatePrimitive(PrimitiveType.Cube, "FrameBottom", screenRoot.transform, new Vector3(0f, -2.8f, 0.2f),
                new Vector3(18f, 0.8f, 0.8f), materials.Stadium);
            CreatePrimitive(PrimitiveType.Cube, "FrameL", screenRoot.transform, new Vector3(-8.8f, 1f, 0.2f),
                new Vector3(0.8f, 7f, 0.8f), materials.Stadium);
            CreatePrimitive(PrimitiveType.Cube, "FrameR", screenRoot.transform, new Vector3(8.8f, 1f, 0.2f),
                new Vector3(0.8f, 7f, 0.8f), materials.Stadium);
            CreatePrimitive(PrimitiveType.Cube, "FrameTop", screenRoot.transform, new Vector3(0f, 5.2f, 0.2f),
                new Vector3(18f, 0.8f, 0.8f), materials.Stadium);
            CreatePrimitive(PrimitiveType.Cube, "ScreenBezel", screenRoot.transform, new Vector3(0f, 1.2f, 0.05f),
                new Vector3(16.5f, 5.8f, 0.25f), materials.Screen);

            var canvasGo = new GameObject("ScoreCanvas");
            canvasGo.transform.SetParent(screenRoot, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.2f, -0.08f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.014f;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 420f);

            var bg = canvasGo.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.06f, 0.1f, 0.96f);

            var title = CreateScreenText(canvasGo.transform, "Title", new Vector2(0f, 150f), 52,
                new Color(0.35f, 1f, 1f), TextAnchor.UpperCenter);
            title.text = "NEON LAP LIVE";

            var status = CreateScreenText(canvasGo.transform, "Status", new Vector2(0f, 95f), 30,
                new Color(1f, 0.9f, 0.35f), TextAnchor.UpperCenter);
            status.text = "NEON GRAND PRIX";

            var position = CreateScreenText(canvasGo.transform, "Position", new Vector2(-180f, 10f), 72,
                Color.white, TextAnchor.MiddleCenter);
            position.text = "P1/10";

            var lap = CreateScreenText(canvasGo.transform, "Lap", new Vector2(180f, 10f), 56,
                new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleCenter);
            lap.text = "LAP 1/3";

            var timer = CreateScreenText(canvasGo.transform, "Timer", new Vector2(0f, -95f), 64,
                new Color(0.45f, 1f, 1f), TextAnchor.MiddleCenter);
            timer.text = "00:00.0";

            jumbotronTitle = title;
            jumbotronPosition = position;
            jumbotronLap = lap;
            jumbotronTimer = timer;
            jumbotronStatus = status;

            var board = screenRoot.gameObject.AddComponent<StadiumScoreboard>();
            scoreboard = board;

            var glow = new GameObject("ScreenGlow");
            glow.transform.SetParent(screenRoot, false);
            glow.transform.localPosition = new Vector3(0f, 1.2f, 0.35f);
            var glowLight = glow.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0.35f, 0.95f, 1f);
            glowLight.intensity = 3f;
            glowLight.range = 22f;
        }

        void BuildCitySkyline(float half, float turnRadius)
        {
            var cityRoot = CreateEmpty("CitySkyline", environmentRoot);
            var random = new System.Random(4242);
            var buildingCount = Mathf.Max(8, Mathf.RoundToInt(28 * environmentDensity));

            for (var i = 0; i < buildingCount; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var distance = half + 55f + (float)random.NextDouble() * 45f;
                var x = Mathf.Cos(angle) * distance;
                var z = Mathf.Sin(angle) * (turnRadius + 35f + (float)random.NextDouble() * 40f);
                var height = 18f + (float)random.NextDouble() * 42f;
                var width = 4f + (float)random.NextDouble() * 7f;
                var depth = 4f + (float)random.NextDouble() * 7f;

                var building = CreatePrimitive(PrimitiveType.Cube, "Building_" + i, cityRoot,
                    new Vector3(x, height * 0.5f, z), new Vector3(width, height, depth), materials.Building);

                if (random.NextDouble() > 0.35)
                {
                    var accentHeight = height * (0.35f + (float)random.NextDouble() * 0.45f);
                    CreatePrimitive(PrimitiveType.Cube, "Accent_" + i, building.transform,
                        new Vector3(0f, 0.15f, depth * 0.55f),
                        new Vector3(width * 0.85f, accentHeight, 0.15f), materials.BuildingAccent);
                }

                if (random.NextDouble() > 0.6f)
                {
                    CreatePrimitive(PrimitiveType.Cylinder, "Antenna_" + i, building.transform,
                        new Vector3(0f, 0.55f, 0f), new Vector3(0.15f, 0.4f, 0.15f), materials.BuildingAccent);
                }
            }
        }

        void BuildTrees(float half, float turnRadius, float trackWidth)
        {
            var treeRoot = CreateEmpty("Trees", environmentRoot);
            var random = new System.Random(9001);
            var treeCount = Mathf.Max(12, Mathf.RoundToInt(64 * environmentDensity));
            var innerRadius = half * 0.55f + trackWidth;
            var outerRadius = half + 38f;

            for (var i = 0; i < treeCount; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = Mathf.Lerp(innerRadius, outerRadius, (float)random.NextDouble());
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * (turnRadius + 18f + (float)random.NextDouble() * (half * 0.45f));

                if (Mathf.Abs(x) < half + 8f && Mathf.Abs(z) < turnRadius + 8f)
                    continue;

                BuildTree(treeRoot, new Vector3(x, 0f, z), 0.75f + (float)random.NextDouble() * 0.55f, random);
            }
        }

        void BuildTree(Transform parent, Vector3 position, float scale, System.Random random)
        {
            var tree = CreateEmpty("Tree", parent);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

            CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform, new Vector3(0f, 1.2f * scale, 0f),
                new Vector3(0.35f * scale, 1.2f * scale, 0.35f * scale), materials.Trunk);
            CreatePrimitive(PrimitiveType.Sphere, "Canopy", tree.transform, new Vector3(0f, 2.8f * scale, 0f),
                new Vector3(2.2f * scale, 2.4f * scale, 2.2f * scale), materials.Foliage);
            CreatePrimitive(PrimitiveType.Sphere, "CanopyGlow", tree.transform, new Vector3(0f, 2.8f * scale, 0f),
                new Vector3(1.2f * scale, 1.3f * scale, 1.2f * scale), materials.BuildingAccent);
        }

        void BuildStadiumLightingSystem(float half, float turnRadius, float trackWidth)
        {
            var lightingRoot = CreateEmpty("StadiumLighting", environmentRoot);
            var towerInset = trackWidth * 0.5f + 16f;
            var towerHeight = 30f;
            var trackCenterY = 0.5f;

            var northZ = turnRadius + towerInset;
            var southZ = -turnRadius - towerInset;
            var eastX = half + towerInset;
            var westX = -half - towerInset;

            var straightTowerXs = new[] { -half * 0.65f, 0f, half * 0.65f };
            foreach (var x in straightTowerXs)
            {
                BuildMegaLightTower(lightingRoot, new Vector3(x, 0f, northZ),
                    new Vector3(x, trackCenterY, turnRadius * 0.35f), towerHeight, 2);
                BuildMegaLightTower(lightingRoot, new Vector3(x, 0f, southZ),
                    new Vector3(x, trackCenterY, -turnRadius * 0.35f), towerHeight, 2);
            }

            var turnTowerZs = new[] { 0f };
            foreach (var z in turnTowerZs)
            {
                BuildMegaLightTower(lightingRoot, new Vector3(eastX, 0f, z),
                    new Vector3(half * 0.55f, trackCenterY, z), towerHeight - 2f, 1);
                BuildMegaLightTower(lightingRoot, new Vector3(westX, 0f, z),
                    new Vector3(-half * 0.55f, trackCenterY, z), towerHeight - 2f, 1);
            }

            BuildOverheadLightTruss(lightingRoot,
                new Vector3(-half * 0.92f, 24f, turnRadius + 10f),
                new Vector3(half * 0.92f, 24f, turnRadius + 10f),
                Vector3.zero, 5);
            BuildOverheadLightTruss(lightingRoot,
                new Vector3(-half * 0.92f, 24f, -turnRadius - 10f),
                new Vector3(half * 0.92f, 24f, -turnRadius - 10f),
                Vector3.zero, 5);

            BuildCornerLightCluster(lightingRoot, new Vector3(half + 8f, 18f, turnRadius + 8f),
                new Vector3(half * 0.5f, trackCenterY, turnRadius * 0.5f));
            BuildCornerLightCluster(lightingRoot, new Vector3(half + 8f, 18f, -turnRadius - 8f),
                new Vector3(half * 0.5f, trackCenterY, -turnRadius * 0.5f));
            BuildCornerLightCluster(lightingRoot, new Vector3(-half - 8f, 18f, turnRadius + 8f),
                new Vector3(-half * 0.5f, trackCenterY, turnRadius * 0.5f));
            BuildCornerLightCluster(lightingRoot, new Vector3(-half - 8f, 18f, -turnRadius - 8f),
                new Vector3(-half * 0.5f, trackCenterY, -turnRadius * 0.5f));

            BuildTrackRimLights(lightingRoot, half, turnRadius, trackWidth);
            AddStadiumFillLight(lightingRoot, new Vector3(0f, 38f, 0f), new Color(0.7f, 0.82f, 1f), 2.5f, 110f);
            AddStadiumFillLight(lightingRoot, new Vector3(0f, 28f, turnRadius + 6f), new Color(0.85f, 0.92f, 1f), 1.8f, 75f);
            AddStadiumFillLight(lightingRoot, new Vector3(0f, 28f, -turnRadius - 6f), new Color(0.85f, 0.92f, 1f), 1.8f, 75f);
        }

        void BuildMegaLightTower(Transform parent, Vector3 basePosition, Vector3 aimPoint, float height,
            int lampCount)
        {
            var rig = CreateEmpty("MegaLightTower", parent);
            rig.position = basePosition;

            CreatePrimitive(PrimitiveType.Cylinder, "TowerMast", rig,
                Vector3.up * (height * 0.5f), new Vector3(2.4f, height, 2.4f), materials.LightTower);
            CreatePrimitive(PrimitiveType.Cube, "TowerBase", rig,
                new Vector3(0f, 0.6f, 0f), new Vector3(3.6f, 1.2f, 3.6f), materials.LightTower);

            var headHeight = height + 1.2f;
            var toAim = aimPoint - (basePosition + Vector3.up * headHeight);
            toAim.y = 0f;
            var forward = toAim.sqrMagnitude > 0.01f ? toAim.normalized : Vector3.forward;

            var head = CreateEmpty("LightHead", rig);
            head.localPosition = Vector3.up * headHeight;
            head.rotation = Quaternion.LookRotation(forward, Vector3.up);

            CreatePrimitive(PrimitiveType.Cube, "CrossArm", head,
                Vector3.forward * 1.8f, new Vector3(7.5f, 0.7f, 0.7f), materials.LightTower);
            CreatePrimitive(PrimitiveType.Cube, "HeadBackbone", head,
                Vector3.forward * 2.4f, new Vector3(1.2f, 1.2f, 3.6f), materials.LightTower);

            var spacing = 7f / Mathf.Max(lampCount, 1);
            for (var i = 0; i < lampCount; i++)
            {
                var lateral = (i - (lampCount - 1) * 0.5f) * spacing;
                var lampLocal = Vector3.forward * 2.6f + Vector3.right * lateral;
                CreatePrimitive(PrimitiveType.Cube, "LampHousing_" + i, head,
                    lampLocal, new Vector3(1.35f, 0.85f, 1.1f), materials.LightTower);
                CreatePrimitive(PrimitiveType.Sphere, "LampGlow_" + i, head,
                    lampLocal + Vector3.forward * 0.55f, new Vector3(1.1f, 1.1f, 0.65f), materials.LampHead);

                var lampWorld = head.TransformPoint(lampLocal + Vector3.forward * 0.4f);
                AddTrackSpotLight(head, lampWorld, aimPoint + Vector3.right * lateral * 0.15f,
                    new Color(0.88f, 0.94f, 1f), 7.5f, 95f, 58f, 36f);
            }
        }

        void BuildOverheadLightTruss(Transform parent, Vector3 start, Vector3 end, Vector3 aimOffset, int lampCount)
        {
            var truss = CreateEmpty("LightTruss", parent);
            truss.position = (start + end) * 0.5f;
            var span = end - start;
            var length = span.magnitude;
            truss.rotation = Quaternion.LookRotation(span.normalized, Vector3.up);

            CreatePrimitive(PrimitiveType.Cube, "TrussMain", truss,
                Vector3.zero, new Vector3(0.9f, 0.9f, length), materials.LightTower);
            CreatePrimitive(PrimitiveType.Cube, "TrussTop", truss,
                new Vector3(0f, 0.55f, 0f), new Vector3(0.45f, 0.45f, length), materials.LightTower);
            CreatePrimitive(PrimitiveType.Cube, "TrussBottom", truss,
                new Vector3(0f, -0.55f, 0f), new Vector3(0.45f, 0.45f, length), materials.LightTower);

            var aimCenter = (start + end) * 0.5f + aimOffset;
            var step = length / Mathf.Max(lampCount - 1, 1);
            for (var i = 0; i < lampCount; i++)
            {
                var zLocal = -length * 0.5f + step * i;
                var lampLocal = new Vector3(0f, -0.85f, zLocal);
                CreatePrimitive(PrimitiveType.Cube, "TrussLamp_" + i, truss,
                    lampLocal, new Vector3(1.2f, 0.55f, 1.2f), materials.LampHead);

                var lampWorld = truss.TransformPoint(lampLocal);
                var aim = aimCenter + truss.right * (i - (lampCount - 1) * 0.5f) * 2.5f;
                aim.y = 0.4f;
                AddTrackSpotLight(truss, lampWorld, aim, new Color(0.92f, 0.96f, 1f), 5.5f, 80f, 72f, 48f);
            }
        }

        void BuildCornerLightCluster(Transform parent, Vector3 clusterPosition, Vector3 aimPoint)
        {
            var cluster = CreateEmpty("CornerLights", parent);
            cluster.position = clusterPosition;

            CreatePrimitive(PrimitiveType.Cylinder, "CornerMast", cluster,
                Vector3.up * 5f, new Vector3(1.4f, 10f, 1.4f), materials.LightTower);
            CreatePrimitive(PrimitiveType.Sphere, "CornerGlow", cluster,
                Vector3.up * 10.5f, new Vector3(2.2f, 2.2f, 2.2f), materials.LampHead);

            AddTrackSpotLight(cluster, clusterPosition + Vector3.up * 10f, aimPoint,
                new Color(0.8f, 0.9f, 1f), 6f, 85f, 65f, 40f);
            AddTrackSpotLight(cluster, clusterPosition + Vector3.up * 9f + cluster.forward * 1.5f, aimPoint,
                new Color(0.75f, 0.88f, 1f), 4.5f, 70f, 55f, 32f);
        }

        void BuildTrackRimLights(Transform parent, float half, float turnRadius, float trackWidth)
        {
            var rimRoot = CreateEmpty("TrackRimLights", parent);
            var rimOffset = trackWidth * 0.5f + 2.5f;
            var straightLamps = Mathf.Max(2, Mathf.RoundToInt(6 * environmentDensity));

            for (var i = 0; i < straightLamps; i++)
            {
                var t = i / (float)(straightLamps - 1);
                var x = Mathf.Lerp(-half * 0.9f, half * 0.9f, t);
                BuildRimLightPair(rimRoot, new Vector3(x, 3.5f, turnRadius + rimOffset),
                    new Vector3(x, 0.4f, turnRadius * 0.85f));
                BuildRimLightPair(rimRoot, new Vector3(x, 3.5f, -turnRadius - rimOffset),
                    new Vector3(x, 0.4f, -turnRadius * 0.85f));
            }

            var turnLamps = environmentDensity >= 0.55f ? 4 : 2;
            for (var i = 0; i < turnLamps; i++)
            {
                var t = (i + 1) / (turnLamps + 1f);
                var z = Mathf.Lerp(-turnRadius * 0.85f, turnRadius * 0.85f, t);
                BuildRimLightPair(rimRoot, new Vector3(half + rimOffset, 3.5f, z),
                    new Vector3(half * 0.85f, 0.4f, z));
                BuildRimLightPair(rimRoot, new Vector3(-half - rimOffset, 3.5f, z),
                    new Vector3(-half * 0.85f, 0.4f, z));
            }
        }

        void BuildRimLightPair(Transform parent, Vector3 lampPosition, Vector3 aimPoint)
        {
            var rim = CreateEmpty("RimLight", parent);
            rim.position = lampPosition;

            CreatePrimitive(PrimitiveType.Cylinder, "RimPole", rim,
                Vector3.up * 1.75f, new Vector3(0.18f, 3.5f, 0.18f), materials.LightTower);
            CreatePrimitive(PrimitiveType.Sphere, "RimBulb", rim,
                Vector3.up * 3.6f, new Vector3(0.55f, 0.55f, 0.55f), materials.LampHead);
            AddTrackSpotLight(rim, lampPosition + Vector3.up * 3.75f, aimPoint,
                new Color(0.55f, 0.95f, 1f), 3.2f, 40f, 75f, 52f);
        }

        void BuildTracksideRetainingWalls(Transform parent, float half, float turnRadius, float trackWidth)
        {
            var outerOffset = trackWidth * 0.5f + 10f;
            var standDepth = 18f;
            var wallHeight = 9f;
            var wallThickness = 1.2f;
            var wallOffset = outerOffset + standDepth + 6f;
            var wallRoot = CreateEmpty("RetainingWalls", parent);

            CreateVisualWorldBlock(wallRoot, "RetainingWallNorth",
                new Vector3(0f, wallHeight * 0.5f, turnRadius + wallOffset),
                new Vector3(half * 2f + 28f, wallHeight, wallThickness), materials.Stadium);
            CreateVisualWorldBlock(wallRoot, "RetainingWallSouth",
                new Vector3(0f, wallHeight * 0.5f, -(turnRadius + wallOffset)),
                new Vector3(half * 2f + 28f, wallHeight, wallThickness), materials.Stadium);
        }

        static void CreateVisualWorldBlock(Transform parent, string name, Vector3 worldPosition, Vector3 size,
            Material material)
        {
            var go = CreatePrimitive(PrimitiveType.Cube, name, parent, Vector3.zero, size, material);
            go.transform.position = worldPosition;
        }

        static void AddTrackSpotLight(Transform parent, Vector3 position, Vector3 aimPoint, Color color,
            float intensity, float range, float outerAngle, float innerAngle, bool castShadows = false)
        {
            var lightGo = new GameObject("TrackSpot");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = position;

            var direction = aimPoint - position;
            if (direction.sqrMagnitude < 0.01f)
                direction = parent != null ? parent.forward : Vector3.forward;
            lightGo.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = outerAngle;
            light.innerSpotAngle = innerAngle;
            light.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
        }

        static void AddStadiumFillLight(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var lightGo = new GameObject("StadiumFill");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = position;

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        static Text CreateScreenText(Transform parent, string name, Vector2 anchoredPos, int fontSize, Color color,
            TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 120f);
            rect.anchoredPosition = anchoredPos;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static Transform CreateEmpty(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Object.Destroy(go.GetComponent<Collider>());
            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
    }
}
