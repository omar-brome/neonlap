using UnityEngine;

namespace NeonLap.Vehicle
{
    public class DetachableVehiclePart : MonoBehaviour
    {
        [SerializeField] float mass = 4f;
        [SerializeField] float breakThreshold = 1f;

        public float Mass => mass;
        public float BreakThreshold => breakThreshold;
        public bool IsAttached { get; private set; } = true;

        public void Configure(float partMass, float threshold = 1f)
        {
            mass = partMass;
            breakThreshold = threshold;
        }

        public void MarkDetached()
        {
            IsAttached = false;
        }

        public void MarkAttached()
        {
            IsAttached = true;
        }

        public static float EstimateMass(Transform partTransform)
        {
            return Mathf.Clamp(partTransform.localScale.magnitude * 5.5f, 1.2f, 16f);
        }

        public static float EstimateBreakThreshold(string partName)
        {
            if (partName.Contains("Mirror") || partName.Contains("Antenna") || partName.Contains("Badge"))
                return 0.55f;
            if (partName.Contains("Headlight") || partName.Contains("TailLight") || partName.Contains("ReverseLight")
                || partName.Contains("TurnSignal"))
                return 0.65f;
            if (partName.Contains("Spoiler") || partName.Contains("Gurney") || partName.Contains("DivePlane"))
                return 0.75f;
            if (partName.Contains("Splitter") || partName.Contains("Stripe") || partName.Contains("Underglow"))
                return 0.8f;
            if (partName.Contains("Canopy") || partName.Contains("Window") || partName.Contains("Fender"))
                return 0.95f;
            if (partName.Contains("Pod") || partName.Contains("Intake") || partName.Contains("Vent"))
                return 0.85f;
            return 0.9f;
        }
    }
}
