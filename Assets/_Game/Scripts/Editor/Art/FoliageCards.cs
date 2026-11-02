using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Leaf cards: gives every leafy blob (bushes, tree canopies, birch leaves) a brushy outline.
    /// Each blob gets a "Leaf Cards" child: a cloud of camera-facing cards scattered over the blob's surface,
    /// cut out by a painted leaf-cluster texture and shaded as one soft ball (Farm/Toon Foliage).
    /// The solid blob stays inside to fill the gaps. Running it again only adds cards where they're missing.
    /// </summary>
    public static class FoliageCards
    {
        public const string ChildName = "Leaf Cards";
        private const string ArtRoot = "Assets/_Game/Art";
        private const string TexturePath = ArtRoot + "/Textures/LeafCluster.png";
        private const string MeshFolder = ArtRoot + "/Meshes/Foliage";
        private const string MaterialFolder = ArtRoot + "/Materials";
        private const string DecorPrefabFolder = ArtRoot + "/Prefabs/Decor";
        private const string ShaderName = "Farm/Toon Foliage";

        // Toon Lit materials that are leaves. Anything else (trunks, rocks, flowers, pines) is left alone.
        private static readonly HashSet<string> LeafyMaterials = new HashSet<string>
        {
            "BushGreen", "BushOrange", "LeafOrange", "LeafYellow", "LeafRed",
            "DecorBushOlive", "DecorBushDark", "DecorBushYellow", "DecorBirchLeaf", "DecorBirchLeafDeep",
            "Pine", "DecorFir",
        };

        private const float CoreBrightness = 0.6f;

        // Conifers get smaller cards, so their layered cone shape survives under the fluff.
        private static float CardScale(string materialName) => materialName == "Pine" || materialName == "DecorFir" ? 0.5f : 1f;

        private static readonly HashSet<string> builtThisRun = new HashSet<string>();

        [MenuItem("Farm/Art/Add Leaf Cards (Open Scene and Decor Prefabs)")]
        public static void AddToSceneAndPrefabsMenu()
        {
            builtThisRun.Clear();
            int prefabs = AddToDecorPrefabs();
            int added = 0;
            Scene scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects()) added += Decorate(go, skipPrefabInstances: true);
            if (added > 0) EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Leaf cards: {prefabs} decor prefabs updated, {added} blobs in {scene.name} given cards.");
        }

        [MenuItem("Farm/Art/Remove Leaf Cards (Open Scene)")]
        public static void RemoveFromSceneMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            var doomed = new List<GameObject>();
            foreach (var go in scene.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    if (t.name == ChildName && !PrefabUtility.IsPartOfPrefabInstance(t)) doomed.Add(t.gameObject);
            foreach (var go in doomed) Object.DestroyImmediate(go);
            if (doomed.Count > 0) EditorSceneManager.MarkSceneDirty(scene);
        }

        // Repaints the texture and regenerates every card mesh already in the project (in place).
        [MenuItem("Farm/Art/Rebuild Leaf Cards")]
        public static void RebuildMenu()
        {
            builtThisRun.Clear();
            RepaintTextureMenu();
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { MeshFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string sourceName = Path.GetFileNameWithoutExtension(path).Replace(" Cards", "");
                Mesh source = null;
                foreach (string g in AssetDatabase.FindAssets(sourceName + " t:Mesh", new[] { ArtRoot + "/Meshes" }))
                {
                    var m = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(g));
                    if (m != null && m.name == sourceName) { source = m; break; }
                }
                if (source != null) CardMesh(source, sourceName.Contains("Cone") ? 0.5f : 1f);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Farm/Art/Repaint Leaf Texture")]
        public static void RepaintTextureMenu()
        {
            PaintLeafTexture();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat.shader != null && mat.shader.name == ShaderName) mat.SetTexture("_LeafMap", LeafTexture());
            }
        }

        public static int AddToDecorPrefabs()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { DecorPrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (Decorate(contents, skipPrefabInstances: false) > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        changed++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            return changed;
        }

        /// <summary>Adds leaf cards under every leafy blob below <paramref name="root"/>. Returns how many were added.</summary>
        public static int Decorate(GameObject root, bool skipPrefabInstances)
        {
            int added = 0;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var source = renderer.sharedMaterial;
                if (source == null || !LeafyMaterials.Contains(source.name)) continue;
                // The blob inside becomes a darker, round core, so the gaps between cards read as depth, not facets.
                if (source.HasProperty("_SphereNormal") && (source.GetFloat("_SphereNormal") < 1f || source.GetFloat("_Brightness") != CoreBrightness))
                {
                    source.SetFloat("_SphereNormal", 1f);
                    source.SetFloat("_Brightness", CoreBrightness);
                    EditorUtility.SetDirty(source);
                }
                if (skipPrefabInstances && PrefabUtility.IsPartOfPrefabInstance(renderer)) continue;
                var existingCards = renderer.transform.Find(ChildName);
                if (existingCards != null)
                {
                    LeafMaterial(source); // keep colours in step
                    continue;
                }
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                var cards = new GameObject(ChildName);
                cards.transform.SetParent(renderer.transform, false);
                cards.AddComponent<MeshFilter>().sharedMesh = CardMesh(filter.sharedMesh, CardScale(source.name));
                cards.AddComponent<MeshRenderer>().sharedMaterial = LeafMaterial(source);
                added++;
            }
            return added;
        }

        // ---------- Card meshes ----------

        // Cards scattered evenly over the blob's surface (by area), each about three quarters of the blob's radius wide,
        // so their painted edges stick out past the blob. Normals point away from the blob's middle.
        public static Mesh CardMesh(Mesh source, float scale = 1f)
        {
            string path = $"{MeshFolder}/{source.name} Cards.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null && !builtThisRun.Add(path)) return existing; // rebuilt once per run, then shared

            var vertices = source.vertices;
            var triangles = source.triangles;
            var bounds = source.bounds;
            Vector3 centre = bounds.center;
            float radius = (bounds.extents.x + bounds.extents.y + bounds.extents.z) / 3f;
            float cardSize = radius * 0.95f * scale;

            var areas = new float[triangles.Length / 3];
            float total = 0f;
            for (int t = 0; t < areas.Length; t++)
            {
                Vector3 a = vertices[triangles[t * 3]], b = vertices[triangles[t * 3 + 1]], c = vertices[triangles[t * 3 + 2]];
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                areas[t] = total;
            }
            int count = Mathf.Clamp(Mathf.RoundToInt(total / (cardSize * cardSize) * 3.2f), 16, 260);

            var random = new System.Random(source.name.GetHashCode());
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var corners = new List<Vector2>();
            var cardData = new List<Vector4>();
            var indices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                float pick = (float)random.NextDouble() * total;
                int t = System.Array.BinarySearch(areas, pick);
                if (t < 0) t = Mathf.Min(~t, areas.Length - 1);
                Vector3 a = vertices[triangles[t * 3]], b = vertices[triangles[t * 3 + 1]], c = vertices[triangles[t * 3 + 2]];
                float u = (float)random.NextDouble(), v = (float)random.NextDouble();
                if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                Vector3 p = a + (b - a) * u + (c - a) * v;
                Vector3 face = Vector3.Cross(b - a, c - a).normalized;
                Vector3 outward = (p - centre).normalized;
                Vector3 normal = Vector3.Lerp(face, outward, 0.8f).normalized;
                p += normal * cardSize * 0.08f;

                float size = cardSize * Mathf.Lerp(0.8f, 1.2f, (float)random.NextDouble());
                float spin = (float)random.NextDouble() * Mathf.PI * 2f;
                float shade = (float)random.NextDouble() * 2f - 1f;
                int first = positions.Count;
                foreach (var corner in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) })
                {
                    positions.Add(p);
                    normals.Add(normal);
                    corners.Add(corner);
                    cardData.Add(new Vector4(size, spin, shade, 0f));
                }
                indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            var mesh = new Mesh { name = source.name + " Cards" };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, corners);
            mesh.SetUVs(1, cardData);
            mesh.SetTriangles(indices, 0);
            // The corners only spread out on the GPU, so grow the bounds by a card or the cloud gets culled early.
            var b2 = bounds;
            b2.Expand(cardSize * 2.6f);
            mesh.bounds = b2;
            if (existing != null)
            {
                // Copy into the existing asset so scenes and prefabs keep pointing at it.
                existing.Clear();
                existing.SetVertices(positions);
                existing.SetNormals(normals);
                existing.SetUVs(0, corners);
                existing.SetUVs(1, cardData);
                existing.SetTriangles(indices, 0);
                existing.bounds = b2;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            EditorAssetUtil.EnsureFolder(MeshFolder);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------- Materials ----------

        // "Leaves" + the blob's material name, in the blob's colour.
        public static Material LeafMaterial(Material source)
        {
            string path = $"{MaterialFolder}/Leaves{source.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(ShaderName)) { name = "Leaves" + source.name };
                mat.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                if (source.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", source.GetColor("_ShadowColor"));
                mat.SetTexture("_LeafMap", LeafTexture());
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, path);
            }
            // Always follow the blob's colour, so retinting a bush retints its leaves.
            mat.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            mat.SetFloat("_HighlightStrength", 0.08f);
            if (source.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", source.GetColor("_ShadowColor"));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- Leaf texture ----------

        private static Texture2D LeafTexture()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (tex == null)
            {
                PaintLeafTexture();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            }
            return tex;
        }

        // A round cluster of pointed leaves, painted back to front. Each leaf gets its own shade and a darker
        // lower edge, so overlapping leaves read as separate brush strokes. White means "use the material colour".
        private static void PaintLeafTexture()
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            var random = new System.Random(7);
            for (int leaf = 0; leaf < 13; leaf++)
            {
                float ring = Mathf.Sqrt((float)random.NextDouble()) * 0.22f;
                float around = (float)random.NextDouble() * Mathf.PI * 2f;
                var centre = new Vector2(0.5f + Mathf.Cos(around) * ring, 0.5f + Mathf.Sin(around) * ring);
                float angle = around + ((float)random.NextDouble() - 0.5f) * 1.4f; // mostly pointing outwards
                float length = Mathf.Lerp(0.16f, 0.24f, (float)random.NextDouble());
                float width = length * Mathf.Lerp(0.5f, 0.65f, (float)random.NextDouble());
                float shade = Mathf.Lerp(0.86f, 1f, (float)random.NextDouble());
                var along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var across = new Vector2(-along.y, along.x);

                int min = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(centre.x, centre.y) - length) * size));
                int max = Mathf.Min(size - 1, Mathf.CeilToInt((Mathf.Max(centre.x, centre.y) + length) * size));
                for (int y = min; y <= max; y++)
                for (int x = min; x <= max; x++)
                {
                    var d = new Vector2((x + 0.5f) / size, (y + 0.5f) / size) - centre;
                    float t = Vector2.Dot(d, along) / length;
                    if (t <= -1f || t >= 1f) continue;
                    float half = width * Mathf.Pow(1f - t * t, 0.6f) * (t > 0f ? 1f - t * 0.35f : 1f); // round base, pointed tip
                    float s = Vector2.Dot(d, across);
                    if (Mathf.Abs(s) >= half) continue;
                    // The lower edge (relative to the image) is darker, like a shadowed stroke.
                    bool under = s * Mathf.Sign(across.y + 1e-4f) < -half * 0.45f;
                    byte v = (byte)(255 * shade * (under ? 0.86f : 1f));
                    pixels[y * size + x] = new Color32(v, v, v, 255);
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(TexturePath).Replace('\\', '/'));
            File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = true; // keeps the cut-out the same size when far away
            importer.alphaTestReferenceValue = 0.5f;
            importer.SaveAndReimport();
        }
    }
}
