using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Track
{
    public class TrackGameplayZoneRegistry : MonoBehaviour
    {
        static TrackGameplayZoneRegistry instance;

        readonly List<TrackGameplayZone> zones = new();
        IReadOnlyList<Vector3> centerline;
        float loopLength;

        public static TrackGameplayZoneRegistry Instance => instance;

        public float LoopLength => loopLength;

        public static TrackGameplayZoneRegistry Setup(Transform parent, IReadOnlyList<Vector3> centerline,
            IReadOnlyList<TrackGameplayZone> zoneList)
        {
            if (instance != null)
                Destroy(instance.gameObject);

            var go = new GameObject("TrackGameplayZones");
            go.transform.SetParent(parent, false);
            var registry = go.AddComponent<TrackGameplayZoneRegistry>();
            registry.Configure(centerline, zoneList);
            instance = registry;
            return registry;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void Configure(IReadOnlyList<Vector3> centerlinePoints, IReadOnlyList<TrackGameplayZone> zoneList)
        {
            centerline = centerlinePoints;
            zones.Clear();
            loopLength = TrackCenterlineSampler.ComputeLoopLength(centerline);
            if (zoneList != null)
                zones.AddRange(zoneList);
        }

        public void Query(Vector3 worldPosition, ref TrackZoneQueryResult result)
        {
            result.Reset();
            if (zones.Count == 0 || loopLength <= 0.01f || centerline == null || centerline.Count < 2)
                return;

            var sample = TrackCenterlineSampler.SampleClosest(centerline, worldPosition);
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (!TrackCenterlineSampler.IsDistanceInside(sample.DistanceAlong, zone.StartDistance, zone.EndDistance,
                        loopLength))
                    continue;

                result.Apply(zone, sample.Right);
            }
        }

        public IReadOnlyList<TrackGameplayZone> Zones => zones;
    }

    public struct TrackZoneQueryResult
    {
        public bool InOverdrive;
        public bool InGravityWell;
        public bool InWindGust;
        public bool InMetroTunnel;
        public bool InDriftMultiplier;
        public bool InAirCrest;
        public float OverdriveStrength;
        public float GravityStrength;
        public float WindStrength;
        public float DriftScoreMultiplier;
        public Vector3 WindDirection;
        public float AiTrackHalfWidthScale;

        public void Reset()
        {
            InOverdrive = false;
            InGravityWell = false;
            InWindGust = false;
            InMetroTunnel = false;
            InDriftMultiplier = false;
            InAirCrest = false;
            OverdriveStrength = 0f;
            GravityStrength = 0f;
            WindStrength = 0f;
            DriftScoreMultiplier = 1f;
            WindDirection = Vector3.right;
            AiTrackHalfWidthScale = 1f;
        }

        public void Apply(TrackGameplayZone zone, Vector3 trackRight)
        {
            switch (zone.Type)
            {
                case TrackZoneType.Overdrive:
                    InOverdrive = true;
                    OverdriveStrength = Mathf.Max(OverdriveStrength, zone.Strength);
                    break;
                case TrackZoneType.GravityWell:
                    InGravityWell = true;
                    GravityStrength = Mathf.Max(GravityStrength, zone.Strength);
                    break;
                case TrackZoneType.WindGust:
                    InWindGust = true;
                    WindStrength = Mathf.Max(WindStrength, zone.Strength);
                    WindDirection = zone.WindDirection.sqrMagnitude > 0.01f ? zone.WindDirection : trackRight;
                    break;
                case TrackZoneType.MetroTunnel:
                    InMetroTunnel = true;
                    AiTrackHalfWidthScale = Mathf.Min(AiTrackHalfWidthScale, zone.AiTrackHalfWidthScale);
                    break;
                case TrackZoneType.DriftMultiplier:
                    InDriftMultiplier = true;
                    DriftScoreMultiplier = Mathf.Max(DriftScoreMultiplier, zone.Strength);
                    break;
                case TrackZoneType.AirCrest:
                    InAirCrest = true;
                    break;
            }
        }
    }
}
