using System.IO;
using Farm.Farming;
using Farm.Items;
using Farm.Time;
using Farm.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools
{
    public static class FarmViewSceneBuilder
    {
        private const string CropPath = "Assets/_Game/Data/Crops/Turnip.asset";
        private const string ItemPath = "Assets/_Game/Data/Items/Turnip.asset";
        private const string ScenePath = "Assets/_Game/Scenes/FarmView.unity";

        [MenuItem("Farm/Build Farm View Scene")]
        public static void Build()
        {
            var turnipItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemPath);
            if (turnipItem == null)
            {
                EnsureFolder(Path.GetDirectoryName(ItemPath));
                turnipItem = ScriptableObject.CreateInstance<ItemDefinition>();
                turnipItem.Configure("turnip", 60);
                AssetDatabase.CreateAsset(turnipItem, ItemPath);
            }

            var turnip = AssetDatabase.LoadAssetAtPath<CropDefinition>(CropPath);
            if (turnip == null)
            {
                EnsureFolder(Path.GetDirectoryName(CropPath));
                turnip = ScriptableObject.CreateInstance<CropDefinition>();
                turnip.Configure("turnip", new[] { Season.Spring }, new[] { 1, 1, 1, 1 }, turnipItem);
                AssetDatabase.CreateAsset(turnip, CropPath);
            }
            else if (turnip.HarvestItem == null)
            {
                turnip.Configure(turnip.Id, turnip.Seasons, turnip.DaysPerStage, turnipItem);
            }
            EditorUtility.SetDirty(turnipItem);
            EditorUtility.SetDirty(turnip);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Opening a new scene unloads unused assets, which invalidates the reference loaded
            // above (the scene would save an empty link). Load it again now.
            turnip = AssetDatabase.LoadAssetAtPath<CropDefinition>(CropPath);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 9.5f, -6.8f);
            camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            var farm = new GameObject("FarmView");
            var gridView = farm.AddComponent<FarmGridView>();
            var controller = farm.AddComponent<FarmViewController>();
            var so = new SerializedObject(controller);
            so.FindProperty("turnip").objectReferenceValue = turnip;
            so.FindProperty("gridView").objectReferenceValue = gridView;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
        }

        // Creates the folder (and parents) through the AssetDatabase so Unity imports it with a
        // .meta file right away, instead of discovering a System.IO folder on the next refresh.
        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
