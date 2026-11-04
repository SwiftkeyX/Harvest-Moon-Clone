using UnityEditor;
using UnityEngine;

namespace Farm.EditorTools.Art
{
    // Saves the Cliff Ledge props as prefab assets in Art/Prefabs, for the scene builder to place as instances.
    // Each prefab's root sits where the prop meets the ground and has scale 1; offsets and sizes are on the children.
    // Every build saves to the same paths, so the GUIDs stay stable. Materials end up as the Shader Graph versions.
    internal sealed class CliffLedgePrefabs
    {
        const string Dir = "Assets/_Game/Art/Prefabs";

        // The tallest flower stem. Shorter flowers are placed lower, with the rest of the stem under the grass.
        public const float FlowerStem = 0.7f;
        // Height of a pebble's centre above the ground at scale 1.
        public const float PebbleLift = 0.12f;

        // Where the fence's first post stands: just past the ledge's front-left corner, so the posts reach the
        // bottom of the view.
        public static Vector3 FenceBase => CliffLedgeSceneBuilder.Ground(0.035f, 1.0f, -3f);

        public GameObject BushOlive, BushYellow, BushTop;
        public GameObject BirchSmall, BirchMedium, BirchLarge;
        public GameObject Pine, Boulder, Fence, Flower, Pebble, Character;

        // `rnd` is the scene's random stream. The pine and the birch canopies draw from it in this order.
        public static CliffLedgePrefabs Build(CliffLedgeSceneBuilder.Materials m, System.Random rnd)
        {
            EditorAssetUtil.EnsureFolder(Dir);
            var p = new CliffLedgePrefabs();

            // Bush: a ball of radius 1 with two smaller lumps tucked behind it. Instances scale it by the radius.
            var bushBase = Save(BushRoot(m.BushOlive));
            p.BushOlive = Variant(bushBase, "Bush_Olive", _ => { });
            p.BushYellow = Variant(bushBase, "Bush_Yellow", g => SetMaterial(g, m.BushYellow));
            p.BushTop = Variant(bushBase, "Bush_Top", g => SetMaterial(g, m.BushTop));

            p.Pine = Save(PineRoot(m, rnd));

            // Birch: the base is the bare trunk; each size variant sets the trunk height and adds its own canopy.
            var birch = new GameObject("Birch");
            Trunk(Part(birch, PrimitiveType.Cylinder, m.BirchTrunk, Vector3.zero, Vector3.one), 19f).name = "Trunk";
            var birchBase = Save(birch);
            p.BirchLarge = Variant(birchBase, "Birch_Large", g => BirchCanopy(g, m, 21f, rnd));
            p.BirchSmall = Variant(birchBase, "Birch_Small", g => BirchCanopy(g, m, 17f, rnd));
            p.BirchMedium = Variant(birchBase, "Birch_Medium", g => BirchCanopy(g, m, 19f, rnd));

            // Boulder: a unit cube resting on the ground. Instances scale and tilt it.
            var boulder = new GameObject("Boulder");
            Part(boulder, PrimitiveType.Cube, m.Mauve, new Vector3(0, 0.5f, 0), Vector3.one).name = "Rock";
            p.Boulder = Save(boulder);

            p.Fence = Save(FenceRoot(m));

            var flower = new GameObject("Flower");
            Part(flower, PrimitiveType.Cube, m.Tuft, new Vector3(0, FlowerStem * 0.5f, 0), new Vector3(0.08f, FlowerStem, 0.08f)).name = "Stem";
            Part(flower, PrimitiveType.Sphere, m.Flower, new Vector3(0, FlowerStem, 0), Vector3.one * 0.38f).name = "Head";
            p.Flower = Save(flower);

            var pebble = new GameObject("Pebble");
            Part(pebble, PrimitiveType.Sphere, m.Pebble, new Vector3(0, PebbleLift, 0), new Vector3(0.45f, 0.25f, 0.4f)).name = "Stone";
            p.Pebble = Save(pebble);

            var hero = new GameObject("Character");
            Part(hero, PrimitiveType.Capsule, m.Jacket, new Vector3(0, 0.75f, 0), new Vector3(1.05f, 0.75f, 0.9f)).name = "Body";
            Part(hero, PrimitiveType.Sphere, m.Hood, new Vector3(0, 2.0f, 0.05f), new Vector3(1.45f, 1.4f, 1.35f)).name = "Hood";
            Part(hero, PrimitiveType.Cube, m.Hood, new Vector3(0.38f, 0.75f, 0.1f), new Vector3(0.25f, 1.0f, 0.5f), new Vector3(0, 0, -6f)).name = "Arm";
            p.Character = Save(hero);

            return p;
        }

        // Places a prefab instance under `parent`. Instances only ever change their root transform.
        public static GameObject Instance(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetLocalPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            return go;
        }

        public static GameObject Instance(GameObject prefab, Transform parent, Vector3 position) =>
            Instance(prefab, parent, position, Quaternion.identity, Vector3.one);

        // ---------------------------------------------------------------- props

        static GameObject BushRoot(Material mat)
        {
            const float lump = 0.675f;
            var bush = new GameObject("Bush");
            Part(bush, PrimitiveType.Sphere, mat, new Vector3(0, 0.5f, 0), new Vector3(2f, 1.6f, 2f)).name = "Ball";
            var lumpScale = new Vector3(lump * 2f, lump * 1.7f, lump * 2f);
            Part(bush, PrimitiveType.Sphere, mat, new Vector3(-0.75f, 0.65f, 0.45f), lumpScale).name = "Lump L";
            Part(bush, PrimitiveType.Sphere, mat, new Vector3(0.75f, 0.65f, 0.45f), lumpScale).name = "Lump R";
            return bush;
        }

        static GameObject PineRoot(CliffLedgeSceneBuilder.Materials m, System.Random rnd)
        {
            var mb = new CliffLedgeMeshBuilder(2);
            float h = 0f;
            // Many short, wide, jagged tiers so the outline steps like layered branches.
            for (int i = 0; i < 15; i++)
            {
                float t = i / 14f;
                float rad = Mathf.Lerp(3.3f, 0.7f, t) * (0.85f + (float)rnd.NextDouble() * 0.3f);
                CliffLedgeMeshBuilder.Cone(mb, new Vector3((float)(rnd.NextDouble() - 0.5) * 0.4f, 1.0f + h, 0), rad, rad * 0.8f, 16,
                    (float)rnd.NextDouble() * 6f, 0, 0.5f);
                h += 1.05f;
            }
            CliffLedgeMeshBuilder.Cone(mb, Vector3.zero, 0.35f, 3f, 6, 0f, 1);
            var pine = new GameObject("Pine");
            CliffLedgeSceneBuilder.Place(pine.transform, "Model", mb.ToMesh("CliffLedgePine"), m.Pine, m.Trunk);
            return pine;
        }

        static Transform Trunk(GameObject trunk, float height)
        {
            trunk.transform.localPosition = new Vector3(0, height * 0.5f, 0);
            trunk.transform.localScale = new Vector3(0.32f, height * 0.5f, 0.32f);
            return trunk.transform;
        }

        static void BirchCanopy(GameObject tree, CliffLedgeSceneBuilder.Materials m, float height, System.Random rnd)
        {
            Trunk(tree.transform.Find("Trunk").gameObject, height);
            var canopy = new GameObject("Canopy");
            canopy.transform.SetParent(tree.transform, false);
            for (float y = 5f; y < height; y += 1.5f)
            {
                float side = (float)(rnd.NextDouble() - 0.5) * 3.2f;
                float s = 1.0f + (float)rnd.NextDouble() * 0.9f;
                // Flat slabs: lit tops over shaded edges read as layered leaves.
                Part(canopy, PrimitiveType.Cylinder, rnd.NextDouble() < 0.25 ? m.BirchLeafDark : m.BirchLeaf,
                    new Vector3(side, y, (float)(rnd.NextDouble() - 0.5) * 1.5f), new Vector3(s * 1.8f, 0.2f, s * 0.9f),
                    new Vector3(0, rnd.Next(360), (float)(rnd.NextDouble() - 0.5) * 20f)).name = "Leaves";
            }
        }

        // Two posts joined by two rails, built from nested Post and Rail prefabs. The root is the first post's base.
        static GameObject FenceRoot(CliffLedgeSceneBuilder.Materials m)
        {
            // Post: a unit-height pole; instances stretch it to their height.
            var post = new GameObject("Post");
            Part(post, PrimitiveType.Cylinder, m.Fence, new Vector3(0, 0.5f, 0), new Vector3(0.55f, 0.5f, 0.55f)).name = "Pole";
            var postPrefab = Save(post);
            // Rail: a unit-length bar from the root along +z; instances point and stretch it.
            var rail = new GameObject("Rail");
            Part(rail, PrimitiveType.Cube, m.Fence, new Vector3(0, 0, 0.5f), new Vector3(0.3f, 0.3f, 1f)).name = "Bar";
            var railPrefab = Save(rail);

            var fa = Vector3.zero;
            var fb = CliffLedgeSceneBuilder.Ground(0.112f, 0.905f, -2.5f) - FenceBase;
            var fence = new GameObject("Fence");
            PostAt(postPrefab, fence.transform, fa, 6.2f);
            PostAt(postPrefab, fence.transform, fb, 5.2f);
            RailAt(railPrefab, fence.transform, fa + Vector3.up * 4.6f, fb + Vector3.up * 4.4f);
            RailAt(railPrefab, fence.transform, fa + Vector3.up * 2.6f, fb + Vector3.up * 2.6f);
            return fence;
        }

        // A post leaning 4 degrees about its middle, as the original cylinder did.
        static void PostAt(GameObject prefab, Transform parent, Vector3 basePos, float height)
        {
            var rot = Quaternion.Euler(0, 0, 4f);
            var middle = basePos + Vector3.up * height * 0.5f;
            Instance(prefab, parent, middle - rot * (Vector3.up * height * 0.5f), rot, new Vector3(1f, height, 1f));
        }

        static void RailAt(GameObject prefab, Transform parent, Vector3 a, Vector3 b) =>
            Instance(prefab, parent, a, Quaternion.LookRotation(b - a), new Vector3(1f, 1f, (b - a).magnitude));

        // ---------------------------------------------------------------- saving

        static GameObject Part(GameObject parent, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, Vector3 euler = default) =>
            CliffLedgeSceneBuilder.Primitive(parent.transform, type, mat, pos, scale, euler, true);

        static void SetMaterial(GameObject root, Material mat)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
        }

        // Saves a temporary object as a prefab (a variant, if it is a prefab instance) and destroys the object.
        static GameObject Save(GameObject temp)
        {
            CliffLedgeGraphMaterials.ApplyTo(temp);
            string path = $"{Dir}/{temp.name}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(temp, path, out bool ok);
            Object.DestroyImmediate(temp);
            if (!ok) throw new System.InvalidOperationException("Could not save " + path);
            return asset;
        }

        static GameObject Variant(GameObject basePrefab, string name, System.Action<GameObject> edit)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            go.name = name;
            edit(go);
            return Save(go);
        }
    }
}
