using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.VFX
{
  public class DriftTrailVFX : MonoBehaviour
  {
    [SerializeField] VehicleController vehicle;
    [SerializeField] TrailRenderer leftTrail;
    [SerializeField] TrailRenderer rightTrail;

    void Awake()
    {
      if (vehicle == null)
        vehicle = GetComponentInParent<VehicleController>();
    }

    void Update()
    {
      var drifting = vehicle != null && vehicle.IsDrifting;
      SetTrail(leftTrail, drifting);
      SetTrail(rightTrail, drifting);
    }

    static void SetTrail(TrailRenderer trail, bool emit)
    {
      if (trail == null)
        return;

      trail.emitting = emit;
    }
  }
}
