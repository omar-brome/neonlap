using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public struct GroundProbeResult
    {
        public bool IsGrounded;
        public Vector3 GroundNormal;
        public float Distance;
        public Vector3 AveragePoint;
    }

    public class VehicleGroundProbe : MonoBehaviour
    {
        [SerializeField] float rayLength = 4f;

        public void SetRayLength(float length)
        {
            rayLength = Mathf.Max(2f, length);
        }
        [SerializeField] Vector3[] localProbeOffsets =
        {
            Vector3.zero,
            new(0.8f, 0f, 1.2f),
            new(-0.8f, 0f, 1.2f),
            new(0.8f, 0f, -1.2f),
            new(-0.8f, 0f, -1.2f)
        };

        public GroundProbeResult Probe()
        {
            var result = new GroundProbeResult();
            var sumNormal = Vector3.zero;
            var sumPoint = Vector3.zero;
            var hits = 0;
            var minDistance = float.MaxValue;

            foreach (var offset in localProbeOffsets)
            {
                var origin = transform.TransformPoint(offset + Vector3.up * 0.5f);
                if (!Physics.Raycast(origin, Vector3.down, out var hit, rayLength, NeonLapLayers.TrackMask,
                        QueryTriggerInteraction.Ignore))
                    continue;

                hits++;
                sumNormal += hit.normal;
                sumPoint += hit.point;
                minDistance = Mathf.Min(minDistance, hit.distance - 0.5f);
            }

            result.IsGrounded = hits > 0;
            if (!result.IsGrounded)
                return result;

            result.GroundNormal = (sumNormal / hits).normalized;
            result.AveragePoint = sumPoint / hits;
            result.Distance = minDistance;
            return result;
        }
    }
}
