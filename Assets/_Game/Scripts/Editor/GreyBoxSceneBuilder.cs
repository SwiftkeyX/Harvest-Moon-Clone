using System.IO;
using Farm.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools
{
    /// <summary>
    /// Builds an empty grey-box scene for practising level layout with ProBuilder: a 30×30 ground,
    /// a walking capsule at the game's speed, the game's camera angle, scale references and one
    /// example shelf with stairs. Press Play and walk with WASD.
    /// </summary>
    public static class GreyBoxSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/GreyBox.unity";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Farm/Build Grey-Box Scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Rebuild grey-box scene?",
                    "This replaces GreyBox.unity and everything you built in it.", "Replace", "Cancel"))
                return;

            // Build next to any scene with unsaved changes, then close, so nothing is thrown away.
            bool keepOpenScenes = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                keepOpenScenes |= SceneManager.GetSceneAt(i).isDirty;
            Scene previousActive = SceneManager.GetActiveScene();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                keepOpenScenes ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var level = new GameObject("Level").transform;
            Block(level, "Ground", new Vector3(-15f, -1f, -15f), new Vector3(30f, 1f, 30f), new Color(0.8f, 0.8f, 0.8f));

            var example = new GameObject("Example shelf (delete me)").transform;
            example.SetParent(level);
            Block(example, "Shelf", new Vector3(7f, 0f, 8f), new Vector3(6f, 1f, 4f), new Color(0.7f, 0.7f, 0.75f));
            Stairs(example, "Stairs", new Vector3(9f, 0f, 6f), new Vector3(2f, 1f, 2f), 5, new Color(0.7f, 0.7f, 0.75f));

            // Same sizes as the farm, so layouts are judged at the right scale.
            var references = new GameObject("Scale references").transform;
            Block(references, "House (3 x 2.4)", new Vector3(-13f, 0f, 9f), new Vector3(3f, 2f, 2.4f), new Color(0.9f, 0.6f, 0.55f));
            Block(references, "Farm plot (6 x 3 tiles)", new Vector3(-13f, 0f, 4f), new Vector3(6f, 0.1f, 3f), new Color(0.7f, 0.55f, 0.4f));
            Block(references, "Tree (1.4 wide, 2.3 tall)", new Vector3(-6f, 0f, 9f), new Vector3(1.4f, 2.3f, 1.4f), new Color(0.95f, 0.7f, 0.4f));

            Transform player = BuildPlayer(actions);
            BuildCamera(player);

            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            if (keepOpenScenes)
            {
                SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log($"Grey-box scene saved to {ScenePath}. It was closed because another open scene has unsaved changes.");
            }
        }

        private static Transform BuildPlayer(InputActionAsset actions)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var body = player.AddComponent<CharacterController>();
            body.height = 1.2f;
            body.radius = 0.3f;
            body.center = new Vector3(0f, 0.6f, 0f);
            body.stepOffset = 0.25f;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.transform.SetParent(player.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            capsule.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.85f, 0.3f);
            nose.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            var walker = player.AddComponent<GreyBoxWalker>();
            EditorAssetUtil.Wire(walker, ("actions", actions));
            return player.transform;
        }

        private static void BuildCamera(Transform player)
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            // Same offset and angle as the farm's follow camera.
            cam.transform.position = player.position + new Vector3(0f, 9.5f, -6.8f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            var follow = cam.AddComponent<FollowCamera>();
            EditorAssetUtil.Wire(follow, ("target", player));
        }

        private static ProBuilderMesh Block(Transform parent, string name, Vector3 corner, Vector3 size, Color color)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            return Finish(mesh, parent, name, corner + size * 0.5f, color);
        }

        private static ProBuilderMesh Stairs(Transform parent, string name, Vector3 corner, Vector3 size, int steps, Color color)
        {
            var mesh = ShapeGenerator.GenerateStair(PivotLocation.Center, size, steps, true);
            return Finish(mesh, parent, name, corner + size * 0.5f, color);
        }

        private static ProBuilderMesh Finish(ProBuilderMesh mesh, Transform parent, string name, Vector3 center, Color color)
        {
            mesh.gameObject.name = name;
            mesh.transform.SetParent(parent, false);
            mesh.transform.position = center;
            foreach (Face face in mesh.faces)
                mesh.SetFaceColor(face, color);
            mesh.ToMesh();
            mesh.Refresh();
            mesh.GetComponent<MeshRenderer>().sharedMaterial = BuiltinMaterials.defaultMaterial;
            mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            return mesh;
        }
    }
}
