using Farm.Core;
using Farm.Day;
using Farm.Economy;
using Farm.Farming;
using Farm.Items;
using Farm.Player;
using Farm.Time;
using Farm.Tools;
using Farm.Weather;

namespace Farm.Tests
{
    /// <summary>
    /// A test-friendly view of a <see cref="GameState"/>: the game builds its world the same way,
    /// so tests run against the real wiring. By default it never rains and no weeds sprout.
    /// </summary>
    public class TestWorld
    {
        public GameState State { get; }

        public GameCalendar Calendar => State.Calendar;
        public GameClock Clock => State.Clock;
        public WeatherSystem Weather => State.Weather;
        public FarmGrid Grid => State.Grid;
        public Stamina Stamina => State.Stamina;
        public Inventory Inventory => State.Inventory;
        public Wallet Wallet => State.Wallet;
        public ShippingBin Bin => State.Bin;
        public DayCycleService DayCycle => State.DayCycle;
        public ToolUser Tools => State.Tools;

        /// <summary>Starts on the given date (default: Spring 1, Year 1) with full stamina, an empty inventory, an empty shipping bin and starting money.</summary>
        public TestWorld(int day = 1, Season season = Season.Spring, int year = 1, int gridWidth = 8, int gridHeight = 8, int maxStamina = 100, int inventorySlots = 12, System.Random random = null)
        {
            State = new GameState(new GameSetup
            {
                Day = day,
                Season = season,
                Year = year,
                GridWidth = gridWidth,
                GridHeight = gridHeight,
                MaxStamina = maxStamina,
                InventorySlots = inventorySlots,
                Random = random ?? new FixedRandom(0.99),
            });
        }

        /// <summary>A seed shop selling the given packets to this world's player.</summary>
        public SeedShop OpenShop(params SeedDefinition[] stock) => new SeedShop(stock, Wallet, Inventory, Clock, Calendar);

        /// <summary>Waters the tile, then sleeps through one night.</summary>
        public void WaterAndSleep(int x, int y)
        {
            Grid.Water(x, y);
            DayCycle.EndDay();
        }
    }
}
