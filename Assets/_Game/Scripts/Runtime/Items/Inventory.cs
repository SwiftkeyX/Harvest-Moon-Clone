using System.Collections.Generic;

namespace Farm.Items
{
    public class Inventory
    {
        private readonly ItemStack[] slots;

        public IReadOnlyList<ItemStack> Slots => slots;

        public Inventory(int slotCount = 12)
        {
            slots = new ItemStack[slotCount];
        }

        internal void SetSlot(int index, ItemDefinition item, int count) => slots[index] = new ItemStack(item, count);

        public int CountOf(ItemDefinition item)
        {
            int total = 0;
            foreach (var stack in slots)
                if (stack != null && stack.Item == item) total += stack.Count;
            return total;
        }

        /// <summary>True if the whole amount would fit.</summary>
        public bool CanAdd(ItemDefinition item, int count)
        {
            int room = 0;
            foreach (var stack in slots)
            {
                if (stack == null) room += item.MaxStack;
                else if (stack.Item == item) room += item.MaxStack - stack.Count;
            }
            return room >= count;
        }

        /// <summary>Adds as much as fits, topping up existing stacks first. Returns the leftover.</summary>
        public int Add(ItemDefinition item, int count)
        {
            foreach (var stack in slots)
            {
                if (count <= 0) break;
                if (stack == null || stack.Item != item) continue;
                int moved = System.Math.Min(count, item.MaxStack - stack.Count);
                stack.Count += moved;
                count -= moved;
            }
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (slots[i] != null) continue;
                int moved = System.Math.Min(count, item.MaxStack);
                slots[i] = new ItemStack(item, moved);
                count -= moved;
            }
            return count;
        }

        /// <summary>Removes the whole amount or nothing.</summary>
        public bool Remove(ItemDefinition item, int count)
        {
            if (CountOf(item) < count) return false;
            for (int i = slots.Length - 1; i >= 0 && count > 0; i--)
            {
                var stack = slots[i];
                if (stack == null || stack.Item != item) continue;
                int taken = System.Math.Min(count, stack.Count);
                stack.Count -= taken;
                count -= taken;
                if (stack.Count == 0) slots[i] = null;
            }
            return true;
        }
    }
}
