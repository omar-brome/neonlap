using System.Collections.Generic;
using NeonLap.Core;
using NeonLap.Environment;
using NeonLap.Race;
using NeonLap.VFX;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleHealthSystem : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly Color TotalledTint = new(0.38f, 0.4f, 0.42f);

        [SerializeField] float maxHealth = 100f;
        [SerializeField] float minImpactSpeed = 5f;
        [SerializeField] float heavyImpactSpeed = 16f;
        [SerializeField] float impactDamageScale = 1.35f;
        [SerializeField] float environmentDamageScale = 1.6f;
        [SerializeField] float fallDamage = 32f;
        [SerializeField] float offTrackDamage = 24f;
        [SerializeField] float damageCooldown = 0.35f;
        [SerializeField] float raceStartGracePeriod = 3f;

        readonly List<Renderer> visualRenderers = new();
        readonly List<Material> visualMaterials = new();
        readonly List<Color> originalBaseColors = new();
        readonly List<Color> originalEmissionColors = new();
        readonly List<bool> originalEmissionEnabled = new();

        Rigidbody rb;
        RacerProgress racerProgress;
        AIVehicleController aiController;
        VehicleController playerController;
        RaceManager raceManager;
        AIVehicleHealthBar healthBar;
        RaceDamageProfile damageProfile;
        float currentHealth;
        float lastDamageTime;
        float healthDamageMultiplier = 1f;
        bool hardcoreInstantElim;
        bool isTotalled;
        bool visualsCached;
        bool isPlayer;

        public float HealthNormalized => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public float DamagePercent => 1f - HealthNormalized;
        public bool IsTotalled => isTotalled;

        public void Configure(RaceDamageProfile profile)
        {
            damageProfile = profile;
            isPlayer = racerProgress != null && racerProgress.IsPlayer;
            hardcoreInstantElim = profile.HardcoreInstantElimination;
            healthDamageMultiplier = profile.GetHealthDamageMultiplier(isPlayer);

            if (!profile.UsesHealth(isPlayer))
            {
                enabled = false;
                healthBar?.SetVisible(false);
                return;
            }

            enabled = true;
            if (!isPlayer && healthBar == null)
            {
                healthBar = gameObject.AddComponent<AIVehicleHealthBar>();
                healthBar.Build(transform);
            }

            healthBar?.SetVisible(!isPlayer);
            Restore();
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            racerProgress = GetComponent<RacerProgress>();
            aiController = GetComponent<AIVehicleController>();
            playerController = GetComponent<VehicleController>();
            raceManager = FindAnyObjectByType<RaceManager>();
            isPlayer = racerProgress != null && racerProgress.IsPlayer;
            damageProfile = RaceModeDamageRules.GetDamageProfile();

            if (!isPlayer)
            {
                healthBar = gameObject.AddComponent<AIVehicleHealthBar>();
                healthBar.Build(transform);
            }

            CacheVisuals();
            Configure(damageProfile);
        }

        void Update()
        {
            if (isTotalled || isPlayer || healthBar == null)
                return;

            healthBar.SetFill(HealthNormalized);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!CanTakeDamage())
                return;

            if (Time.time - lastDamageTime < damageCooldown)
                return;

            if (collision.contactCount == 0)
                return;

            if (!ShouldDamageFromCollider(collision.collider))
                return;

            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minImpactSpeed)
                return;

            if (isPlayer)
            {
                var shield = GetComponent<VehicleCombatShield>();
                if (shield != null && shield.TryAbsorbHit())
                    return;
            }

            if (hardcoreInstantElim && impactSpeed >= heavyImpactSpeed)
            {
                EliminateFromHeavyHit();
                return;
            }

            var scale = collision.collider.gameObject.layer == NeonLapLayers.Vehicle
                ? impactDamageScale
                : environmentDamageScale;
            ApplyDamage(impactSpeed * scale * healthDamageMultiplier);
            lastDamageTime = Time.time;
        }

        public void ApplyFallDamage()
        {
            if (!CanTakeDamage())
                return;

            ApplyDamage(fallDamage * healthDamageMultiplier);
            lastDamageTime = Time.time;
        }

        public void ApplyOffTrackDamage()
        {
            if (!CanTakeDamage())
                return;

            ApplyDamage(offTrackDamage * healthDamageMultiplier);
            lastDamageTime = Time.time;
        }

        public void ApplyDamage(float amount)
        {
            if (isTotalled || amount <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (currentHealth <= 0f)
                TotalOut();
        }

        public void RestoreHealth()
        {
            if (!enabled)
                return;

            Restore();
        }

        public void Restore()
        {
            isTotalled = false;
            currentHealth = maxHealth;
            lastDamageTime = 0f;

            if (healthBar != null)
            {
                healthBar.SetVisible(!isPlayer);
                healthBar.SetFill(1f);
            }

            RestoreVisualColors();

            if (aiController != null)
                aiController.enabled = true;

            if (playerController != null)
                playerController.enabled = true;

            if (rb != null)
                rb.isKinematic = false;
        }

        void EliminateFromHeavyHit()
        {
            currentHealth = 0f;
            TotalOut();
            Environment.StadiumIncidentHub.Report(isPlayer ? "PLAYER WRECKED" : "RIVAL WRECKED");
        }

        void TotalOut()
        {
            if (isTotalled)
                return;

            isTotalled = true;
            currentHealth = 0f;

            if (aiController != null)
                aiController.enabled = false;

            if (playerController != null)
                playerController.enabled = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            var eliminationTime = raceManager != null ? raceManager.RaceTime : Time.time;
            if (raceManager != null && racerProgress != null)
                raceManager.EliminateRacer(racerProgress);
            else
                racerProgress?.MarkEliminated(eliminationTime);

            ApplyTotalledVisuals();
            healthBar?.SetFill(0f);
            healthBar?.SetVisible(!isPlayer);

            SetBehaviourEnabled<VehicleDriftMarkEmitter>(false);
        }

        void SetBehaviourEnabled<T>(bool behaviourEnabled) where T : Behaviour
        {
            var behaviour = GetComponent<T>();
            if (behaviour != null)
                behaviour.enabled = behaviourEnabled;
        }

        bool CanTakeDamage()
        {
            if (isTotalled || !enabled)
                return false;

            if (racerProgress != null && (racerProgress.IsEliminated || racerProgress.IsFinished))
                return false;

            if (raceManager != null)
            {
                if (raceManager.State != RaceState.Racing)
                    return false;

                if (raceManager.RaceTime < raceStartGracePeriod)
                    return false;
            }

            return true;
        }

        bool ShouldDamageFromCollider(Collider other)
        {
            if (other == null)
                return false;

            if (CollisionHazardUtility.IsDebris(other))
                return false;

            if (other.GetComponentInParent<RollingBarrelImpact>() != null)
                return false;

            if (other.gameObject.layer == NeonLapLayers.Vehicle)
                return other.attachedRigidbody != null && other.attachedRigidbody != rb;

            return CollisionHazardUtility.IsHazard(other, this);
        }

        void CacheVisuals()
        {
            if (visualsCached)
                return;

            visualRenderers.Clear();
            visualMaterials.Clear();
            originalBaseColors.Clear();
            originalEmissionColors.Clear();
            originalEmissionEnabled.Clear();

            var visual = transform.Find("Visual");
            if (visual == null)
                return;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                visualRenderers.Add(renderer);
                var source = renderer.material;
                var instance = new Material(source);
                renderer.material = instance;
                visualMaterials.Add(instance);
                originalBaseColors.Add(instance.HasProperty(BaseColorId)
                    ? instance.GetColor(BaseColorId)
                    : Color.white);
                originalEmissionColors.Add(instance.HasProperty(EmissionColorId)
                    ? instance.GetColor(EmissionColorId)
                    : Color.black);
                originalEmissionEnabled.Add(instance.IsKeywordEnabled("_EMISSION"));
            }

            visualsCached = true;
        }

        void ApplyTotalledVisuals()
        {
            CacheVisuals();

            foreach (var material in visualMaterials)
            {
                if (material == null)
                    continue;

                if (material.HasProperty(BaseColorId))
                    material.SetColor(BaseColorId, TotalledTint);

                if (material.HasProperty(EmissionColorId))
                {
                    material.SetColor(EmissionColorId, Color.black);
                    material.DisableKeyword("_EMISSION");
                }
            }
        }

        void RestoreVisualColors()
        {
            CacheVisuals();

            for (var i = 0; i < visualMaterials.Count; i++)
            {
                var material = visualMaterials[i];
                if (material == null)
                    continue;

                if (material.HasProperty(BaseColorId))
                    material.SetColor(BaseColorId, originalBaseColors[i]);

                if (material.HasProperty(EmissionColorId))
                {
                    material.SetColor(EmissionColorId, originalEmissionColors[i]);
                    if (originalEmissionEnabled[i])
                        material.EnableKeyword("_EMISSION");
                    else
                        material.DisableKeyword("_EMISSION");
                }
            }

            SetBehaviourEnabled<VehicleDriftMarkEmitter>(true);
            SetBehaviourEnabled<VehicleTurnSignalController>(true);
            SetBehaviourEnabled<VehicleTaillightController>(true);
        }
    }
}
