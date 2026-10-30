using UnityEngine;

namespace Farm.Items
{
    [CreateAssetMenu(menuName = "Farm/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private int sellPrice;
        [SerializeField] private int maxStack = 99;

        public string Id => id;
        public int SellPrice => sellPrice;
        public int MaxStack => maxStack;

        public void Configure(string id, int sellPrice, int maxStack = 99)
        {
            this.id = id;
            this.sellPrice = sellPrice;
            this.maxStack = maxStack;
        }
    }
}
