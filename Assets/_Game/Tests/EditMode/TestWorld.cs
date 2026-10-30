using Farm.Day;
using Farm.Farming;
using Farm.Player;
using Farm.Time;
using Farm.Tools;

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
        public Stamina Stamina { get; }
        public DayCycleService DayCycle { get; }
        public ToolUser Tools { get; }

        /// <summary>Starts on the given date (default: Spring 1, Year 1) with full stamina.</summary>
        public TestWorld(int day = 1, Season season = Season.Spring, int year = 1, int gridWidth = 8, int gridHeight = 8, int maxStamina = 100)
        {
            Calendar = new GameCalendar(day, season, year);
            Grid = new FarmGrid(gridWidth, gridHeight);
            Stamina = new Stamina(maxStamina);
            DayCycle = new DayCycleService(Calendar, Grid, Stamina);
            Tools = new ToolUser(Grid, Stamina, DayCycle, Calendar);
        }

        /// <summary>Waters the tile, then sleeps through one night.</summary>
        public void WaterAndSleep(int x, int y)
        {
            Grid.Water(x, y);
            DayCycle.EndDay();
        }
    }
}
