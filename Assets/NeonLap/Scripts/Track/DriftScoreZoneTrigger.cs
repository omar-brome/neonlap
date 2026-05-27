using NeonLap.Race;
using NeonLap.Vehicle;
using UnityEngine;

namespace NeonLap.Track
{
    public class DriftScoreZoneTrigger : MonoBehaviour
    {
        float scoreMultiplier = 1f;

        public void Configure(float multiplier)
        {
            scoreMultiplier = Mathf.Max(1f, multiplier);
        }

        void OnTriggerEnter(Collider other) => Handle(other, entered: true);

        void OnTriggerStay(Collider other) => Handle(other, entered: false);

        void OnTriggerExit(Collider other)
        {
            var presence = other.GetComponentInParent<DriftZonePresence>();
            presence?.LeaveZone(this);
        }

        void Handle(Collider other, bool entered)
        {
            var vehicle = other.GetComponentInParent<VehicleController>();
            if (vehicle == null || !vehicle.gameObject.activeInHierarchy)
                return;

            var presence = vehicle.GetComponent<DriftZonePresence>();
            if (presence == null)
                presence = vehicle.gameObject.AddComponent<DriftZonePresence>();

            presence.EnterZone(this, scoreMultiplier);

            if (!entered || !vehicle.IsDrifting)
                return;

            var score = vehicle.GetComponent<RaceScoreSystem>();
            score?.RegisterDriftZoneEntry(scoreMultiplier);
        }
    }
}
