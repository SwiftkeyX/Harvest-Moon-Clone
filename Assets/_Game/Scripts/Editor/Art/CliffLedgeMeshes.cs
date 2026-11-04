using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Farm.EditorTools.Art
{
    // Builds flat-shaded meshes for the Cliff Ledge scene: rock prisms with a rounded grass lip,
    // cones for the pine, and double-sided leaf cards.
    internal class CliffLedgeMeshBuilder
    {
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<List<int>> _subs = new List<List<int>>();

        public CliffLedgeMeshBuilder(int submeshes = 1)
        {
            for (int i = 0; i < submeshes; i++) _subs.Add(new List<int>());
        }

        // Adds a triangle facing `outward`; each triangle gets its own vertices, so shading is flat.
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, int sub = 0, Color? color = null)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) (b, c) = (c, b);
            int i = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c);
            var col = color ?? Color.white;
            _colors.Add(col); _colors.Add(col); _colors.Add(col);
            _subs[sub].Add(i); _subs[sub].Add(i + 1); _subs[sub].Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, int sub = 0)
        {
            Tri(a, b, c, outward, sub);
            Tri(a, c, d, outward, sub);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_verts);
            mesh.SetColors(_colors);
            mesh.subMeshCount = _subs.Count;
            for (int i = 0; i < _subs.Count; i++) mesh.SetTriangles(_subs[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // A vertical prism over the xz polygon. `rings` describe the rounded top edge from the inside out:
        // (inset from the outline, height below the top, submesh). The top face uses ring 0 and submesh 0;
        // the walls below the last ring use the submesh `wallSub` picks from each wall's outward direction.
        // The walls are split into rows with a little jitter.
        public static Mesh Prism(string name, IList<Vector2> outline, float top, float bottom,
            IList<(float inset, float drop, int sub)> rings, System.Func<Vector3, int> wallSub, float jitter, int seed,
            int submeshes = 2)
        {
            var mb = new CliffLedgeMeshBuilder(submeshes);
            var rnd = new System.Random(seed);
            bool ccw = SignedArea(outline) > 0;
            int n = outline.Count;

            var ringPts = new List<Vector2[]>();
            foreach (var r in rings) ringPts.Add(Offset(outline, -r.inset, ccw));

            // Top face.
            var topPoly = ringPts[0];
            var tris = Triangulate(topPoly);
            float y0 = top - rings[0].drop;
            for (int t = 0; t < tris.Count; t += 3)
                mb.Tri(V(topPoly[tris[t]], y0), V(topPoly[tris[t + 1]], y0), V(topPoly[tris[t + 2]], y0), Vector3.up, rings[0].sub);

            // Rounded lip bands.
            for (int k = 1; k < rings.Count; k++)
            {
                float yi = top - rings[k - 1].drop, yo = top - rings[k].drop;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    var outN = EdgeOutward(outline[i], outline[j], ccw);
                    mb.Quad(V(ringPts[k - 1][i], yi), V(ringPts[k - 1][j], yi), V(ringPts[k][j], yo), V(ringPts[k][i], yo),
                        outN + Vector3.up, rings[k].sub);
                }
            }

            // Walls, in rows, with each row's outline pushed in or out a little.
            var last = ringPts[rings.Count - 1];
            float wallTop = top - rings[rings.Count - 1].drop;
            int rows = Mathf.Max(1, Mathf.CeilToInt((wallTop - bottom) / 2.5f));
            var prev = last;
            for (int r = 1; r <= rows; r++)
            {
                float ya = Mathf.Lerp(wallTop, bottom, (r - 1) / (float)rows);
                float yb = Mathf.Lerp(wallTop, bottom, r / (float)rows);
                var next = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    var p = last[i];
                    var d = VertexOutward(last, i, ccw);
                    next[i] = p + d * (float)((rnd.NextDouble() - 0.5) * 2 * jitter);
                }
                if (r == rows) next = (Vector2[])last.Clone();
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    var outN = EdgeOutward(last[i], last[j], ccw);
                    mb.Quad(V(prev[i], ya), V(prev[j], ya), V(next[j], yb), V(next[i], yb), outN, wallSub(outN));
                }
                prev = next;
            }
            return mb.ToMesh(name);
        }

        // A cone with `segments` sides and a flat bottom, pointing up. With `jag` > 0 every other rim point is pulled
        // in by that fraction, giving a star-shaped, spiky rim.
        public static void Cone(CliffLedgeMeshBuilder mb, Vector3 baseCentre, float radius, float height, int segments, float twist,
            int sub = 0, float jag = 0f)
        {
            var apex = baseCentre + Vector3.up * height;
            for (int i = 0; i < segments; i++)
            {
                float a0 = twist + i * Mathf.PI * 2 / segments, a1 = twist + (i + 1) * Mathf.PI * 2 / segments;
                float r0 = i % 2 == 0 ? radius : radius * (1f - jag), r1 = (i + 1) % 2 == 0 || i + 1 == segments ? radius : radius * (1f - jag);
                var p0 = baseCentre + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r0;
                var p1 = baseCentre + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r1;
                var mid = (p0 + p1) * 0.5f - baseCentre;
                mb.Tri(p0, p1, apex, mid.normalized * height + Vector3.up * radius, sub);
                mb.Tri(p0, p1, baseCentre, Vector3.down, sub);
            }
        }

        // A diamond leaf card in the local XY plane, one-sided: its front is local -Z, so turn it toward the camera.
        public static void LeafCard(CliffLedgeMeshBuilder mb, Matrix4x4 m, float length, float width, int sub = 0)
        {
            var a = m.MultiplyPoint3x4(new Vector3(0, 0, 0));
            var b = m.MultiplyPoint3x4(new Vector3(-width * 0.5f, length * 0.45f, 0));
            var c = m.MultiplyPoint3x4(new Vector3(0, length, 0));
            var d = m.MultiplyPoint3x4(new Vector3(width * 0.5f, length * 0.45f, 0));
            var f = m.MultiplyVector(Vector3.back);
            mb.Tri(a, b, c, f, sub); mb.Tri(a, c, d, f, sub);
        }

        static Vector3 V(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        static float SignedArea(IList<Vector2> p)
        {
            float a = 0;
            for (int i = 0; i < p.Count; i++)
            {
                var q = p[i]; var r = p[(i + 1) % p.Count];
                a += q.x * r.y - r.x * q.y;
            }
            return a * 0.5f;
        }

        static Vector3 EdgeOutward(Vector2 a, Vector2 b, bool ccw)
        {
            var d = (b - a).normalized;
            var o = ccw ? new Vector2(d.y, -d.x) : new Vector2(-d.y, d.x);
            return new Vector3(o.x, 0, o.y);
        }

        static Vector2 VertexOutward(IList<Vector2> p, int i, bool ccw)
        {
            int n = p.Count;
            var e0 = EdgeOutward(p[(i + n - 1) % n], p[i], ccw);
            var e1 = EdgeOutward(p[i], p[(i + 1) % n], ccw);
            var m = new Vector2(e0.x + e1.x, e0.z + e1.z);
            return m.sqrMagnitude < 1e-6f ? new Vector2(e0.x, e0.z) : m.normalized;
        }

        // Moves each vertex along its mitred outward direction (negative = inwards).
        static Vector2[] Offset(IList<Vector2> p, float amount, bool ccw)
        {
            int n = p.Count;
            var res = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var e0 = EdgeOutward(p[(i + n - 1) % n], p[i], ccw);
                var e1 = EdgeOutward(p[i], p[(i + 1) % n], ccw);
                var m = new Vector2(e0.x + e1.x, e0.z + e1.z).normalized;
                float cos = Mathf.Max(0.35f, Vector2.Dot(m, new Vector2(e0.x, e0.z)));
                res[i] = p[i] + m * (amount / cos);
            }
            return res;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a), d2 = Cross(c - b, p - b), d3 = Cross(a - c, p - c);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        // Ear clipping for a simple polygon.
        static List<int> Triangulate(IList<Vector2> p)
        {
            var idx = new List<int>();
            for (int i = 0; i < p.Count; i++) idx.Add(i);
            bool ccw = SignedArea(p) > 0;
            var tris = new List<int>();
            int guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                bool found = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                    float cr = Cross(p[b] - p[a], p[c] - p[b]);
                    if (ccw ? cr <= 0 : cr >= 0) continue;
                    bool inside = false;
                    foreach (int j in idx)
                    {
                        if (j == a || j == b || j == c) continue;
                        if (InTri(p[j], p[a], p[b], p[c])) { inside = true; break; }
                    }
                    if (inside) continue;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    idx.RemoveAt(i);
                    found = true;
                    break;
                }
                if (!found) break;
            }
            if (idx.Count == 3) tris.AddRange(idx);
            return tris;
        }
    }
}
