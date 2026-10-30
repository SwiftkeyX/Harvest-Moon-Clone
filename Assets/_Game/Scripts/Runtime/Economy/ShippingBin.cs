using System.Collections.Generic;
using Farm.Items;

namespace Farm.Economy
{
    public class ShippingBin
    {
        private readonly Dictionary<ItemDefinition, int> contents = new Dictionary<ItemDefinition, int>();

        internal IEnumerable<KeyValuePair<ItemDefinition, int>> Contents => contents;

        internal void AddRaw(ItemDefinition item, int count)
        {
            contents.TryGetValue(item, out int held);
            contents[item] = held + count;
        }

        public int PendingValue
        {
            get
            {
                int total = 0;
                foreach (var entry in contents) total += entry.Key.SellPrice * entry.Value;
                return total;
            }
        }

        /// <summary>Moves the items out of the inventory into the bin. Does nothing if the inventory holds too few.</summary>
        public bool Ship(Inventory from, ItemDefinition item, int count)
        {
            if (!from.Remove(item, count)) return false;
            contents.TryGetValue(item, out int held);
            contents[item] = held + count;
            return true;
        }

        /// <summary>Sells everything in the bin into the wallet, empties the bin and returns the amount earned.</summary>
        public int SellAll(Wallet wallet)
        {
            int earned = PendingValue;
            wallet.Add(earned);
            contents.Clear();
            return earned;
        }
    }
}
