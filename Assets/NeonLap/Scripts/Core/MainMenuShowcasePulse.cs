using UnityEngine;

namespace NeonLap.Core
{
    /// <summary>
    /// Pulses emissive track edge material in the menu showcase.
    /// </summary>
    public class MainMenuShowcasePulse : MonoBehaviour
    {
        Material edgeMaterial;
        Color baseEmission;
        float phase;

        public void Configure(Material material)
        {
            edgeMaterial = material;
            if (edgeMaterial == null)
                return;

            baseEmission = edgeMaterial.GetColor("_EmissionColor");
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            if (edgeMaterial == null)
                return;

            var pulse = 0.75f + Mathf.Sin(Time.unscaledTime * 1.35f + phase) * 0.25f;
            edgeMaterial.SetColor("_EmissionColor", baseEmission * pulse);
        }

        void OnDestroy()
        {
            if (edgeMaterial != null)
                edgeMaterial.SetColor("_EmissionColor", baseEmission);
        }
    }
}
