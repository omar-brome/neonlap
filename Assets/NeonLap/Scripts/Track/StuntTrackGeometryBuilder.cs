using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;

namespace NeonLap.Track
{
    public static class StuntTrackGeometryBuilder
    {
        public static void Build(Transform trackRoot, IReadOnlyList<Vector3> centerline, float trackWidth, Material accent)
        {
            if (trackRoot == null || centerline == null || centerline.Count < 4)
                return;

            var props = new GameObject("StuntProps").transform;
            props.SetParent(trackRoot, false);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            accent ??= new Material(lit);
            var railMat = new Material(accent);
            railMat.SetColor("_BaseColor", new Color(0.2f, 0.95f, 1f));
            railMat.EnableKeyword("_EMISSION");
            railMat.SetColor("_EmissionColor", new Color(0.15f, 1.2f, 1.5f));

            var rampMat = new Material(accent);
            rampMat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f));

            BuildLaunchRamps(props, centerline, trackWidth, rampMat, railMat);
            BuildLoopRing(props, centerline, trackWidth, railMat);
            BuildHalfPipeRails(props, centerline, trackWidth, railMat);
            BuildFreestylePads(props, centerline, trackWidth, railMat);
        }

        static void BuildLaunchRamps(Transform parent, IReadOnlyList<Vector3> centerline, float trackWidth,
            Material rampMat, Material railMat)
        {
            for (var i = 2; i < centerline.Count - 2; i++)
            {
                var a = centerline[i - 1];
                var b = centerline[i];
                var c = centerline[i + 1];
                var rise = b.y - a.y;
                var drop = b.y - c.y;
                if (rise < 6f || drop < 4f)
                    continue;

                var forward = (c - a);
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f)
                    continue;
                forward.Normalize();
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                var basePos = b + Vector3.down * 0.2f;

                CreateWedge(parent, "LaunchRamp_" + i, basePos, forward, right, trackWidth * 1.1f, 8f, 3.5f, rampMat);
                CreateRail(parent, "RampRailL_" + i, basePos + right * (trackWidth * 0.55f), forward, 10f, railMat);
                CreateRail(parent, "RampRailR_" + i, basePos - right * (trackWidth * 0.55f), forward, 10f, railMat);
            }
        }

        static void BuildLoopRing(Transform parent, IReadOnlyList<Vector3> centerline, float trackWidth, Material railMat)
        {
            var bestIndex = -1;
            var bestHeight = 0f;
            for (var i = 0; i < centerline.Count; i++)
            {
                if (centerline[i].y > bestHeight)
                {
                    bestHeight = centerline[i].y;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0 || bestHeight < 8f)
                return;

            var peak = centerline[bestIndex];
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "LoopRing";
            ring.transform.SetParent(parent, false);
            ring.transform.position = peak + Vector3.up * 2f;
            ring.transform.localScale = new Vector3(trackWidth * 1.35f, 0.35f, trackWidth * 1.35f);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<Renderer>().sharedMaterial = railMat;

            for (var i = 0; i < 8; i++)
            {
                var angle = i * 45f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (trackWidth * 0.75f);
                CreatePillar(parent, "LoopPillar_" + i, peak + offset, bestHeight + 4f, railMat);
            }
        }

        static void BuildHalfPipeRails(Transform parent, IReadOnlyList<Vector3> centerline, float trackWidth,
            Material railMat)
        {
            var pipeRuns = 0;
            for (var i = 1; i < centerline.Count - 1; i++)
            {
                var prev = centerline[i - 1];
                var current = centerline[i];
                var next = centerline[i + 1];
                var lateral = Vector3.Distance(new Vector3(prev.x, 0f, prev.z), new Vector3(next.x, 0f, next.z));
                var verticalSwing = Mathf.Abs(current.y - prev.y) + Mathf.Abs(next.y - current.y);
                if (lateral < 18f || verticalSwing < 8f)
                    continue;

                pipeRuns++;
                if (pipeRuns > 2)
                    break;

                var forward = (next - prev);
                forward.y = 0f;
                forward.Normalize();
                var right = Vector3.Cross(Vector3.up, forward).normalized;
                var lipHeight = Mathf.Max(current.y, prev.y, next.y) + 2f;

                CreateRail(parent, "PipeRailL_" + pipeRuns, current + right * (trackWidth * 0.62f) + Vector3.up * lipHeight,
                    forward, lateral * 0.85f, railMat);
                CreateRail(parent, "PipeRailR_" + pipeRuns, current - right * (trackWidth * 0.62f) + Vector3.up * lipHeight,
                    forward, lateral * 0.85f, railMat);
            }
        }

        static void BuildFreestylePads(Transform parent, IReadOnlyList<Vector3> centerline, float trackWidth,
            Material railMat)
        {
            var start = centerline[0];
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "FreestylePad";
            pad.transform.SetParent(parent, false);
            pad.transform.position = new Vector3(start.x, -0.05f, start.z);
            pad.transform.localScale = new Vector3(trackWidth * 2.2f, 0.12f, trackWidth * 2.2f);
            Object.Destroy(pad.GetComponent<Collider>());
            var padMat = new Material(railMat);
            padMat.SetColor("_BaseColor", new Color(0.08f, 0.1f, 0.14f));
            padMat.SetColor("_EmissionColor", new Color(0.05f, 0.35f, 0.45f));
            pad.GetComponent<Renderer>().sharedMaterial = padMat;
        }

        static void CreateWedge(Transform parent, string name, Vector3 basePos, Vector3 forward, Vector3 right,
            float width, float depth, float height, Material mat)
        {
            var wedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wedge.name = name;
            wedge.transform.SetParent(parent, false);
            wedge.transform.position = basePos + forward * (depth * 0.35f) + Vector3.up * (height * 0.35f);
            wedge.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            wedge.transform.localScale = new Vector3(width, height, depth);
            wedge.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(wedge.GetComponent<Collider>());
        }

        static void CreateRail(Transform parent, string name, Vector3 position, Vector3 forward, float length, Material mat)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent, false);
            rail.transform.position = position + forward * (length * 0.5f);
            rail.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            rail.transform.localScale = new Vector3(0.25f, 1.2f, length);
            rail.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(rail.GetComponent<Collider>());
        }

        static void CreatePillar(Transform parent, string name, Vector3 basePos, float height, Material mat)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = name;
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = basePos + Vector3.up * (height * 0.5f);
            pillar.transform.localScale = new Vector3(0.45f, height * 0.5f, 0.45f);
            pillar.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(pillar.GetComponent<Collider>());
        }
    }
}
