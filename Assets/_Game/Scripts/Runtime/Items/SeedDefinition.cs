using Farm.Farming;
using UnityEngine;

namespace Farm.Items
{
    [CreateAssetMenu(menuName = "Farm/Seed Packet")]
    public class SeedDefinition : ItemDefinition
    {
        [SerializeField] private CropDefinition crop;
        [SerializeField] private int buyPrice;

        public CropDefinition Crop => crop;
        public int BuyPrice => buyPrice;

        public void Configure(string id, CropDefinition crop, int buyPrice)
        {
            Configure(id, 0);
            this.crop = crop;
            this.buyPrice = buyPrice;
        }
    }
}
