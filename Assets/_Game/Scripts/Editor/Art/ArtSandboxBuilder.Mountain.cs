using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Dresses the hand-made grey-box mountain in the art style. It copies Graybox.unity to
    /// GrayboxDecorated.unity and only changes the copy: each block becomes a cliff with a chunky
    /// grass lip on top, the island gets a sea and beaches, and the shelves get trees, bushes and rocks.
    /// </summary>
    public static partial class ArtSandboxBuilder
    {
        private const string GreyBoxPath = "Assets/_Game/Scenes/Graybox.unity";
        private const string DecoratedPath = "Assets/_Game/Scenes/GrayboxDecorated.unity";
        private const float PropScale = 6.5f; // the mountain is far bigger than the farm, so props are scaled up to read from afar

        private static readonly Color Cliff = new Color(0.86f, 0.66f, 0.50f);
        private static readonly Color Snow = new Color(0.96f, 0.97f, 1.00f);
        private static readonly Color CliffDark = new Color(0.76f, 0.54f, 0.42f);

        [MenuItem("Farm/Decorate Grey-Box Mountain")]
        public static void DecorateMountain()
        {
            bool keepOpenScenes = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                keepOpenScenes |= SceneManager.GetSceneAt(i).isDirty;
            Scene previousActive = SceneManager.GetActiveScene();

            // Overwrite the file in place so the decorated scene keeps its GUID (and any links to it).
            if (System.IO.File.Exists(DecoratedPath))
            {
                System.IO.File.Copy(GreyBoxPath, DecoratedPath, true);
                AssetDatabase.ImportAsset(DecoratedPath, ImportAssetOptions.ForceUpdate);
            }
            else
                AssetDatabase.CopyAsset(GreyBoxPath, DecoratedPath);
            Scene scene = EditorSceneManager.OpenScene(DecoratedPath,
                keepOpenScenes ? OpenSceneMode.Additive : OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            // Only ever touch the copy: the grey-box scene may be open beside it with the same names.
            root = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Mountain") root = go.transform;
                else Object.DestroyImmediate(go); // old light, camera and player
            }
            if (root == null)
            {
                Debug.LogError($"No 'Mountain' object in {GreyBoxPath}; nothing to decorate.");
                return;
            }

            var surfaces = new List<(Transform block, string name)>();
            foreach (var pb in root.GetComponentsInChildren<ProBuilderMesh>(true))
            {
                string name = pb.name;
                surfaces.Add((DressBlock(pb), name));
            }
            BuildSea();
            BuildMountainLighting();

            Physics.SyncTransforms();
            mountainColliders = root.GetComponentsInChildren<MeshCollider>();
            var random = new System.Random(7);
            foreach (var (block, name) in surfaces)
                Scatter(block, name, random);
            Vector3? flag = BuildSummitFlag();
            BuildBridgeRails();
            // Caps get collision last, so the prop scatter above only sees the cliff bodies.
            foreach (string cap in new[] { "Grass", "Beach", "Snow", "Deck" })
                foreach (var t in root.GetComponentsInChildren<Transform>())
                    if (t.name == cap && t.GetComponent<MeshCollider>() == null)
                        t.gameObject.AddComponent<MeshCollider>().sharedMesh = t.GetComponent<MeshFilter>().sharedMesh;
            BuildWalker(flag ?? new Vector3(113f, -80f, -123f));

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (keepOpenScenes)
            {
                SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log($"Decorated mountain saved to {DecoratedPath}. It was closed because another open scene has unsaved changes.");
            }
        }

        // ---------- Blocks ----------

        // Replaces a ProBuilder block with a cliff mesh plus a grass (or sand, snow, wood) cap.
        private static Transform DressBlock(ProBuilderMesh pb)
        {
            string name = pb.name;
            var all = new List<Vector3>();
            foreach (Face face in pb.faces)
                foreach (int i in face.indexes)
                    all.Add(pb.transform.TransformPoint(pb.positions[i]));

            var tops = new List<Vector3>();
            for (int i = 0; i < all.Count; i += 3)
            {
                Vector3 n = Vector3.Cross(all[i + 1] - all[i], all[i + 2] - all[i]).normalized;
                if (n.y > 0.7f) tops.AddRange(new[] { all[i], all[i + 1], all[i + 2] });
            }

            var block = new GameObject(name).transform;
            block.SetParent(pb.transform.parent, false);

            bool bridge = name == "Bridge";
            var body = new LowPolyMesh();
            for (int i = 0; i < all.Count; i += 3) body.Tri(all[i], all[i + 1], all[i + 2]);
            var bodyMesh = SaveMesh(body.Build("Mountain/" + name + " Body", true));
            var bodyGo = Spawn(block, "Cliff", bodyMesh, bridge ? Mat("WoodDark", Palette.WoodDark, 1.5f) : Mat("Cliff", Cliff, 2f), Vector3.zero);
            bodyGo.AddComponent<MeshCollider>().sharedMesh = bodyMesh;

            if (bridge)
                Spawn(block, "Deck", SaveMesh(Cap(tops, "Mountain/Bridge Deck", 0.4f, 0.6f, 0.3f)), Mat("Wood", Palette.Wood, 2f), Vector3.zero);
            else if (name.StartsWith("Lowland"))
            {
                // A sand beach all round, with the grass pulled in from the edge.
                Spawn(block, "Beach", SaveMesh(Cap(tops, "Mountain/" + name + " Beach", 0.15f, 3f, 0.8f)), Mat("MountainSand", Palette.Sand, 2f), Vector3.zero);
                Spawn(block, "Grass", SaveMesh(Cap(tops, "Mountain/" + name + " Grass", 0.6f, -7f, 1f)), Mat("MountainGrass", Palette.Grass, 2f), Vector3.zero);
            }
            else if (name == "Peak")
                Spawn(block, "Snow", SaveMesh(Cap(tops, "Mountain/Peak Snow", 0.5f, 1.2f, 3f)), Mat("Snow", Snow, 2f), Vector3.zero);
            else
                Spawn(block, "Grass", SaveMesh(Cap(tops, "Mountain/" + name + " Grass", 0.4f, 1.2f, 2.5f)), Mat("MountainGrass", Palette.Grass, 2f), Vector3.zero);

            if (!bridge && !name.StartsWith("Lowland"))
            {
                var bands = Bands(all, "Mountain/" + name + " Bands");
                if (bands != null) Spawn(block, "Rock Bands", SaveMesh(bands), Mat("CliffDark", CliffDark, 2f), Vector3.zero);
            }

            Object.DestroyImmediate(pb.gameObject);
            return block;
        }

        // A slab over the top faces: lifted by `lift`, pushed out past the edge by `outset`
        // (negative pulls it in), with a skirt `depth` deep. Gives cliffs a chunky lip.
        private static Mesh Cap(List<Vector3> tops, string name, float lift, float outset, float depth)
        {
            var edgeCount = new Dictionary<(Vector3Int, Vector3Int), int>();
            var edgeThird = new Dictionary<(Vector3Int, Vector3Int), Vector3>();
            var points = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < tops.Count; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    Vector3 a = tops[i + k], b = tops[i + (k + 1) % 3], c = tops[i + (k + 2) % 3];
                    Vector3Int ka = Key(a), kb = Key(b);
                    points[ka] = a; points[kb] = b;
                    var key = Less(ka, kb) ? (ka, kb) : (kb, ka);
                    edgeCount.TryGetValue(key, out int count);
                    edgeCount[key] = count + 1;
                    edgeThird[key] = c;
                }

            // Outward direction of every edge vertex, summed from its edges.
            var push = new Dictionary<Vector3Int, Vector3>();
            var outer = new List<(Vector3Int a, Vector3Int b, Vector3 normal)>();
            foreach (var pair in edgeCount)
            {
                if (pair.Value != 1) continue;
                Vector3 a = points[pair.Key.Item1], b = points[pair.Key.Item2];
                Vector3 d = b - a;
                Vector3 n = new Vector3(d.z, 0f, -d.x).normalized;
                if (Vector3.Dot(n, edgeThird[pair.Key] - a) > 0f) n = -n;
                outer.Add((pair.Key.Item1, pair.Key.Item2, n));
                foreach (var k in new[] { pair.Key.Item1, pair.Key.Item2 })
                {
                    push.TryGetValue(k, out Vector3 sum);
                    push[k] = sum + n;
                }
            }
            var moved = new Dictionary<Vector3Int, Vector3>();
            foreach (var pair in points)
            {
                Vector3 offset = Vector3.zero;
                if (push.TryGetValue(pair.Key, out Vector3 sum) && sum.sqrMagnitude > 1e-6f)
                {
                    Vector3 dir = sum.normalized;
                    float miter = 0f;
                    foreach (var e in outer)
                        if (e.a == pair.Key || e.b == pair.Key) miter = Mathf.Max(miter, Vector3.Dot(dir, e.normal));
                    offset = dir * outset / Mathf.Max(0.5f, miter);
                }
                moved[pair.Key] = pair.Value + offset + Vector3.up * lift;
            }

            var m = new LowPolyMesh();
            for (int i = 0; i < tops.Count; i += 3)
                Facing(m, moved[Key(tops[i])], moved[Key(tops[i + 1])], moved[Key(tops[i + 2])], Vector3.up);
            Vector3 down = Vector3.down * depth;
            foreach (var e in outer)
            {
                Vector3 a = moved[e.a], b = moved[e.b];
                Facing(m, a, b, b + down, e.normal);
                Facing(m, a, b + down, a + down, e.normal);
                // Underside of the lip, seen from low angles.
                Vector3 ai = points[e.a] + Vector3.up * (lift - depth), bi = points[e.b] + Vector3.up * (lift - depth);
                Facing(m, a + down, b + down, bi, Vector3.down);
                Facing(m, a + down, bi, ai, Vector3.down);
            }
            return m.Build(name, true);
        }

        // Chunky horizontal ledges around a cliff every few metres: the cliff is sliced at each
        // height and every wall segment gets a slab that sticks out from the face.
        private static Mesh Bands(List<Vector3> tris, string name)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in tris) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
            minY = Mathf.Max(minY, -86f); // nothing below the lowland
            // Uneven gaps and sizes, so the bands read as rock layers rather than building floors.
            float[] gaps = { 21f, 13f, 27f, 17f };
            float[] sizes = { 1.8f, 1.1f, 2.4f, 1.3f };
            const float outset = 1.2f;

            var m = new LowPolyMesh();
            int bandIndex = 0;
            for (float y = maxY - 11f; y > minY + 4f; y -= gaps[bandIndex % gaps.Length], bandIndex++)
            {
                float thick = sizes[bandIndex % sizes.Length];
                float stick = outset * (bandIndex % 2 == 0 ? 1f : 0.7f);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    Vector3 n = Vector3.Cross(tris[i + 1] - tris[i], tris[i + 2] - tris[i]);
                    var flat = new Vector3(n.x, 0f, n.z);
                    if (Mathf.Abs(n.normalized.y) > 0.5f || flat.sqrMagnitude < 1e-6f) continue; // walls only
                    if (!Slice(tris[i], tris[i + 1], tris[i + 2], y, out Vector3 a, out Vector3 b)) continue;
                    Vector3 o = flat.normalized;
                    Vector3 along = (b - a).normalized * stick; // overlap neighbours so corners close
                    a -= along; b += along;
                    Vector3 up = Vector3.up * (thick * 0.5f), dn = -up, out_ = o * stick;
                    Facing(m, a + out_ + up, b + out_ + up, b + out_ + dn, o);
                    Facing(m, a + out_ + up, b + out_ + dn, a + out_ + dn, o);
                    Facing(m, a + up, b + up, b + out_ + up, Vector3.up);
                    Facing(m, a + up, b + out_ + up, a + out_ + up, Vector3.up);
                    Facing(m, a + dn, b + out_ + dn, b + dn, Vector3.down);
                    Facing(m, a + dn, a + out_ + dn, b + out_ + dn, Vector3.down);
                }
            }
            return bandIndex == 0 ? null : m.Build(name, true);
        }

        // Where a triangle crosses the plane at height y.
        private static bool Slice(Vector3 p0, Vector3 p1, Vector3 p2, float y, out Vector3 a, out Vector3 b)
        {
            var hits = new List<Vector3>(2);
            var p = new[] { p0, p1, p2 };
            for (int k = 0; k < 3 && hits.Count < 2; k++)
            {
                Vector3 u = p[k], v = p[(k + 1) % 3];
                if ((u.y - y) * (v.y - y) >= 0f || Mathf.Approximately(u.y, v.y)) continue;
                hits.Add(Vector3.Lerp(u, v, (y - u.y) / (v.y - u.y)));
            }
            a = hits.Count > 0 ? hits[0] : default;
            b = hits.Count > 1 ? hits[1] : default;
            return hits.Count == 2 && (a - b).sqrMagnitude > 0.01f;
        }

        private static void Facing(LowPolyMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 wanted)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), wanted) >= 0f) m.Tri(a, b, c);
            else m.Tri(a, c, b);
        }

        private static Vector3Int Key(Vector3 v) =>
            new Vector3Int(Mathf.RoundToInt(v.x * 100f), Mathf.RoundToInt(v.y * 100f), Mathf.RoundToInt(v.z * 100f));

        private static bool Less(Vector3Int a, Vector3Int b) =>
            a.x != b.x ? a.x < b.x : a.y != b.y ? a.y < b.y : a.z < b.z;

        // ---------- Sea, light, camera ----------

        private static void BuildSea()
        {
            var sea = SaveMesh(LowPolyMesh.Box("Mountain/Sea", new Vector3(1400f, 1f, 1400f)));
            Spawn(root, "Sea", sea, Mat("Water", Palette.Water, 0f), new Vector3(113f, -93.2f, -123f));
        }

        private static void BuildMountainLighting()
        {
            var lightGo = new GameObject("Sun");
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
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Sky;
            RenderSettings.fogStartDistance = 450f;
            RenderSettings.fogEndDistance = 1300f;

            // The game's camera angle, pulled far back to frame the whole mountain.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(100f, 340f, -445f);
            camGo.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.farClipPlane = 2000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            camGo.AddComponent<AudioListener>();
            mountainCamera = cam;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ArtRoot + "/ArtSandboxVolume.asset");
            if (profile != null)
            {
                var volume = new GameObject("Post Processing").AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
            }
        }

        // ---------- Props ----------

        private static readonly int[] CanopySeeds = { 11, 12, 13, 14, 15, 16 };
        private static readonly int[] RockSeeds = { 21, 22, 23, 24, 25 };

        // Drops clusters of trees, bushes and rocks on a block's top, away from its edges.
        private static void Scatter(Transform block, string name, System.Random random)
        {
            if (name == "Bridge") return;
            var collider = block.GetComponentInChildren<MeshCollider>();
            Bounds b = collider.bounds;
            bool lowland = name.StartsWith("Lowland");
            bool high = name == "Ledge" || name == "Ledge South" || name == "Peak";
            float margin = lowland ? 16f : 8f;
            float spacing = lowland ? 21f : 15f;
            int tries = Mathf.RoundToInt(b.size.x * b.size.z / (spacing * spacing));
            var props = new GameObject("Props").transform;
            props.SetParent(block, false);

            for (int t = 0; t < tries; t++)
            {
                var p = new Vector3(Range(random, b.min.x, b.max.x), 0f, Range(random, b.min.z, b.max.z));
                if (!SurfaceAt(collider, p, margin, out Vector3 at)) continue;
                int kind = random.Next(10);
                if (name == "Peak")
                {
                    if (kind < 4) MountainRock(props, at, Range(random, 3f, 6f), random);
                    continue;
                }
                if (kind < 2) { MountainRock(props, at, Range(random, 3f, 7f), random); continue; }
                bool pine = high || (!lowland && kind < 6) || (lowland && kind == 9);
                if (pine)
                {
                    Pine(props, at, PropScale * Range(random, 1.1f, 1.6f));
                    Pine(props, at + Offset(random, 6f), PropScale * Range(random, 0.8f, 1.2f));
                }
                else
                {
                    AutumnTree(props, at, CanopySeeds[random.Next(CanopySeeds.Length)], PropScale * Range(random, 1.0f, 1.4f));
                    Bush(props, at + Offset(random, 6f), PropScale * Range(random, 0.8f, 1.3f), random.Next(2) == 0, random.Next(3));
                }
                if (random.Next(3) == 0)
                    Bush(props, at + Offset(random, 9f), PropScale * Range(random, 0.7f, 1.1f), random.Next(3) == 0, random.Next(3));
            }

            // Boulders at the foot of the cliffs, on the lowland only.
            if (lowland)
            {
                for (int t = 0; t < tries; t++)
                {
                    var p = new Vector3(Range(random, b.min.x, b.max.x), 0f, Range(random, b.min.z, b.max.z));
                    if (SurfaceAt(collider, p, 0f, out Vector3 at) && !SurfaceAt(collider, p, 8f, out _))
                        MountainRock(props, at, Range(random, 4f, 9f), random);
                }
            }
        }

        // True if p and four points `margin` around it all land on this collider's flat top.
        private static bool SurfaceAt(MeshCollider collider, Vector3 p, float margin, out Vector3 at)
        {
            at = default;
            var probes = new[] { Vector3.zero, new Vector3(margin, 0, 0), new Vector3(-margin, 0, 0), new Vector3(0, 0, margin), new Vector3(0, 0, -margin) };
            foreach (var o in probes)
            {
                if (!MountainRaycast(new Vector3(p.x + o.x, 500f, p.z + o.z), out RaycastHit hit)) return false;
                if (hit.collider != collider || hit.normal.y < 0.8f) return false;
                if (o == Vector3.zero) at = hit.point;
            }
            return true;
        }

        private static MeshCollider[] mountainColliders;
        private static Camera mountainCamera;

        // Casts straight down against the decorated blocks only, so other open scenes can't get in the way.
        private static bool MountainRaycast(Vector3 from, out RaycastHit nearest)
        {
            nearest = default;
            bool found = false;
            var ray = new Ray(from, Vector3.down);
            foreach (var c in mountainColliders)
                if (c.Raycast(ray, out RaycastHit hit, 1000f) && (!found || hit.distance < nearest.distance))
                {
                    nearest = hit;
                    found = true;
                }
            return found;
        }

        private static Vector3 Offset(System.Random random, float radius)
        {
            float a = Range(random, 0f, Mathf.PI * 2f), r = Range(random, radius * 0.6f, radius);
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        private static void MountainRock(Transform parent, Vector3 at, float size, System.Random random)
        {
            int seed = RockSeeds[random.Next(RockSeeds.Length)];
            var mesh = SaveMesh(LowPolyMesh.Blob("Rock" + seed, 3, 6, 1f, 0.25f, seed));
            Spawn(parent, "Rock", mesh, Mat("Rock", Palette.Rock, 2f), at + Vector3.up * size * 0.2f,
                Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), new Vector3(size, size * 0.7f, size));
        }

        // A walkable player (MapWalker) with the zoomable follow camera (MapCamera).
        private static void BuildWalker(Vector3 start)
        {
            var walker = new GameObject("Walker");
            walker.transform.position = start;
            var body = walker.AddComponent<CharacterController>();
            body.height = 1.2f;
            body.radius = 0.3f;
            body.center = new Vector3(0f, 0.62f, 0f);
            body.stepOffset = 0.5f;
            walker.AddComponent<Farm.View.MapWalker>();
            walker.AddComponent<Farm.View.MapPins>();
            walker.AddComponent<Farm.View.MapControlsPanel>();

            BuildPlayer(start);
            var model = root.Find("Player");
            model.SetParent(walker.transform, true);
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;

            var cam = mountainCamera;
            var follow = cam.gameObject.AddComponent<Farm.View.MapCamera>();
            EditorAssetUtil.Wire(follow, ("target", walker.transform));
        }

        private static Vector3? BuildSummitFlag()
        {
            var peak = root.Find("4 Summit/Peak");
            if (peak == null) return null;
            var collider = peak.GetComponentInChildren<MeshCollider>();
            Vector3 c = collider.bounds.center;
            if (!MountainRaycast(new Vector3(c.x, 500f, c.z), out RaycastHit hit)) return null;
            var flag = new GameObject("Summit Flag").transform;
            flag.SetParent(peak, false);
            flag.position = hit.point;
            Spawn(flag, "Pole", SaveMesh(LowPolyMesh.Box("Mountain/Flag Pole", new Vector3(0.8f, 16f, 0.8f))), Mat("WoodDark", Palette.WoodDark, 1.5f), Vector3.zero);
            Spawn(flag, "Flag", SaveMesh(LowPolyMesh.Box("Mountain/Flag", new Vector3(7f, 4.5f, 0.4f))), Mat("Roof", Palette.Roof, 2f), new Vector3(3.9f, 11f, 0f));
            return hit.point + new Vector3(-4f, 1f, -4f); // start beside the flag, on the snow
        }

        private static void BuildBridgeRails()
        {
            var bridge = root.Find("2 Gorge/Bridge");
            if (bridge == null) return;
            Bounds b = bridge.GetComponentInChildren<MeshCollider>().bounds;
            var post = SaveMesh(LowPolyMesh.Box("Mountain/Bridge Post", new Vector3(0.6f, 2.4f, 0.6f)));
            var rail = SaveMesh(LowPolyMesh.Box("Mountain/Bridge Rail", new Vector3(0.35f, 0.35f, b.size.z)));
            var mat = Mat("WoodDark", Palette.WoodDark, 1.5f);
            var rails = new GameObject("Rails").transform;
            rails.SetParent(bridge, false);
            float top = b.max.y + 0.4f;
            foreach (float x in new[] { b.min.x + 0.2f, b.max.x - 0.2f })
            {
                for (float z = b.min.z + 0.5f; z <= b.max.z; z += 4f)
                    Spawn(rails, "Post", post, mat, new Vector3(x, top, z));
                Spawn(rails, "Rail", rail, mat, new Vector3(x, top + 1.9f, b.center.z));
            }
        }
    }
}
