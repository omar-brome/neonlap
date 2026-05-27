using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Track
{
    public static class TrackSpecialZoneBuilder
    {
        public static void Build(Transform trackRoot, TrackLayout layout, IReadOnlyList<Vector3> centerline,
            float trackWidth, Material surfaceMaterial)
        {
            if (trackRoot == null || centerline == null || centerline.Count < 8)
                return;

            var zones = new List<TrackGameplayZone>();
            var normalized = TrackLayoutUtility.Normalize(layout);
            PopulateZones(zones, normalized, centerline, trackWidth);
            TrackGameplayZoneRegistry.Setup(trackRoot, centerline, zones);
            BuildVisuals(trackRoot, centerline, trackWidth, surfaceMaterial, zones, normalized);
            DriftScoreZoneVolumeBuilder.Build(trackRoot, centerline, trackWidth, zones);
        }

        static void PopulateZones(List<TrackGameplayZone> zones, TrackLayout layout, IReadOnlyList<Vector3> centerline,
            float trackWidth)
        {
            switch (layout)
            {
                case TrackLayout.Level6RidgeRun:
                    AddZoneByFraction(zones, centerline, TrackZoneType.AirCrest, 0.06f, 0.22f, 1f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.AirCrest, 0.88f, 0.98f, 1f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.WindGust, 0.04f, 0.2f, 1f,
                        GetTrackRightAtFraction(centerline, 0.12f));
                    AddZoneByFraction(zones, centerline, TrackZoneType.WindGust, 0.2f, 0.36f, 1.15f,
                        -GetTrackRightAtFraction(centerline, 0.28f));
                    AddZoneByFraction(zones, centerline, TrackZoneType.GravityWell, 0.52f, 0.62f, 1f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.Overdrive, 0.68f, 0.8f, 1f, Vector3.zero);
                    break;
                case TrackLayout.Level3MetroGauntlet:
                    AddZoneByFraction(zones, centerline, TrackZoneType.MetroTunnel, 0.58f, 0.86f, 1f, Vector3.zero,
                        aiHalfWidthScale: 0.62f);
                    AddZoneByFraction(zones, centerline, TrackZoneType.Overdrive, 0.04f, 0.16f, 1f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.GravityWell, 0.34f, 0.44f, 0.9f, Vector3.zero);
                    break;
                case TrackLayout.Level4ZigZagThunder:
                    AddZoneByFraction(zones, centerline, TrackZoneType.DriftMultiplier, 0.08f, 0.18f, 1.6f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.DriftMultiplier, 0.52f, 0.66f, 1.45f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.GravityWell, 0.78f, 0.86f, 1f, Vector3.zero);
                    break;
                case TrackLayout.Level7NeonCrossover:
                    AddZoneByFraction(zones, centerline, TrackZoneType.Overdrive, 0.48f, 0.56f, 1.2f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.WindGust, 0.12f, 0.24f, 1f,
                        GetTrackRightAtFraction(centerline, 0.18f));
                    AddZoneByFraction(zones, centerline, TrackZoneType.WindGust, 0.62f, 0.74f, 1f,
                        -GetTrackRightAtFraction(centerline, 0.68f));
                    AddZoneByFraction(zones, centerline, TrackZoneType.GravityWell, 0.86f, 0.94f, 1f, Vector3.zero);
                    break;
                default:
                    AddZoneByFraction(zones, centerline, TrackZoneType.Overdrive, 0.08f, 0.18f, 1f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.Overdrive, 0.55f, 0.66f, 0.95f, Vector3.zero);
                    AddZoneByFraction(zones, centerline, TrackZoneType.GravityWell, 0.78f, 0.86f, 1f, Vector3.zero);
                    break;
            }
        }

        static Vector3 GetTrackRightAtFraction(IReadOnlyList<Vector3> centerline, float fraction)
        {
            var index = TrackCenterlineSampler.IndexFromFraction(centerline, fraction);
            var point = centerline[index];
            return TrackCenterlineSampler.SampleClosest(centerline, point).Right;
        }

        static void AddZoneByFraction(List<TrackGameplayZone> zones, IReadOnlyList<Vector3> centerline,
            TrackZoneType type, float startFraction, float endFraction, float strength, Vector3 windDirection,
            float aiHalfWidthScale = 1f)
        {
            var startIndex = TrackCenterlineSampler.IndexFromFraction(centerline, startFraction);
            var endIndex = TrackCenterlineSampler.IndexFromFraction(centerline, endFraction);
            TrackCenterlineSampler.GetDistanceRange(centerline, startIndex, endIndex, out var startDistance,
                out var endDistance);

            zones.Add(new TrackGameplayZone
            {
                Type = type,
                StartDistance = startDistance,
                EndDistance = endDistance,
                Strength = strength,
                WindDirection = windDirection,
                AiTrackHalfWidthScale = aiHalfWidthScale,
            });
        }

        static void BuildVisuals(Transform trackRoot, IReadOnlyList<Vector3> centerline, float trackWidth,
            Material surfaceMaterial, List<TrackGameplayZone> zones, TrackLayout layout)
        {
            var root = new GameObject("SpecialZones").transform;
            root.SetParent(trackRoot, false);

            var overdriveMat = CreateZoneMaterial(new Color(1f, 0.15f, 0.85f), 2.2f);
            var gravityMat = CreateZoneMaterial(new Color(0.55f, 0.2f, 1f), 1.6f);
            var tunnelMat = CreateZoneMaterial(new Color(0.35f, 0.9f, 1f), 0.9f);
            var driftMat = CreateZoneMaterial(new Color(1f, 0.75f, 0.2f), 1.8f);

            foreach (var zone in zones)
            {
                var startIndex = FindIndexForDistance(centerline, zone.StartDistance);
                var endIndex = FindIndexForDistance(centerline, zone.EndDistance);
                switch (zone.Type)
                {
                    case TrackZoneType.Overdrive:
                        BuildStripedSection(root, centerline, startIndex, endIndex, trackWidth, overdriveMat, 0.22f);
                        break;
                    case TrackZoneType.GravityWell:
                        BuildStripedSection(root, centerline, startIndex, endIndex, trackWidth, gravityMat, 0.12f);
                        break;
                    case TrackZoneType.MetroTunnel:
                        BuildMetroTunnel(root, centerline, startIndex, endIndex, trackWidth, tunnelMat);
                        break;
                    case TrackZoneType.DriftMultiplier:
                        BuildStripedSection(root, centerline, startIndex, endIndex, trackWidth, driftMat, -0.18f);
                        break;
                }
            }
        }

        static int FindIndexForDistance(IReadOnlyList<Vector3> centerline, float targetDistance)
        {
            var accumulated = 0f;
            for (var i = 0; i < centerline.Count; i++)
            {
                var a = centerline[i];
                var b = centerline[(i + 1) % centerline.Count];
                var segmentLength = Vector3.Distance(a, b);
                if (accumulated + segmentLength >= targetDistance)
                    return i;
                accumulated += segmentLength;
            }

            return 0;
        }

        static void BuildStripedSection(Transform parent, IReadOnlyList<Vector3> centerline, int startIndex,
            int endIndex, float trackWidth, Material material, float lateralOffset)
        {
            if (material == null)
                return;

            var count = centerline.Count;
            var index = startIndex;
            var safety = 0;
            while (safety++ < count + 2)
            {
                var next = (index + 1) % count;
                var a = centerline[index];
                var b = centerline[next];
                BuildOverdriveStrip(parent, a, b, trackWidth, material, lateralOffset);
                if (index == endIndex)
                    break;
                index = next;
            }
        }

        static void BuildOverdriveStrip(Transform parent, Vector3 a, Vector3 b, float trackWidth, Material material,
            float lateralOffset)
        {
            var direction = b - a;
            direction.y = 0f;
            var length = direction.magnitude;
            if (length < 0.5f)
                return;

            direction /= length;
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var cursor = 0f;
            var stripe = 0;

            while (cursor < length)
            {
                var dashEnd = Mathf.Min(cursor + 2.4f, length);
                var mid = a + direction * ((cursor + dashEnd) * 0.5f) + right * (trackWidth * lateralOffset) +
                          Vector3.up * 0.11f;
                var rotation = Quaternion.LookRotation(direction, Vector3.up);
                var stripeWidth = stripe % 2 == 0 ? 0.42f : 0.28f;
                CreateMarkCube(parent, "OverdriveStripe", mid, rotation,
                    new Vector3(stripeWidth, 0.02f, dashEnd - cursor + 0.2f), material);
                cursor += 2.8f;
                stripe++;
            }
        }

        static void BuildMetroTunnel(Transform parent, IReadOnlyList<Vector3> centerline, int startIndex, int endIndex,
            float trackWidth, Material material)
        {
            var count = centerline.Count;
            var index = startIndex;
            var step = 0;
            var safety = 0;
            while (safety++ < count + 2)
            {
                if (step % 3 == 0)
                {
                    var point = centerline[index];
                    var next = centerline[(index + 1) % count];
                    var forward = next - point;
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 0.01f)
                    {
                        var rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                        var arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        arch.name = "MetroTunnelArch";
                        arch.transform.SetParent(parent, false);
                        arch.transform.SetPositionAndRotation(point + Vector3.up * 5.5f, rotation);
                        arch.transform.localScale = new Vector3(trackWidth + 4f, 0.55f, 1.2f);
                        Object.Destroy(arch.GetComponent<Collider>());
                        arch.GetComponent<Renderer>().sharedMaterial = material;
                    }
                }

                if (index == endIndex)
                    break;

                index = (index + 1) % count;
                step++;
            }

        }

        static void CreateMarkCube(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale,
            Material material)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = name;
            mark.transform.SetParent(parent, false);
            mark.transform.SetPositionAndRotation(position, rotation);
            mark.transform.localScale = scale;
            mark.layer = NeonLapLayers.Track;
            Object.Destroy(mark.GetComponent<Collider>());
            if (material != null)
                mark.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Material CreateZoneMaterial(Color color, float emissionIntensity)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color * 0.35f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionIntensity);
            mat.SetFloat("_Smoothness", 0.85f);
            return mat;
        }
    }
}
