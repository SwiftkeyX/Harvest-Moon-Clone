using Farm.Day;
using Farm.Farming;
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
        private bool canStarted;

        public int WaterLeft { get; private set; }

        public ToolUser(FarmGrid grid, Stamina stamina, DayCycleService dayCycle, GameCalendar calendar)
        {
            this.grid = grid;
            this.stamina = stamina;
            this.dayCycle = dayCycle;
            this.calendar = calendar;
        }

        public ToolResult Use(ToolDefinition tool, int x, int y)
        {
            bool success = Apply(tool, x, y);

            stamina.Spend(tool.StaminaCost);
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
                case ToolType.SeedBag:
                    return PlantSquare(tool, x, y);
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

        private bool PlantSquare(ToolDefinition tool, int x, int y)
        {
            bool planted = false;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    planted |= grid.Plant(x + dx, y + dy, tool.Crop, calendar.Season);
            return planted;
        }
    }
}
