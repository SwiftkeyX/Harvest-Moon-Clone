using Farm.Day;
using Farm.Farming;
using Farm.Time;

namespace Farm.Tests
{
    /// <summary>
    /// A small game world wired up the way the game wires it. Tests build their world here so
    /// that when a constructor grows new dependencies, only this file changes.
    /// </summary>
    public class TestWorld
    {
        public GameCalendar Calendar { get; }
        public FarmGrid Grid { get; }
        public DayCycleService DayCycle { get; }

        public TestWorld(int gridWidth = 8, int gridHeight = 8)
        {
            Calendar = new GameCalendar();
            Grid = new FarmGrid(gridWidth, gridHeight);
            DayCycle = new DayCycleService(Calendar, Grid);
        }

        /// <summary>Waters the tile, then sleeps through one night.</summary>
        public void WaterAndSleep(int x, int y)
        {
            Grid.Water(x, y);
            DayCycle.EndDay();
        }
    }
}
