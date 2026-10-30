using System.Collections.Generic;
using Farm.Day;
using Farm.Economy;
using Farm.Farming;
using Farm.Items;
using Farm.Player;
using Farm.Time;
using Farm.Tools;
using Farm.Weather;

namespace Farm.Core
{
    /// <summary>
    /// The whole game world, wired together. <see cref="Restore"/> rebuilds every system from a
    /// save, so listeners attached to the old ones (such as DayCycle.DayEnded) must be attached after it.
    /// </summary>
    public class GameState
    {
        public GameCalendar Calendar { get; private set; }
        public GameClock Clock { get; private set; }
        public WeatherSystem Weather { get; private set; }
        public FarmGrid Grid { get; private set; }
        public Stamina Stamina { get; private set; }
        public Inventory Inventory { get; private set; }
        public Wallet Wallet { get; private set; }
        public ShippingBin Bin { get; private set; }
        public DayCycleService DayCycle { get; private set; }
        public ToolUser Tools { get; private set; }
        public System.Random Random { get; private set; }

        public GameState(GameSetup setup)
        {
            Build(new GameCalendar(setup.Day, setup.Season, setup.Year), new GameClock(),
                setup.GridWidth, setup.GridHeight, setup.MaxStamina, setup.InventorySlots,
                setup.StartingMoney, setup.Random ?? new System.Random());
        }

        private void Build(GameCalendar calendar, GameClock clock, int gridWidth, int gridHeight,
            int maxStamina, int inventorySlots, int money, System.Random random)
        {
            Random = random;
            Calendar = calendar;
            Clock = clock;
            Weather = new WeatherSystem(random);
            Grid = new FarmGrid(gridWidth, gridHeight);
            Stamina = new Stamina(maxStamina);
            Inventory = new Inventory(inventorySlots);
            Wallet = new Wallet(money);
            Bin = new ShippingBin();
            DayCycle = new DayCycleService(Calendar, Clock, Grid, Stamina, Bin, Wallet, Weather, random);
            Tools = new ToolUser(Grid, Stamina, DayCycle, Calendar, Inventory);
        }

        public SaveData Capture()
        {
            var data = new SaveData
            {
                day = Calendar.Day,
                season = (int)Calendar.Season,
                year = Calendar.Year,
                clockMinute = Clock.MinuteOfDay,
                weatherToday = (int)Weather.Today,
                weatherTomorrow = (int)Weather.Tomorrow,
                gridWidth = Grid.Width,
                gridHeight = Grid.Height,
                staminaMax = Stamina.Max,
                staminaCurrent = Stamina.Current,
                money = Wallet.Money,
                canStarted = Tools.WateringCanStarted,
                waterLeft = Tools.WaterLeft,
                randomSeed = Random.Next(),
            };

            data.tiles = new SaveData.TileSave[Grid.Width * Grid.Height];
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int y = 0; y < Grid.Height; y++)
                {
                    FarmTile tile = Grid.Get(x, y);
                    var save = new SaveData.TileSave { state = (int)tile.State, watered = tile.IsWatered };
                    if (tile.Crop != null)
                    {
                        save.crop = tile.Crop.Definition.Id;
                        save.stage = tile.Crop.Stage;
                        save.daysInStage = tile.Crop.DaysInStage;
                        save.regrowing = tile.Crop.IsRegrowing;
                        save.regrowNights = tile.Crop.RegrowNights;
                    }
                    data.tiles[x * Grid.Height + y] = save;
                }
            }

            data.inventory = new SaveData.ItemSlotSave[Inventory.Slots.Count];
            for (int i = 0; i < data.inventory.Length; i++)
            {
                ItemStack stack = Inventory.Slots[i];
                data.inventory[i] = stack == null
                    ? new SaveData.ItemSlotSave()
                    : new SaveData.ItemSlotSave { item = stack.Item.Id, count = stack.Count };
            }

            var binSaves = new List<SaveData.ItemSlotSave>();
            foreach (var entry in Bin.Contents)
                binSaves.Add(new SaveData.ItemSlotSave { item = entry.Key.Id, count = entry.Value });
            data.bin = binSaves.ToArray();

            return data;
        }

        /// <summary>Replaces this world's contents with the save. Ids the database doesn't know are skipped.</summary>
        public void Restore(SaveData data, GameDatabase database)
        {
            Build(new GameCalendar(data.day, (Season)data.season, data.year), new GameClock(),
                data.gridWidth, data.gridHeight, data.staminaMax, data.inventory.Length,
                data.money, new System.Random(data.randomSeed));

            Clock.SetMinuteOfDay(data.clockMinute);
            Weather.Restore((WeatherType)data.weatherToday, (WeatherType)data.weatherTomorrow);
            Stamina.SetCurrent(data.staminaCurrent);
            Tools.RestoreWateringCan(data.canStarted, data.waterLeft);

            for (int x = 0; x < data.gridWidth; x++)
            {
                for (int y = 0; y < data.gridHeight; y++)
                {
                    var save = data.tiles[x * data.gridHeight + y];
                    CropInstance crop = null;
                    CropDefinition definition = save.crop.Length > 0 ? database.GetCrop(save.crop) : null;
                    if (definition != null)
                    {
                        crop = new CropInstance(definition);
                        crop.Restore(save.stage, save.daysInStage, save.regrowing, save.regrowNights);
                    }
                    Grid.RestoreTile(x, y, (TileState)save.state, save.watered, crop);
                }
            }

            for (int i = 0; i < data.inventory.Length; i++)
            {
                var slot = data.inventory[i];
                ItemDefinition item = slot.item.Length > 0 ? database.GetItem(slot.item) : null;
                if (item != null) Inventory.SetSlot(i, item, slot.count);
            }

            foreach (var entry in data.bin)
            {
                ItemDefinition item = database.GetItem(entry.item);
                if (item != null) Bin.AddRaw(item, entry.count);
            }
        }
    }
}
