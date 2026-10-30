namespace Farm.Farming
{
    public class FarmTile
    {
        public TileState State { get; internal set; }
        public bool IsWatered { get; internal set; }
        public CropInstance Crop { get; internal set; }
    }
}
