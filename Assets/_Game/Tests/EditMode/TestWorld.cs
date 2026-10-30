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
    /// A small game world wired up the way the game wires it. Tests build their world here so
    /// that when a constructor grows new dependencies, only this file changes.
    /// </summary>
    public class TestWorld
    {
        public GameCalendar Calendar { get; }
        public GameClock Clock { get; }
        public WeatherSystem Weather { get; }
        public FarmGrid Grid { get; }
        public Stamina Stamina { get; }
        public Inventory Inventory { get; }
        public Wallet Wallet { get; }
        public ShippingBin Bin { get; }
        public DayCycleService DayCycle { get; }
        public ToolUser Tools { get; }

        /// <summary>Starts on the given date (default: Spring 1, Year 1) with full stamina, an empty inventory, an empty shipping bin and starting money.</summary>
        public TestWorld(int day = 1, Season season = Season.Spring, int year = 1, int gridWidth = 8, int gridHeight = 8, int maxStamina = 100, int inventorySlots = 12, System.Random random = null)
        {
            Calendar = new GameCalendar(day, season, year);
            Clock = new GameClock();
            // Tests are deterministic: by default it never rains and no weeds sprout.
            random ??= new FixedRandom(0.99);
            Weather = new WeatherSystem(random);
            Grid = new FarmGrid(gridWidth, gridHeight);
            Stamina = new Stamina(maxStamina);
            Inventory = new Inventory(inventorySlots);
            Wallet = new Wallet();
            Bin = new ShippingBin();
            DayCycle = new DayCycleService(Calendar, Clock, Grid, Stamina, Bin, Wallet, Weather, random);
            Tools = new ToolUser(Grid, Stamina, DayCycle, Calendar, Inventory);
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
