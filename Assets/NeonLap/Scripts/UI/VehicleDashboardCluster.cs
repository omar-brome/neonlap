using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Input;
using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    public class VehicleDashboardCluster : MonoBehaviour
    {
        const float MetersPerSecondToMph = 2.23694f;

        struct GaugeVisual
        {
            public RectTransform Needle;
            public Text DigitalReadout;
            public float MinAngle;
            public float MaxAngle;
            public float MaxValue;
            public float DisplayValue;
            public float Velocity;
        }

        struct WarningLightVisual
        {
            public string Id;
            public Image Icon;
            public Text Label;
            public Color ActiveColor;
            public bool IsOn;
            public bool Flicker;
            public float NextToggleTime;
        }

        struct FuelGaugeVisual
        {
            public Image FillBar;
            public Text Readout;
            public Text RefillPrompt;
            public float DisplayLevel;
            public float Velocity;
        }

        struct DamageGaugeVisual
        {
            public Image FillBar;
            public Text Readout;
            public float DisplayLevel;
            public float Velocity;
            public bool Visible;
        }

        [SerializeField] float speedMaxMph = 120f;
        [SerializeField] float rpmMax = 8000f;
        [SerializeField] float fuelDepletionDuration;
        [SerializeField] float needleSmoothTime = 0.12f;
        [SerializeField] float turnSignalSteerThreshold = 0.12f;
        [SerializeField] float turnSignalBlinkInterval = 0.45f;

        GaugeVisual speedGauge;
        GaugeVisual rpmGauge;
        FuelGaugeVisual fuelGauge;
        DamageGaugeVisual damageGauge;
        RectTransform clusterRoot;
        Image clusterBezel;
        Text nitroReadout;
        Text slipReadout;
        Text driftZoneReadout;
        Text ghostDeltaReadout;
        Text medalProgressReadout;
        Text turnSignalLeft;
        Text turnSignalRight;
        Image turnSignalLeftGlow;
        Image turnSignalRightGlow;
        readonly List<WarningLightVisual> warningLights = new();

        RaceManager raceManager;
        RaceScoreSystem raceScoreSystem;
        VehicleController playerVehicle;
        VehicleFuelSystem playerFuel;
        VehicleHealthSystem playerHealth;
        VehicleDamageSystem playerDamage;
        VehicleNitroBoost nitroBoost;
        VehicleSlipEffect playerSlip;
        DriftZonePresence driftZonePresence;
        GhostHudController ghostHud;
        Rigidbody playerRigidbody;
        IVehicleInputProvider inputProvider;
        Font uiFont;
        bool built;

        public void Build(Transform canvasRoot)
        {
            if (built)
                return;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            clusterRoot = CreateClusterRoot(canvasRoot);
            speedGauge = CreateGauge(clusterRoot, "SpeedGauge", new Vector2(118f, 18f), 220f,
                0f, 120f, 135f, -135f, "MPH", new Color(0.35f, 1f, 1f),
                new[] { 0f, 20f, 40f, 60f, 80f, 100f, 120f });
            rpmGauge = CreateGauge(clusterRoot, "RpmGauge", new Vector2(-118f, 28f), 180f,
                0f, 8f, 130f, -130f, "x1000 RPM", new Color(1f, 0.62f, 0.18f),
                new[] { 0f, 2f, 4f, 6f, 8f });
            BuildFuelGauge(clusterRoot);
            BuildDamageGauge(clusterRoot);
            BuildTurnSignalIndicators(clusterRoot);
            BuildWarningLights(clusterRoot);
            BuildTelemetryReadouts(clusterRoot);
            built = true;
        }

        public void Configure(RaceManager manager, VehicleController player, GhostHudController ghostHudController = null,
            RaceScoreSystem scoreSystem = null)
        {
            raceManager = manager;
            raceScoreSystem = scoreSystem;
            ghostHud = ghostHudController;
            playerVehicle = player;
            playerFuel = player != null ? player.GetComponent<VehicleFuelSystem>() : null;
            playerHealth = player != null ? player.GetComponent<VehicleHealthSystem>() : null;
            playerDamage = player != null ? player.GetComponent<VehicleDamageSystem>() : null;
            playerRigidbody = player != null ? player.GetComponent<Rigidbody>() : null;
            nitroBoost = player != null ? player.GetComponent<VehicleNitroBoost>() : null;
            playerSlip = player != null ? player.GetComponent<VehicleSlipEffect>() : null;
            driftZonePresence = player != null ? player.GetComponent<DriftZonePresence>() : null;
            inputProvider = player != null ? player.GetComponent<IVehicleInputProvider>() : null;

            if (playerFuel != null)
            {
                var tankDuration = fuelDepletionDuration > 0.01f
                    ? fuelDepletionDuration
                    : GameFuelEconomy.GetTankDuration(GameLapSettings.CurrentLaps);
                playerFuel.Configure(tankDuration, manager);
            }

            ScheduleInitialWarningLights();
            ApplyProfileSkin(player?.Profile);
        }

        public void ApplyProfileSkin(VehicleProfile profile)
        {
            if (!built || profile == null)
                return;

            var kind = PlayerVehicleProfileStore.GetKindForProfile(profile);
            var skin = PlayerVehicleProfileStore.GetDashboardSkin(kind);
            speedMaxMph = Mathf.Max(profile.maxSpeed * MetersPerSecondToMph, 60f);
            speedGauge.MaxValue = speedMaxMph;

            SetNeedleColor(speedGauge.Needle, skin.SpeedNeedleColor);
            SetNeedleColor(rpmGauge.Needle, skin.RpmNeedleColor);

            if (clusterBezel != null)
                clusterBezel.color = skin.BezelColor;
        }

        static void SetNeedleColor(RectTransform needle, Color color)
        {
            if (needle == null)
                return;

            var image = needle.GetComponent<Image>();
            if (image != null)
                image.color = color;
        }

        public void SetVisible(bool visible)
        {
            if (clusterRoot != null)
                clusterRoot.gameObject.SetActive(visible);
        }

        void Update()
        {
            if (!built || raceManager == null)
                return;

            var show = raceManager.State == RaceState.Racing || raceManager.State == RaceState.Finished;
            SetVisible(show);
            if (!show)
                return;

            ResolvePlayerReferences();
            UpdateGauges();
            UpdateFuelGauge();
            UpdateDamageGauge();
            UpdateTurnSignalIndicators();
            UpdateWarningLights();
            UpdateTelemetryReadouts();
        }

        void ResolvePlayerReferences()
        {
            if (playerVehicle != null && playerRigidbody != null)
                return;

            if (raceManager == null)
                return;

            foreach (var racer in raceManager.Racers)
            {
                if (racer == null || !racer.IsPlayer)
                    continue;

                playerVehicle ??= racer.GetComponent<VehicleController>();
                playerFuel ??= racer.GetComponent<VehicleFuelSystem>();
                playerHealth ??= racer.GetComponent<VehicleHealthSystem>();
                playerDamage ??= racer.GetComponent<VehicleDamageSystem>();
                playerRigidbody ??= racer.GetComponent<Rigidbody>();
                nitroBoost ??= racer.GetComponent<VehicleNitroBoost>();
                playerSlip ??= racer.GetComponent<VehicleSlipEffect>();
                driftZonePresence ??= racer.GetComponent<DriftZonePresence>();
                inputProvider ??= racer.GetComponent<IVehicleInputProvider>();
                ApplyProfileSkin(playerVehicle?.Profile);
                return;
            }
        }

        void UpdateGauges()
        {
            var speedMph = GetPlayerSpeedMetersPerSecond() * MetersPerSecondToMph;
            var rpm = ComputeSimulatedRpm(speedMph);

            UpdateGauge(ref speedGauge, speedMph, "{0:000}");
            UpdateGauge(ref rpmGauge, rpm / 1000f, "{0:0.0}");
        }

        float GetFuelLevel()
        {
            if (playerFuel != null)
                return playerFuel.NormalizedFuel;

            if (raceManager == null)
                return 1f;

            if (raceManager.State is RaceState.Waiting or RaceState.Countdown)
                return 1f;

            if (fuelDepletionDuration <= 0.01f)
                return 0f;

            return Mathf.Clamp01(1f - raceManager.RaceTime / fuelDepletionDuration);
        }

        bool IsFuelEmpty()
        {
            return playerFuel != null ? playerFuel.IsEmpty : GetFuelLevel() <= 0.01f;
        }

        void UpdateFuelGauge()
        {
            if (fuelGauge.FillBar == null)
                return;

            var hideFuel = playerFuel != null && playerFuel.IsInfinite;
            var gaugeRoot = fuelGauge.FillBar.transform.parent != null
                ? fuelGauge.FillBar.transform.parent.gameObject
                : null;
            if (gaugeRoot != null && gaugeRoot.activeSelf == hideFuel)
                gaugeRoot.SetActive(!hideFuel);

            if (hideFuel)
            {
                if (fuelGauge.RefillPrompt != null)
                    fuelGauge.RefillPrompt.gameObject.SetActive(false);
                return;
            }

            var targetLevel = GetFuelLevel();
            fuelGauge.DisplayLevel = Mathf.SmoothDamp(fuelGauge.DisplayLevel, targetLevel, ref fuelGauge.Velocity,
                needleSmoothTime);
            fuelGauge.FillBar.fillAmount = fuelGauge.DisplayLevel;

            var fillColor = fuelGauge.DisplayLevel > 0.5f
                ? Color.Lerp(new Color(0.95f, 0.82f, 0.12f), new Color(0.25f, 0.95f, 0.35f),
                    (fuelGauge.DisplayLevel - 0.5f) / 0.5f)
                : Color.Lerp(new Color(1f, 0.22f, 0.12f), new Color(0.95f, 0.82f, 0.12f), fuelGauge.DisplayLevel / 0.5f);
            fuelGauge.FillBar.color = fillColor;

            var empty = raceManager != null && raceManager.State == RaceState.Racing && IsFuelEmpty();
            if (fuelGauge.Readout != null)
                fuelGauge.Readout.text = empty ? "EMPTY" : Mathf.RoundToInt(fuelGauge.DisplayLevel * 100f) + "%";

            if (fuelGauge.RefillPrompt != null)
            {
                fuelGauge.RefillPrompt.gameObject.SetActive(empty);
                if (empty)
                {
                    var blinkOn = Mathf.FloorToInt(Time.time * 2.4f) % 2 == 0;
                    fuelGauge.RefillPrompt.color = blinkOn
                        ? new Color(1f, 0.82f, 0.18f)
                        : new Color(1f, 0.55f, 0.12f, 0.55f);
                }
            }
        }

        float GetPlayerDamagePercent()
        {
            if (playerHealth != null && playerHealth.enabled)
                return playerHealth.DamagePercent * 100f;

            if (playerDamage != null)
                return playerDamage.DamagePercent * 100f;

            return 0f;
        }

        bool ShouldShowDamageGauge()
        {
            return RaceModeDamageRules.GetDamageProfile().DamageMode != VehicleDamageMode.Off;
        }

        void UpdateDamageGauge()
        {
            if (damageGauge.FillBar == null)
                return;

            var show = ShouldShowDamageGauge();
            if (damageGauge.FillBar.transform.parent != null)
                damageGauge.FillBar.transform.parent.gameObject.SetActive(show);

            if (!show)
                return;

            var targetDamage = Mathf.Clamp01(GetPlayerDamagePercent() / 100f);
            damageGauge.DisplayLevel = Mathf.SmoothDamp(damageGauge.DisplayLevel, targetDamage, ref damageGauge.Velocity,
                needleSmoothTime);
            damageGauge.FillBar.fillAmount = damageGauge.DisplayLevel;

            var fillColor = damageGauge.DisplayLevel > 0.65f
                ? Color.Lerp(new Color(1f, 0.72f, 0.2f), new Color(1f, 0.22f, 0.12f),
                    (damageGauge.DisplayLevel - 0.65f) / 0.35f)
                : Color.Lerp(new Color(0.35f, 0.95f, 1f), new Color(1f, 0.72f, 0.2f), damageGauge.DisplayLevel / 0.65f);
            damageGauge.FillBar.color = fillColor;

            if (damageGauge.Readout != null)
                damageGauge.Readout.text = Mathf.RoundToInt(damageGauge.DisplayLevel * 100f) + "% DMG";
        }

        void BuildDamageGauge(RectTransform parent)
        {
            var gaugeGo = new GameObject("DamageGauge");
            gaugeGo.transform.SetParent(parent, false);
            var gaugeRect = gaugeGo.AddComponent<RectTransform>();
            gaugeRect.anchorMin = new Vector2(0.5f, 0f);
            gaugeRect.anchorMax = new Vector2(0.5f, 0f);
            gaugeRect.pivot = new Vector2(0.5f, 0f);
            gaugeRect.anchoredPosition = new Vector2(0f, 8f);
            gaugeRect.sizeDelta = new Vector2(168f, 52f);

            CreateImage(gaugeGo.transform, "DamagePanel", Vector2.zero, new Vector2(168f, 52f),
                new Color(0.06f, 0.08f, 0.11f, 0.95f));

            var titleGo = new GameObject("DamageTitle");
            titleGo.transform.SetParent(gaugeGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0f);
            titleRect.anchorMax = new Vector2(0.5f, 0f);
            titleRect.pivot = new Vector2(0.5f, 0f);
            titleRect.anchoredPosition = new Vector2(0f, 30f);
            titleRect.sizeDelta = new Vector2(168f, 18f);
            var title = titleGo.AddComponent<Text>();
            title.font = uiFont;
            title.fontSize = 13;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.72f, 0.78f, 0.84f);
            title.text = "HULL";
            title.raycastTarget = false;

            CreateImage(gaugeGo.transform, "DamageTrack", new Vector2(0f, 12f), new Vector2(140f, 14f),
                new Color(0.12f, 0.14f, 0.18f, 0.98f));

            var fillRect = CreateImage(gaugeGo.transform, "DamageFill", new Vector2(0f, 12f), new Vector2(136f, 10f),
                new Color(0.35f, 0.95f, 1f));
            var fillImage = fillRect.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;

            var readoutGo = new GameObject("DamageReadout");
            readoutGo.transform.SetParent(gaugeGo.transform, false);
            var readoutRect = readoutGo.AddComponent<RectTransform>();
            readoutRect.anchorMin = new Vector2(0.5f, 0f);
            readoutRect.anchorMax = new Vector2(0.5f, 0f);
            readoutRect.pivot = new Vector2(0.5f, 0f);
            readoutRect.anchoredPosition = new Vector2(0f, -2f);
            readoutRect.sizeDelta = new Vector2(168f, 18f);
            var readout = readoutGo.AddComponent<Text>();
            readout.font = uiFont;
            readout.fontSize = 12;
            readout.fontStyle = FontStyle.Bold;
            readout.alignment = TextAnchor.MiddleCenter;
            readout.color = new Color(0.45f, 1f, 1f);
            readout.text = "0% DMG";
            readout.raycastTarget = false;

            damageGauge = new DamageGaugeVisual
            {
                FillBar = fillImage,
                Readout = readout,
            };

            gaugeGo.SetActive(false);
        }

        void BuildFuelGauge(RectTransform parent)
        {
            var gaugeGo = new GameObject("FuelGauge");
            gaugeGo.transform.SetParent(parent, false);
            var gaugeRect = gaugeGo.AddComponent<RectTransform>();
            gaugeRect.anchorMin = new Vector2(0.5f, 0f);
            gaugeRect.anchorMax = new Vector2(0.5f, 0f);
            gaugeRect.pivot = new Vector2(0.5f, 0f);
            gaugeRect.anchoredPosition = new Vector2(0f, 58f);
            gaugeRect.sizeDelta = new Vector2(168f, 72f);

            CreateImage(gaugeGo.transform, "FuelPanel", Vector2.zero, new Vector2(168f, 72f),
                new Color(0.06f, 0.08f, 0.11f, 0.95f));

            var titleGo = new GameObject("FuelTitle");
            titleGo.transform.SetParent(gaugeGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0f);
            titleRect.anchorMax = new Vector2(0.5f, 0f);
            titleRect.pivot = new Vector2(0.5f, 0f);
            titleRect.anchoredPosition = new Vector2(0f, 48f);
            titleRect.sizeDelta = new Vector2(168f, 20f);
            var title = titleGo.AddComponent<Text>();
            title.font = uiFont;
            title.fontSize = 13;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.72f, 0.78f, 0.84f);
            title.text = "GAS";
            title.raycastTarget = false;

            CreateImage(gaugeGo.transform, "FuelTrack", new Vector2(0f, 24f), new Vector2(140f, 16f),
                new Color(0.12f, 0.14f, 0.18f, 0.98f));

            var fillRect = CreateImage(gaugeGo.transform, "FuelFill", new Vector2(0f, 24f), new Vector2(136f, 12f),
                new Color(0.25f, 0.95f, 0.35f));
            var fillImage = fillRect.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            CreateFuelEndLabel(gaugeGo.transform, "E", new Vector2(-74f, 24f));
            CreateFuelEndLabel(gaugeGo.transform, "F", new Vector2(74f, 24f));

            var readoutGo = new GameObject("FuelReadout");
            readoutGo.transform.SetParent(gaugeGo.transform, false);
            var readoutRect = readoutGo.AddComponent<RectTransform>();
            readoutRect.anchorMin = new Vector2(0.5f, 0f);
            readoutRect.anchorMax = new Vector2(0.5f, 0f);
            readoutRect.pivot = new Vector2(0.5f, 0f);
            readoutRect.anchoredPosition = new Vector2(0f, 2f);
            readoutRect.sizeDelta = new Vector2(168f, 18f);
            var readout = readoutGo.AddComponent<Text>();
            readout.font = uiFont;
            readout.fontSize = 12;
            readout.fontStyle = FontStyle.Bold;
            readout.alignment = TextAnchor.MiddleCenter;
            readout.color = new Color(0.65f, 0.9f, 0.72f);
            readout.text = "100%";
            readout.raycastTarget = false;

            var promptGo = new GameObject("FuelRefillPrompt");
            promptGo.transform.SetParent(parent, false);
            var promptRect = promptGo.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 138f);
            promptRect.sizeDelta = new Vector2(520f, 34f);
            var prompt = promptGo.AddComponent<Text>();
            prompt.font = uiFont;
            prompt.fontSize = 18;
            prompt.fontStyle = FontStyle.Bold;
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.color = new Color(1f, 0.82f, 0.18f);
            prompt.text = "HIT FUEL PAD / NITRO  ·  R TO REFILL";
            prompt.raycastTarget = false;
            promptGo.SetActive(false);

            fuelGauge = new FuelGaugeVisual
            {
                FillBar = fillImage,
                Readout = readout,
                RefillPrompt = prompt,
                DisplayLevel = 1f,
            };
        }

        void CreateFuelEndLabel(Transform parent, string text, Vector2 anchoredPos)
        {
            var go = new GameObject($"FuelLabel_{text}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(18f, 18f);

            var label = go.AddComponent<Text>();
            label.font = uiFont;
            label.fontSize = 11;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.58f, 0.64f, 0.7f);
            label.text = text;
            label.raycastTarget = false;
        }

        void UpdateTurnSignalIndicators()
        {
            if (turnSignalLeft == null || turnSignalRight == null)
                return;

            var steer = playerVehicle != null ? playerVehicle.SteerInput : 0f;
            var blinkOn = Mathf.FloorToInt(Time.time / turnSignalBlinkInterval) % 2 == 0;
            var leftActive = steer < -turnSignalSteerThreshold && blinkOn;
            var rightActive = steer > turnSignalSteerThreshold && blinkOn;

            ApplyTurnSignalVisual(turnSignalLeft, turnSignalLeftGlow, leftActive);
            ApplyTurnSignalVisual(turnSignalRight, turnSignalRightGlow, rightActive);
        }

        static void ApplyTurnSignalVisual(Text arrow, Image glow, bool active)
        {
            var activeColor = new Color(0.25f, 1f, 0.35f);
            var inactiveColor = new Color(0.12f, 0.18f, 0.14f, 0.45f);
            arrow.color = active ? activeColor : inactiveColor;

            if (glow == null)
                return;

            var glowColor = active ? new Color(0.2f, 1f, 0.35f, 0.55f) : new Color(0f, 0f, 0f, 0f);
            glow.color = glowColor;
        }

        float GetPlayerSpeedMetersPerSecond()
        {
            if (playerVehicle != null)
                return playerVehicle.CurrentSpeed;

            if (playerRigidbody == null)
                return 0f;

            var velocity = playerRigidbody.linearVelocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        float ComputeSimulatedRpm(float speedMph)
        {
            var accel = inputProvider != null ? inputProvider.Accelerate : 0f;
            var brake = inputProvider != null ? inputProvider.Brake : 0f;
            var speedRatio = Mathf.Clamp01(speedMph / speedMaxMph);

            var idleRpm = 780f;
            var cruiseRpm = idleRpm + speedRatio * 2800f;
            var loadRpm = accel * 2400f;
            var shiftDrop = speedMph > 1f && accel < 0.05f ? 120f : 0f;
            var target = cruiseRpm + loadRpm - brake * 500f - shiftDrop;

            if (nitroBoost != null && nitroBoost.IsActive)
                target += 900f;

            if (speedMph < 1f && accel < 0.05f)
                target = idleRpm + Mathf.Sin(Time.time * 3.5f) * 25f;

            return Mathf.Clamp(target, 650f, rpmMax);
        }

        void UpdateGauge(ref GaugeVisual gauge, float targetValue, string digitalFormat)
        {
            gauge.DisplayValue = Mathf.SmoothDamp(gauge.DisplayValue, targetValue, ref gauge.Velocity,
                needleSmoothTime);
            var normalized = Mathf.Clamp01(gauge.DisplayValue / gauge.MaxValue);
            var angle = Mathf.Lerp(gauge.MinAngle, gauge.MaxAngle, normalized);
            gauge.Needle.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (gauge.DigitalReadout != null)
                gauge.DigitalReadout.text = string.Format(digitalFormat, gauge.DisplayValue);
        }

        void UpdateWarningLights()
        {
            var time = Time.time;

            for (var i = 0; i < warningLights.Count; i++)
            {
                var light = warningLights[i];

                if (light.Id == "NITRO")
                {
                    var nitroActive = nitroBoost != null && nitroBoost.IsActive;
                    light.IsOn = nitroActive;
                    light.Flicker = false;
                    ApplyWarningLightVisual(ref light);
                    warningLights[i] = light;
                    continue;
                }

                if (light.Id == "FUEL")
                {
                    var fuelLevel = GetFuelLevel();
                    light.IsOn = fuelLevel < 0.22f;
                    light.Flicker = fuelLevel < 0.1f;
                    ApplyWarningLightVisual(ref light);
                    warningLights[i] = light;
                    continue;
                }

                if (time >= light.NextToggleTime)
                {
                    light.IsOn = !light.IsOn;
                    light.NextToggleTime = time + Random.Range(light.IsOn ? 4f : 12f, light.IsOn ? 14f : 45f);
                    if (light.Id is "ABS" or "TC" or "ENG")
                    {
                        light.Flicker = light.IsOn && Random.value < 0.75f;
                        light.NextToggleTime = time + Random.Range(0.35f, 1.8f);
                    }
                }

                ApplyWarningLightVisual(ref light);
                warningLights[i] = light;
            }
        }

        void ApplyWarningLightVisual(ref WarningLightVisual light)
        {
            if (light.Icon == null)
                return;

            var alpha = 0.18f;
            if (light.IsOn)
            {
                alpha = light.Flicker
                    ? 0.55f + Mathf.Sin(Time.time * 18f) * 0.35f
                    : 0.95f;
            }

            var color = light.ActiveColor;
            color.a = alpha;
            light.Icon.color = color;

            if (light.Label != null)
            {
                var labelColor = Color.Lerp(new Color(0.55f, 0.58f, 0.62f), Color.white, alpha);
                labelColor.a = Mathf.Lerp(0.35f, 0.95f, alpha);
                light.Label.color = labelColor;
            }
        }

        void ScheduleInitialWarningLights()
        {
            var time = Time.time;
            for (var i = 0; i < warningLights.Count; i++)
            {
                var light = warningLights[i];
                light.IsOn = Random.value < 0.25f;
                light.Flicker = light.Id is "ABS" or "TC" or "ENG";
                if (light.Id == "FUEL")
                {
                    light.IsOn = false;
                    light.Flicker = false;
                }

                light.NextToggleTime = time + Random.Range(2f, 18f);
                ApplyWarningLightVisual(ref light);
                warningLights[i] = light;
            }
        }

        RectTransform CreateClusterRoot(Transform canvasRoot)
        {
            var root = new GameObject("VehicleDashboard");
            root.transform.SetParent(canvasRoot, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(560f, 250f);

            clusterBezel = root.AddComponent<Image>();
            clusterBezel.sprite = UiSpriteUtility.White;
            clusterBezel.color = new Color(0.03f, 0.05f, 0.08f, 0.88f);
            clusterBezel.raycastTarget = false;
            return rect;
        }

        GaugeVisual CreateGauge(RectTransform parent, string name, Vector2 anchoredPos, float size,
            float minValue, float maxValue, float minAngle, float maxAngle, string unitLabel, Color needleColor,
            float[] tickValues)
        {
            var gaugeGo = new GameObject(name);
            gaugeGo.transform.SetParent(parent, false);
            var gaugeRect = gaugeGo.AddComponent<RectTransform>();
            gaugeRect.anchorMin = new Vector2(0.5f, 0f);
            gaugeRect.anchorMax = new Vector2(0.5f, 0f);
            gaugeRect.pivot = new Vector2(0.5f, 0f);
            gaugeRect.anchoredPosition = anchoredPos;
            gaugeRect.sizeDelta = new Vector2(size, size);

            CreateImage(gaugeGo.transform, "Face", Vector2.zero, new Vector2(size, size), new Color(0.07f, 0.09f, 0.12f, 0.98f));
            CreateImage(gaugeGo.transform, "FaceRing", Vector2.zero, new Vector2(size * 0.92f, size * 0.92f),
                new Color(0.12f, 0.16f, 0.2f, 0.95f));
            CreateImage(gaugeGo.transform, "FaceInner", Vector2.zero, new Vector2(size * 0.72f, size * 0.72f),
                new Color(0.04f, 0.05f, 0.07f, 0.98f));

            CreateTickMarks(gaugeRect, minAngle, maxAngle, maxValue, tickValues, size);

            var needle = CreateImage(gaugeGo.transform, "Needle", new Vector2(0f, size * 0.08f),
                new Vector2(4f, size * 0.38f), needleColor);
            needle.pivot = new Vector2(0.5f, 0.08f);
            needle.anchorMin = new Vector2(0.5f, 0f);
            needle.anchorMax = new Vector2(0.5f, 0f);

            CreateImage(gaugeGo.transform, "NeedleCap", new Vector2(0f, size * 0.08f), new Vector2(16f, 16f),
                new Color(0.85f, 0.88f, 0.92f));

            var digitalGo = new GameObject("DigitalReadout");
            digitalGo.transform.SetParent(gaugeGo.transform, false);
            var digitalRect = digitalGo.AddComponent<RectTransform>();
            digitalRect.anchorMin = new Vector2(0.5f, 0f);
            digitalRect.anchorMax = new Vector2(0.5f, 0f);
            digitalRect.pivot = new Vector2(0.5f, 0f);
            digitalRect.anchoredPosition = new Vector2(0f, size * 0.28f);
            digitalRect.sizeDelta = new Vector2(size * 0.7f, 34f);
            var digital = digitalGo.AddComponent<Text>();
            digital.font = uiFont;
            digital.fontSize = name == "SpeedGauge" ? 28 : 24;
            digital.fontStyle = FontStyle.Bold;
            digital.alignment = TextAnchor.MiddleCenter;
            digital.color = new Color(0.75f, 0.95f, 1f);
            digital.text = name == "SpeedGauge" ? "000" : "0.0";
            digital.raycastTarget = false;

            var unitGo = new GameObject("UnitLabel");
            unitGo.transform.SetParent(gaugeGo.transform, false);
            var unitRect = unitGo.AddComponent<RectTransform>();
            unitRect.anchorMin = new Vector2(0.5f, 0f);
            unitRect.anchorMax = new Vector2(0.5f, 0f);
            unitRect.pivot = new Vector2(0.5f, 0f);
            unitRect.anchoredPosition = new Vector2(0f, size * 0.16f);
            unitRect.sizeDelta = new Vector2(size, 22f);
            var unitText = unitGo.AddComponent<Text>();
            unitText.font = uiFont;
            unitText.fontSize = 14;
            unitText.alignment = TextAnchor.MiddleCenter;
            unitText.color = new Color(0.55f, 0.65f, 0.72f);
            unitText.text = unitLabel;
            unitText.raycastTarget = false;

            return new GaugeVisual
            {
                Needle = needle,
                DigitalReadout = digital,
                MinAngle = minAngle,
                MaxAngle = maxAngle,
                MaxValue = maxValue,
                DisplayValue = 0f,
            };
        }

        void CreateTickMarks(RectTransform gaugeRect, float minAngle, float maxAngle, float maxValue, float[] tickValues,
            float size)
        {
            var radius = size * 0.36f;
            foreach (var tick in tickValues)
            {
                var t = tick / maxValue;
                var angle = Mathf.Lerp(minAngle, maxAngle, t) * Mathf.Deg2Rad;
                var pos = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius + new Vector2(0f, size * 0.08f);

                var tickGo = new GameObject($"Tick_{tick}");
                tickGo.transform.SetParent(gaugeRect, false);
                var tickRect = tickGo.AddComponent<RectTransform>();
                tickRect.anchorMin = new Vector2(0.5f, 0f);
                tickRect.anchorMax = new Vector2(0.5f, 0f);
                tickRect.pivot = new Vector2(0.5f, 0.5f);
                tickRect.anchoredPosition = pos;
                tickRect.sizeDelta = new Vector2(3f, 12f);
                tickRect.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(minAngle, maxAngle, t));

                var tickImage = tickGo.AddComponent<Image>();
                tickImage.sprite = UiSpriteUtility.White;
                tickImage.color = new Color(0.65f, 0.72f, 0.78f, 0.9f);
                tickImage.raycastTarget = false;

                var labelGo = new GameObject($"TickLabel_{tick}");
                labelGo.transform.SetParent(gaugeRect, false);
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0f);
                labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                var labelPos = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (radius + 18f) +
                               new Vector2(0f, size * 0.08f);
                labelRect.anchoredPosition = labelPos;
                labelRect.sizeDelta = new Vector2(34f, 20f);
                var label = labelGo.AddComponent<Text>();
                label.font = uiFont;
                label.fontSize = 13;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(0.78f, 0.84f, 0.9f);
                label.text = tick >= 10f ? tick.ToString("0") : tick.ToString("0.#");
                label.raycastTarget = false;
            }
        }

        void BuildTelemetryReadouts(RectTransform parent)
        {
            var panel = new GameObject("TelemetryReadouts");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 132f);
            panelRect.sizeDelta = new Vector2(520f, 88f);

            nitroReadout = CreateTelemetryText(panel.transform, "NitroReadout", new Vector2(0f, 66f), 15);
            slipReadout = CreateTelemetryText(panel.transform, "SlipReadout", new Vector2(0f, 48f), 15);
            driftZoneReadout = CreateTelemetryText(panel.transform, "DriftZoneReadout", new Vector2(0f, 30f), 14);
            ghostDeltaReadout = CreateTelemetryText(panel.transform, "GhostDeltaReadout", new Vector2(0f, 12f), 15);
            medalProgressReadout = CreateTelemetryText(panel.transform, "MedalProgressReadout", new Vector2(0f, -4f), 14);
            nitroReadout.text = "NITRO  --";
            slipReadout.text = string.Empty;
            driftZoneReadout.text = string.Empty;
            ghostDeltaReadout.text = string.Empty;
            medalProgressReadout.text = string.Empty;
            slipReadout.gameObject.SetActive(false);
            driftZoneReadout.gameObject.SetActive(false);
            ghostDeltaReadout.gameObject.SetActive(false);
        }

        Text CreateTelemetryText(Transform parent, string name, Vector2 anchoredPosition, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(520f, 20f);

            var text = go.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.7f, 0.92f, 1f, 0.92f);
            text.raycastTarget = false;
            return text;
        }

        void UpdateTelemetryReadouts()
        {
            if (nitroReadout != null)
            {
                if (nitroBoost == null)
                {
                    nitroReadout.text = "NITRO  --";
                }
                else if (nitroBoost.IsActive)
                {
                    nitroReadout.text = "NITRO  BOOST";
                    nitroReadout.color = new Color(0.45f, 0.95f, 1f);
                }
                else
                {
                    nitroReadout.text = $"NITRO  {nitroBoost.Charges}/{nitroBoost.MaxCharges}";
                    nitroReadout.color = nitroBoost.Charges > 0
                        ? new Color(0.7f, 0.92f, 1f, 0.92f)
                        : new Color(0.75f, 0.55f, 0.55f, 0.9f);
                }
            }

            if (ghostDeltaReadout != null)
            {
                var showGhost = ghostHud != null
                                && (GameRaceModeSettings.IsTimeTrial || GameRaceModeSettings.IsGhostDuel);
                ghostDeltaReadout.gameObject.SetActive(showGhost);
                if (showGhost)
                    UpdateGhostDeltaReadout();
            }

            if (medalProgressReadout != null)
            {
                var line = HudMedalProgressFormatter.GetDashboardLine(raceManager, raceScoreSystem);
                medalProgressReadout.gameObject.SetActive(!string.IsNullOrEmpty(line));
                medalProgressReadout.text = line;
            }

            UpdateSlipReadout();
            UpdateDriftZoneReadout();
        }

        void UpdateSlipReadout()
        {
            if (slipReadout == null)
                return;

            if (playerSlip == null || !playerSlip.IsSlipping)
            {
                slipReadout.gameObject.SetActive(false);
                return;
            }

            slipReadout.gameObject.SetActive(true);
            var remaining = playerSlip.SlipTimeRemaining;
            slipReadout.text = $"BANANA SLIP  {remaining:0.0}s";
            slipReadout.color = Color.Lerp(new Color(1f, 0.85f, 0.25f), new Color(1f, 0.35f, 0.35f),
                1f - Mathf.Clamp01(remaining / Mathf.Max(playerSlip.SlipDuration, 0.01f)));
        }

        void UpdateDriftZoneReadout()
        {
            if (driftZoneReadout == null)
                return;

            var inZone = driftZonePresence != null && driftZonePresence.InDriftZone;
            if (!inZone)
            {
                var registry = Track.TrackGameplayZoneRegistry.Instance;
                if (registry != null && playerVehicle != null)
                {
                    var query = new Track.TrackZoneQueryResult();
                    registry.Query(playerVehicle.transform.position, ref query);
                    inZone = query.InDriftMultiplier;
                    if (inZone)
                    {
                        driftZoneReadout.gameObject.SetActive(true);
                        driftZoneReadout.text = $"DRIFT ZONE  x{query.DriftScoreMultiplier:0.0}";
                        driftZoneReadout.color = new Color(1f, 0.82f, 0.25f);
                        return;
                    }
                }

                driftZoneReadout.gameObject.SetActive(false);
                return;
            }

            driftZoneReadout.gameObject.SetActive(true);
            driftZoneReadout.text = $"DRIFT ZONE  x{driftZonePresence.ActiveMultiplier:0.0}";
            driftZoneReadout.color = new Color(1f, 0.82f, 0.25f);
        }

        void UpdateGhostDeltaReadout()
        {
            if (ghostDeltaReadout == null || ghostHud == null || playerVehicle == null)
                return;

            var ghost = ghostHud.PrimaryGhost;
            if (ghost == null || !ghost.IsVisible || !ghost.HasGhost)
            {
                ghostDeltaReadout.text = "GHOST  OFF";
                ghostDeltaReadout.color = new Color(0.75f, 0.85f, 1f, 0.85f);
                return;
            }

            if (!ghost.TryGetDeltaSeconds(playerVehicle.transform.position, out var delta))
            {
                ghostDeltaReadout.text = "GHOST  --";
                return;
            }

            var label = ghost.IsDevGhost ? "DEV" : "PB";
            ghostDeltaReadout.text = $"GHOST  {label} {GhostPlaybackDelta.FormatDelta(delta)}";
            ghostDeltaReadout.color = delta < -0.01f
                ? new Color(0.35f, 1f, 0.65f)
                : delta > 0.01f
                    ? new Color(1f, 0.45f, 0.55f)
                    : new Color(0.55f, 0.95f, 1f);
        }

        void BuildTurnSignalIndicators(RectTransform parent)
        {
            var panel = new GameObject("TurnSignals");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 228f);
            panelRect.sizeDelta = new Vector2(180f, 42f);

            turnSignalLeftGlow = CreateImage(panel.transform, "LeftGlow", new Vector2(-34f, 0f), new Vector2(34f, 34f),
                new Color(0f, 0f, 0f, 0f)).GetComponent<Image>();
            turnSignalRightGlow = CreateImage(panel.transform, "RightGlow", new Vector2(34f, 0f), new Vector2(34f, 34f),
                new Color(0f, 0f, 0f, 0f)).GetComponent<Image>();

            turnSignalLeft = CreateTurnSignalArrow(panel.transform, "LeftArrow", "◄", new Vector2(-34f, 0f));
            turnSignalRight = CreateTurnSignalArrow(panel.transform, "RightArrow", "►", new Vector2(34f, 0f));
        }

        Text CreateTurnSignalArrow(Transform parent, string name, string symbol, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(40f, 40f);

            var text = go.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = symbol;
            text.color = new Color(0.12f, 0.18f, 0.14f, 0.45f);
            text.raycastTarget = false;
            return text;
        }

        void BuildWarningLights(RectTransform parent)
        {
            var panel = new GameObject("WarningLights");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 10f);
            panelRect.sizeDelta = new Vector2(500f, 56f);

            var specs = new[]
            {
                ("ENG", "CHECK", new Color(1f, 0.72f, 0.08f)),
                ("OIL", "OIL", new Color(1f, 0.2f, 0.16f)),
                ("BAT", "BATT", new Color(1f, 0.24f, 0.2f)),
                ("TEMP", "TEMP", new Color(0.95f, 0.28f, 0.18f)),
                ("ABS", "ABS", new Color(1f, 0.82f, 0.12f)),
                ("TC", "TRAC", new Color(1f, 0.82f, 0.12f)),
                ("FUEL", "FUEL", new Color(1f, 0.72f, 0.08f)),
                ("NITRO", "NOS", new Color(0.45f, 0.85f, 1f)),
            };

            const float spacing = 58f;
            var startX = -(specs.Length - 1) * spacing * 0.5f;
            for (var i = 0; i < specs.Length; i++)
            {
                var (id, label, color) = specs[i];
                var x = startX + i * spacing;
                warningLights.Add(CreateWarningLight(panel.transform, id, label, color, new Vector2(x, 18f)));
            }
        }

        WarningLightVisual CreateWarningLight(Transform parent, string id, string label, Color activeColor,
            Vector2 anchoredPos)
        {
            var root = new GameObject($"Warn_{id}");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(48f, 48f);

            var icon = CreateImage(root.transform, "Icon", new Vector2(0f, 8f), new Vector2(22f, 22f), activeColor)
                .GetComponent<Image>();
            icon.type = Image.Type.Simple;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, -2f);
            labelRect.sizeDelta = new Vector2(52f, 16f);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = uiFont;
            labelText.fontSize = 10;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = label;
            labelText.raycastTarget = false;

            return new WarningLightVisual
            {
                Id = id,
                Icon = icon,
                Label = labelText,
                ActiveColor = activeColor,
            };
        }

        static RectTransform CreateImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.sprite = UiSpriteUtility.White;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
