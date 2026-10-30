using System.Collections.Generic;
using Farm.Items;
using Farm.Tools;

namespace Farm.View
{
    /// <summary>The selectable slots: the tools, then one slot per kind of seed packet in the inventory.</summary>
    public class Hotbar
    {
        public class Entry
        {
            public ToolDefinition Tool;
            public SeedDefinition Seeds;
        }

        private readonly List<Entry> toolEntries = new List<Entry>();
        private readonly List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
        public int Selected { get; private set; }
        public Entry Current => entries[Selected];

        public Hotbar(IEnumerable<ToolDefinition> tools)
        {
            foreach (var tool in tools) toolEntries.Add(new Entry { Tool = tool });
            entries.AddRange(toolEntries);
        }

        /// <summary>Rebuilds the seed slots from the inventory, keeping the same slot selected if it still exists.</summary>
        public void Refresh(Inventory inventory)
        {
            SeedDefinition selectedSeeds = Current.Seeds;
            ToolDefinition selectedTool = Current.Tool;

            entries.Clear();
            entries.AddRange(toolEntries);
            foreach (var stack in inventory.Slots)
            {
                if (stack == null || !(stack.Item is SeedDefinition seeds)) continue;
                if (entries.Exists(e => e.Seeds == seeds)) continue;
                entries.Add(new Entry { Seeds = seeds });
            }

            int index = entries.FindIndex(e => e.Seeds != null ? e.Seeds == selectedSeeds : e.Tool == selectedTool);
            Selected = index >= 0 ? index : System.Math.Min(Selected, entries.Count - 1);
        }

        public void Next() => Selected = (Selected + 1) % entries.Count;

        public void Previous() => Selected = (Selected - 1 + entries.Count) % entries.Count;
    }
}
