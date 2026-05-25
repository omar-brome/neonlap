using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class VehicleTaillightController : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        static readonly Color TailBaseDim = new(0.42f, 0.03f, 0.03f, 1f);
        static readonly Color TailBaseBright = new(0.95f, 0.08f, 0.06f, 1f);
        static readonly Color TailEmissionDim = new(2.2f, 0.12f, 0.1f);
        static readonly Color TailEmissionBright = new(10f, 0.45f, 0.28f);

        static readonly Color BrakeBaseDim = new(0.18f, 0.02f, 0.02f, 1f);
        static readonly Color BrakeBaseBright = new(1f, 0.05f, 0.04f, 1f);
        static readonly Color BrakeEmissionDim = new(0.35f, 0.03f, 0.03f);
        static readonly Color BrakeEmissionBright = new(16f, 0.55f, 0.35f);

        readonly List<Material> tailMaterials = new();
        readonly List<Material> brakeMaterials = new();
        readonly List<Light> rearLights = new();

        VehicleController vehicleController;
        AIVehicleController aiController;
        float brakeBlend;

        void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            aiController = GetComponent<AIVehicleController>();
            CacheLightRenderers();
            CreateRearPointLights();
        }

        void Update()
        {
            var targetBlend = IsBraking() ? 1f : 0f;
            brakeBlend = Mathf.MoveTowards(brakeBlend, targetBlend, Time.deltaTime * (targetBlend > brakeBlend ? 14f : 8f));
            ApplyVisuals(brakeBlend);
        }

        public void Refresh()
        {
            foreach (var light in rearLights)
            {
                if (light != null)
                    Destroy(light.gameObject);
            }

            rearLights.Clear();
            tailMaterials.Clear();
            brakeMaterials.Clear();
            brakeBlend = 0f;
            CacheLightRenderers();
            CreateRearPointLights();
        }

        bool IsBraking()
        {
            if (vehicleController != null)
                return vehicleController.IsBraking;

            if (aiController != null)
                return aiController.IsBraking;

            return false;
        }

        void CacheLightRenderers()
        {
            tailMaterials.Clear();
            brakeMaterials.Clear();

            var visual = transform.Find("Visual");
            if (visual == null)
                return;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                switch (renderer.name)
                {
                    case "TailLightBar":
                    case "TailLightL":
                    case "TailLightR":
                        AddMaterialInstance(renderer, tailMaterials);
                        break;
                    case "BrakeLight":
                        AddMaterialInstance(renderer, brakeMaterials);
                        break;
                }
            }
        }

        static void AddMaterialInstance(Renderer renderer, List<Material> materials)
        {
            if (renderer == null)
                return;

            var source = renderer.material;
            var instance = new Material(source);
            renderer.material = instance;
            materials.Add(instance);
        }

        void CreateRearPointLights()
        {
            var visual = transform.Find("Visual");
            if (visual == null)
                return;

            CreatePointLight(visual, "TailLightL", new Vector3(-0.58f, 0.26f, -1.34f));
            CreatePointLight(visual, "TailLightR", new Vector3(0.58f, 0.26f, -1.34f));
        }

        void CreatePointLight(Transform visual, string anchorName, Vector3 localPosition)
        {
            var anchor = visual.Find(anchorName);
            var parent = anchor != null ? anchor : visual;

            var lightGo = new GameObject(anchorName + "_Glow");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = anchor != null ? new Vector3(0f, 0f, -0.04f) : localPosition;

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.12f, 0.08f);
            light.range = 3.2f;
            light.intensity = 0.45f;
            light.shadows = LightShadows.None;
            rearLights.Add(light);
        }

        void ApplyVisuals(float blend)
        {
            SetMaterialGroup(tailMaterials, TailBaseDim, TailBaseBright, TailEmissionDim, TailEmissionBright, blend);
            SetMaterialGroup(brakeMaterials, BrakeBaseDim, BrakeBaseBright, BrakeEmissionDim, BrakeEmissionBright,
                blend);

            var lightIntensity = Mathf.Lerp(0.45f, 2.8f, blend);
            foreach (var light in rearLights)
            {
                if (light != null)
                    light.intensity = lightIntensity;
            }
        }

        static void SetMaterialGroup(List<Material> materials, Color baseDim, Color baseBright, Color emissionDim,
            Color emissionBright, float blend)
        {
            var baseColor = Color.Lerp(baseDim, baseBright, blend);
            var emissionColor = Color.Lerp(emissionDim, emissionBright, blend);

            foreach (var material in materials)
            {
                if (material == null)
                    continue;

                material.SetColor(BaseColorId, baseColor);
                material.SetColor(EmissionColorId, emissionColor);
                material.EnableKeyword("_EMISSION");
            }
        }
    }
}
