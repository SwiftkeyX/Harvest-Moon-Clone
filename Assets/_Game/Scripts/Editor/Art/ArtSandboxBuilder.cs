using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Builds the art test scene: a small farm made of low-poly shapes with toon materials,
    /// seen from the same camera angle as the game. It doesn't touch the game scene.
    /// </summary>
    public static partial class ArtSandboxBuilder
    {
        private const string ArtRoot = "Assets/_Game/Art";
        private const string ScenePath = "Assets/_Game/Scenes/ArtSandbox.unity";
        private const string ShaderName = "Farm/Toon Lit";

        // The palette: warm, bright colours with cool, purple-blue shadows.
        private static class Palette
        {
            public static readonly Color Grass = new Color(0.45f, 0.74f, 0.34f);
            public static readonly Color GrassDark = new Color(0.37f, 0.64f, 0.30f);
            public static readonly Color GrassLight = new Color(0.53f, 0.80f, 0.38f);
            public static readonly Color WoodDark = new Color(0.55f, 0.36f, 0.22f);
            public static readonly Color BushGreen = new Color(0.26f, 0.55f, 0.30f);
            public static readonly Color BushOrange = new Color(0.92f, 0.45f, 0.20f);
            public static readonly Color FlowerWhite = new Color(0.98f, 0.97f, 0.94f);
            public static readonly Color Soil = new Color(0.70f, 0.45f, 0.28f);
            public static readonly Color SoilWet = new Color(0.43f, 0.27f, 0.18f);
            public static readonly Color Sand = new Color(0.95f, 0.84f, 0.60f);
            public static readonly Color Wood = new Color(0.78f, 0.52f, 0.32f);
            public static readonly Color Trunk = new Color(0.48f, 0.32f, 0.24f);
            public static readonly Color LeafOrange = new Color(1.00f, 0.58f, 0.20f);
            public static readonly Color LeafRed = new Color(0.90f, 0.30f, 0.20f);
            public static readonly Color LeafYellow = new Color(1.00f, 0.80f, 0.28f);
            public static readonly Color Pine = new Color(0.18f, 0.52f, 0.38f);
            public static readonly Color Rock = new Color(0.66f, 0.66f, 0.72f);
            public static readonly Color Water = new Color(0.36f, 0.72f, 0.92f);
            public static readonly Color Wall = new Color(0.98f, 0.91f, 0.76f);
            public static readonly Color Roof = new Color(0.88f, 0.32f, 0.27f);
            public static readonly Color Stone = new Color(0.62f, 0.60f, 0.62f);
            public static readonly Color Window = new Color(0.55f, 0.82f, 0.95f);
            public static readonly Color BinBlue = new Color(0.30f, 0.55f, 0.85f);
            public static readonly Color Skin = new Color(1.00f, 0.80f, 0.64f);
            public static readonly Color Shirt = new Color(0.22f, 0.72f, 0.66f);
            public static readonly Color Pants = new Color(0.32f, 0.42f, 0.72f);
            public static readonly Color Straw = new Color(1.00f, 0.86f, 0.48f);
            public static readonly Color Ink = new Color(0.08f, 0.06f, 0.08f);
            public static readonly Color CropLeaf = new Color(0.42f, 0.78f, 0.30f);
            public static readonly Color TurnipWhite = new Color(0.98f, 0.96f, 0.92f);
            public static readonly Color TurnipPurple = new Color(0.78f, 0.38f, 0.72f);
            public static readonly Color Withered = new Color(0.60f, 0.48f, 0.30f);
            public static readonly Color Seed = new Color(0.40f, 0.28f, 0.18f);
            public static readonly Color FlowerPink = new Color(1.00f, 0.55f, 0.75f);
            public static readonly Color FlowerYellow = new Color(1.00f, 0.88f, 0.30f);
            public static readonly Color ShadowTint = new Color(0.58f, 0.56f, 0.82f);
            public static readonly Color Sky = new Color(0.62f, 0.82f, 0.98f);
        }

        private static Transform root;

        [MenuItem("Farm/Build Art Sandbox Scene")]
        public static void Build()
        {
            // If an open scene has unsaved changes, build next to it and close afterwards,
            // so those changes are never thrown away.
            bool keepOpenScenes = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                keepOpenScenes |= SceneManager.GetSceneAt(i).isDirty;
            Scene previousActive = SceneManager.GetActiveScene();

            // Make the scene first: opening a new scene unloads assets loaded before it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                keepOpenScenes ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene); // lighting settings belong to the active scene
            root = new GameObject("ArtSandbox").transform;

            BuildLightingAndCamera();
            BuildGround();
            BuildCropRow();
            BuildPlayer(new Vector3(-3.8f, 0f, -0.6f));
            BuildHouse(new Vector3(-6.2f, 0f, 3.6f));
            BuildShippingBin(new Vector3(-3.7f, 0f, 2.9f));
            BuildFence();
            BuildNature();
            BuildPlotBorder();
            BuildPatches();
            BuildDressing();

            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (keepOpenScenes)
            {
                SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log($"Art sandbox saved to {ScenePath}. It was closed because another open scene has unsaved changes.");
            }
        }

        // ---------- Lighting, camera, post-processing ----------

        private static void BuildLightingAndCamera()
        {
            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(root);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.85f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.80f, 0.75f);
            RenderSettings.ambientGroundColor = new Color(0.50f, 0.45f, 0.40f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            // Same height and angle as the game's follow camera.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(root);
            camGo.transform.position = new Vector3(0.3f, 9.5f, -6.6f);
            camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;

            var volumeGo = new GameObject("Post Processing");
            volumeGo.transform.SetParent(root);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = BuildVolumeProfile();
        }

        private static VolumeProfile BuildVolumeProfile()
        {
            string path = ArtRoot + "/ArtSandboxVolume.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemap = Add<Tonemapping>(profile);
            tonemap.mode.Override(TonemappingMode.Neutral);

            var grade = Add<ColorAdjustments>(profile);
            grade.postExposure.Override(0.15f);
            grade.contrast.Override(8f);
            grade.saturation.Override(12f);

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.3f);

            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.5f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // ---------- Scene content ----------

        private static void BuildGround()
        {
            var ground = Group("Ground");
            Spawn(ground, "Grass", SaveMesh(LowPolyMesh.Box("Ground", new Vector3(60f, 0.2f, 44f))), Mat("Grass", Palette.Grass, 0f),
                new Vector3(0f, -0.2f, 10f));
            Spawn(ground, "Path", SaveMesh(LowPolyMesh.Box("Path", new Vector3(1.2f, 0.03f, 8f))), Mat("Sand", Palette.Sand, 0f),
                new Vector3(-6.2f, 0f, -1.7f));
        }

        private static void BuildCropRow()
        {
            var plot = Group("Crop Plot");
            var tile = SaveMesh(LowPolyMesh.Box("SoilTile", new Vector3(0.94f, 0.08f, 0.94f)));
            var dry = Mat("Soil", Palette.Soil, 1f);
            var wet = Mat("SoilWet", Palette.SoilWet, 1f);

            for (int x = -2; x <= 3; x++)
            for (int z = -1; z <= 1; z++)
            {
                bool watered = z == 0 ? x > -2 && x < 3 : (x + z) % 2 == 0;
                Spawn(plot, $"Tile {x},{z}", tile, watered ? wet : dry, new Vector3(x, 0f, z));
            }

            // Front-to-back reading order along the middle row: every growth stage of one crop.
            Seeds(Group("Seeds", plot, new Vector3(-1f, 0.08f, 0f)));
            Plant(Group("Sprout", plot, new Vector3(0f, 0.08f, 0f)), 0.6f, 3, false);
            Plant(Group("Growing", plot, new Vector3(1f, 0.08f, 0f)), 0.95f, 5, false);
            Turnip(Group("Mature", plot, new Vector3(2f, 0.08f, 0f)));
            Plant(Group("Withered", plot, new Vector3(3f, 0.08f, 0f)), 0.8f, 4, true);

            foreach (int x in new[] { -1, 1, 3 })
                Turnip(Group("Mature", plot, new Vector3(x, 0.08f, 1f)));
        }

        private static void Seeds(Transform at)
        {
            var mesh = SaveMesh(LowPolyMesh.Blob("Seed", 2, 4, 0.07f, 0f, 2));
            var mat = Mat("Seed", Palette.Seed, 0f);
            Spawn(at, "Seed", mesh, mat, new Vector3(-0.15f, 0f, 0.1f));
            Spawn(at, "Seed", mesh, mat, new Vector3(0.12f, 0f, 0.14f));
            Spawn(at, "Seed", mesh, mat, new Vector3(0.02f, 0f, -0.15f));
        }

        private static void Plant(Transform at, float size, int leaves, bool withered)
        {
            var leaf = SaveMesh(LowPolyMesh.Leaf("CropLeaf", 0.5f, 0.22f, 0.03f));
            var mat = withered ? Mat("Withered", Palette.Withered, 1.5f) : Mat("CropLeaf", Palette.CropLeaf, 1.5f);
            float pitch = withered ? -5f : 50f; // withered leaves droop flat
            for (int i = 0; i < leaves; i++)
                Spawn(at, "Leaf", leaf, mat, Vector3.zero, Quaternion.Euler(-pitch, i * 360f / leaves + 20f, 0f), Vector3.one * size);
        }

        private static void Turnip(Transform at)
        {
            var bulb = SaveMesh(LowPolyMesh.Blob("TurnipBulb", 5, 8, 0.24f, 0.06f, 3));
            Spawn(at, "Bulb", bulb, Mat("TurnipWhite", Palette.TurnipWhite, 2f), new Vector3(0f, 0.1f, 0f), Quaternion.identity, new Vector3(1f, 0.85f, 1f));
            Spawn(at, "Top", bulb, Mat("TurnipPurple", Palette.TurnipPurple, 2f), new Vector3(0f, 0.18f, 0f), Quaternion.identity, new Vector3(0.9f, 0.55f, 0.9f));
            var top = Group("Leaves", at, new Vector3(0f, 0.25f, 0f));
            Plant(top, 1.15f, 6, false);
        }

        private static void BuildPlayer(Vector3 position)
        {
            var player = Group("Player");
            player.position = position;
            player.rotation = Quaternion.Euler(0f, 160f, 0f); // turned towards the camera

            var leg = SaveMesh(LowPolyMesh.Box("Leg", new Vector3(0.13f, 0.26f, 0.15f)));
            var pants = Mat("Pants", Palette.Pants, 2f);
            Spawn(player, "Leg L", leg, pants, new Vector3(-0.09f, 0f, 0f));
            Spawn(player, "Leg R", leg, pants, new Vector3(0.09f, 0f, 0f));
            Spawn(player, "Body", SaveMesh(LowPolyMesh.Cylinder("Body", 8, 0.21f, 0.16f, 0.38f)), Mat("Shirt", Palette.Shirt, 2f),
                new Vector3(0f, 0.24f, 0f));

            var skin = Mat("Skin", Palette.Skin, 2f);
            var hand = SaveMesh(LowPolyMesh.Blob("Hand", 3, 6, 0.065f, 0f, 4, false));
            Spawn(player, "Hand L", hand, skin, new Vector3(-0.25f, 0.34f, 0.02f));
            Spawn(player, "Hand R", hand, skin, new Vector3(0.25f, 0.34f, 0.02f));

            // A big head reads well from far above.
            Spawn(player, "Head", SaveMesh(LowPolyMesh.Blob("Head", 8, 12, 0.27f, 0f, 5, false)), skin, new Vector3(0f, 0.86f, 0f));
            var eye = SaveMesh(LowPolyMesh.Blob("Eye", 3, 6, 0.04f, 0f, 6, false));
            var ink = Mat("Ink", Palette.Ink, 0f);
            Spawn(player, "Eye L", eye, ink, new Vector3(-0.1f, 0.88f, 0.245f), Quaternion.identity, new Vector3(1f, 1.5f, 0.6f));
            Spawn(player, "Eye R", eye, ink, new Vector3(0.1f, 0.88f, 0.245f), Quaternion.identity, new Vector3(1f, 1.5f, 0.6f));

            var straw = Mat("Straw", Palette.Straw, 2f);
            Spawn(player, "Hat Brim", SaveMesh(LowPolyMesh.Cylinder("HatBrim", 10, 0.35f, 0.33f, 0.04f)), straw, new Vector3(0f, 1.04f, 0f),
                Quaternion.Euler(-8f, 0f, 0f));
            Spawn(player, "Hat Crown", SaveMesh(LowPolyMesh.Cylinder("HatCrown", 10, 0.22f, 0.18f, 0.18f)), straw, new Vector3(0f, 1.06f, 0f));
        }

        private static void BuildHouse(Vector3 position)
        {
            var house = Group("House");
            house.position = position;
            Spawn(house, "Foundation", SaveMesh(LowPolyMesh.Box("Foundation", new Vector3(3.2f, 0.2f, 2.6f))), Mat("Stone", Palette.Stone, 1.5f), Vector3.zero);
            Spawn(house, "Walls", SaveMesh(LowPolyMesh.Box("Walls", new Vector3(3f, 1.8f, 2.4f))), Mat("Wall", Palette.Wall, 2f), new Vector3(0f, 0.2f, 0f));
            Spawn(house, "Roof", SaveMesh(LowPolyMesh.Roof("Roof", 3.6f, 3.0f, 1.3f)), Mat("Roof", Palette.Roof, 2f), new Vector3(0f, 2.0f, 0f));
            Spawn(house, "Chimney", SaveMesh(LowPolyMesh.Box("Chimney", new Vector3(0.35f, 0.9f, 0.35f))), Mat("Stone", Palette.Stone, 1.5f), new Vector3(0.9f, 2.3f, 0.5f));
            Spawn(house, "Door", SaveMesh(LowPolyMesh.Box("Door", new Vector3(0.6f, 1.05f, 0.06f))), Mat("Wood", Palette.Wood, 2f), new Vector3(0f, 0.2f, -1.22f));
            var window = SaveMesh(LowPolyMesh.Box("Window", new Vector3(0.5f, 0.5f, 0.06f)));
            var glass = Mat("Window", Palette.Window, 1.5f);
            Spawn(house, "Window L", window, glass, new Vector3(-0.95f, 0.8f, -1.22f));
            Spawn(house, "Window R", window, glass, new Vector3(0.95f, 0.8f, -1.22f));
        }

        private static void BuildShippingBin(Vector3 position)
        {
            var bin = Group("Shipping Bin");
            bin.position = position;
            Spawn(bin, "Box", SaveMesh(LowPolyMesh.Box("BinBox", new Vector3(0.9f, 0.55f, 0.6f))), Mat("Wood", Palette.Wood, 2f), Vector3.zero);
            Spawn(bin, "Lid", SaveMesh(LowPolyMesh.Box("BinLid", new Vector3(0.98f, 0.1f, 0.68f))), Mat("BinBlue", Palette.BinBlue, 2f), new Vector3(0f, 0.55f, 0f));
        }

        private static void BuildFence()
        {
            var fence = Group("Fence");
            var post = SaveMesh(LowPolyMesh.Box("FencePost", new Vector3(0.12f, 0.6f, 0.12f)));
            var rail = SaveMesh(LowPolyMesh.Box("FenceRail", new Vector3(7.2f, 0.07f, 0.05f)));
            var wood = Mat("Wood", Palette.Wood, 2f);
            for (float x = -2.6f; x <= 4.4f; x += 0.875f)
                Spawn(fence, "Post", post, wood, new Vector3(x, 0f, 2.1f));
            Spawn(fence, "Rail", rail, wood, new Vector3(0.9f, 0.22f, 2.06f));
            Spawn(fence, "Rail", rail, wood, new Vector3(0.9f, 0.44f, 2.06f));
        }

        private static void BuildNature()
        {
            var nature = Group("Nature");
            AutumnTree(nature, new Vector3(6f, 0f, 4f), 11);
            AutumnTree(nature, new Vector3(8.2f, 0f, 1.4f), 12);
            AutumnTree(nature, new Vector3(4.4f, 0f, 6f), 13);
            AutumnTree(nature, new Vector3(-9f, 0f, 5.5f), 14);
            Pine(nature, new Vector3(-9.2f, 0f, 1.2f));
            Pine(nature, new Vector3(9.4f, 0f, -1.8f));
            Pine(nature, new Vector3(-1.5f, 0f, 6.5f));

            var rockMat = Mat("Rock", Palette.Rock, 2f);
            Rock(nature, rockMat, new Vector3(1.8f, 0f, -3.1f), 0.35f, 21);
            Rock(nature, rockMat, new Vector3(2.3f, 0f, -2.8f), 0.2f, 22);
            Rock(nature, rockMat, new Vector3(-8.2f, 0f, -2.5f), 0.45f, 23);

            // Pond: a flat many-sided disc, with rocks around it.
            var pond = Spawn(nature, "Pond", SaveMesh(LowPolyMesh.Cylinder("Pond", 12, 1.6f, 1.6f, 0.03f)), Mat("Water", Palette.Water, 0f),
                new Vector3(5.1f, -0.01f, -2.2f));
            pond.transform.localScale = new Vector3(1.15f, 1f, 0.75f);
            Rock(nature, rockMat, new Vector3(3.3f, 0f, -1.5f), 0.22f, 24);
            Rock(nature, rockMat, new Vector3(6.8f, 0f, -3.0f), 0.28f, 25);
        }

        private static void AutumnTree(Transform parent, Vector3 position, int seed, float scale = 1f)
        {
            var tree = Group("Autumn Tree", parent);
            tree.position = position;
            tree.localScale = Vector3.one * scale;
            Spawn(tree, "Trunk", SaveMesh(LowPolyMesh.Cylinder("Trunk", 6, 0.16f, 0.1f, 1.2f)), Mat("Trunk", Palette.Trunk, 2f), Vector3.zero);
            var canopy = SaveMesh(LowPolyMesh.Blob("Canopy" + seed, 4, 7, 0.7f, 0.15f, seed));
            Spawn(tree, "Canopy", canopy, Mat("LeafOrange", Palette.LeafOrange, 2f), new Vector3(0f, 1.6f, 0f));
            Spawn(tree, "Canopy", canopy, Mat("LeafYellow", Palette.LeafYellow, 2f), new Vector3(0.45f, 1.3f, -0.2f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * 0.65f);
            Spawn(tree, "Canopy", canopy, Mat("LeafRed", Palette.LeafRed, 2f), new Vector3(-0.4f, 1.35f, 0.15f), Quaternion.Euler(0f, 110f, 0f), Vector3.one * 0.6f);
        }

        private static void Pine(Transform parent, Vector3 position, float scale = 1f)
        {
            var pine = Group("Pine", parent);
            pine.position = position;
            pine.localScale = Vector3.one * scale;
            Spawn(pine, "Trunk", SaveMesh(LowPolyMesh.Cylinder("Trunk", 6, 0.16f, 0.1f, 1.2f)), Mat("Trunk", Palette.Trunk, 2f), Vector3.zero);
            var cone = SaveMesh(LowPolyMesh.Cylinder("PineCone", 7, 0.8f, 0f, 1.1f));
            var mat = Mat("Pine", Palette.Pine, 2f);
            Spawn(pine, "Layer", cone, mat, new Vector3(0f, 0.5f, 0f));
            Spawn(pine, "Layer", cone, mat, new Vector3(0f, 1.1f, 0f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 0.78f);
            Spawn(pine, "Layer", cone, mat, new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 50f, 0f), Vector3.one * 0.55f);
        }

        private static void Rock(Transform parent, Material mat, Vector3 position, float size, int seed)
        {
            var mesh = SaveMesh(LowPolyMesh.Blob("Rock" + seed, 3, 6, 1f, 0.25f, seed));
            Spawn(parent, "Rock", mesh, mat, position + new Vector3(0f, size * 0.25f, 0f), Quaternion.Euler(0f, seed * 37f, 0f),
                new Vector3(size, size * 0.7f, size));
        }

        // ---------- Helpers ----------

        private static Transform Group(string name, Transform parent = null, Vector3 localPosition = default)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : root, false);
            t.localPosition = localPosition;
            return t;
        }

        private static GameObject Spawn(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPosition) =>
            Spawn(parent, name, mesh, mat, localPosition, Quaternion.identity, Vector3.one);

        private static GameObject Spawn(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPosition,
            Quaternion localRotation, Vector3? localScale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // Saves the mesh as an asset, reusing the existing file (and its GUID) on rebuilds.
        private static Mesh SaveMesh(Mesh built)
        {
            string path = $"{ArtRoot}/Meshes/{built.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(built, path);
                return built;
            }
            // Copy the data in through the Mesh API; CopySerialized doesn't update mesh assets.
            var smoothNormals = new System.Collections.Generic.List<Vector3>();
            built.GetUVs(3, smoothNormals);
            existing.Clear();
            existing.indexFormat = built.indexFormat;
            existing.vertices = built.vertices;
            existing.normals = built.normals;
            existing.tangents = built.tangents;
            existing.uv = built.uv;
            existing.SetUVs(3, smoothNormals);
            existing.triangles = built.triangles;
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(built);
            return existing;
        }

        // Loads or creates a toon material and sets its colours. The outline is a darker shade of the colour.
        private static Material Mat(string name, Color color, float outlineWidth)
        {
            string path = $"{ArtRoot}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(path));
                mat = new Material(Shader.Find(ShaderName)) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find(ShaderName);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_ShadowColor", Palette.ShadowTint);
            mat.SetColor("_OutlineColor", Color.Lerp(color * 0.3f, new Color(0.16f, 0.08f, 0.14f), 0.5f));
            mat.SetFloat("_OutlineWidth", outlineWidth);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        private static bool Inside(Vector3 p, float minX, float maxX, float minZ, float maxZ) =>
            p.x >= minX && p.x <= maxX && p.z >= minZ && p.z <= maxZ;
    }
}
