using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Tall grass: clumps of painted blades round the foot of every bush, tree and rock in MountainTerrain.unity,
    /// so props sit in the grass instead of on it. All clumps go into one mesh (one draw call) under
    /// "Mountain Terrain/Tall Grass", drawn with Farm/Toon Foliage in upright mode. Running it again replaces them.
    /// </summary>
    public static class TallGrassClumps
    {
        private const string GroupName = "Tall Grass";
        private const string ArtRoot = "Assets/_Game/Art";
        private const string TexturePath = ArtRoot + "/Textures/GrassClump.png";
        private const string MaterialPath = ArtRoot + "/Materials/LeavesTallGrass.mat";
        private const string MeshPath = ArtRoot + "/Meshes/Foliage/Tall Grass.asset";
        private static readonly Color GrassColour = new Color(0.56f, 0.70f, 0.27f);

        [MenuItem("Farm/Art/Grow Tall Grass Round Props (Open Scene)")]
        public static void GrowMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            Transform root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Mountain Terrain") root = go.transform;
            var terrain = root != null ? root.GetComponentInChildren<Terrain>() : null;
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Tall grass", "Open MountainTerrain.unity first.", "OK");
                return;
            }
            int clumps = Grow(root, terrain);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Tall grass: {clumps} clumps round the props.");
        }

        public static int Grow(Transform root, Terrain terrain)
        {
            var old = root.Find(GroupName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Every prop: the direct children of Props, and the items inside each pin group of Pin Decor.
            var props = new List<Transform>();
            var propsGroup = root.Find("Props");
            if (propsGroup != null)
                foreach (Transform child in propsGroup)
                {
                    if (child.name == "Pin Decor") foreach (Transform pin in child) foreach (Transform item in pin) props.Add(item);
                    else props.Add(child);
                }
            var pinDecor = root.Find("Pin Decor");
            if (pinDecor != null) foreach (Transform pin in pinDecor) foreach (Transform item in pin) props.Add(item);

            var random = new System.Random(5);
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var corners = new List<Vector2>();
            var cardData = new List<Vector4>();
            var indices = new List<int>();
            int clumps = 0;
            foreach (var prop in props)
            {
                if (!Footprint(prop, out Vector3 foot, out float radius)) continue;
                // Flowers already are low plants; give them a single tuft. Everything else gets a ring.
                bool small = prop.name.StartsWith("Daisies") || prop.name.StartsWith("Yellow Flowers");
                int count = small ? 1 : Mathf.Clamp(Mathf.RoundToInt(radius * 3f), 2, 7);
                for (int i = 0; i < count; i++)
                {
                    float a = Range(random, 0f, Mathf.PI * 2f);
                    float r = radius * Range(random, 0.85f, 1.25f) + 0.15f;
                    var p = foot + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    if (!OnFlatGround(terrain, ref p)) continue;
                    AddClump(p, Range(random, 0.75f, 1.15f), random, positions, normals, corners, cardData, indices);
                    clumps++;
                }
            }

            string meshPath = EditorAssetUtil.PerScenePath(MeshPath, root.gameObject.scene);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Tall Grass" };
                EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(meshPath).Replace('\\', '/'));
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, corners);
            mesh.SetUVs(1, cardData);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(3f); // cards grow on the GPU
            mesh.bounds = bounds;
            EditorUtility.SetDirty(mesh);

            var group = new GameObject(GroupName);
            group.transform.SetParent(root, false);
            group.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = group.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GrassMaterial();
            return clumps;
        }

        // A clump: four or five upright cards, slightly apart and leaning different ways.
        private static void AddClump(Vector3 at, float size, System.Random random, List<Vector3> positions, List<Vector3> normals,
            List<Vector2> corners, List<Vector4> cardData, List<int> indices)
        {
            int cards = random.Next(4, 6);
            for (int c = 0; c < cards; c++)
            {
                var p = at + new Vector3(Range(random, -0.25f, 0.25f), -0.05f, Range(random, -0.25f, 0.25f)) * size;
                var data = new Vector4(size * Range(random, 0.7f, 1.05f), Range(random, -0.25f, 0.25f), Range(random, -1f, 1f), 0f);
                int first = positions.Count;
                foreach (var corner in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) })
                {
                    positions.Add(p);
                    normals.Add(Vector3.up); // lit like the ground it grows from
                    corners.Add(corner);
                    cardData.Add(data);
                }
                indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
        }

        // Where the prop stands and how wide it is, from its renderers.
        private static bool Footprint(Transform prop, out Vector3 foot, out float radius)
        {
            foot = prop.position;
            radius = 0f;
            var renderers = prop.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return false;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            radius = Mathf.Min(Mathf.Max(b.extents.x, b.extents.z), 4f);
            foot = new Vector3(b.center.x, prop.position.y, b.center.z);
            return true;
        }

        private static bool OnFlatGround(Terrain terrain, ref Vector3 p)
        {
            var data = terrain.terrainData;
            var local = p - terrain.transform.position;
            float u = local.x / data.size.x, v = local.z / data.size.z;
            if (u < 0f || u > 1f || v < 0f || v > 1f) return false;
            if (data.GetInterpolatedNormal(u, v).y < 0.8f) return false;
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            return true;
        }

        private static Material GrassMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Farm/Toon Foliage")) { name = "LeavesTallGrass" };
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.SetColor("_BaseColor", GrassColour);
            mat.SetColor("_ShadowColor", new Color(0.70f, 0.63f, 0.80f));
            mat.SetFloat("_Upright", 1f);
            mat.SetFloat("_HighlightStrength", 0f);
            mat.SetFloat("_ShadeJitter", 0.1f);
            mat.SetTexture("_LeafMap", GrassTexture());
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static bool repainted;

        private static Texture2D GrassTexture()
        {
            if (!repainted) PaintGrassTexture(); // once per Editor session, so edits to the painter show up
            repainted = true;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        // Blades fanning up from the bottom middle: tapered, slightly bent, darker at the root.
        private static void PaintGrassTexture()
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            var random = new System.Random(11);
            for (int blade = 0; blade < 10; blade++)
            {
                float rootX = 0.5f + Range(random, -0.14f, 0.14f);
                float lean = Range(random, -0.38f, 0.38f);
                float height = Range(random, 0.55f, 0.97f);
                float width = Range(random, 0.06f, 0.09f);
                float bend = Range(random, -0.1f, 0.1f);
                float shade = blade % 3 == 0 ? 0.88f : 1f;
                for (int y = 0; y < size; y++)
                {
                    float t = (y + 0.5f) / size / height;
                    if (t >= 1f) break;
                    float centre = rootX + lean * t + bend * t * t;
                    float half = width * (1f - t);
                    int x0 = Mathf.Max(0, Mathf.FloorToInt((centre - half) * size));
                    int x1 = Mathf.Min(size - 1, Mathf.CeilToInt((centre + half) * size));
                    byte v = (byte)(255 * shade * Mathf.Lerp(0.72f, 1f, t));
                    for (int x = x0; x <= x1; x++)
                        if (Mathf.Abs((x + 0.5f) / size - centre) < half) pixels[y * size + x] = new Color32(v, v, v, 255);
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
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.5f;
            importer.SaveAndReimport();
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
