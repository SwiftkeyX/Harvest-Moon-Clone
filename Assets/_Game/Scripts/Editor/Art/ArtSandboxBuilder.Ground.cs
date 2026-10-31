using System.Collections.Generic;
using UnityEngine;

namespace Farm.EditorTools.Art
{
    // The ground: a faceted hill mesh with painted vertex colours, plus strokes and pebbles.
    public static partial class ArtSandboxBuilder
    {
        // A soft dark patch on the ground around a box (or a point, when half is zero).
        private struct Shade
        {
            public Vector2 Center, Half;
            public float Reach, Strength;
        }

        private static readonly List<Shade> shades = new List<Shade>();
        private static readonly List<Vector3> treeSpots = new List<Vector3>(); // x, z, radius

        private static void AddShade(float x, float z, float halfX, float halfZ, float reach, float strength) =>
            shades.Add(new Shade { Center = new Vector2(x, z), Half = new Vector2(halfX, halfZ), Reach = reach, Strength = strength });

        private static void RegisterTree(Vector3 p, float scale)
        {
            AddShade(p.x, p.z, 0.15f * scale, 0.15f * scale, 1.0f * scale, 0.8f);
            treeSpots.Add(new Vector3(p.x, p.z, 2.6f * scale));
        }

        // Flat inside the playable area, hills towards the back and the sides.
        private static float GroundHeight(float x, float z)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(x) - 8.5f), dz = Mathf.Max(0f, z - 7f);
            float ramp = Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(dx * dx + dz * dz) / 4f);
            if (ramp <= 0f) return 0f;
            float big = Mathf.Lerp(1.4f, 3f, Mathf.PerlinNoise(x * 0.11f + 40f, z * 0.11f + 17f));
            float bumps = Mathf.PerlinNoise(x * 0.35f + 5f, z * 0.35f + 9f) - 0.5f;
            return ramp * (big + bumps * 0.5f);
        }

        private static Vector3 OnGround(Vector3 p) => new Vector3(p.x, GroundHeight(p.x, p.z), p.z);

        private static Vector3 OnGround(float x, float z) => new Vector3(x, GroundHeight(x, z), z);

        private static float BoxDistance(float x, float z, Vector2 center, Vector2 half)
        {
            float dx = Mathf.Max(Mathf.Abs(x - center.x) - half.x, 0f);
            float dz = Mathf.Max(Mathf.Abs(z - center.y) - half.y, 0f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static Color Scale(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, 1f);

        private static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x - a) / (b - a)));

        private static float Perlin(float x, float z) => Mathf.PerlinNoise(x + 100f, z + 100f);

        private static Color GroundColor(float x, float z, float height)
        {
            // Low-frequency light/dark patches around the base grass.
            float n = Mathf.Clamp01((Perlin(x * 0.09f, z * 0.09f) - 0.5f) * 3.6f + 0.5f);
            Color c = n < 0.5f ? Color.Lerp(Palette.GrassLight, Palette.Grass, n * 2f)
                               : Color.Lerp(Palette.Grass, Palette.GrassDark, (n - 0.5f) * 2f);
            c = Scale(c, 1f + (Perlin(x * 0.6f, z * 0.6f) - 0.5f) * 0.10f);
            c = Color.Lerp(c, Palette.GrassDark, Mathf.Clamp01(height / 3f) * 0.45f);

            foreach (var t in treeSpots)
            {
                float d = Vector2.Distance(new Vector2(x, z), new Vector2(t.x, t.y));
                c = Color.Lerp(c, Scale(Palette.GrassDark, 0.88f), (1f - Mathf.SmoothStep(0f, 1f, d / t.z)) * 0.7f);
            }

            // Worn dirt: path edges, the door step and around the bin.
            float worn = 0f;
            float pathZ = Mathf.Clamp(z, -5.7f, 2.3f);
            float pathD = Vector2.Distance(new Vector2(x, z), new Vector2(-6.2f, pathZ));
            worn = Mathf.Max(worn, 0.7f * (1f - Smooth(0.55f, 1.35f, pathD)));
            float doorD = Vector2.Distance(new Vector2(x, z), new Vector2(-6.2f, 1.9f));
            worn = Mathf.Max(worn, 0.9f * (1f - Smooth(0.3f, 1.6f, doorD)));
            float binD = BoxDistance(x, z, new Vector2(-3.7f, 2.9f), new Vector2(0.45f, 0.3f));
            worn = Mathf.Max(worn, 0.8f * (1f - Smooth(0.2f, 1.2f, binD)));
            c = Color.Lerp(c, Color.Lerp(Palette.Sand, Palette.Soil, 0.45f), worn);

            // Contact shadows: a purple-blue darkening around solid objects.
            var shadeTint = new Color(0.66f, 0.64f, 0.82f);
            foreach (var s in shades)
            {
                float t = 1f - Smooth(0f, s.Reach, BoxDistance(x, z, s.Center, s.Half));
                c = Color.Lerp(c, c * shadeTint, t * s.Strength);
            }
            c.a = 1f;
            return c;
        }

        private static void BuildGround()
        {
            var ground = Group("Ground");

            // Grid of corners, with height and colour per corner (colours stored in linear space).
            const float cell = 0.75f;
            const float x0 = -30f, z0 = -12f;
            const int nx = 80, nz = 60;
            var pos = new Vector3[nx + 1, nz + 1];
            var col = new Color[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++)
            {
                float x = x0 + i * cell, z = z0 + j * cell;
                float h = GroundHeight(x, z);
                pos[i, j] = new Vector3(x, h, z);
                col[i, j] = GroundColor(x, z, h).linear;
            }

            var m = new LowPolyMesh();
            for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                Vector3 a = pos[i, j], b = pos[i, j + 1], c = pos[i + 1, j + 1], d = pos[i + 1, j];
                Color ca = col[i, j], cb = col[i, j + 1], cc = col[i + 1, j + 1], cd = col[i + 1, j];
                if ((i + j) % 2 == 0)
                {
                    m.Tri(a, b, c, ca, cb, cc);
                    m.Tri(a, c, d, ca, cc, cd);
                }
                else
                {
                    m.Tri(a, b, d, ca, cb, cd);
                    m.Tri(b, c, d, cb, cc, cd);
                }
            }
            // Vertex colours drive the grass, so the material is white.
            Spawn(ground, "Grass", SaveMesh(m.Build("Ground", true)), Mat("Grass", Color.white, 0f), Vector3.zero);
            Spawn(ground, "Path", SaveMesh(LowPolyMesh.Box("Path", new Vector3(1.2f, 0.03f, 8f))), Mat("Sand", Palette.Sand, 0f),
                new Vector3(-6.2f, 0f, -1.7f));

            BuildPaintedDetails(ground);
        }

        // Keeps strokes and pebbles off the plot, path, pond, house and bin.
        private static bool IsReserved(float x, float z)
        {
            if (x > -2.9f && x < 3.9f && z > -1.9f && z < 2.4f) return true;      // plot and fence
            if (x > -7.2f && x < -5.2f && z > -6f && z < 2.6f) return true;       // path
            if (x > -8.4f && x < -3.0f && z > 1.8f && z < 5.4f) return true;      // house and bin
            float ex = (x - 5.1f) / 2.3f, ez = (z + 2.2f) / 1.7f;
            return ex * ex + ez * ez < 1f;                                         // pond
        }

        private static void BuildPaintedDetails(Transform ground)
        {
            var random = new System.Random(7);

            // Grass strokes: thin diamonds lying on the ground.
            var stroke = new LowPolyMesh();
            Vector3 s0 = new Vector3(0f, 0.02f, -0.22f), s1 = new Vector3(-0.05f, 0.02f, 0f);
            Vector3 s2 = new Vector3(0f, 0.02f, 0.22f), s3 = new Vector3(0.05f, 0.02f, 0f);
            stroke.Quad(s0, s1, s2, s3);
            var strokeMesh = SaveMesh(stroke.Build("GrassStroke", true));
            var light = Mat("StrokeLight", Palette.GrassLight, 0f);
            var dark = Mat("StrokeDark", Palette.GrassDark, 0f);
            var strokes = Group("Grass Strokes", ground);
            int placed = 0;
            while (placed < 150)
            {
                float x = Range(random, -12f, 12f), z = Range(random, -5.5f, 8f);
                if (IsReserved(x, z) || GroundHeight(x, z) > 0.02f) continue;
                float len = Range(random, 0.8f, 1.6f);
                Spawn(strokes, "Stroke", strokeMesh, placed % 2 == 0 ? light : dark, new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), new Vector3(Range(random, 0.8f, 1.2f), 1f, len));
                placed++;
            }

            // Pebbles.
            var pebbleMat = Mat("Pebble", Palette.Pebble, 1f);
            var pebbles = Group("Pebbles", ground);
            placed = 0;
            while (placed < 30)
            {
                float x = Range(random, -12f, 12f), z = Range(random, -5.5f, 8f);
                if (IsReserved(x, z) || GroundHeight(x, z) > 0.02f) continue;
                var mesh = SaveMesh(LowPolyMesh.Blob("Pebble" + placed % 3, 2, 5, 0.1f, 0.25f, 60 + placed % 3));
                float s = Range(random, 0.6f, 1.2f);
                Spawn(pebbles, "Pebble", mesh, pebbleMat, new Vector3(x, 0.02f, z), Quaternion.Euler(0f, Range(random, 0f, 360f), 0f),
                    new Vector3(s, s * 0.6f, s));
                placed++;
            }
        }
    }
}
