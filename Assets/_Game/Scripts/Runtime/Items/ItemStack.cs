namespace Farm.Items
{
    public class ItemStack
    {
        public ItemDefinition Item { get; }
        public int Count { get; internal set; }

        internal ItemStack(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }
    }
}
