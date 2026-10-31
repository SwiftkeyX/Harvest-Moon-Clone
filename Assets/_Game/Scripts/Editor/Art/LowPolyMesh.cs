using System.Collections.Generic;
using UnityEngine;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Builds simple low-poly meshes for the art prototype. Faceted meshes get one normal per
    /// triangle; every mesh also stores smoothed normals in UV3 so the toon outline doesn't
    /// split at hard edges.
    /// </summary>
    internal sealed class LowPolyMesh
    {
        private readonly List<Vector3> corners = new List<Vector3>();
        private readonly List<Color> cornerColors = new List<Color>();
        private bool hasColors;

        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            corners.Add(a);
            corners.Add(b);
            corners.Add(c);
            cornerColors.Add(Color.white);
            cornerColors.Add(Color.white);
            cornerColors.Add(Color.white);
        }

        /// <summary>A triangle with a vertex colour per corner. Meshes with any colour write mesh.colors.</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color colA, Color colB, Color colC)
        {
            Tri(a, b, c);
            int n = cornerColors.Count;
            cornerColors[n - 3] = colA;
            cornerColors[n - 2] = colB;
            cornerColors[n - 1] = colC;
            hasColors = true;
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        public Mesh Build(string name, bool faceted)
        {
            int count = corners.Count;
            var faceNormals = new Vector3[count];
            var smooth = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < count; i += 3)
            {
                Vector3 n = Vector3.Cross(corners[i + 1] - corners[i], corners[i + 2] - corners[i]);
                for (int k = 0; k < 3; k++)
                {
                    faceNormals[i + k] = n.normalized;
                    Vector3 key = Round(corners[i + k]);
                    smooth.TryGetValue(key, out Vector3 sum);
                    smooth[key] = sum + n; // area-weighted
                }
            }

            var normals = new Vector3[count];
            var smoothNormals = new List<Vector3>(count);
            var uvs = new Vector2[count];
            var indices = new int[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 s = smooth[Round(corners[i])].normalized;
                normals[i] = faceted ? faceNormals[i] : s;
                smoothNormals.Add(s);
                uvs[i] = new Vector2(corners[i].x, corners[i].z);
                indices[i] = i;
            }

            var mesh = new Mesh { name = name };
            if (count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(corners);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(3, smoothNormals);
            if (hasColors) mesh.SetColors(cornerColors);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static Vector3 Round(Vector3 v) =>
            new Vector3(Mathf.Round(v.x * 1000f), Mathf.Round(v.y * 1000f), Mathf.Round(v.z * 1000f));

        // ---- Shapes. All sit on y = 0 unless noted. ----

        public static Mesh Box(string name, Vector3 size)
        {
            var m = new LowPolyMesh();
            Vector3 h = new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            float y = size.y;
            var p = new[]
            {
                new Vector3(-h.x, 0, -h.z), new Vector3(h.x, 0, -h.z), new Vector3(h.x, 0, h.z), new Vector3(-h.x, 0, h.z),
                new Vector3(-h.x, y, -h.z), new Vector3(h.x, y, -h.z), new Vector3(h.x, y, h.z), new Vector3(-h.x, y, h.z),
            };
            m.Quad(p[4], p[7], p[6], p[5]); // top
            m.Quad(p[0], p[1], p[2], p[3]); // bottom
            m.Quad(p[0], p[4], p[5], p[1]); // front (-z)
            m.Quad(p[1], p[5], p[6], p[2]); // right
            m.Quad(p[2], p[6], p[7], p[3]); // back
            m.Quad(p[3], p[7], p[4], p[0]); // left
            return m.Build(name, true);
        }

        /// <summary>A cylinder or cone (topRadius 0) with few sides.</summary>
        public static Mesh Cylinder(string name, int sides, float bottomRadius, float topRadius, float height, bool faceted = true)
        {
            var m = new LowPolyMesh();
            Vector3 top = new Vector3(0, height, 0);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 b0 = d0 * bottomRadius, b1 = d1 * bottomRadius;
                if (topRadius > 0f)
                {
                    Vector3 t0 = d0 * topRadius + top, t1 = d1 * topRadius + top;
                    m.Quad(b0, t0, t1, b1);
                    m.Tri(top, t1, t0);
                }
                else
                {
                    m.Tri(b0, top, b1);
                }
                m.Tri(Vector3.zero, b0, b1);
            }
            return m.Build(name, faceted);
        }

        /// <summary>A rough ball centred on the origin. Jitter moves the corners for a hand-made look.</summary>
        public static Mesh Blob(string name, int rings, int segments, float radius, float jitter, int seed, bool faceted = true)
        {
            var random = new System.Random(seed);
            var grid = new Vector3[rings + 1, segments];
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s < segments; s++)
                {
                    float theta = Mathf.PI * 2f * s / segments + (r % 2) * Mathf.PI / segments;
                    Vector3 dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    bool pole = r == 0 || r == rings;
                    float wobble = pole ? 1f : 1f + ((float)random.NextDouble() * 2f - 1f) * jitter;
                    grid[r, s] = pole ? dir * radius : dir * radius * wobble;
                }
            }

            var m = new LowPolyMesh();
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int n = (s + 1) % segments;
                    Vector3 a = grid[r, s], b = grid[r, n], c = grid[r + 1, n], d = grid[r + 1, s];
                    if (r == 0) m.Tri(a, c, d);
                    else if (r == rings - 1) m.Tri(a, b, d);
                    else m.Quad(a, b, c, d);
                }
            }
            return m.Build(name, faceted);
        }

        /// <summary>A gable roof: a triangular prism along x, with overhang.</summary>
        public static Mesh Roof(string name, float width, float depth, float height)
        {
            var m = new LowPolyMesh();
            float hx = width * 0.5f, hz = depth * 0.5f;
            Vector3 fl = new Vector3(-hx, 0, -hz), fr = new Vector3(hx, 0, -hz);
            Vector3 bl = new Vector3(-hx, 0, hz), br = new Vector3(hx, 0, hz);
            Vector3 tl = new Vector3(-hx, height, 0), tr = new Vector3(hx, height, 0);
            m.Quad(fl, tl, tr, fr); // front slope
            m.Quad(br, tr, tl, bl); // back slope
            m.Tri(bl, tl, fl);      // left gable
            m.Tri(fr, tr, br);      // right gable
            m.Quad(fl, fr, br, bl); // underside
            return m.Build(name, true);
        }

        /// <summary>A flat leaf: a thin diamond pointing along +z, tilted up.</summary>
        public static Mesh Leaf(string name, float length, float width, float thickness)
        {
            var m = new LowPolyMesh();
            Vector3 root = Vector3.zero, tip = new Vector3(0, 0, length);
            Vector3 l = new Vector3(-width * 0.5f, 0, length * 0.45f), r = new Vector3(width * 0.5f, 0, length * 0.45f);
            Vector3 up = new Vector3(0, thickness, length * 0.45f), down = new Vector3(0, -thickness, length * 0.45f);
            m.Tri(root, l, up); m.Tri(root, up, r); m.Tri(tip, up, l); m.Tri(tip, r, up);
            m.Tri(root, down, l); m.Tri(root, r, down); m.Tri(tip, l, down); m.Tri(tip, down, r);
            return m.Build(name, true);
        }
    }
}
