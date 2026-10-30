using Farm.Day;
using Farm.Farming;
using Farm.Items;
using Farm.Player;
using Farm.Time;

namespace Farm.Tools
{
    public class ToolUser
    {
        private readonly FarmGrid grid;
        private readonly Stamina stamina;
        private readonly DayCycleService dayCycle;
        private readonly GameCalendar calendar;
        private readonly Inventory inventory;
        private bool canStarted;
        private const int SowStaminaCost = 2;

        public int WaterLeft { get; private set; }

        /// <summary>False until the watering can is first used (it starts full then).</summary>
        public bool WateringCanStarted => canStarted;

        internal void RestoreWateringCan(bool started, int waterLeft)
        {
            canStarted = started;
            WaterLeft = waterLeft;
        }

        public ToolUser(FarmGrid grid, Stamina stamina, DayCycleService dayCycle, GameCalendar calendar, Inventory inventory)
        {
            this.grid = grid;
            this.stamina = stamina;
            this.dayCycle = dayCycle;
            this.calendar = calendar;
            this.inventory = inventory;
        }

        /// <summary>Picks a mature crop by hand (no stamina). Nothing happens unless the whole yield fits.</summary>
        public bool Harvest(int x, int y)
        {
            var crop = grid.Get(x, y)?.Crop;
            if (crop == null || !crop.IsMature) return false;
            var item = crop.Definition.HarvestItem;
            int count = crop.Definition.HarvestYield;
            if (item == null || !inventory.CanAdd(item, count)) return false;

            grid.Harvest(x, y);
            inventory.Add(item, count);
            return true;
        }

        public ToolResult Use(ToolDefinition tool, int x, int y)
        {
            bool success = Apply(tool, x, y);
            return Finish(success, tool.StaminaCost);
        }

        /// <summary>
        /// Sows a seed packet over the 3x3 square around the target. Needs a packet in the inventory
        /// (otherwise nothing happens); a packet is used up only if a seed was planted.
        /// </summary>
        public ToolResult Sow(SeedDefinition seeds, int x, int y)
        {
            if (inventory.CountOf(seeds) < 1) return new ToolResult(false, false);

            bool planted = false;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    planted |= grid.Plant(x + dx, y + dy, seeds.Crop, calendar.Season);
            if (planted) inventory.Remove(seeds, 1);
            return Finish(planted, SowStaminaCost);
        }

        private ToolResult Finish(bool success, int staminaCost)
        {
            stamina.Spend(staminaCost);
            bool passedOut = stamina.IsExhausted;
            if (passedOut) dayCycle.EndDay(passedOut: true);
            return new ToolResult(success, passedOut);
        }

        private bool Apply(ToolDefinition tool, int x, int y)
        {
            FarmTile tile = grid.Get(x, y);
            switch (tool.Type)
            {
                case ToolType.Hoe:
                    return grid.Till(x, y);
                case ToolType.Sickle:
                    if (tile == null) return false;
                    return tile.State == TileState.Weed ? grid.Clear(x, y, TileState.Weed)
                        : grid.Clear(x, y, TileState.Withered);
                case ToolType.Hammer:
                    return grid.Clear(x, y, TileState.Rock);
                case ToolType.Axe:
                    return grid.Clear(x, y, TileState.Stump);
                case ToolType.WateringCan:
                    return UseWateringCan(tool, x, y, tile);
                default:
                    return false;
            }
        }

        private bool UseWateringCan(ToolDefinition tool, int x, int y, FarmTile tile)
        {
            if (!canStarted)
            {
                canStarted = true;
                WaterLeft = tool.Capacity;
            }
            if (tile == null) return false;
            if (tile.State == TileState.Water)
            {
                WaterLeft = tool.Capacity;
                return true;
            }
            if (WaterLeft <= 0) return false;
            WaterLeft--;
            return grid.Water(x, y);
        }
    }
}
