using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Player-sized props in the style of A Short Hike, saved as prefabs in Art/Prefabs/Decor: three kinds
    /// of round bush, two kinds of flower, three kinds of rock, a tall fir and a yellow birch. "Decorate Around Map Pins" places
    /// instances of them round each map pin (MapNotes/pins.json) in MountainTerrain.unity. Only the
    /// "Pin Decor" group is replaced on each run; the terrain, its sculpting and the scattered props are left alone.
    /// </summary>
    public static partial class ArtSandboxBuilder
    {
        private const string PinDecorName = "Pin Decor";
        private const string DecorPrefabFolder = ArtRoot + "/Prefabs/Decor";

        private static class DecorPalette
        {
            public static readonly Color BushOlive = new Color(0.52f, 0.62f, 0.24f);
            public static readonly Color BushDark = new Color(0.30f, 0.42f, 0.20f);
            public static readonly Color BushYellow = new Color(0.88f, 0.76f, 0.24f);
            public static readonly Color FlowerLeaf = new Color(0.38f, 0.62f, 0.26f);
            public static readonly Color FlowerGold = new Color(1.00f, 0.82f, 0.22f);
            public static readonly Color FlowerWhite = new Color(0.98f, 0.97f, 0.92f);
            public static readonly Color Fir = new Color(0.20f, 0.36f, 0.22f);
            public static readonly Color BirchBark = new Color(0.93f, 0.91f, 0.86f);
            public static readonly Color BirchMark = new Color(0.30f, 0.26f, 0.26f);
            public static readonly Color BirchLeaf = new Color(0.98f, 0.78f, 0.22f);
            public static readonly Color BirchLeafDeep = new Color(0.92f, 0.62f, 0.16f);
            public static readonly Color RockGrey = new Color(0.72f, 0.71f, 0.74f);
            public static readonly Color RockMauve = new Color(0.64f, 0.53f, 0.61f);
            public static readonly Color RockDark = new Color(0.55f, 0.54f, 0.59f);
        }

        private class DecorPrefabs
        {
            public GameObject Fir, Birch;
            public GameObject[] Bushes;  // olive, dark, yellow
            public GameObject[] Flowers; // yellow, daisies
            public GameObject[] Rocks;   // boulder, slab, pile
        }

        [MenuItem("Farm/Build Decor Prefabs")]
        public static void BuildDecorPrefabsMenu()
        {
            BuildDecorPrefabs();
            SaveDecorAssets();
            Debug.Log($"Decor prefabs saved to {DecorPrefabFolder}.");
        }

        [MenuItem("Farm/Decorate Around Map Pins")]
        public static void DecorateAroundPinsMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != TerrainScenePath)
            {
                EditorUtility.DisplayDialog("Decorate around pins", "Open MountainTerrain.unity first.", "OK");
                return;
            }
            var pins = Farm.View.MapPins.Load().pins;
            if (pins.Count == 0)
            {
                EditorUtility.DisplayDialog("Decorate around pins", "There are no pins in MapNotes/pins.json.", "OK");
                return;
            }
            DecorateAroundPins(scene, pins);
        }

        public static void DecorateAroundPins(Scene scene, List<Farm.View.MapPin> pins)
        {
            bool wasDirty = scene.isDirty;
            root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == TerrainRootName) root = go.transform;
            if (root == null) return;
            var terrain = root.GetComponentInChildren<Terrain>();
            var prefabs = BuildDecorPrefabs();

            var old = root.Find(PinDecorName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var decor = Group(PinDecorName);

            // Keep clear of the props that are already there.
            var taken = new List<(Vector3 p, float r)>();
            var props = root.Find("Props");
            if (props != null)
                foreach (Transform child in props) taken.Add((child.position, 3f));

            foreach (var pin in pins)
            {
                var group = Group("Pin " + pin.number, decor);
                group.position = pin.position;
                var random = new System.Random(100 + pin.number);
                var placed = new List<(Vector3 p, float r)>(taken) { (pin.position, 2.5f) }; // the pin's own spot stays walkable

                // Trees frame the spot, bushes sit in the middle distance, flowers come closest.
                var counts = new List<int>
                {
                    Place(terrain, placed, random, pin.position, 8f, 25f, 2.2f, 2, at => PlaceDecor(prefabs.Fir, group, at, random, 0.9f, 1.25f)),
                    Place(terrain, placed, random, pin.position, 6f, 25f, 1.6f, 2, at => PlaceDecor(prefabs.Birch, group, at, random, 0.9f, 1.2f)),
                };
                foreach (var rock in prefabs.Rocks)
                    counts.Add(Place(terrain, placed, random, pin.position, 4f, 22f, 1.2f, 1, at => PlaceDecor(rock, group, at, random, 0.8f, 1.3f)));
                foreach (var bush in prefabs.Bushes)
                    counts.Add(Place(terrain, placed, random, pin.position, 3f, 22f, 1.1f, 3, at => PlaceDecor(bush, group, at, random, 0.85f, 1.2f)));
                // One clump at a time, alternating kinds, so both share whatever room the bushes left.
                int yellow = 0, daisies = 0;
                for (int i = 0; i < 10; i++)
                {
                    var flower = prefabs.Flowers[i % 2];
                    int made = Place(terrain, placed, random, pin.position, 2.5f, 20f, 0.8f, 1, at => PlaceDecor(flower, group, at, random, 0.9f, 1.1f));
                    if (i % 2 == 0) yellow += made; else daisies += made;
                }
                counts.Add(yellow);
                counts.Add(daisies);
                Debug.Log($"Pin {pin.number}: {counts[0]} firs, {counts[1]} birches, rocks {counts[2]}/{counts[3]}/{counts[4]} (boulder/slab/pile), " +
                          $"bushes {counts[5]}/{counts[6]}/{counts[7]} (olive/dark/yellow), flowers {counts[8]}/{counts[9]} (yellow/daisies). " +
                          "Fewer than asked means the ledge ran out of flat room.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty) EditorSceneManager.SaveScene(scene); // never save someone else's unsaved edits
            SaveDecorAssets();
        }

        // Saves only the decor meshes and materials. AssetDatabase.SaveAssets would also write any
        // unsaved terrain sculpting, which isn't this tool's to save.
        private static void SaveDecorAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { ArtRoot + "/Meshes/Decor" }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid)));
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { ArtRoot + "/Materials" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith("Decor") || path.EndsWith("/Trunk.mat"))
                    AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(path));
            }
        }

        // Tries random spots in a ring round the pin until `count` items fit on flat ground without overlapping.
        // Only ground at the pin's own level counts, so nothing lands on a shelf above or below it
        // (the pins were dropped on the grey-box, whose cliff edges sit a few metres off the terrain's).
        private static int Place(Terrain terrain, List<(Vector3 p, float r)> placed, System.Random random, Vector3 centre,
            float minRadius, float maxRadius, float footprint, int count, System.Action<Vector3> spawn)
        {
            const float levelTolerance = 4f;
            int made = 0;
            for (int tries = 0; tries < 400 && made < count; tries++)
            {
                float a = Range(random, 0f, Mathf.PI * 2f), r = Range(random, minRadius, maxRadius);
                var p = centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (!FlatGround(terrain, p, footprint, out Vector3 at) || Mathf.Abs(at.y - centre.y) > levelTolerance) continue;
                bool clear = true;
                foreach (var (q, qr) in placed)
                    if (new Vector2(q.x - at.x, q.z - at.z).sqrMagnitude < (qr + footprint) * (qr + footprint)) { clear = false; break; }
                if (!clear) continue;
                placed.Add((at, footprint));
                spawn(at);
                made++;
            }
            return made;
        }

        // A prefab instance, turned and sized at random so repeats don't look stamped.
        private static void PlaceDecor(GameObject prefab, Transform parent, Vector3 at, System.Random random, float minScale, float maxScale)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = at;
            instance.transform.rotation = Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);
            instance.transform.localScale = prefab.transform.localScale * Range(random, minScale, maxScale);
        }

        // ---------- Prefabs ----------

        // Builds (or rebuilds in place, keeping their GUIDs) every decor prefab.
        private static DecorPrefabs BuildDecorPrefabs() => new DecorPrefabs
        {
            Fir = SavePrefab("Fir", BuildFir),
            Birch = SavePrefab("Birch", BuildBirch),
            Bushes = new[]
            {
                SavePrefab("Olive Bush", t => BuildDecorBush(t, 0)),
                SavePrefab("Dark Bush", t => BuildDecorBush(t, 1)),
                SavePrefab("Yellow Bush", t => BuildDecorBush(t, 2)),
            },
            Flowers = new[]
            {
                SavePrefab("Yellow Flowers", t => BuildDecorFlowers(t, 0)),
                SavePrefab("Daisies", t => BuildDecorFlowers(t, 1)),
            },
            Rocks = new[]
            {
                SavePrefab("Boulder", t => BuildDecorRock(t, 0)),
                SavePrefab("Rock Slab", t => BuildDecorRock(t, 1)),
                SavePrefab("Rock Pile", t => BuildDecorRock(t, 2)),
            },
        };

        private static GameObject SavePrefab(string name, System.Action<Transform> build)
        {
            EditorAssetUtil.EnsureFolder(DecorPrefabFolder);
            var go = new GameObject(name);
            try
            {
                build(go.transform);
                return PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(DecorPrefabFolder, name + ".prefab").Replace('\\', '/'));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ---------- Bushes ----------

        // 0: a big olive ball with two smaller ones at its sides.
        // 1: a tall dark bush, a wide ball with a narrower one on top.
        // 2: a low yellow autumn bush made of five small balls.
        private static void BuildDecorBush(Transform bush, int variant)
        {
            var ball = SaveMesh(LowPolyMesh.Blob("Decor/Bush Ball " + variant, 5, 9, 0.5f, 0.06f, 70 + variant, false));
            switch (variant)
            {
                case 0:
                {
                    var mat = Mat("DecorBushOlive", DecorPalette.BushOlive, 2f);
                    Spawn(bush, "Body", ball, mat, new Vector3(0f, 0.45f, 0f), Quaternion.identity, new Vector3(1.7f, 1.5f, 1.6f));
                    Spawn(bush, "Lobe", ball, mat, new Vector3(0.8f, 0.3f, -0.2f), Quaternion.identity, Vector3.one * 1.0f);
                    Spawn(bush, "Lobe", ball, mat, new Vector3(-0.7f, 0.25f, 0.3f), Quaternion.identity, Vector3.one * 0.85f);
                    break;
                }
                case 1:
                {
                    var mat = Mat("DecorBushDark", DecorPalette.BushDark, 2f);
                    Spawn(bush, "Base", ball, mat, new Vector3(0f, 0.5f, 0f), Quaternion.identity, new Vector3(1.6f, 1.3f, 1.6f));
                    Spawn(bush, "Top", ball, mat, new Vector3(0.1f, 1.25f, 0.05f), Quaternion.identity, new Vector3(1.15f, 1.1f, 1.15f));
                    break;
                }
                default:
                {
                    var mat = Mat("DecorBushYellow", DecorPalette.BushYellow, 2f);
                    float[] sizes = { 0.8f, 0.9f, 0.75f, 0.95f };
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 2f / 5f;
                        var p = i == 4 ? new Vector3(0f, 0.55f, 0f) : new Vector3(Mathf.Cos(a) * 0.45f, 0.3f, Mathf.Sin(a) * 0.45f);
                        Spawn(bush, "Ball", ball, mat, p, Quaternion.identity, Vector3.one * (i == 4 ? 1.0f : sizes[i]));
                    }
                    break;
                }
            }
        }

        // ---------- Flowers ----------

        // 0: yellow cup flowers on tall stems over a tuft of leaves.
        // 1: a low clump of white daisies with yellow middles.
        private static void BuildDecorFlowers(Transform clump, int variant)
        {
            clump.localScale = Vector3.one * 1.8f; // chunky enough to read from the follow camera
            var random = new System.Random(90 + variant);

            var leaf = SaveMesh(LowPolyMesh.Leaf("Decor/Flower Leaf", 0.4f, 0.14f, 0.02f));
            var leafMat = Mat("DecorFlowerLeaf", DecorPalette.FlowerLeaf, 1.5f);
            int leaves = variant == 0 ? 5 : 6;
            for (int i = 0; i < leaves; i++)
                Spawn(clump, "Leaf", leaf, leafMat, Vector3.zero, Quaternion.Euler(variant == 0 ? -55f : -30f, i * 360f / leaves + 15f, 0f),
                    Vector3.one * (variant == 0 ? 1f : 0.7f));

            var stem = SaveMesh(LowPolyMesh.Cylinder("Decor/Flower Stem", 4, 0.02f, 0.02f, 1f));
            int heads = variant == 0 ? 3 : 5;
            for (int i = 0; i < heads; i++)
            {
                float a = i * Mathf.PI * 2f / heads + Range(random, -0.3f, 0.3f);
                float spread = variant == 0 ? 0.08f : 0.18f;
                var p = new Vector3(Mathf.Cos(a) * spread, 0f, Mathf.Sin(a) * spread);
                float tall = variant == 0 ? Range(random, 0.45f, 0.6f) : Range(random, 0.18f, 0.26f);
                Spawn(clump, "Stem", stem, leafMat, p, Quaternion.Euler(Range(random, -8f, 8f), 0f, Range(random, -8f, 8f)), new Vector3(1f, tall, 1f));
                var top = p + Vector3.up * tall;
                if (variant == 0)
                {
                    var cup = SaveMesh(LowPolyMesh.Cylinder("Decor/Flower Cup", 6, 0.04f, 0.09f, 0.12f));
                    Spawn(clump, "Flower", cup, Mat("DecorFlowerGold", DecorPalette.FlowerGold, 1f), top);
                }
                else
                {
                    var petals = SaveMesh(LowPolyMesh.Cylinder("Decor/Daisy Petals", 7, 0.08f, 0.08f, 0.02f));
                    var middle = SaveMesh(LowPolyMesh.Blob("Decor/Daisy Middle", 2, 5, 0.035f, 0f, 3));
                    Spawn(clump, "Petals", petals, Mat("DecorFlowerWhite", DecorPalette.FlowerWhite, 1f), top);
                    Spawn(clump, "Middle", middle, Mat("DecorFlowerGold", DecorPalette.FlowerGold, 1f), top + Vector3.up * 0.02f);
                }
            }
        }

        // ---------- Rocks ----------

        // Faceted, so they read as hard next to the round bushes. Each sits a little into the ground,
        // so slopes never show a gap underneath.
        // 0: a big grey boulder, wider than tall.
        // 1: a flat, tilted mauve slab with a chip broken off beside it.
        // 2: a pile of three small stones, grey and dark grey.
        private static void BuildDecorRock(Transform rock, int variant)
        {
            switch (variant)
            {
                case 0:
                {
                    var mesh = SaveMesh(LowPolyMesh.Blob("Decor/Boulder", 3, 7, 0.5f, 0.22f, 31));
                    Spawn(rock, "Boulder", mesh, Mat("DecorRockGrey", DecorPalette.RockGrey, 2f), new Vector3(0f, 0.35f, 0f),
                        Quaternion.Euler(0f, 20f, 0f), new Vector3(1.5f, 1.1f, 1.25f));
                    break;
                }
                case 1:
                {
                    var mesh = SaveMesh(LowPolyMesh.Blob("Decor/Rock Slab", 2, 5, 0.5f, 0.25f, 32));
                    var mat = Mat("DecorRockMauve", DecorPalette.RockMauve, 2f);
                    Spawn(rock, "Slab", mesh, mat, new Vector3(0f, 0.2f, 0f), Quaternion.Euler(10f, 0f, -8f), new Vector3(1.7f, 0.75f, 1.2f));
                    Spawn(rock, "Chip", mesh, mat, new Vector3(1.0f, 0.08f, -0.4f), Quaternion.Euler(-6f, 70f, 12f), new Vector3(0.55f, 0.3f, 0.45f));
                    break;
                }
                default:
                {
                    var mesh = SaveMesh(LowPolyMesh.Blob("Decor/Pebble", 3, 6, 0.5f, 0.2f, 33));
                    var grey = Mat("DecorRockGrey", DecorPalette.RockGrey, 2f);
                    var dark = Mat("DecorRockDark", DecorPalette.RockDark, 2f);
                    Spawn(rock, "Stone", mesh, grey, new Vector3(0f, 0.18f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.8f, 0.6f, 0.7f));
                    Spawn(rock, "Stone", mesh, dark, new Vector3(0.55f, 0.1f, 0.25f), Quaternion.Euler(0f, 50f, 8f), new Vector3(0.5f, 0.4f, 0.45f));
                    Spawn(rock, "Stone", mesh, grey, new Vector3(-0.35f, 0.07f, -0.45f), Quaternion.Euler(0f, 120f, -6f), new Vector3(0.35f, 0.28f, 0.32f));
                    break;
                }
            }
        }

        // ---------- Trees ----------

        // A tall, narrow, dark fir: five stacked cones on a short trunk.
        private static void BuildFir(Transform fir)
        {
            Spawn(fir, "Trunk", SaveMesh(LowPolyMesh.Cylinder("Decor/Fir Trunk", 6, 0.28f, 0.14f, 2.4f)), Mat("Trunk", Palette.Trunk, 2f), Vector3.zero);
            var cone = SaveMesh(LowPolyMesh.Cylinder("Decor/Fir Cone", 8, 1f, 0f, 1f));
            var mat = Mat("DecorFir", DecorPalette.Fir, 2f);
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float width = Mathf.Lerp(2.2f, 0.8f, t);
                Spawn(fir, "Layer", cone, mat, new Vector3(0f, 1.4f + i * 1.55f, 0f), Quaternion.Euler(0f, i * 22f, 0f),
                    new Vector3(width, 2.6f - t * 0.6f, width));
            }
        }

        // A thin white birch with dark bark marks and a column of yellow leaf balls.
        private static void BuildBirch(Transform birch)
        {
            Spawn(birch, "Trunk", SaveMesh(LowPolyMesh.Cylinder("Decor/Birch Trunk", 6, 0.2f, 0.1f, 6.5f)), Mat("DecorBirchBark", DecorPalette.BirchBark, 2f), Vector3.zero);

            var mark = SaveMesh(LowPolyMesh.Box("Decor/Birch Mark", new Vector3(0.16f, 0.07f, 0.06f)));
            var markMat = Mat("DecorBirchMark", DecorPalette.BirchMark, 0f);
            float[] markHeights = { 0.7f, 1.4f, 2.3f, 3.0f };
            for (int i = 0; i < markHeights.Length; i++)
            {
                float radius = Mathf.Lerp(0.2f, 0.1f, markHeights[i] / 6.5f);
                var rot = Quaternion.Euler(0f, i * 97f, 0f);
                Spawn(birch, "Mark", mark, markMat, rot * new Vector3(0f, markHeights[i], -radius + 0.01f), rot);
            }

            var ball = SaveMesh(LowPolyMesh.Blob("Decor/Birch Leaves", 4, 8, 1f, 0.12f, 80, false));
            var light = Mat("DecorBirchLeaf", DecorPalette.BirchLeaf, 2f);
            var deep = Mat("DecorBirchLeafDeep", DecorPalette.BirchLeafDeep, 2f);
            // Leaf balls stacked up the top half of the trunk, getting smaller towards the tip.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float a = i * 2.3f;
                var side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Mathf.Lerp(0.55f, 0.15f, t);
                Spawn(birch, "Leaves", ball, i % 2 == 0 ? light : deep, side + Vector3.up * (3.6f + i * 0.85f), Quaternion.Euler(0f, i * 60f, 0f),
                    new Vector3(1f, 0.8f, 1f) * Mathf.Lerp(1.3f, 0.65f, t));
            }
        }
    }
}
