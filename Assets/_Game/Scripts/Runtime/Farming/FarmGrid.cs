using Farm.Time;

namespace Farm.Farming
{
    public class FarmGrid
    {
        private const double WeedChance = 0.01;

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

        /// <summary>Sets up a weed, rock, stump or pond water on a grass tile.</summary>
        public bool Place(int x, int y, TileState feature)
        {
            if (!InBounds(x, y)) return false;
            if (feature != TileState.Weed && feature != TileState.Rock
                && feature != TileState.Stump && feature != TileState.Water) return false;
            var tile = tiles[x, y];
            if (tile.State != TileState.Grass) return false;
            tile.State = feature;
            return true;
        }

        /// <summary>Clears a tile that is in the given state: weed, rock or stump back to grass, withered back to tilled.</summary>
        public bool Clear(int x, int y, TileState from)
        {
            if (!InBounds(x, y)) return false;
            var tile = tiles[x, y];
            if (tile.State != from) return false;
            switch (from)
            {
                case TileState.Weed:
                case TileState.Rock:
                case TileState.Stump:
                    tile.State = TileState.Grass;
                    return true;
                case TileState.Withered:
                    tile.State = TileState.Tilled;
                    return true;
                default:
                    return false;
            }
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

        /// <summary>
        /// Picks a mature crop. Returns its definition (so the caller knows what was harvested), or null if there is
        /// nothing mature there. Single-harvest crops are removed; regrowing crops start over.
        /// </summary>
        public CropDefinition Harvest(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            var tile = tiles[x, y];
            var crop = tile.Crop;
            if (crop == null || !crop.IsMature) return null;
            if (crop.RegrowsAfterHarvest) crop.StartRegrow();
            else tile.Crop = null;
            return crop.Definition;
        }

        /// <summary>
        /// Passes the night: watered crops grow a day, every tile dries and, if a new season starts,
        /// crops that can't grow in it wither. If the new day is rainy every tilled tile is watered,
        /// and weeds may sprout on grass.
        /// </summary>
        public void PassNight(Night night)
        {
            GrowWateredCrops();
            DryAllTiles();
            if (night.SeasonChanged) WitherOutOfSeasonCrops(night.Season);
            if (night.IsRaining) WaterAllTilledTiles();
            if (night.Random != null) SproutWeeds(night.Random);
        }

        private void WaterAllTilledTiles()
        {
            foreach (var tile in tiles)
                if (tile.State == TileState.Tilled) tile.IsWatered = true;
        }

        private void SproutWeeds(System.Random random)
        {
            foreach (var tile in tiles)
                if (tile.State == TileState.Grass && random.NextDouble() < WeedChance) tile.State = TileState.Weed;
        }

        private void GrowWateredCrops()
        {
            foreach (var tile in tiles)
                if (tile.IsWatered && tile.Crop != null) tile.Crop.GrowOneDay();
        }

        private void DryAllTiles()
        {
            foreach (var tile in tiles)
                tile.IsWatered = false;
        }

        private void WitherOutOfSeasonCrops(Season season)
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
