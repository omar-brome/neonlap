using UnityEngine;

namespace NeonLap.VFX
{
    public class PoliceLightBarVFX : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] float flashInterval = 0.18f;

        Renderer leftLight;
        Renderer rightLight;
        Material leftMaterial;
        Material rightMaterial;
        float nextFlashTime;
        bool leftActive = true;

        public void Configure(Transform visualRoot)
        {
            if (visualRoot == null)
                return;

            var left = visualRoot.Find("PoliceLightL");
            var right = visualRoot.Find("PoliceLightR");
            if (left != null)
            {
                leftLight = left.GetComponent<Renderer>();
                if (leftLight != null)
                    leftMaterial = leftLight.material;
            }

            if (right != null)
            {
                rightLight = right.GetComponent<Renderer>();
                if (rightLight != null)
                    rightMaterial = rightLight.material;
            }

            ApplyFlashState();
        }

        void Update()
        {
            if (leftMaterial == null || rightMaterial == null)
                return;

            if (Time.time < nextFlashTime)
                return;

            nextFlashTime = Time.time + flashInterval;
            leftActive = !leftActive;
            ApplyFlashState();
        }

        void ApplyFlashState()
        {
            SetLight(leftMaterial, leftActive, new Color(4.5f, 0.15f, 0.15f), new Color(0.35f, 0.04f, 0.04f));
            SetLight(rightMaterial, !leftActive, new Color(0.15f, 0.35f, 4.5f), new Color(0.04f, 0.08f, 0.35f));
        }

        static void SetLight(Material material, bool active, Color activeEmission, Color inactiveEmission)
        {
            if (material == null)
                return;

            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, active ? activeEmission : inactiveEmission);
            material.SetColor("_BaseColor", active ? activeEmission * 0.22f : inactiveEmission * 0.22f);
        }
    }
}
