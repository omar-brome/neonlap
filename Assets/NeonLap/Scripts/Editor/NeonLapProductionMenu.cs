#if UNITY_EDITOR
using NeonLap.Core.Content;
using UnityEditor;
using UnityEngine;

namespace NeonLap.Editor
{
    public static class NeonLapProductionMenu
    {
        const string CatalogPath = "Assets/NeonLap/Resources/NeonLap/NeonLapContentCatalog.asset";
        const string PlayerPrefabPath = "Assets/NeonLap/Prefabs/HoverCar_Player.prefab";
        const string AiPrefabPath = "Assets/NeonLap/Prefabs/HoverCar_AI.prefab";

        [MenuItem("NeonLap/Production/Create Content Catalog")]
        public static void CreateContentCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NeonLapContentCatalog>(CatalogPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                Debug.Log("NeonLap content catalog already exists.", existing);
                return;
            }

            EnsureFolder("Assets/NeonLap/Resources/NeonLap");
            var catalog = ScriptableObject.CreateInstance<NeonLapContentCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            Debug.Log($"Created {CatalogPath}. Assign prefabs when ready.", catalog);
        }

        [MenuItem("NeonLap/Production/Assign Selected Prefabs To Catalog")]
        public static void AssignSelectedPrefabsToCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<NeonLapContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning("Create the content catalog first (NeonLap/Production/Create Content Catalog).");
                return;
            }

            foreach (var obj in Selection.objects)
            {
                if (obj is not GameObject go)
                    continue;

                var path = AssetDatabase.GetAssetPath(go);
                if (!path.EndsWith(".prefab"))
                    continue;

                var root = go.GetComponent<NeonLapCarPrefabRoot>();
                if (root != null && root.IsPlayerTemplate)
                    catalog.PlayerCarPrefab = go;
                else
                    catalog.AiRivalCarPrefab = go;
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Updated catalog prefab references.", catalog);
        }

        [MenuItem("NeonLap/Production/Mark Selection As Player Car Prefab")]
        public static void MarkSelectionAsPlayerPrefab()
        {
            MarkSelectionTemplate(player: true);
        }

        [MenuItem("NeonLap/Production/Mark Selection As AI Car Prefab")]
        public static void MarkSelectionAsAiPrefab()
        {
            MarkSelectionTemplate(player: false);
        }

        static void MarkSelectionTemplate(bool player)
        {
            foreach (var obj in Selection.gameObjects)
            {
                var root = obj.GetComponent<NeonLapCarPrefabRoot>();
                if (root == null)
                    root = obj.AddComponent<NeonLapCarPrefabRoot>();
                root.SetTemplate(player);
                EditorUtility.SetDirty(obj);
            }
        }

        [MenuItem("NeonLap/Production/Open Prefab Folder")]
        public static void OpenPrefabFolder()
        {
            EnsureFolder("Assets/NeonLap/Prefabs");
            var folder = AssetDatabase.LoadAssetAtPath<Object>("Assets/NeonLap/Prefabs");
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
