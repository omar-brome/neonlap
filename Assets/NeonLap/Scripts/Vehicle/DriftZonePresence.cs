using System.Collections.Generic;
using NeonLap.Track;
using UnityEngine;

namespace NeonLap.Vehicle
{
    public class DriftZonePresence : MonoBehaviour
    {
        readonly Dictionary<DriftScoreZoneTrigger, float> activeZones = new();

        public float ActiveMultiplier { get; private set; } = 1f;
        public bool InDriftZone => ActiveMultiplier > 1.01f;

        public void EnterZone(DriftScoreZoneTrigger zone, float multiplier)
        {
            if (zone == null)
                return;

            activeZones[zone] = multiplier;
            Recalculate();
        }

        public void LeaveZone(DriftScoreZoneTrigger zone)
        {
            if (zone == null)
                return;

            activeZones.Remove(zone);
            Recalculate();
        }

        void Recalculate()
        {
            ActiveMultiplier = 1f;
            foreach (var pair in activeZones)
                ActiveMultiplier = Mathf.Max(ActiveMultiplier, pair.Value);
        }
    }
}
