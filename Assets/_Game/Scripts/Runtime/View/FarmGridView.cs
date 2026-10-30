using Farm.Farming;
using UnityEngine;

namespace Farm.View
{
    public class FarmGridView : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private static readonly Color Grass = new Color(0.30f, 0.65f, 0.25f);
        private static readonly Color Tilled = new Color(0.55f, 0.37f, 0.20f);
        private static readonly Color Withered = new Color(0.58f, 0.48f, 0.22f); // dry straw
        private static readonly Color Watered = new Color(0.30f, 0.18f, 0.10f);
        private static readonly Color Growing = new Color(0.25f, 0.85f, 0.25f);
        private static readonly Color Mature = new Color(0.95f, 0.85f, 0.15f);

        private const float MinCropHeight = 0.1f;
        private const float MaxCropHeight = 0.8f;

        private Renderer[,] tileRenderers;
        private Renderer[,] cropRenderers;
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
            tileRenderers = new Renderer[width, height];
            cropRenderers = new Renderer[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 center = TileCenter(x, y);

                    var tile = CreatePrimitive(PrimitiveType.Cube, $"Tile {x},{y}");
                    tile.transform.localPosition = center + new Vector3(0f, -0.05f, 0f);
                    // Slightly under 1 so a thin gap shows where each tile ends.
                    tile.transform.localScale = new Vector3(0.94f, 0.1f, 0.94f);
                    tileRenderers[x, y] = tile.GetComponent<Renderer>();

                    var crop = CreatePrimitive(PrimitiveType.Cylinder, $"Crop {x},{y}");
                    crop.transform.localPosition = center;
                    crop.SetActive(false);
                    cropRenderers[x, y] = crop.GetComponent<Renderer>();
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
                    Color tileColor = tile.State == TileState.Grass ? Grass
                        : tile.State == TileState.Withered ? Withered
                        : tile.IsWatered ? Watered : Tilled;
                    Paint(tileRenderers[x, y], tileColor);

                    Renderer cropRenderer = cropRenderers[x, y];
                    CropInstance crop = tile.Crop;
                    cropRenderer.gameObject.SetActive(crop != null);
                    if (crop == null) continue;

                    float progress = crop.IsMature
                        ? 1f
                        : (float)crop.Stage / crop.Definition.DaysPerStage.Length;
                    float cropHeight = Mathf.Max(MinCropHeight, progress * MaxCropHeight);
                    Transform t = cropRenderer.transform;
                    // Unity's cylinder is 2 units tall, so scale.y is half the height.
                    t.localScale = new Vector3(0.5f, cropHeight * 0.5f, 0.5f);
                    t.localPosition = TileCenter(x, y) + new Vector3(0f, cropHeight * 0.5f, 0f);
                    Paint(cropRenderer, crop.IsMature ? Mature : Growing);
                }
            }
        }

        private GameObject CreatePrimitive(PrimitiveType type, string objectName)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = objectName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
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
