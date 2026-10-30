using Farm.Farming;
using UnityEngine;

namespace Farm.View
{
    /// <summary>
    /// Draws the farm from primitives. Each tile owns a small pool of prop groups (grass blades,
    /// weed clump, boulder, tree, pond, furrows, dead plant, crop) built the first time they are
    /// needed and then only shown or hidden.
    /// </summary>
    public class FarmGridView : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        // Ground colours.
        private static readonly Color GrassGround = new Color(0.30f, 0.62f, 0.24f);
        private static readonly Color TilledGround = new Color(0.55f, 0.37f, 0.20f);
        private static readonly Color WateredGround = new Color(0.30f, 0.18f, 0.10f);
        private static readonly Color WitheredGround = new Color(0.45f, 0.36f, 0.20f);
        private static readonly Color PondGround = new Color(0.12f, 0.30f, 0.62f);
        private static readonly Color TilledRidge = new Color(0.45f, 0.29f, 0.14f);
        private static readonly Color WateredRidge = new Color(0.22f, 0.13f, 0.07f);
        private static readonly Color WitheredRidge = new Color(0.38f, 0.30f, 0.16f);

        private enum Group { Grass, Weed, Rock, Tree, Pond, Furrows, Dead, Crop, Count }

        private class CropView
        {
            public GameObject Root;
            public Transform Stem;
            public Transform[] Leaves;
            public Transform Fruit;
            public Renderer FruitRenderer;
            public Transform Cob;
        }

        private class TileView
        {
            public Transform Root;
            public Transform Ground;
            public Renderer GroundRenderer;
            public readonly GameObject[] Groups = new GameObject[(int)Group.Count];
            public Renderer[] Ridges;
            public CropView Crop;
        }

        private TileView[,] tiles;
        private Material material;
        private MaterialPropertyBlock block;
        private int width;
        private int height;

        public void Build(int gridWidth, int gridHeight)
        {
            width = gridWidth;
            height = gridHeight;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            block = new MaterialPropertyBlock();
            tiles = new TileView[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var root = new GameObject($"Tile {x},{y}").transform;
                    root.SetParent(transform, false);
                    root.localPosition = TileCenter(x, y);

                    var ground = CreatePrimitive(root, PrimitiveType.Cube, "Ground");
                    ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                    // Slightly under 1 so a thin gap shows where each tile ends.
                    ground.transform.localScale = new Vector3(0.94f, 0.1f, 0.94f);

                    tiles[x, y] = new TileView { Root = root, Ground = ground.transform, GroundRenderer = ground.GetComponent<Renderer>() };
                }
            }
        }

        public Vector3 TileCenter(int x, int y) =>
            new Vector3(x - (width - 1) * 0.5f, 0f, y - (height - 1) * 0.5f);

        public void Redraw(FarmGrid grid)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    FarmTile tile = grid.Get(x, y);
                    TileView view = tiles[x, y];
                    TileState state = tile.State;
                    bool soil = state == TileState.Tilled || state == TileState.Withered;

                    Show(view, Group.Grass, state == TileState.Grass, x, y);
                    Show(view, Group.Weed, state == TileState.Weed, x, y);
                    Show(view, Group.Rock, state == TileState.Rock, x, y);
                    Show(view, Group.Tree, state == TileState.Stump, x, y);
                    Show(view, Group.Pond, state == TileState.Water, x, y);
                    Show(view, Group.Furrows, soil, x, y);
                    Show(view, Group.Dead, state == TileState.Withered, x, y);

                    // The pond sits a little below the ground.
                    view.Ground.localPosition = new Vector3(0f, state == TileState.Water ? -0.12f : -0.05f, 0f);
                    Paint(view.GroundRenderer, GroundColor(state, tile.IsWatered));
                    if (soil)
                    {
                        Color ridge = state == TileState.Withered ? WitheredRidge : tile.IsWatered ? WateredRidge : TilledRidge;
                        foreach (var r in view.Ridges) Paint(r, ridge);
                    }

                    DrawCrop(view, tile.Crop, x, y);
                }
            }
        }

        private static Color GroundColor(TileState state, bool watered)
        {
            switch (state)
            {
                case TileState.Tilled: return watered ? WateredGround : TilledGround;
                case TileState.Withered: return WitheredGround;
                case TileState.Water: return PondGround;
                default: return GrassGround; // grass, weed, rock and tree all stand on grass
            }
        }

        // ---- Prop groups -------------------------------------------------------------------

        private void Show(TileView view, Group group, bool show, int x, int y)
        {
            GameObject go = view.Groups[(int)group];
            if (go == null)
            {
                if (!show) return;
                go = BuildGroup(view, group, x, y);
                view.Groups[(int)group] = go;
            }
            if (go.activeSelf != show) go.SetActive(show);
        }

        private GameObject BuildGroup(TileView view, Group group, int x, int y)
        {
            var root = new GameObject(group.ToString()).transform;
            root.SetParent(view.Root, false);
            var rng = new System.Random(x * 73856093 ^ y * 19349663 ^ (int)group * 83492791);

            switch (group)
            {
                case Group.Grass:
                    for (int i = 0; i < 5; i++)
                    {
                        float h = 0.18f + (float)rng.NextDouble() * 0.12f;
                        var pos = new Vector3(Range(rng, -0.36f, 0.36f), h * 0.5f, Range(rng, -0.36f, 0.36f));
                        var tilt = new Vector3(Range(rng, -14f, 14f), Range(rng, 0f, 90f), Range(rng, -14f, 14f));
                        Piece(root, PrimitiveType.Cube, pos, new Vector3(0.05f, h, 0.03f),
                            i % 2 == 0 ? new Color(0.16f, 0.50f, 0.13f) : new Color(0.48f, 0.82f, 0.30f), tilt);
                    }
                    break;

                case Group.Weed:
                    for (int i = 0; i < 4; i++)
                    {
                        var pos = new Vector3(Range(rng, -0.17f, 0.17f), 0.07f, Range(rng, -0.17f, 0.17f));
                        float g = 0.30f + (float)rng.NextDouble() * 0.12f;
                        Piece(root, PrimitiveType.Sphere, pos, new Vector3(0.26f, 0.15f, 0.26f),
                            new Color(0.10f, g, 0.08f), new Vector3(0f, Range(rng, 0f, 90f), 0f));
                    }
                    break;

                case Group.Rock:
                    Piece(root, PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0f), new Vector3(0.55f, 0.34f, 0.48f), new Color(0.50f, 0.50f, 0.52f), new Vector3(0f, 20f, 0f));
                    Piece(root, PrimitiveType.Sphere, new Vector3(0.17f, 0.10f, 0.12f), new Vector3(0.36f, 0.26f, 0.34f), new Color(0.40f, 0.40f, 0.43f), Vector3.zero);
                    Piece(root, PrimitiveType.Sphere, new Vector3(-0.18f, 0.09f, -0.10f), new Vector3(0.32f, 0.22f, 0.30f), new Color(0.62f, 0.62f, 0.64f), Vector3.zero);
                    break;

                case Group.Tree: // the "stump" rule is drawn as a small tree
                    Piece(root, PrimitiveType.Cylinder, new Vector3(0f, 0.30f, 0f), new Vector3(0.22f, 0.30f, 0.22f), new Color(0.40f, 0.26f, 0.12f), Vector3.zero);
                    Piece(root, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0f), new Vector3(0.82f, 0.74f, 0.82f), new Color(0.13f, 0.48f, 0.15f), Vector3.zero);
                    Piece(root, PrimitiveType.Sphere, new Vector3(0.2f, 0.72f, 0.14f), new Vector3(0.5f, 0.44f, 0.5f), new Color(0.24f, 0.60f, 0.20f), Vector3.zero);
                    break;

                case Group.Pond:
                    // A lighter, flat highlight just above the lowered water.
                    Piece(root, PrimitiveType.Cube, new Vector3(0f, -0.065f, 0f), new Vector3(0.78f, 0.01f, 0.78f), new Color(0.35f, 0.62f, 0.92f), Vector3.zero);
                    Piece(root, PrimitiveType.Cube, new Vector3(-0.12f, -0.058f, 0.1f), new Vector3(0.3f, 0.01f, 0.08f), new Color(0.75f, 0.9f, 1f), Vector3.zero);
                    break;

                case Group.Furrows:
                    view.Ridges = new Renderer[3];
                    for (int i = 0; i < 3; i++)
                        view.Ridges[i] = Piece(root, PrimitiveType.Cube, new Vector3(0f, 0.03f, (i - 1) * 0.28f), new Vector3(0.82f, 0.07f, 0.1f), TilledRidge, Vector3.zero).GetComponent<Renderer>();
                    break;

                case Group.Dead:
                    Piece(root, PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.04f, 0.15f, 0.04f), new Color(0.55f, 0.43f, 0.20f), new Vector3(0f, 0f, 22f));
                    Piece(root, PrimitiveType.Cube, new Vector3(0.14f, 0.2f, 0f), new Vector3(0.24f, 0.02f, 0.08f), new Color(0.62f, 0.50f, 0.24f), new Vector3(0f, 0f, -38f));
                    Piece(root, PrimitiveType.Cube, new Vector3(-0.12f, 0.2f, 0.04f), new Vector3(0.2f, 0.02f, 0.07f), new Color(0.50f, 0.38f, 0.18f), new Vector3(0f, 0f, 40f));
                    break;
            }
            return root.gameObject;
        }

        // ---- Crops -------------------------------------------------------------------------

        private void DrawCrop(TileView view, CropInstance crop, int x, int y)
        {
            if (crop == null)
            {
                if (view.Crop != null && view.Crop.Root.activeSelf) view.Crop.Root.SetActive(false);
                return;
            }

            if (view.Crop == null) view.Crop = BuildCrop(view.Root);
            CropView c = view.Crop;
            if (!c.Root.activeSelf) c.Root.SetActive(true);

            float p = crop.GrowthProgress;
            string id = crop.Definition.Id;
            float stemHeight = Mathf.Lerp(0.08f, id == "corn" ? 0.75f : 0.5f, p);
            float leafSize = Mathf.Lerp(0.35f, 1f, p);

            // Cylinders are 2 units tall, so scale.y is half the height.
            c.Stem.localScale = new Vector3(0.05f, stemHeight * 0.5f, 0.05f);
            c.Stem.localPosition = new Vector3(0f, stemHeight * 0.5f, 0f);

            for (int i = 0; i < c.Leaves.Length; i++)
            {
                float angle = i * 90f + 20f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                c.Leaves[i].localScale = new Vector3(0.3f * leafSize, 0.04f, 0.13f * leafSize);
                c.Leaves[i].localPosition = direction * (0.13f * leafSize) + new Vector3(0f, stemHeight * 0.75f, 0f);
                c.Leaves[i].localRotation = Quaternion.Euler(0f, angle, 22f);
            }

            c.Fruit.gameObject.SetActive(false);
            c.Cob.gameObject.SetActive(false);
            if (!crop.IsMature) return;

            switch (id)
            {
                case "turnip": ShowFruit(c, new Vector3(0f, 0.06f, 0f), new Vector3(0.3f, 0.28f, 0.3f), new Color(0.88f, 0.72f, 0.92f)); break;
                case "potato": ShowFruit(c, new Vector3(0.06f, 0.07f, 0.04f), new Vector3(0.3f, 0.2f, 0.25f), new Color(0.62f, 0.46f, 0.26f)); break;
                case "tomato": ShowFruit(c, new Vector3(0.12f, 0.38f, 0.05f), new Vector3(0.22f, 0.22f, 0.22f), new Color(0.86f, 0.14f, 0.10f)); break;
                case "eggplant": ShowFruit(c, new Vector3(0.1f, 0.32f, 0.04f), new Vector3(0.2f, 0.32f, 0.2f), new Color(0.40f, 0.12f, 0.50f)); break;
                case "sweet-potato": ShowFruit(c, new Vector3(0.06f, 0.06f, 0.04f), new Vector3(0.34f, 0.2f, 0.22f), new Color(0.92f, 0.50f, 0.15f)); break;
                case "corn":
                    c.Cob.gameObject.SetActive(true);
                    c.Cob.localPosition = new Vector3(0.1f, 0.5f, 0f);
                    c.Cob.localScale = new Vector3(0.11f, 0.19f, 0.11f);
                    break;
                default: ShowFruit(c, new Vector3(0f, 0.25f, 0f), new Vector3(0.25f, 0.25f, 0.25f), new Color(0.95f, 0.85f, 0.15f)); break;
            }
        }

        private void ShowFruit(CropView c, Vector3 position, Vector3 scale, Color color)
        {
            c.Fruit.gameObject.SetActive(true);
            c.Fruit.localPosition = position;
            c.Fruit.localScale = scale;
            Paint(c.FruitRenderer, color);
        }

        private CropView BuildCrop(Transform tileRoot)
        {
            var root = new GameObject("Crop").transform;
            root.SetParent(tileRoot, false);
            var green = new Color(0.20f, 0.68f, 0.20f);

            var view = new CropView { Root = root.gameObject, Leaves = new Transform[4] };
            view.Stem = Piece(root, PrimitiveType.Cylinder, Vector3.zero, Vector3.one, new Color(0.25f, 0.6f, 0.2f), Vector3.zero).transform;
            for (int i = 0; i < 4; i++)
                view.Leaves[i] = Piece(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one, green, Vector3.zero).transform;
            var fruit = Piece(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one, Color.yellow, Vector3.zero);
            view.Fruit = fruit.transform;
            view.FruitRenderer = fruit.GetComponent<Renderer>();
            view.Cob = Piece(root, PrimitiveType.Cylinder, Vector3.zero, Vector3.one, new Color(0.95f, 0.80f, 0.15f), Vector3.zero).transform;
            return view;
        }

        // ---- Helpers -----------------------------------------------------------------------

        private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        private GameObject Piece(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Vector3 euler)
        {
            var go = CreatePrimitive(parent, type, type.ToString());
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            Paint(go.GetComponent<Renderer>(), color);
            return go;
        }

        private GameObject CreatePrimitive(Transform parent, PrimitiveType type, string objectName)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = objectName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private void Paint(Renderer r, Color color)
        {
            block.SetColor(BaseColor, color);
            r.SetPropertyBlock(block);
        }
    }
}
