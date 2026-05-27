using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.VFX
{
    public class PoliceRoofSirenVFX : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] float flashInterval = 0.11f;
        [SerializeField] float pulseSpeed = 5.5f;
        [SerializeField] float bobAmount = 0.06f;
        [SerializeField] float bobSpeed = 7f;

        Transform beaconRoot;
        Transform redLens;
        Transform blueLens;
        Material redMaterial;
        Material blueMaterial;
        Material housingMaterial;
        Vector3 anchorLocalPosition;
        float nextFlashTime;
        bool redActive = true;
        float phaseOffset;

        static readonly Color RedStrobe = new(1f, 0.15f, 0.1f);
        static readonly Color BlueStrobe = new(0.15f, 0.35f, 1f);

        public bool IsRedActive => redActive;

        public Color CurrentStrobeColor => redActive ? RedStrobe : BlueStrobe;

        void OnEnable() => PoliceSirenStrobeSync.Register(this);

        void OnDisable() => PoliceSirenStrobeSync.Unregister(this);

        public void Build(Transform visualRoot)
        {
            if (visualRoot == null)
                return;

            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            anchorLocalPosition = new Vector3(0f, 0.72f, -0.06f);

            beaconRoot = new GameObject("PoliceRoofSiren").transform;
            beaconRoot.SetParent(visualRoot, false);
            beaconRoot.localPosition = anchorLocalPosition;
            beaconRoot.localRotation = Quaternion.identity;

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            housingMaterial = CreateHousingMaterial(lit);
            redMaterial = CreateLensMaterial(lit, new Color(1f, 0.12f, 0.1f), new Color(3.8f, 0.2f, 0.15f));
            blueMaterial = CreateLensMaterial(lit, new Color(0.1f, 0.25f, 1f), new Color(0.2f, 0.55f, 3.8f));

            CreatePart(beaconRoot, "SirenBase", PrimitiveType.Cube, Vector3.zero, new Vector3(0.52f, 0.1f, 0.28f),
                housingMaterial);
            CreatePart(beaconRoot, "SirenHousing", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f),
                new Vector3(0.44f, 0.16f, 0.22f), housingMaterial);
            redLens = CreatePart(beaconRoot, "SirenRed", PrimitiveType.Cylinder, new Vector3(-0.14f, 0.28f, 0f),
                new Vector3(0.2f, 0.08f, 0.2f), redMaterial);
            blueLens = CreatePart(beaconRoot, "SirenBlue", PrimitiveType.Cylinder, new Vector3(0.14f, 0.28f, 0f),
                new Vector3(0.2f, 0.08f, 0.2f), blueMaterial);
            CreatePart(beaconRoot, "SirenCap", PrimitiveType.Cube, new Vector3(0f, 0.36f, 0f),
                new Vector3(0.18f, 0.05f, 0.14f), housingMaterial);

            ApplyFlashState();
        }

        void Update()
        {
            if (redMaterial == null || blueMaterial == null || beaconRoot == null)
                return;

            AnimateBeaconMotion();

            if (Time.time < nextFlashTime)
                return;

            nextFlashTime = Time.time + flashInterval;
            redActive = !redActive;
            ApplyFlashState();
        }

        void AnimateBeaconMotion()
        {
            var time = Time.time + phaseOffset;
            var bob = Mathf.Sin(time * bobSpeed) * bobAmount;
            var pulse = 1f + Mathf.Sin(time * pulseSpeed) * 0.08f;
            var yaw = Mathf.Sin(time * bobSpeed * 0.65f) * 6f;

            beaconRoot.localPosition = anchorLocalPosition + Vector3.up * bob;
            beaconRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            beaconRoot.localScale = Vector3.one * pulse;

            if (redLens != null)
            {
                var redScale = redActive ? 1.12f : 0.92f;
                redLens.localScale = new Vector3(0.2f * redScale, 0.08f * (redActive ? 1.2f : 0.85f), 0.2f * redScale);
            }

            if (blueLens != null)
            {
                var blueScale = redActive ? 0.92f : 1.12f;
                blueLens.localScale = new Vector3(0.2f * blueScale, 0.08f * (redActive ? 0.85f : 1.2f), 0.2f * blueScale);
            }
        }

        void ApplyFlashState()
        {
            SetLens(redMaterial, redActive, new Color(3.8f, 0.2f, 0.15f), new Color(0.45f, 0.06f, 0.06f));
            SetLens(blueMaterial, !redActive, new Color(0.2f, 0.55f, 3.8f), new Color(0.06f, 0.1f, 0.45f));
        }

        static void SetLens(Material material, bool active, Color activeEmission, Color inactiveEmission)
        {
            if (material == null)
                return;

            material.EnableKeyword("_EMISSION");
            var emission = active ? activeEmission : inactiveEmission;
            material.SetColor(EmissionColorId, emission);
            material.SetColor("_BaseColor", emission * 0.28f);
        }

        static Material CreateHousingMaterial(Shader lit)
        {
            var mat = new Material(lit) { name = "PoliceSirenHousing" };
            mat.SetColor("_BaseColor", new Color(0.06f, 0.07f, 0.1f));
            mat.SetFloat("_Metallic", 0.65f);
            mat.SetFloat("_Smoothness", 0.78f);
            return mat;
        }

        static Material CreateLensMaterial(Shader lit, Color baseColor, Color emission)
        {
            var mat = new Material(lit) { name = "PoliceSirenLens" };
            mat.SetColor("_BaseColor", baseColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(EmissionColorId, emission);
            return mat;
        }

        static Transform CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            if (type == PrimitiveType.Cylinder)
                part.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var collider = part.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return part.transform;
        }
    }
}
