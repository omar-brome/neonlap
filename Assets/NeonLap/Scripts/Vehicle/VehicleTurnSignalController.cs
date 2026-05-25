using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class VehicleTurnSignalController : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        static readonly Color SignalOffBase = new(0.18f, 0.12f, 0.02f, 1f);
        static readonly Color SignalOnBase = new(1f, 0.72f, 0.08f, 1f);
        static readonly Color SignalOffEmission = new(0.08f, 0.05f, 0.01f);
        static readonly Color SignalOnEmission = new(4.5f, 2.8f, 0.35f);

        [SerializeField] float steerThreshold = 0.12f;
        [SerializeField] float blinkInterval = 0.45f;

        readonly List<Material> leftMaterials = new();
        readonly List<Material> rightMaterials = new();
        readonly List<Light> leftLights = new();
        readonly List<Light> rightLights = new();

        VehicleController vehicleController;

        void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            CacheSignalRenderers();
            CreateSignalPointLights();
            SetSideActive(leftMaterials, leftLights, false);
            SetSideActive(rightMaterials, rightLights, false);
        }

        void Update()
        {
            if (vehicleController == null)
                return;

            var steer = vehicleController.SteerInput;
            var blinkOn = Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;
            var leftActive = steer < -steerThreshold && blinkOn;
            var rightActive = steer > steerThreshold && blinkOn;

            SetSideActive(leftMaterials, leftLights, leftActive);
            SetSideActive(rightMaterials, rightLights, rightActive);
        }

        public void Refresh()
        {
            foreach (var light in leftLights)
            {
                if (light != null)
                    Destroy(light.gameObject);
            }

            foreach (var light in rightLights)
            {
                if (light != null)
                    Destroy(light.gameObject);
            }

            leftLights.Clear();
            rightLights.Clear();
            leftMaterials.Clear();
            rightMaterials.Clear();
            CacheSignalRenderers();
            CreateSignalPointLights();
        }

        void CacheSignalRenderers()
        {
            leftMaterials.Clear();
            rightMaterials.Clear();

            var visual = transform.Find("Visual");
            if (visual == null)
                return;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                switch (renderer.name)
                {
                    case "TurnSignalFL":
                    case "TurnSignalRL":
                        AddMaterialInstance(renderer, leftMaterials);
                        break;
                    case "TurnSignalFR":
                    case "TurnSignalRR":
                        AddMaterialInstance(renderer, rightMaterials);
                        break;
                }
            }
        }

        static void AddMaterialInstance(Renderer renderer, List<Material> materials)
        {
            if (renderer == null)
                return;

            var instance = new Material(renderer.material);
            renderer.material = instance;
            materials.Add(instance);
        }

        void CreateSignalPointLights()
        {
            var visual = transform.Find("Visual");
            if (visual == null)
                return;

            CreatePointLight(visual, "TurnSignalFL", leftLights, new Vector3(-0.72f, 0.24f, 0.95f));
            CreatePointLight(visual, "TurnSignalFR", rightLights, new Vector3(0.72f, 0.24f, 0.95f));
            CreatePointLight(visual, "TurnSignalRL", leftLights, new Vector3(-0.62f, 0.26f, -1.32f));
            CreatePointLight(visual, "TurnSignalRR", rightLights, new Vector3(0.62f, 0.26f, -1.32f));
        }

        void CreatePointLight(Transform visual, string anchorName, List<Light> lights, Vector3 fallbackLocalPos)
        {
            var anchor = visual.Find(anchorName);
            var parent = anchor != null ? anchor : visual;

            var lightGo = new GameObject(anchorName + "_Blink");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = anchor != null ? new Vector3(0f, 0f, -0.03f) : fallbackLocalPos;

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.12f);
            light.range = 2.4f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            lights.Add(light);
        }

        static void SetSideActive(List<Material> materials, List<Light> lights, bool active)
        {
            var baseColor = active ? SignalOnBase : SignalOffBase;
            var emissionColor = active ? SignalOnEmission : SignalOffEmission;

            foreach (var material in materials)
            {
                if (material == null)
                    continue;

                material.SetColor(BaseColorId, baseColor);
                material.SetColor(EmissionColorId, emissionColor);
                material.EnableKeyword("_EMISSION");
            }

            var intensity = active ? 1.8f : 0f;
            foreach (var light in lights)
            {
                if (light != null)
                    light.intensity = intensity;
            }
        }
    }
}
