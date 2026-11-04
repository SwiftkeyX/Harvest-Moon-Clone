using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Farm.EditorTools.Art
{
    // Builds Assets/_Game/Scenes/CliffLedge.unity: a pixelated, high-angle view of an autumn cliff ledge.
    // Everything is laid out in screen terms: Ground(u, v) is the world point on the ledge (y = 0) that the
    // camera shows at u (0 left .. 1 right) and v (0 top .. 1 bottom), so the layout can be read off the
    // reference picture.
    public static class CliffLedgeSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/CliffLedge.unity";
        const string MatDir = "Assets/_Game/Art/Materials";
        const string MeshDir = "Assets/_Game/Art/Meshes";
        const string TexDir = "Assets/_Game/Art/Textures";
        const string ToonShader = "CliffLedge/Toon";
        const string BackdropShader = "CliffLedge/Backdrop";

        // Camera: orthographic, looking north and down. The view is 2*HalfHeight units tall.
        const float Pitch = 50f;
        const float HalfHeight = 10f;
        const float Aspect = 16f / 9f;
        const float CameraDistance = 40f;
        public const int PixelWidth = 384, PixelHeight = 216;

        static float Sin => Mathf.Sin(Pitch * Mathf.Deg2Rad);
        static float Cos => Mathf.Cos(Pitch * Mathf.Deg2Rad);

        [MenuItem("Farm/Art/Cliff Ledge/1 Graybox")] static void Stage1() => Build(1);
        [MenuItem("Farm/Art/Cliff Ledge/2 Terrain")] static void Stage2() => Build(2);
        [MenuItem("Farm/Art/Cliff Ledge/3 Shaders")] static void Stage3() => Build(3);
        [MenuItem("Farm/Art/Cliff Ledge/4 Decorated (full)")] static void Stage4() => Build(4);

        // The world point at height y that the camera shows at screen position (u, v).
        public static Vector3 Ground(float u, float v, float y = 0f)
        {
            float x = (u - 0.5f) * 2f * HalfHeight * Aspect;
            float z = ((0.5f - v) * 2f * HalfHeight - y * Cos) / Sin;
            return new Vector3(x, y, z);
        }

        static Vector2 G2(float u, float v) { var p = Ground(u, v); return new Vector2(p.x, p.z); }

        public static void Build(int stage)
        {
            EditorAssetUtil.EnsureFolder(MatDir);
            EditorAssetUtil.EnsureFolder(MeshDir);
            EditorAssetUtil.EnsureFolder(TexDir);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene unloads unused assets, so everything is created or loaded after it.
            var mats = new Materials(stage);

            BuildLighting(stage);
            var cam = BuildCamera(stage, mats);
            BuildTerrain(stage, mats);
            if (stage >= 4) Decorate(mats);
            // From the shader stage on, the scene uses the Shader Graph materials (tuning on them is kept).
            if (stage >= 3) CliffLedgeGraphMaterials.ApplyToOpenScene();

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = cam.gameObject;
        }

        // Renders the open scene's main camera to a 1920x1080 PNG at `absolutePath`, the way the screen shows it:
        // a pixelated camera is rendered at its own size and scaled up without smoothing.
        public static string Capture(string absolutePath)
        {
            var cam = Camera.main;
            if (cam == null) return "no main camera";
            const int w = 1920, h = 1080;
            var target = cam.targetTexture;
            var temp = target == null ? new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) : null;
            var rt = target != null ? target : temp;
            cam.targetTexture = rt;
            // Compile shader variants now, or new ones are skipped and objects are missing from the picture.
            bool async = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            cam.Render();
            ShaderUtil.allowAsyncCompilation = async;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var small = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            small.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = target;
            if (temp != null) Object.DestroyImmediate(temp);

            var src = small.GetPixels32();
            var dst = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                dst[y * w + x] = src[(y * small.height / h) * small.width + x * small.width / w];
            var big = new Texture2D(w, h, TextureFormat.RGB24, false);
            big.SetPixels32(dst);
            big.Apply();
            File.WriteAllBytes(absolutePath, big.EncodeToPNG());
            Object.DestroyImmediate(small);
            Object.DestroyImmediate(big);
            return absolutePath;
        }

        // ---------------------------------------------------------------- lighting and camera

        static void BuildLighting(int stage)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Color.white;
            sun.intensity = 1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.58f, 0.55f);
            RenderSettings.fog = stage >= 3;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("fde4c2");
            RenderSettings.fogStartDistance = 62f;
            RenderSettings.fogEndDistance = 150f;
        }

        static Camera BuildCamera(int stage, Materials mats)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = HalfHeight;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("fde4c2");
            cam.allowMSAA = false;
            cam.allowHDR = false;
            go.transform.rotation = Quaternion.Euler(Pitch, 0f, 0f);
            go.transform.position = -go.transform.forward * CameraDistance;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.renderShadows = true;

            // Backdrop: a camera-facing quad at the far end that fills the whole view.
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Backdrop";
            Object.DestroyImmediate(backdrop.GetComponent<Collider>());
            backdrop.transform.SetParent(go.transform, false);
            backdrop.transform.localPosition = new Vector3(0, 0, 380f);
            backdrop.transform.localScale = new Vector3(2f * HalfHeight * Aspect, 2f * HalfHeight, 1f);
            var r = backdrop.GetComponent<MeshRenderer>();
            r.sharedMaterial = mats.Backdrop;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            if (stage >= 3) Pixelate(cam);
            return cam;
        }

        // Renders the camera into a small point-filtered texture and shows it full screen, so only this
        // scene is pixelated: the pipeline assets and renderers are untouched.
        static void Pixelate(Camera cam)
        {
            string rtPath = TexDir + "/CliffLedgeView.renderTexture";
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (rt == null)
            {
                rt = new RenderTexture(PixelWidth, PixelHeight, 24, RenderTextureFormat.ARGB32);
                AssetDatabase.CreateAsset(rt, rtPath);
            }
            rt.Release();
            rt.width = PixelWidth;
            rt.height = PixelHeight;
            rt.depth = 24;
            rt.antiAliasing = 1;
            rt.filterMode = FilterMode.Point;
            rt.useMipMap = false;
            EditorUtility.SetDirty(rt);
            cam.targetTexture = rt;

            // An empty camera that only clears the screen; the canvas draws the picture on top.
            var screenCam = new GameObject("Screen Camera").AddComponent<Camera>();
            screenCam.clearFlags = CameraClearFlags.SolidColor;
            screenCam.backgroundColor = Color.black;
            screenCam.cullingMask = 0;
            screenCam.depth = 10;
            screenCam.transform.position = new Vector3(0, -500, 0);
            var sd = screenCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            sd.renderPostProcessing = false;
            sd.renderShadows = false;

            var canvasGo = new GameObject("Pixel Screen", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            var imgGo = new GameObject("View", typeof(RectTransform));
            imgGo.transform.SetParent(canvasGo.transform, false);
            var img = imgGo.AddComponent<RawImage>();
            img.texture = rt;
            img.raycastTarget = false;
            var rect = (RectTransform)imgGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var fitter = imgGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = Aspect;
        }

        // ---------------------------------------------------------------- terrain

        static void BuildTerrain(int stage, Materials mats)
        {
            var root = new GameObject("Terrain").transform;

            // The ledge: a lobe of grass that reaches toward the camera, its front wall the lower rock shelf.
            var lobe = Outline(
                (-0.05f, -0.10f), (-0.05f, 0.76f), (0.06f, 0.795f), (0.14f, 0.82f), (0.22f, 0.835f), (0.30f, 0.84f),
                (0.37f, 0.825f), (0.43f, 0.80f), (0.48f, 0.765f), (0.51f, 0.715f), (0.525f, 0.64f), (0.532f, 0.55f),
                (0.538f, 0.45f), (0.545f, 0.36f), (0.552f, 0.27f), (0.56f, 0.10f), (0.50f, -0.10f));
            var lip = new List<(float, float, int)> { (0.9f, 0f, 0), (0.35f, 0.3f, 0), (0f, 0.9f, 1) };
            // Walls facing right are lit cliff face; the rest is the darker shelf.
            Place(root, "Ledge", CliffLedgeMeshBuilder.Prism("CliffLedgeLedge", lobe, 0f, -45f, lip, n => n.x > 0.55f ? 2 : 1, 0.25f, 3, 3),
                mats.Grass, mats.RockShelf, mats.RockFace);

            // The cliff: set back on the right, its face drops out of the bottom of the view.
            var mass = Outline(
                (0.46f, 0.31f), (0.51f, 0.30f), (0.555f, 0.29f), (0.60f, 0.27f), (0.645f, 0.245f), (0.69f, 0.215f),
                (0.672f, 0.12f), (0.645f, 0.03f), (0.62f, -0.15f), (0.46f, -0.15f));
            var lip2 = new List<(float, float, int)> { (0.7f, 0f, 0), (0.25f, 0.25f, 1), (0f, 0.8f, 1) };
            Place(root, "Cliff", CliffLedgeMeshBuilder.Prism("CliffLedgeCliff", mass, 0f, -60f, lip2, _ => 1, 0.15f, 5),
                mats.Grass, mats.RockFace);

            // The upper rock wall behind the ledge, top left.
            var upper = Outline(
                (-0.08f, 0.285f), (0.05f, 0.30f), (0.13f, 0.30f), (0.22f, 0.29f), (0.30f, 0.28f), (0.38f, 0.26f),
                (0.45f, 0.235f), (0.49f, 0.18f), (0.50f, -0.40f), (-0.08f, -0.40f));
            var lip3 = new List<(float, float, int)> { (0.6f, 0f, 0), (0.2f, 0.25f, 0), (0f, 0.7f, 1) };
            Place(root, "Upper Wall", CliffLedgeMeshBuilder.Prism("CliffLedgeUpperWall", upper, 11f, -2f, lip3, _ => 1, 0.3f, 7),
                mats.Grass, mats.RockUpper);
        }

        static List<Vector2> Outline(params (float u, float v)[] pts)
        {
            var list = new List<Vector2>();
            foreach (var p in pts) list.Add(G2(p.u, p.v));
            return list;
        }

        internal static GameObject Place(Transform parent, string name, Mesh mesh, params Material[] materials)
        {
            string path = $"{MeshDir}/{mesh.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }

        // ---------------------------------------------------------------- decoration

        static void Decorate(Materials m)
        {
            var root = new GameObject("Decor").transform;
            var rnd = new System.Random(42);
            // The prefab step draws the pine and birch canopies from rnd first, so the scatter below is unchanged.
            var p = CliffLedgePrefabs.Build(m, rnd);

            // Bushes on top of the cliff, top centre.
            var bushes = Group(root, "Bushes");
            Bush(bushes, p.BushOlive, 0.445f, 0.07f, 2.0f);
            Bush(bushes, p.BushOlive, 0.49f, 0.16f, 1.6f);
            Bush(bushes, p.BushTop, 0.51f, 0.06f, 2.2f);
            Bush(bushes, p.BushTop, 0.565f, 0.13f, 2.0f);
            Bush(bushes, p.BushOlive, 0.60f, 0.05f, 1.7f);
            Bush(bushes, p.BushTop, 0.625f, 0.17f, 1.5f);
            Bush(bushes, p.BushTop, 0.53f, 0.23f, 1.3f);
            Bush(bushes, p.BushTop, 0.66f, 0.12f, 1.2f);
            Bush(bushes, p.BushOlive, 0.585f, 0.235f, 1.0f);

            // Bushes on the ledge.
            Bush(bushes, p.BushOlive, 0.437f, 0.515f, 1.15f);
            Bush(bushes, p.BushOlive, 0.425f, 0.56f, 0.9f);
            Bush(bushes, p.BushOlive, 0.47f, 0.32f, 1.05f);
            Bush(bushes, p.BushYellow, 0.39f, 0.45f, 0.95f);
            Bush(bushes, p.BushYellow, 0.30f, 0.47f, 1.05f);
            Bush(bushes, p.BushYellow, 0.245f, 0.55f, 1.15f);
            Bush(bushes, p.BushOlive, 0.275f, 0.655f, 0.8f);
            Bush(bushes, p.BushOlive, 0.29f, 0.745f, 0.6f);
            Bush(bushes, p.BushYellow, 0.155f, 0.66f, 1.45f);
            Bush(bushes, p.BushYellow, 0.12f, 0.75f, 1.25f);
            Bush(bushes, p.BushYellow, 0.205f, 0.715f, 1.0f);
            Bush(bushes, p.BushYellow, 0.045f, 0.74f, 1.1f);
            Bush(bushes, p.BushOlive, 0.32f, 0.38f, 0.8f);
            Bush(bushes, p.BushOlive, 0.22f, 0.44f, 0.9f);

            // Pine behind the character, birches on the left.
            var trees = Group(root, "Trees");
            CliffLedgePrefabs.Instance(p.Pine, trees, Ground(0.355f, 0.40f));
            CliffLedgePrefabs.Instance(p.BirchLarge, trees, Ground(0.065f, 0.80f));
            CliffLedgePrefabs.Instance(p.BirchSmall, trees, Ground(0.175f, 0.40f));
            CliffLedgePrefabs.Instance(p.BirchMedium, trees, Ground(0.0f, 0.50f));

            // Mauve boulders.
            var rocks = Group(root, "Rocks");
            Boulder(rocks, p.Boulder, 0.262f, 0.43f, new Vector3(2.6f, 2.6f, 2.0f), new Vector3(-10f, 28f, 14f));
            Boulder(rocks, p.Boulder, 0.235f, 0.45f, new Vector3(1.6f, 1.5f, 1.4f), new Vector3(12f, -22f, -8f));

            // Fence at the bottom left.
            CliffLedgePrefabs.Instance(p.Fence, Group(root, "Fences"), CliffLedgePrefabs.FenceBase);

            // Character, mid-ledge.
            CliffLedgePrefabs.Instance(p.Character, Group(root, "Characters"), Ground(0.478f, 0.555f));

            // One-off pieces stay plain scene objects.
            var scenery = Group(root, "Scenery");
            var leaves = new CliffLedgeMeshBuilder();
            CliffLedgeMeshBuilder.LeafCard(leaves, Matrix4x4.TRS(Ground(0.255f, 0.345f, 5f), Quaternion.Euler(45f, 0f, -62f), Vector3.one), 3.6f, 2.2f);
            CliffLedgeMeshBuilder.LeafCard(leaves, Matrix4x4.TRS(Ground(0.285f, 0.31f, 3.4f), Quaternion.Euler(45f, 0f, -25f), Vector3.one), 2.0f, 1.3f);
            CliffLedgeMeshBuilder.LeafCard(leaves, Matrix4x4.TRS(Ground(0.6f, 0.09f, 8f), Quaternion.Euler(50f, 0f, -75f), Vector3.one), 2.4f, 1.3f);
            CliffLedgeMeshBuilder.LeafCard(leaves, Matrix4x4.TRS(Ground(0.63f, 0.06f, 8f), Quaternion.Euler(50f, 0f, -60f), Vector3.one), 3.4f, 1.8f);
            CliffLedgeMeshBuilder.LeafCard(leaves, Matrix4x4.TRS(Ground(0.645f, 0.07f, 8f), Quaternion.Euler(50f, 0f, -20f), Vector3.one), 2.6f, 1.5f);
            Place(scenery, "Orange Leaves", leaves.ToMesh("CliffLedgeOrangeLeaves"), m.LeafOrange);

            // A grassy bank leaning on the upper wall, between the birches and the pine.
            Primitive(scenery, PrimitiveType.Sphere, m.Grass, Ground(0.235f, 0.25f, 0.5f), new Vector3(5.5f, 9f, 6f), new Vector3(-20f, 0f, 0f)).name = "Grass Bank";

            // Flowers and pebbles.
            (float u, float v)[] flowers =
            {
                (0.40f, 0.31f), (0.415f, 0.325f), (0.43f, 0.305f), (0.445f, 0.33f), (0.18f, 0.345f), (0.195f, 0.33f),
                (0.21f, 0.62f), (0.465f, 0.585f), (0.455f, 0.565f), (0.345f, 0.42f), (0.36f, 0.43f), (0.21f, 0.68f),
                (0.45f, 0.64f), (0.33f, 0.53f), (0.505f, 0.42f), (0.135f, 0.5f), (0.28f, 0.6f)
            };
            var flowerGroup = Group(root, "Flowers");
            foreach (var f in flowers)
            {
                // A shorter flower is the prefab sunk into the grass, so its head sits at the right height.
                float h = 0.35f + (float)rnd.NextDouble() * 0.35f;
                CliffLedgePrefabs.Instance(p.Flower, flowerGroup, Ground(f.u, f.v) - Vector3.up * (CliffLedgePrefabs.FlowerStem - h));
            }
            (float u, float v)[] pebbles =
            {
                (0.432f, 0.585f), (0.41f, 0.6f), (0.425f, 0.612f), (0.395f, 0.628f), (0.375f, 0.618f),
                (0.36f, 0.64f), (0.315f, 0.635f), (0.40f, 0.645f)
            };
            var pebbleGroup = Group(root, "Pebbles");
            foreach (var pb in pebbles)
            {
                float k = 0.8f + (float)rnd.NextDouble() * 0.6f;
                var rot = Quaternion.Euler(0, rnd.Next(360), 0);
                // Every pebble's centre sits at the same height, whatever its size.
                var pos = Ground(pb.u, pb.v, CliffLedgePrefabs.PebbleLift) - Vector3.up * CliffLedgePrefabs.PebbleLift * k;
                CliffLedgePrefabs.Instance(p.Pebble, pebbleGroup, pos, rot, Vector3.one * k);
            }

            var tufts = new CliffLedgeMeshBuilder();
            for (int i = 0; i < 70; i++)
            {
                float u = 0.12f + (float)rnd.NextDouble() * 0.41f, v = 0.33f + (float)rnd.NextDouble() * 0.48f;
                if (!InsideLedge(u, v)) continue;
                var basePos = Ground(u, v);
                for (int k = 0; k < 3; k++)
                    CliffLedgeMeshBuilder.LeafCard(tufts, Matrix4x4.TRS(basePos,
                        Quaternion.Euler(40f, 0f, -45f + k * 45f + (float)rnd.NextDouble() * 20f), Vector3.one), 0.8f, 0.35f);
            }
            Place(scenery, "Grass Tufts", tufts.ToMesh("CliffLedgeGrassTufts"), m.Tuft);

            // Out-of-focus branch in the bottom right, below the ledge.
            var plant = new CliffLedgeMeshBuilder();
            var origin = Ground(0.70f, 1.02f, -22f);
            // Leaves in pairs along a stem that rises to the right.
            var stemDir = new Vector3(0.63f, 0.77f, 0f).normalized;
            CliffLedgeMeshBuilder.LeafCard(plant, Matrix4x4.TRS(origin, Quaternion.Euler(55f, 0f, -45f), Vector3.one), 12f, 0.3f);
            for (int i = 0; i < 5; i++)
            {
                var pos = origin + stemDir * (1.4f + i * 2.0f);
                float size = Mathf.Lerp(2.4f, 1.5f, i / 4f);
                CliffLedgeMeshBuilder.LeafCard(plant, Matrix4x4.TRS(pos, Quaternion.Euler(55f, 0f, 10f + (float)rnd.NextDouble() * 20f), Vector3.one), size, size * 0.55f);
                CliffLedgeMeshBuilder.LeafCard(plant, Matrix4x4.TRS(pos, Quaternion.Euler(55f, 0f, -160f - (float)rnd.NextDouble() * 20f), Vector3.one), size, size * 0.55f);
            }
            Place(scenery, "Foreground Branch", plant.ToMesh("CliffLedgeForegroundBranch"), m.Plant);
        }

        static bool InsideLedge(float u, float v) =>
            v > 0.33f && v < 0.80f - 0.12f * Mathf.Max(0, u - 0.4f) * 8f && u < 0.52f;

        static Transform Group(Transform root, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            return t;
        }

        // A bush of radius r whose main ball is centred r/2 above the ground at screen point (u, v).
        static void Bush(Transform parent, GameObject prefab, float u, float v, float r) =>
            CliffLedgePrefabs.Instance(prefab, parent, Ground(u, v, r * 0.5f) - Vector3.up * r * 0.5f, Quaternion.identity, Vector3.one * r);

        // A boulder of the given size, tilted about its centre, which sits at 0.4 of its height.
        static void Boulder(Transform parent, GameObject prefab, float u, float v, Vector3 size, Vector3 euler)
        {
            var rot = Quaternion.Euler(euler);
            var centre = Ground(u, v, size.y * 0.4f);
            CliffLedgePrefabs.Instance(prefab, parent, centre - rot * (Vector3.up * size.y * 0.5f), rot, size);
        }

        internal static GameObject Primitive(Transform parent, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, Vector3 euler,
            bool local = false)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = pos;
            else go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // ---------------------------------------------------------------- materials

        internal sealed class Materials
        {
            public Material Grass, RockShelf, RockFace, RockUpper, Backdrop;
            public Material Pine, Trunk, BirchTrunk, BirchLeaf, BirchLeafDark;
            public Material BushYellow, BushOlive, BushTop, LeafOrange, Mauve, Fence;
            public Material Flower, Pebble, Tuft, Jacket, Hood, Plant;

            public Materials(int stage)
            {
                bool gray = stage == 1;
                bool shaded = stage >= 3;

                Grass = Toon("Grass", "b4ac4a", "8f8a3c", "c4bb55", gray);
                if (shaded) { Set(Grass, "_PatchColor", "a2994a"); Grass.SetFloat("_PatchAmount", 0.4f); Grass.SetFloat("_PatchScale", 0.6f); Grass.SetFloat("_PatchWarp", 2.2f); }

                RockFace = Toon("RockFace", "c5b29f", "99887c", "ddcfc0", gray);
                RockShelf = Toon("RockShelf", "8a7f77", "6b605c", "978c82", gray);
                RockUpper = Toon("RockUpper", "c6b0a2", "a8948a", "d6c2b2", gray);
                if (shaded)
                {
                    Streaks(RockFace, "6e615a", new Vector4(1.25f, 0.13f, 1.25f, 0), 0.085f);
                    Streaks(RockShelf, "514640", new Vector4(0.16f, 0.9f, 0.16f, 0), 0.09f);
                    Streaks(RockUpper, "7e665a", new Vector4(0.9f, 0.14f, 0.9f, 0), 0.09f);
                }

                Pine = Toon("Pine", "5c5432", "463f27", "6a6038", false);
                Trunk = Toon("Trunk", "5a4030", "40302a", "6a4c38", false);
                BirchTrunk = Toon("BirchTrunk", "f2ece2", "c9bdb2", "fbf6ee", false);
                Streaks(BirchTrunk, "5a4a42", new Vector4(0.6f, 2.8f, 0.6f, 0), 0.05f);
                BirchLeaf = Toon("BirchLeaf", "f6c43c", "d9a84e", "fbd560", false);
                BirchLeafDark = Toon("BirchLeafDark", "e3b04a", "bb8f3e", "f2c552", false);
                BushYellow = Toon("BushYellow", "c4aa31", "a28c2b", "d6c048", false);
                BushOlive = Toon("BushOlive", "8a7531", "6b5b27", "a08b3d", false);
                BushTop = Toon("BushTop", "d1ac4c", "a97a39", "e3c269", false);
                foreach (var b in new[] { BushYellow, BushOlive, BushTop }) { b.SetFloat("_ShadeThreshold", 0.3f); b.SetFloat("_HighlightThreshold", 0.93f); }
                LeafOrange = Toon("LeafOrange", "b8622f", "984f29", "c96d3c", false);
                Mauve = Toon("Mauve", "9b6f70", "7a5658", "ad8282", false);
                Fence = Toon("Fence", "6a2f21", "4e2219", "80402c", false);
                Flower = Toon("Flower", "f2d03c", "d8b02c", "fbe060", false);
                Pebble = Toon("Pebble", "f4efe4", "d8d0c4", "ffffff", false);
                Tuft = Toon("Tuft", "7c7a2c", "626020", "8e8b34", false);
                Jacket = Toon("Jacket", "cb2d35", "9e1f2a", "de3d44", false);
                Hood = Toon("Hood", "473d5e", "2f2944", "574c72", false);
                Plant = Toon("ForegroundBranch", "937a58", "6c5940", "a68c68", false);
                Plant.SetFloat("_FogAmount", 0f);
                Outline(Jacket, "2a1a2a", 0.09f);
                Outline(Hood, "1e1830", 0.09f);
                Outline(BushTop, "6e5426", 0.12f);
                Outline(RockFace, "5a4a44", 0.12f); // shows as the dark line along the cliff's outer edge

                Backdrop = LoadOrCreate("Backdrop", BackdropShader);
                var tex = PaintBackdrop(gray || !shaded);
                Backdrop.SetTexture("_BaseMap", tex);
                Backdrop.SetFloat("_Cutoff", 0f);
                EditorUtility.SetDirty(Backdrop);
            }

            static Material Toon(string name, string lit, string shade, string hi, bool gray)
            {
                var m = LoadOrCreate(name, ToonShader);
                if (gray) { lit = "9c9c9c"; shade = "707070"; hi = "b4b4b4"; }
                Set(m, "_BaseColor", lit);
                Set(m, "_ShadeColor", shade);
                Set(m, "_HighlightColor", hi);
                m.SetFloat("_ShadeThreshold", 0.15f);
                m.SetFloat("_HighlightThreshold", 0.8f);
                m.SetFloat("_PatchAmount", 0f);
                m.SetFloat("_StreakWidth", 0f);
                m.SetFloat("_FogAmount", 1f);
                m.SetFloat("_OutlineWidth", 0f);
                EditorUtility.SetDirty(m);
                return m;
            }

            static void Outline(Material m, string col, float width)
            {
                Set(m, "_OutlineColor", col);
                m.SetFloat("_OutlineWidth", width);
            }

            static void Streaks(Material m, string col, Vector4 stretch, float width)
            {
                Set(m, "_StreakColor", col);
                m.SetVector("_StreakStretch", stretch);
                m.SetFloat("_StreakWidth", width);
            }

            static Material LoadOrCreate(string name, string shaderName)
            {
                string path = $"{MatDir}/CliffLedge{name}.mat";
                var shader = Shader.Find(shaderName);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                else m.shader = shader;
                return m;
            }

            static void Set(Material m, string prop, string hex) => m.SetColor(prop, Hex(hex));

            static Texture2D PaintBackdrop(bool gray)
            {
                string path = TexDir + "/CliffLedgeBackdrop.png";
                var tex = CliffLedgeBackdropPainter.Paint(PixelWidth, PixelHeight, gray);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Point;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
