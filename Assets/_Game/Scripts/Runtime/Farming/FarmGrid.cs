using Farm.Time;

namespace Farm.Farming
{
    public class FarmGrid
    {
        private readonly FarmTile[,] tiles;

        public int Width { get; }
        public int Height { get; }

        public FarmGrid(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new FarmTile[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tiles[x, y] = new FarmTile { State = TileState.Grass };
        }

        public FarmTile Get(int x, int y) => InBounds(x, y) ? tiles[x, y] : null;

        public bool Till(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            var tile = tiles[x, y];
            if (tile.State != TileState.Grass) return false;
            tile.State = TileState.Tilled;
            return true;
        }

        public bool Water(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            var tile = tiles[x, y];
            if (tile.State != TileState.Tilled) return false;
            tile.IsWatered = true;
            return true;
        }

        public bool Plant(int x, int y, CropDefinition crop, Season season)
        {
            if (!InBounds(x, y) || crop == null) return false;
            var tile = tiles[x, y];
            if (tile.State != TileState.Tilled || tile.Crop != null || !crop.GrowsIn(season)) return false;
            tile.Crop = new CropInstance(crop);
            return true;
        }

        public void AdvanceDay()
        {
            foreach (var tile in tiles)
            {
                if (tile.IsWatered && tile.Crop != null) tile.Crop.GrowOneDay();
                tile.IsWatered = false;
            }
        }

        public void WitherOutOfSeasonCrops(Season season)
        {
            foreach (var tile in tiles)
            {
                if (tile.Crop == null || tile.Crop.Definition.GrowsIn(season)) continue;
                tile.Crop = null;
                tile.State = TileState.Withered;
            }
        }

        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    }
}
