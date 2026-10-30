using System.Collections.Generic;
using System.IO;
using Farm.Core;
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
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(SampleDataBuilder.DatabasePath);
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
                ("sickle", sickle), ("hammer", hammer), ("axe", axe), ("turnipSeeds", seeds), ("database", database));
            Wire(player, ("actions", actions), ("game", game));
            Wire(playerActions, ("actions", actions), ("game", game), ("player", player));
            Wire(follow, ("target", playerGo.transform));
            var timeController = farm.AddComponent<TimeController>();
            Wire(timeController, ("game", game), ("playerActions", playerActions));
            Wire(hud, ("game", game), ("playerActions", playerActions));

            var shopPanel = farm.AddComponent<ShopPanel>();
            Wire(shopPanel, ("actions", actions), ("game", game), ("player", player), ("playerActions", playerActions));

            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            MakeFirstScene();
        }

        // FarmView becomes the first enabled scene; the other scenes stay in the list, disabled.
        private static void MakeFirstScene()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var existing in EditorBuildSettings.scenes)
                if (existing.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(existing.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorApplication.ExecuteMenuItem("File/Save Project"); // writes ProjectSettings now
        }

        private static void CreateDataAssets()
        {
            SampleDataBuilder.Create();

            ConfigureTool(ToolType.Hoe, 2, 0);
            ConfigureTool(ToolType.WateringCan, 1, 30);
            ConfigureTool(ToolType.Sickle, 2, 0);
            ConfigureTool(ToolType.Hammer, 2, 0);
            ConfigureTool(ToolType.Axe, 2, 0);

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureTool(ToolType type, int staminaCost, int capacity)
        {
            var tool = EditorAssetUtil.LoadOrCreate<ToolDefinition>(ToolPath(type), out bool isNew);
            if (isNew) tool.Configure(type, staminaCost, capacity);
        }

        private static string ToolPath(ToolType type) => $"{ToolFolder}/{type}.asset";

        private static ToolDefinition LoadTool(ToolType type) =>
            AssetDatabase.LoadAssetAtPath<ToolDefinition>(ToolPath(type));

        private static void Wire(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
                so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
