using System.IO;
using UnityEditor;
using UnityEngine;

namespace Farm.EditorTools
{
    internal static class EditorAssetUtil
    {
        // Loads the asset, or creates it (and its folders) and marks it dirty so it is saved.
        public static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                EnsureFolder(Path.GetDirectoryName(path));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // Sets object references on a component's serialized fields by name.
        public static void Wire(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
                so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The asset path a scene's own generated asset (baked map, combined mesh) should use: the main scene keeps
        // basePath, any other scene (a copy made to try something) gets its name added, so the two never overwrite each other.
        public static string PerScenePath(string basePath, UnityEngine.SceneManagement.Scene scene, string mainScene = "MountainTerrain")
        {
            if (string.IsNullOrEmpty(scene.name) || scene.name == mainScene) return basePath;
            string ext = Path.GetExtension(basePath);
            return basePath.Substring(0, basePath.Length - ext.Length) + " (" + scene.name + ")" + ext;
        }

        // Creates the folder (and parents) through the AssetDatabase so Unity imports it with a
        // .meta file right away, instead of discovering a System.IO folder on the next refresh.
        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
