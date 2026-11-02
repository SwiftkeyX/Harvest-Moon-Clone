using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Bakes the grass shape map: where on the terrain the grass shapes should go, so they follow the land
    /// instead of being random. The terrain shader (Farm/Toon Terrain) reads it from above, in world XZ.
    ///   R = near a prop: darker grass pools round trees, bushes and rocks (a little more on their shady side)
    ///   G = foot of a slope: darker grass gathers where the ground dips (concave)
    ///   B = top of a ledge: lighter grass on ground that bulges up (convex)
    /// </summary>
    public static class GrassShapeMap
    {
        public const string TexturePath = "Assets/_Game/Art/Textures/MountainGrassShape.png";
        private const int Size = 512;

        [MenuItem("Farm/Art/Bake Grass Shape Map (Open Scene)")]
        public static void BakeMenu()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogWarning("Grass shape map: no terrain in the open scene.");
                return;
            }
            var props = terrain.transform.parent != null ? terrain.transform.parent.Find("Props") : null;
            Bake(terrain, props);
        }

        /// <summary>Bakes the map for this terrain and points its material at it.</summary>
        public static Texture2D Bake(Terrain terrain, Transform props)
        {
            var data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            Vector3 size = data.size;
            float metresPerTexel = size.x / Size;

            var height = new float[Size, Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                height[x, y] = data.GetInterpolatedHeight((x + 0.5f) / Size, (y + 0.5f) / Size);

            var near = PropField(props, origin, size, metresPerTexel);
            var foot = new float[Size, Size];
            var top = new float[Size, Size];
            // Curvature: the ground round a point (about 5 m away) compared with the point itself.
            int r = Mathf.Max(2, Mathf.RoundToInt(5f / metresPerTexel));
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float sum = 0;
                int n = 0;
                for (int a = 0; a < 8; a++)
                {
                    float ang = a * Mathf.PI / 4f;
                    int sx = Mathf.Clamp(x + Mathf.RoundToInt(Mathf.Cos(ang) * r), 0, Size - 1);
                    int sy = Mathf.Clamp(y + Mathf.RoundToInt(Mathf.Sin(ang) * r), 0, Size - 1);
                    sum += height[sx, sy];
                    n++;
                }
                float rise = sum / n - height[x, y]; // > 0: a dip, < 0: a bump
                foot[x, y] = Mathf.Clamp01(rise / 2.5f);
                top[x, y] = Mathf.Clamp01(-rise / 1.5f);
            }
            Blur(foot, 2);
            Blur(top, 2);

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = new Color(near[x, y], foot[x, y], top[x, y], 1f);
            tex.SetPixels(pixels);
            tex.Apply();

            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(TexturePath));
            File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var map = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            var mat = terrain.materialTemplate;
            if (mat != null && mat.HasProperty("_ShapeMap"))
            {
                mat.SetTexture("_ShapeMap", map);
                mat.SetVector("_ShapeRect", new Vector4(origin.x, origin.z, size.x, size.z));
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
            }
            Debug.Log($"Grass shape map baked ({Size}x{Size}, {metresPerTexel:F2} m per texel).");
            return map;
        }

        // Darker grass round every prop bigger than a flower: full inside its footprint, fading out over a few metres,
        // pushed a little away from the sun so it leans to the prop's shady side.
        private static float[,] PropField(Transform props, Vector3 origin, Vector3 size, float metresPerTexel)
        {
            var field = new float[Size, Size];
            if (props == null) return field;
            Vector3 away = Vector3.zero;
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                away = sun.transform.forward;
                away.y = 0;
                away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.zero;
            }
            foreach (var prop in Footprints(props))
            {
                Vector3 c = prop.center + away * prop.radius * 0.5f;
                float inner = prop.radius * 0.6f;
                float reach = prop.radius + 1.2f + prop.radius * 0.3f;
                int x0 = Mathf.FloorToInt((c.x - reach - origin.x) / metresPerTexel), x1 = Mathf.CeilToInt((c.x + reach - origin.x) / metresPerTexel);
                int y0 = Mathf.FloorToInt((c.z - reach - origin.z) / metresPerTexel), y1 = Mathf.CeilToInt((c.z + reach - origin.z) / metresPerTexel);
                for (int y = Mathf.Max(0, y0); y <= Mathf.Min(Size - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(Size - 1, x1); x++)
                {
                    float wx = origin.x + (x + 0.5f) * metresPerTexel, wz = origin.z + (y + 0.5f) * metresPerTexel;
                    float d = Vector2.Distance(new Vector2(wx, wz), new Vector2(c.x, c.z));
                    float v = 1f - Mathf.Clamp01((d - inner) / (reach - inner));
                    field[x, y] = Mathf.Max(field[x, y], v * v * (3f - 2f * v));
                }
            }
            return field;
        }

        private struct Footprint
        {
            public Vector3 center;
            public float radius;
        }

        // One footprint per prop: the direct children of Props, and the items inside each map pin's decor.
        private static IEnumerable<Footprint> Footprints(Transform props)
        {
            foreach (Transform child in props)
            {
                if (child.name == "Pin Decor")
                {
                    foreach (Transform pin in child)
                    foreach (Transform item in pin)
                        if (TryFootprint(item, out var f)) yield return f;
                    continue;
                }
                if (TryFootprint(child, out var footprint)) yield return footprint;
            }
        }

        private static bool TryFootprint(Transform t, out Footprint footprint)
        {
            footprint = default;
            if (t.name.StartsWith("Daisies") || t.name.StartsWith("Yellow Flowers")) return false; // flowers leave the grass alone
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            float radius = Mathf.Max(b.extents.x, b.extents.z);
            if (radius < 0.6f) return false; // flowers and pebbles leave the grass alone
            footprint = new Footprint { center = b.center, radius = Mathf.Min(radius, 6f) };
            return true;
        }

        private static void Blur(float[,] v, int passes)
        {
            var tmp = new float[Size, Size];
            for (int p = 0; p < passes; p++)
            {
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float s = 0;
                    for (int k = -1; k <= 1; k++) s += v[Mathf.Clamp(x + k, 0, Size - 1), y];
                    tmp[x, y] = s / 3f;
                }
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float s = 0;
                    for (int k = -1; k <= 1; k++) s += tmp[x, Mathf.Clamp(y + k, 0, Size - 1)];
                    v[x, y] = s / 3f;
                }
            }
        }
    }
}
