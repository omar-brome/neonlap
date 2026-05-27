#if UNITY_EDITOR
using NeonLap.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NeonLap.Editor
{
    public static class NeonLapRenderingSetup
    {
        const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";

        [MenuItem("NeonLap/URP/Add Neon Edge Bloom Feature")]
        public static void AddNeonEdgeBloomFeature()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            if (rendererData == null)
            {
                Debug.LogError("NeonLap: Could not load renderer at " + PcRendererPath);
                return;
            }

            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is NeonTrackEdgeBloomFeature)
                {
                    Debug.Log("NeonLap: Neon edge bloom feature is already on PC_Renderer.");
                    return;
                }
            }

            var bloomFeature = ScriptableObject.CreateInstance<NeonTrackEdgeBloomFeature>();
            bloomFeature.name = "NeonTrackEdgeBloom";
            AssetDatabase.AddObjectToAsset(bloomFeature, rendererData);
            rendererData.rendererFeatures.Add(bloomFeature);
            rendererData.SetDirty();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            Debug.Log("NeonLap: Added NeonTrackEdgeBloomFeature to PC_Renderer.");
        }
    }
}
#endif
