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
