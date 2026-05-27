#if UNITY_EDITOR
using System.Collections.Generic;
using NeonLap.Track;
using UnityEditor;
using UnityEngine;

namespace NeonLap.Editor
{
    [CustomEditor(typeof(TrackDefinition))]
    public class TrackDefinitionEditor : UnityEditor.Editor
    {
        const float NodePickRadius = 1.8f;

        TrackDefinition track;
        TrackPathResult previewPath;
        bool paintHazards;
        int hoveredWaypointIndex = -1;

        void OnEnable()
        {
            track = (TrackDefinition)target;
            RebuildPreview();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Track preview", EditorStyles.boldLabel);

            if (GUILayout.Button("Rebuild Centerline Preview") || GUI.changed)
                RebuildPreview();

            if (previewPath != null)
            {
                EditorGUILayout.HelpBox(
                    $"Centerline points: {previewPath.Centerline.Count}  •  Shortcuts: {previewPath.Shortcuts.Count}",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Hazard paint (waypoint indices)", EditorStyles.boldLabel);

            paintHazards = EditorGUILayout.Toggle("Paint mode (Scene view)", paintHazards);
            track.useAuthoringHazardIndices = EditorGUILayout.Toggle("Use authored indices at runtime",
                track.useAuthoringHazardIndices);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-fill (procedural)"))
                AutoFillHazardIndices();
            if (GUILayout.Button("Clear painted"))
                ClearHazardIndices();
            EditorGUILayout.EndHorizontal();

            DrawHazardIndexList();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(track);
            }
        }

        void DrawHazardIndexList()
        {
            var indices = track.hazardWaypointIndices ?? System.Array.Empty<int>();
            EditorGUILayout.LabelField($"Painted indices ({indices.Length})",
                indices.Length == 0 ? "—" : string.Join(", ", indices));
        }

        void OnSceneGUI(SceneView view)
        {
            if (track == null || previewPath == null || previewPath.Centerline.Count < 2)
                return;

            var centerline = previewPath.Centerline;
            var waypointCount = centerline.Count;
            var painted = new HashSet<int>(track.hazardWaypointIndices ?? System.Array.Empty<int>());

            Handles.color = new Color(0.2f, 1f, 1f, 0.9f);
            for (var i = 0; i < centerline.Count; i++)
            {
                var next = centerline[(i + 1) % centerline.Count];
                Handles.DrawLine(centerline[i] + Vector3.up * 0.15f, next + Vector3.up * 0.15f);
            }

            foreach (var shortcut in previewPath.Shortcuts)
            {
                if (shortcut.Path == null || shortcut.Path.Count < 2)
                    continue;

                Handles.color = new Color(1f, 0.72f, 0.2f, 0.85f);
                for (var i = 0; i < shortcut.Path.Count - 1; i++)
                    Handles.DrawLine(shortcut.Path[i] + Vector3.up * 0.2f, shortcut.Path[i + 1] + Vector3.up * 0.2f);
            }

            hoveredWaypointIndex = -1;
            var pickRadius = HandleUtility.GetHandleSize(centerline[0]) * 0.22f + NodePickRadius * 0.05f;

            for (var i = 0; i < waypointCount; i++)
            {
                var point = centerline[i] + Vector3.up * 0.35f;
                var isPainted = painted.Contains(i);
                Handles.color = isPainted
                    ? new Color(1f, 0.35f, 0.45f, 0.95f)
                    : new Color(0.35f, 0.85f, 1f, paintHazards ? 0.55f : 0.25f);

                if (Handles.Button(point, Quaternion.identity, pickRadius, pickRadius, Handles.SphereHandleCap))
                {
                    if (paintHazards)
                        ToggleHazardIndex(i);
                    else
                        Selection.activeObject = track;
                }

                if (paintHazards && Vector3.Distance(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin,
                        point) < 0.01f)
                    hoveredWaypointIndex = i;
            }

            if (paintHazards)
            {
                Handles.BeginGUI();
                var rect = new Rect(12f, 12f, 360f, 48f);
                GUI.Box(rect, GUIContent.none);
                GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 36f),
                    "Hazard paint: click spheres to toggle waypoint indices.\nEsc exits paint mode.");
                Handles.EndGUI();

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    paintHazards = false;
                    Repaint();
                    Event.current.Use();
                }
            }

            if (paintHazards && hoveredWaypointIndex >= 0)
                Handles.Label(centerline[hoveredWaypointIndex] + Vector3.up * 2f, $"WP {hoveredWaypointIndex}");
        }

        void ToggleHazardIndex(int index)
        {
            var list = new List<int>(track.hazardWaypointIndices ?? System.Array.Empty<int>());
            if (list.Contains(index))
                list.Remove(index);
            else
                list.Add(index);

            list.Sort();
            track.hazardWaypointIndices = list.ToArray();
            track.useAuthoringHazardIndices = list.Count > 0;
            EditorUtility.SetDirty(track);
            Repaint();
        }

        void AutoFillHazardIndices()
        {
            var count = TrackAuthoringUtility.EstimateWaypointCount(track);
            var startSkip = Mathf.Max(28, count / 2);
            var density = track.hazardDensity switch
            {
                TrackHazardDensity.Low => 0.35f,
                TrackHazardDensity.High => 0.95f,
                _ => 0.65f,
            };

            var divisor = Mathf.Max(6, Mathf.RoundToInt(14f / Mathf.Max(density, 0.2f)));
            var step = Mathf.Max(4, count / divisor);
            var list = new List<int>();
            for (var i = startSkip; i < count - 2; i += step)
                list.Add(i);

            track.hazardWaypointIndices = list.ToArray();
            track.useAuthoringHazardIndices = list.Count > 0;
            EditorUtility.SetDirty(track);
        }

        void ClearHazardIndices()
        {
            track.hazardWaypointIndices = System.Array.Empty<int>();
            track.useAuthoringHazardIndices = false;
            EditorUtility.SetDirty(track);
        }

        void RebuildPreview()
        {
            previewPath = TrackAuthoringUtility.BuildPreviewPath(track);
            SceneView.RepaintAll();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawSelectedGizmo(TrackDefinition definition, GizmoType gizmoType)
        {
            if (definition == null)
                return;

            var path = TrackAuthoringUtility.BuildPreviewPath(definition);
            if (path.Centerline.Count < 2)
                return;

            Gizmos.color = new Color(0.2f, 1f, 1f, gizmoType == GizmoType.Selected ? 0.85f : 0.35f);
            for (var i = 0; i < path.Centerline.Count; i++)
            {
                var a = path.Centerline[i] + Vector3.up * 0.12f;
                var b = path.Centerline[(i + 1) % path.Centerline.Count] + Vector3.up * 0.12f;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
#endif
