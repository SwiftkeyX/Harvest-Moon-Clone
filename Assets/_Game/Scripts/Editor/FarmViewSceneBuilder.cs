using System.IO;
using Farm.Farming;
using Farm.Items;
using Farm.Time;
using Farm.Tools;
using Farm.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools
{
    public static class FarmViewSceneBuilder
    {
        private const string CropPath = "Assets/_Game/Data/Crops/Turnip.asset";
        private const string ItemPath = "Assets/_Game/Data/Items/Turnip.asset";
        private const string SeedPath = "Assets/_Game/Data/Seeds/TurnipSeeds.asset";
        private const string ToolFolder = "Assets/_Game/Data/Tools";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private const string ScenePath = "Assets/_Game/Scenes/FarmView.unity";

        [MenuItem("Farm/Build Farm View Scene")]
        public static void Build()
        {
            CreateDataAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Opening a new scene unloads unused assets, which invalidates references loaded
            // above (the scene would save empty links). Load every asset again now.
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var seeds = AssetDatabase.LoadAssetAtPath<SeedDefinition>(SeedPath);
            var hoe = LoadTool(ToolType.Hoe);
            var can = LoadTool(ToolType.WateringCan);
            var sickle = LoadTool(ToolType.Sickle);
            var hammer = LoadTool(ToolType.Hammer);
            var axe = LoadTool(ToolType.Axe);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var farm = new GameObject("FarmView");
            var gridView = farm.AddComponent<FarmGridView>();
            var game = farm.AddComponent<FarmGame>();

            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGo.name = "Player";
            Object.DestroyImmediate(playerGo.GetComponent<Collider>());
            playerGo.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Vector3 start = FarmGame.CellToWorld(FarmGame.PlayerStartCell) + new Vector3(0f, 0.6f, 0f);
            playerGo.transform.position = start;
            var player = playerGo.AddComponent<PlayerController>();
            var playerActions = playerGo.AddComponent<PlayerActions>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = start + new Vector3(0f, 9.5f, -6.8f);
            camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            var follow = camGo.AddComponent<FollowCamera>();

            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<FarmHud>();

            Wire(game, ("actions", actions), ("gridView", gridView), ("hoe", hoe), ("wateringCan", can),
                ("sickle", sickle), ("hammer", hammer), ("axe", axe), ("turnipSeeds", seeds));
            Wire(player, ("actions", actions));
            Wire(playerActions, ("actions", actions), ("game", game), ("player", player));
            Wire(follow, ("target", playerGo.transform));
            Wire(hud, ("game", game), ("playerActions", playerActions));

            EnsureFolder(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static void CreateDataAssets()
        {
            var turnipItem = LoadOrCreate<ItemDefinition>(ItemPath, out bool newItem);
            if (newItem) turnipItem.Configure("turnip", 60);

            var turnip = LoadOrCreate<CropDefinition>(CropPath, out bool newCrop);
            if (newCrop)
                turnip.Configure("turnip", new[] { Season.Spring }, new[] { 1, 1, 1, 1 }, turnipItem);
            else if (turnip.HarvestItem == null)
                turnip.Configure(turnip.Id, turnip.Seasons, turnip.DaysPerStage, turnipItem);

            var seeds = LoadOrCreate<SeedDefinition>(SeedPath, out bool newSeeds);
            if (newSeeds) seeds.Configure("turnip-seeds", turnip, 20);

            ConfigureTool(ToolType.Hoe, 2, 0);
            ConfigureTool(ToolType.WateringCan, 1, 30);
            ConfigureTool(ToolType.Sickle, 2, 0);
            ConfigureTool(ToolType.Hammer, 2, 0);
            ConfigureTool(ToolType.Axe, 2, 0);

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureTool(ToolType type, int staminaCost, int capacity)
        {
            var tool = LoadOrCreate<ToolDefinition>(ToolPath(type), out bool isNew);
            if (isNew) tool.Configure(type, staminaCost, capacity);
        }

        private static string ToolPath(ToolType type) => $"{ToolFolder}/{type}.asset";

        private static ToolDefinition LoadTool(ToolType type) =>
            AssetDatabase.LoadAssetAtPath<ToolDefinition>(ToolPath(type));

        // Loads the asset, or creates it (and its folders) and marks it dirty so it is saved.
        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
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

        private static void Wire(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
                so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
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
