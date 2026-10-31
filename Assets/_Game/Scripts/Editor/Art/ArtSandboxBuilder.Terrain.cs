using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Turns the grey-box mountain into a sculptable Unity Terrain (MountainTerrain.unity), the way
    /// A Short Hike's island is one continuous terrain rather than boxes. The block heights are read
    /// from Graybox.unity, then the outlines are bent so cliff edges wave, cliff tops are rounded,
    /// the ground gets gentle bumps and the shores slope into beaches. Colour comes from the
    /// Farm/Toon Terrain shader (grass on flat ground, rock on steep ground), so after sculpting
    /// with Unity's terrain brushes there's nothing to repaint.
    /// </summary>
    public static partial class ArtSandboxBuilder
    {
        private const string TerrainScenePath = "Assets/_Game/Scenes/MountainTerrain.unity";
        private const string TerrainDataPath = ArtRoot + "/Terrain/MountainTerrain.asset";
        private const string TerrainRootName = "Mountain Terrain";
        private const int TerrainResolution = 513; // 0.58 m between height samples
        private static readonly Vector3 TerrainOrigin = new Vector3(-30f, -100f, -280f);
        private static readonly Vector3 TerrainSize = new Vector3(300f, 180f, 300f);
        private const float SeaFloor = -100f;

        private struct TopTri
        {
            public Vector3 A, B, C;
            public float MinX, MaxX, MinZ, MaxZ;
        }

        [MenuItem("Farm/Build Mountain Terrain From Grey-Box")]
        public static void BuildMountainTerrainMenu()
        {
            if (File.Exists(TerrainDataPath) &&
                !EditorUtility.DisplayDialog("Rebuild mountain terrain?",
                    "This rebuilds the terrain from Graybox.unity and replaces any sculpting you did on it.", "Rebuild", "Cancel"))
                return;
            BuildMountainTerrain();
        }

        [MenuItem("Farm/Re-scatter Mountain Terrain Props")]
        public static void RescatterTerrainPropsMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != TerrainScenePath)
            {
                EditorUtility.DisplayDialog("Re-scatter props", "Open MountainTerrain.unity first.", "OK");
                return;
            }
            RescatterTerrainProps(scene);
        }

        public static void BuildMountainTerrain()
        {
            var (tops, bridgeTris) = ReadGreyBox();
            float[,] heights = TerrainHeights(tops, bridgeTris);

            bool keepOpenScenes = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                keepOpenScenes |= SceneManager.GetSceneAt(i).isDirty;
            Scene previousActive = SceneManager.GetActiveScene();

            // Make the scene first: opening a new scene unloads assets loaded before it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, keepOpenScenes ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            root = new GameObject(TerrainRootName).transform;

            var terrain = BuildTerrainObject(heights);
            if (bridgeTris.Count > 0) BuildTerrainBridge(bridgeTris);
            BuildSea();
            BuildMountainLighting();
            Physics.SyncTransforms();

            Vector3 summit = Summit(terrain);
            PlaceFlag(summit);
            ScatterOnTerrain(terrain, new System.Random(7));
            BuildWalker(summit + new Vector3(-4f, 1.5f, -4f));

            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(TerrainScenePath));
            EditorSceneManager.SaveScene(scene, TerrainScenePath);
            AssetDatabase.SaveAssets();

            if (keepOpenScenes)
            {
                SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log($"Mountain terrain saved to {TerrainScenePath}. It was closed because another open scene has unsaved changes.");
            }
        }

        // ---------- Reading the grey-box ----------

        // Upward-facing triangles of every block (the walkable tops), plus the bridge's triangles,
        // which stay a separate mesh because a heightmap can't have a gap under a bridge.
        private static (List<TopTri> tops, List<Vector3> bridge) ReadGreyBox()
        {
            var tops = new List<TopTri>();
            var bridge = new List<Vector3>();
            Scene grey = EditorSceneManager.OpenPreviewScene(GreyBoxPath);
            try
            {
                foreach (var go in grey.GetRootGameObjects())
                {
                    if (go.name != "Mountain") continue;
                    foreach (var pb in go.GetComponentsInChildren<ProBuilderMesh>(true))
                    {
                        bool isBridge = pb.name == "Bridge";
                        foreach (Face face in pb.faces)
                        {
                            var idx = face.indexes;
                            for (int i = 0; i < idx.Count; i += 3)
                            {
                                Vector3 a = pb.transform.TransformPoint(pb.positions[idx[i]]);
                                Vector3 b = pb.transform.TransformPoint(pb.positions[idx[i + 1]]);
                                Vector3 c = pb.transform.TransformPoint(pb.positions[idx[i + 2]]);
                                if (isBridge) { bridge.Add(a); bridge.Add(b); bridge.Add(c); continue; }
                                if (Vector3.Cross(b - a, c - a).normalized.y < 0.3f) continue;
                                tops.Add(new TopTri
                                {
                                    A = a, B = b, C = c,
                                    MinX = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), MaxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x)),
                                    MinZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z)), MaxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z)),
                                });
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(grey);
            }
            return (tops, bridge);
        }

        // ---------- Heights ----------

        private static float[,] TerrainHeights(List<TopTri> tops, List<Vector3> bridge)
        {
            int n = TerrainResolution;
            var raw = new float[n, n];
            Rect bridgeRect = XZBounds(bridge);

            for (int zi = 0; zi < n; zi++)
            {
                float z = TerrainOrigin.z + zi / (n - 1f) * TerrainSize.z;
                for (int xi = 0; xi < n; xi++)
                {
                    float x = TerrainOrigin.x + xi / (n - 1f) * TerrainSize.x;

                    // Bend the outlines with a slow, smooth wobble, so cliff edges wave instead of running straight.
                    // It fades out near the bridge so both ends still meet the shelves.
                    float fade = bridge.Count == 0 ? 1f : Mathf.Clamp01(DistanceToRect(bridgeRect, x, z) / 12f);
                    // A big slow bend (about ±5 m over 50 m) plus a smaller one (±1.5 m over 12 m).
                    float wx = ((Mathf.PerlinNoise(x * 0.02f + 17.3f, z * 0.02f + 5.1f) - 0.5f) * 10f
                              + (Mathf.PerlinNoise(x * 0.08f + 3.3f, z * 0.08f + 61.2f) - 0.5f) * 3f) * fade;
                    float wz = ((Mathf.PerlinNoise(x * 0.02f + 41.7f, z * 0.02f + 29.9f) - 0.5f) * 10f
                              + (Mathf.PerlinNoise(x * 0.08f + 77.1f, z * 0.08f + 12.8f) - 0.5f) * 3f) * fade;
                    raw[zi, xi] = GreyBoxHeight(tops, x + wx, z + wz);
                }
            }

            // Round the cliff tops a little: near an edge, the ground dips towards the blurred height.
            // The foot of each cliff stays sharp (rocks hide it later). Kept small so the 3 m ledge survives.
            float[,] near = BoxBlur(raw, 2);
            float[,] wide = BoxBlur(raw, 12);
            var h = new float[n, n];
            for (int zi = 0; zi < n; zi++)
            for (int xi = 0; xi < n; xi++)
            {
                float x = TerrainOrigin.x + xi / (n - 1f) * TerrainSize.x;
                float z = TerrainOrigin.z + zi / (n - 1f) * TerrainSize.z;
                if (raw[zi, xi] <= SeaFloor + 0.01f)
                {
                    // Sea floor: slope gently out from the shore, so the beaches run into the water.
                    h[zi, xi] = Mathf.Min(wide[zi, xi], SeaFloor + 6f);
                    continue;
                }
                float rounded = Mathf.Min(raw[zi, xi], near[zi, xi] + 0.25f);
                // Gentle bumps: big soft swells plus small lumps, so no surface is perfectly flat.
                float bumps = (Mathf.PerlinNoise(x / 25f + 3.1f, z / 25f + 8.7f) - 0.5f) * 1.8f
                            + (Mathf.PerlinNoise(x / 7f + 11.4f, z / 7f + 2.2f) - 0.5f) * 0.6f;
                h[zi, xi] = rounded + bumps;
            }

            // Lowland shores: pull the last few metres of land down so they slope into the sea as beaches.
            for (int zi = 0; zi < n; zi++)
            for (int xi = 0; xi < n; xi++)
                if (raw[zi, xi] > SeaFloor + 0.01f && raw[zi, xi] < -80f)
                    h[zi, xi] = Mathf.Min(h[zi, xi], wide[zi, xi] + 4.5f);

            h = BoxBlur(h, 1); // smooths the stair-steps the height grid leaves on steep cliffs

            var normalized = new float[n, n];
            for (int zi = 0; zi < n; zi++)
            for (int xi = 0; xi < n; xi++)
                normalized[zi, xi] = Mathf.Clamp01((h[zi, xi] - TerrainOrigin.y) / TerrainSize.y);
            return normalized;
        }

        private static float GreyBoxHeight(List<TopTri> tops, float x, float z)
        {
            float best = SeaFloor;
            foreach (var t in tops)
            {
                if (x < t.MinX || x > t.MaxX || z < t.MinZ || z > t.MaxZ) continue;
                if (HeightInTriangle(t.A, t.B, t.C, x, z, out float y) && y > best) best = y;
            }
            return best;
        }

        private static bool HeightInTriangle(Vector3 a, Vector3 b, Vector3 c, float x, float z, out float y)
        {
            y = 0f;
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(d) < 1e-6f) return false;
            float w1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / d;
            float w2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / d;
            float w3 = 1f - w1 - w2;
            y = w1 * a.y + w2 * b.y + w3 * c.y;
            return w1 >= -1e-4f && w2 >= -1e-4f && w3 >= -1e-4f;
        }

        // Box blur with a sliding window, run along rows then columns. Edges repeat the border value.
        private static float[,] BoxBlur(float[,] src, int r)
        {
            int rows = src.GetLength(0), cols = src.GetLength(1);
            float size = 2 * r + 1;
            var tmp = new float[rows, cols];
            var dst = new float[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                float sum = 0f;
                for (int j = -r; j <= r; j++) sum += src[i, Mathf.Clamp(j, 0, cols - 1)];
                for (int j = 0; j < cols; j++)
                {
                    tmp[i, j] = sum / size;
                    sum += src[i, Mathf.Min(j + r + 1, cols - 1)] - src[i, Mathf.Max(j - r, 0)];
                }
            }
            for (int j = 0; j < cols; j++)
            {
                float sum = 0f;
                for (int i = -r; i <= r; i++) sum += tmp[Mathf.Clamp(i, 0, rows - 1), j];
                for (int i = 0; i < rows; i++)
                {
                    dst[i, j] = sum / size;
                    sum += tmp[Mathf.Min(i + r + 1, rows - 1), j] - tmp[Mathf.Max(i - r, 0), j];
                }
            }
            return dst;
        }

        private static Rect XZBounds(List<Vector3> points)
        {
            if (points.Count == 0) return Rect.zero;
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        private static float DistanceToRect(Rect r, float x, float z)
        {
            float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
            float dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ---------- Scene pieces ----------

        private static Terrain BuildTerrainObject(float[,] heights)
        {
            // Reuse the asset if it exists, so its GUID (and the scene's link to it) stays the same.
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(TerrainDataPath));
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
            data.heightmapResolution = TerrainResolution;
            data.size = TerrainSize;
            data.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(data);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(root, false);
            go.transform.position = TerrainOrigin;
            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainMaterial();
            terrain.drawInstanced = false; // the toon shader reads the mesh normals directly
            terrain.heightmapPixelError = 2f;
            terrain.shadowCastingMode = ShadowCastingMode.On;
            return terrain;
        }

        private static Material TerrainMaterial()
        {
            string path = ArtRoot + "/Materials/MountainTerrain.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Farm/Toon Terrain")) { name = "MountainTerrain" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find("Farm/Toon Terrain");
            mat.SetColor("_GrassColor", Palette.Grass);
            mat.SetColor("_GrassDark", Palette.GrassDark);
            mat.SetColor("_RockColor", Cliff);
            mat.SetColor("_RockDark", CliffDark);
            mat.SetColor("_SandColor", Palette.Sand);
            mat.SetColor("_SnowColor", Snow);
            mat.SetColor("_ShadowColor", Palette.ShadowTint);
            mat.SetFloat("_SandHeight", -89.6f); // below the lowland (about -87.5), so sand only shows on the beach slopes
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // The grey-box bridge as a wooden mesh with a deck and rails, under "2 Gorge/Bridge" like the decorated scene.
        private static void BuildTerrainBridge(List<Vector3> tris)
        {
            var gorge = new GameObject("2 Gorge").transform;
            gorge.SetParent(root, false);
            var block = new GameObject("Bridge").transform;
            block.SetParent(gorge, false);

            var body = new LowPolyMesh();
            var tops = new List<Vector3>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                body.Tri(tris[i], tris[i + 1], tris[i + 2]);
                if (Vector3.Cross(tris[i + 1] - tris[i], tris[i + 2] - tris[i]).normalized.y > 0.7f)
                    tops.AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
            }
            var bodyMesh = SaveMesh(body.Build("Mountain/Bridge Body", true));
            Spawn(block, "Cliff", bodyMesh, Mat("WoodDark", Palette.WoodDark, 1.5f), Vector3.zero).AddComponent<MeshCollider>().sharedMesh = bodyMesh;
            var deckMesh = SaveMesh(Cap(tops, "Mountain/Bridge Deck", 0.4f, 0.6f, 0.3f));
            Spawn(block, "Deck", deckMesh, Mat("Wood", Palette.Wood, 2f), Vector3.zero).AddComponent<MeshCollider>().sharedMesh = deckMesh;
            BuildBridgeRails();
        }

        private static Vector3 Summit(Terrain terrain)
        {
            var data = terrain.terrainData;
            int n = data.heightmapResolution;
            float[,] h = data.GetHeights(0, 0, n, n);
            int bestX = 0, bestZ = 0;
            float best = -1f;
            // The highest point, ignoring the very border.
            for (int zi = 8; zi < n - 8; zi++)
            for (int xi = 8; xi < n - 8; xi++)
                if (h[zi, xi] > best) { best = h[zi, xi]; bestX = xi; bestZ = zi; }
            // Step back towards the middle of the summit so the flag isn't on the lip.
            var p = new Vector3(TerrainOrigin.x + bestX / (n - 1f) * TerrainSize.x, 0f, TerrainOrigin.z + bestZ / (n - 1f) * TerrainSize.z);
            var centre = new Vector3(35f, 0f, -25f);
            p = Vector3.Lerp(p, centre, 0.6f);
            p.y = terrain.SampleHeight(p) + TerrainOrigin.y;
            return p;
        }

        private static void PlaceFlag(Vector3 at)
        {
            var flag = new GameObject("Summit Flag").transform;
            flag.SetParent(root, false);
            flag.position = at;
            Spawn(flag, "Pole", SaveMesh(LowPolyMesh.Box("Mountain/Flag Pole", new Vector3(0.8f, 16f, 0.8f))), Mat("WoodDark", Palette.WoodDark, 1.5f), Vector3.zero);
            Spawn(flag, "Flag", SaveMesh(LowPolyMesh.Box("Mountain/Flag", new Vector3(7f, 4.5f, 0.4f))), Mat("Roof", Palette.Roof, 2f), new Vector3(3.9f, 11f, 0f));
        }

        // ---------- Props ----------

        private static void RescatterTerrainProps(Scene scene)
        {
            root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == TerrainRootName) root = go.transform;
            if (root == null) return;
            var old = root.Find("Props");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            ScatterOnTerrain(root.GetComponentInChildren<Terrain>(), new System.Random(7));
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // Clusters of trees, bushes and rocks on flat ground away from cliff edges: autumn trees low down,
        // pines higher up, only rocks on the snowy summit. Boulders also gather at the foot of cliffs.
        private static void ScatterOnTerrain(Terrain terrain, System.Random random)
        {
            var props = new GameObject("Props").transform;
            props.SetParent(root, false);
            const float spacing = 16f, minX = -15f, maxX = 241f, minZ = -264f, maxZ = 17f;
            int tries = Mathf.RoundToInt((maxX - minX) * (maxZ - minZ) / (spacing * spacing));

            for (int t = 0; t < tries; t++)
            {
                var p = new Vector3(Range(random, minX, maxX), 0f, Range(random, minZ, maxZ));
                if (!FlatGround(terrain, p, 6f, out Vector3 at) || at.y < -89.5f) continue;
                int kind = random.Next(10);
                if (at.y > 60f)
                {
                    if (kind < 3) MountainRock(props, at, Range(random, 3f, 6f), random);
                    continue;
                }
                if (kind < 2) { MountainRock(props, at, Range(random, 3f, 7f), random); continue; }
                bool pine = at.y > 15f || (at.y > -70f && kind < 6) || kind == 9;
                if (pine)
                {
                    Pine(props, at, PropScale * Range(random, 1.1f, 1.6f));
                    Pine(props, Ground(terrain, at + Offset(random, 6f)), PropScale * Range(random, 0.8f, 1.2f));
                }
                else
                {
                    AutumnTree(props, at, CanopySeeds[random.Next(CanopySeeds.Length)], PropScale * Range(random, 1.0f, 1.4f));
                    Bush(props, Ground(terrain, at + Offset(random, 6f)), PropScale * Range(random, 0.8f, 1.3f), random.Next(2) == 0, random.Next(3));
                }
                if (random.Next(3) == 0)
                    Bush(props, Ground(terrain, at + Offset(random, 9f)), PropScale * Range(random, 0.7f, 1.1f), random.Next(3) == 0, random.Next(3));
            }

            // Boulders where flat ground meets a wall rising behind it.
            for (int t = 0; t < tries; t++)
            {
                var p = new Vector3(Range(random, minX, maxX), 0f, Range(random, minZ, maxZ));
                if (!FlatGround(terrain, p, 0f, out Vector3 at) || at.y < -90f) continue;
                bool wallNearby = false;
                foreach (var o in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                    wallNearby |= Ground(terrain, p + o * 6f).y > at.y + 6f;
                if (wallNearby) MountainRock(props, at, Range(random, 4f, 8f), random);
            }
        }

        private static Vector3 Ground(Terrain terrain, Vector3 p)
        {
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            return p;
        }

        // True if the ground at p is gentle and stays within a metre or so of the same height `margin` around it.
        private static bool FlatGround(Terrain terrain, Vector3 p, float margin, out Vector3 at)
        {
            at = Ground(terrain, p);
            var data = terrain.terrainData;
            Vector3 local = p - terrain.transform.position;
            Vector3 normal = data.GetInterpolatedNormal(local.x / data.size.x, local.z / data.size.z);
            if (local.x < 0f || local.z < 0f || local.x > data.size.x || local.z > data.size.z || normal.y < 0.9f) return false;
            if (margin <= 0f) return true;
            foreach (var o in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                if (Mathf.Abs(Ground(terrain, p + o * margin).y - at.y) > 1.2f) return false;
            return true;
        }
    }
}
