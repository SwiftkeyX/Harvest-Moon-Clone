using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Painted flowers for the pin decor: a brushy tuft of leaf cards at the bottom (the same painted leaf clusters
    /// the bushes use) and one card per flower that always faces the camera, with the stem and head painted on it.
    /// Both use Farm/Toon Foliage, so they shade softly like the bushes instead of looking like solid plastic shapes.
    /// </summary>
    public static class FlowerCards
    {
        public enum Kind { Yellow, Daisy }

        private const string ArtRoot = "Assets/_Game/Art";
        private const string MeshFolder = ArtRoot + "/Meshes/Foliage";
        private const string TextureFolder = ArtRoot + "/Textures";
        private const string ShaderName = "Farm/Toon Foliage";

        // Where the head sits on a flower card (0..1 from the bottom). The stem runs from the bottom up to it.
        public const float HeadHeight = 0.72f;

        public struct Card
        {
            public Vector3 position; // the card's middle
            public Vector3 normal;   // which way it shades
            public float size;
            public float spin;       // radians; flower cards keep 0 so they stay upright on screen
            public float shade;      // -1..1 jitter
        }

        /// <summary>Saves (or refreshes in place) a card mesh for Farm/Toon Foliage.</summary>
        public static Mesh CardMesh(string name, List<Card> cards)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var corners = new List<Vector2>();
            var data = new List<Vector4>();
            var indices = new List<int>();
            float largest = 0f;
            foreach (var card in cards)
            {
                int first = positions.Count;
                foreach (var corner in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) })
                {
                    positions.Add(card.position);
                    normals.Add(card.normal.normalized);
                    corners.Add(corner);
                    data.Add(new Vector4(card.size, card.spin, card.shade, 0f));
                }
                indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
                largest = Mathf.Max(largest, card.size);
            }

            string path = $"{MeshFolder}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                EditorAssetUtil.EnsureFolder(MeshFolder);
                mesh = new Mesh { name = name };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, corners);
            mesh.SetUVs(1, data);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(largest); // the corners only spread out on the GPU
            mesh.bounds = bounds;
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        /// <summary>The material for one kind of flower card. Colour comes from the painted texture.</summary>
        public static Material HeadMaterial(Kind kind, Color leaf, Color petal, Color middle)
        {
            string name = kind == Kind.Yellow ? "DecorFlowerYellowCard" : "DecorFlowerDaisyCard";
            string path = $"{ArtRoot}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(ShaderName)) { name = name, enableInstancing = true };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_LeafMap", PaintFlower(kind, leaf, petal, middle));
            mat.SetColor("_ShadowColor", new Color(0.72f, 0.70f, 0.86f));
            mat.SetFloat("_ShadowThreshold", -0.6f); // heads stay lit unless something shades them
            mat.SetFloat("_HighlightStrength", 0f);
            mat.SetFloat("_ShadeJitter", 0.06f);
            mat.SetFloat("_AmbientStrength", 0.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- Painting ----------

        // One flower on a stem, seen a little from above (the head is squashed upright). Petals are painted back to front,
        // each with its own shade and a darker lower edge, so they read as separate brush strokes.
        private static Texture2D PaintFlower(Kind kind, Color leaf, Color petal, Color middle)
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            var random = new System.Random(kind == Kind.Yellow ? 31 : 47);
            var head = new Vector2(0.5f, HeadHeight);
            const float squash = 0.72f;

            // Stem: slightly bent, darker on its right side.
            for (int y = 0; y < Mathf.RoundToInt(head.y * size); y++)
            {
                float v = (y + 0.5f) / size;
                float centre = 0.5f + Mathf.Sin(v * 3.2f) * 0.015f;
                float half = Mathf.Lerp(0.022f, 0.016f, v / head.y);
                for (int x = 0; x < size; x++)
                {
                    float d = (x + 0.5f) / size - centre;
                    if (Mathf.Abs(d) < half) pixels[y * size + x] = (Color32)(leaf * (d > half * 0.2f ? 0.82f : 1f));
                }
            }

            int petals = kind == Kind.Yellow ? 5 : 11;
            float length = kind == Kind.Yellow ? 0.2f : 0.25f;
            float width = kind == Kind.Yellow ? 0.13f : 0.055f;
            float start = (float)random.NextDouble() * Mathf.PI * 2f;
            // Back petals (top of the image) first, so the front ones overlap them.
            var order = new List<int>();
            for (int i = 0; i < petals; i++) order.Add(i);
            order.Sort((a, b) => Mathf.Sin(start + b * Mathf.PI * 2f / petals).CompareTo(Mathf.Sin(start + a * Mathf.PI * 2f / petals)));
            foreach (int i in order)
            {
                float angle = start + i * Mathf.PI * 2f / petals + ((float)random.NextDouble() - 0.5f) * 0.25f;
                float len = length * Mathf.Lerp(0.88f, 1.08f, (float)random.NextDouble());
                float shade = Mathf.Lerp(0.88f, 1f, (float)random.NextDouble());
                var along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var across = new Vector2(-along.y, along.x);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var d = new Vector2((x + 0.5f) / size - head.x, ((y + 0.5f) / size - head.y) / squash);
                    float t = Vector2.Dot(d, along) / len; // 0 at the middle, 1 at the tip
                    if (t <= 0f || t >= 1f) continue;
                    // Narrow at the middle, widest two thirds out, round at the tip.
                    float half = width * Mathf.Sqrt(Mathf.Clamp01(t * 1.6f)) * Mathf.Sqrt(Mathf.Clamp01((1f - t) * 3f));
                    float s = Vector2.Dot(d, across);
                    if (Mathf.Abs(s) >= half) continue;
                    bool under = s * Mathf.Sign(across.y + 1e-4f) < -half * 0.35f;
                    // Front petals (lower on screen) are a touch darker: they tilt away from the light.
                    float front = along.y < 0f ? 0.94f : 1f;
                    var c = petal * shade * front * (under ? 0.86f : 1f);
                    if (kind == Kind.Yellow && t < 0.35f) c *= 0.88f; // a warmer cup inside
                    c.a = 1f;
                    pixels[y * size + x] = (Color32)c;
                }
            }

            // The middle: a disc, darker on its lower side.
            float r = kind == Kind.Yellow ? 0.045f : 0.065f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var d = new Vector2((x + 0.5f) / size - head.x, ((y + 0.5f) / size - head.y) / squash);
                if (d.magnitude >= r) continue;
                var c = middle * (d.y < -r * 0.25f ? 0.82f : 1f);
                c.a = 1f;
                pixels[y * size + x] = (Color32)c;
            }

            string path = $"{TextureFolder}/Flower{kind}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            EditorAssetUtil.EnsureFolder(TextureFolder);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.5f;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
