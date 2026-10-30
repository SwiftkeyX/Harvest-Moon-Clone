using Farm.Core;
using Farm.Farming;
using Farm.Items;
using Farm.Time;
using Farm.Tools;
using Farm.Weather;
using NUnit.Framework;

namespace Farm.Tests
{
    public class SaveTests
    {
        // Saves to JSON text and loads it into a brand-new world, like quitting and reopening the game.
        private static TestWorld SaveAndReload(TestWorld world, GameDatabase database)
        {
            string json = SaveSerializer.ToJson(world.State.Capture());
            var reopened = new TestWorld();
            reopened.State.Restore(SaveSerializer.FromJson(json), database);
            return reopened;
        }

        private static GameDatabase EmptyDatabase() => TestData.Database(new ItemDefinition[0], new CropDefinition[0]);

        // The date, time, money and stamina come back exactly as they were.
        [Test]
        public void Date_time_money_and_stamina_survive_a_reload()
        {
            var world = new TestWorld(day: 12, season: Season.Summer, year: 2);
            world.Clock.Advance(200);
            world.Wallet.Add(1000);
            world.Tools.Use(TestData.Tool(ToolType.Hoe, staminaCost: 30), 1, 1);

            var reopened = SaveAndReload(world, EmptyDatabase());

            Assert.AreEqual(12, reopened.Calendar.Day);
            Assert.AreEqual(Season.Summer, reopened.Calendar.Season);
            Assert.AreEqual(2, reopened.Calendar.Year);
            Assert.AreEqual("9:20 AM", reopened.Clock.ToDisplayString());
            Assert.AreEqual(1500, reopened.Wallet.Money);
            Assert.AreEqual(70, reopened.Stamina.Current);
        }

        // Tiles keep their state (tilled, watered, debris) and crops keep growing from where they were.
        [Test]
        public void Farm_tiles_and_growing_crops_survive_a_reload()
        {
            var turnip = TestData.Turnip();
            var world = new TestWorld();
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, turnip, world.Calendar.Season);
            world.WaterAndSleep(1, 1);
            world.WaterAndSleep(1, 1);                 // turnip: 2 of 4 watered nights
            world.Grid.Till(2, 2);
            world.Grid.Water(2, 2);
            world.Grid.Place(3, 3, TileState.Rock);

            var reopened = SaveAndReload(world, TestData.Database(new[] { turnip.HarvestItem }, new[] { turnip }));

            Assert.AreEqual(TileState.Tilled, reopened.Grid.Get(2, 2).State);
            Assert.IsTrue(reopened.Grid.Get(2, 2).IsWatered);
            Assert.AreEqual(TileState.Rock, reopened.Grid.Get(3, 3).State);
            Assert.AreEqual(TileState.Grass, reopened.Grid.Get(0, 0).State);
            Assert.AreSame(turnip, reopened.Grid.Get(1, 1).Crop.Definition);

            reopened.WaterAndSleep(1, 1);
            Assert.IsFalse(reopened.Grid.Get(1, 1).Crop.IsMature, "3 of 4");
            reopened.WaterAndSleep(1, 1);
            Assert.IsTrue(reopened.Grid.Get(1, 1).Crop.IsMature, "4 of 4: growth carried over");
        }

        // A crop that is regrowing after a harvest keeps its regrow progress.
        [Test]
        public void Regrowing_crop_survives_a_reload()
        {
            var corn = TestData.Corn(regrowDays: 2);
            var world = new TestWorld();
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, corn, world.Calendar.Season);
            world.WaterAndSleep(1, 1);
            world.WaterAndSleep(1, 1);
            world.Tools.Harvest(1, 1);
            world.WaterAndSleep(1, 1);                 // 1 of 2 regrow nights

            var reopened = SaveAndReload(world, TestData.Database(new[] { corn.HarvestItem }, new[] { corn }));

            Assert.IsFalse(reopened.Grid.Get(1, 1).Crop.IsMature);
            reopened.WaterAndSleep(1, 1);
            Assert.IsTrue(reopened.Grid.Get(1, 1).Crop.IsMature, "regrow progress carried over");
        }

        // What the player carries and what's waiting in the shipping bin both come back.
        [Test]
        public void Inventory_and_shipping_bin_survive_a_reload()
        {
            var turnipItem = TestData.Item("turnip", sellPrice: 60, maxStack: 3);
            var cornItem = TestData.Item("corn", sellPrice: 100);
            var seeds = TestData.SeedPacket(TestData.Turnip(turnipItem));
            var world = new TestWorld();
            world.Inventory.Add(turnipItem, 5);        // two slots: 3 + 2
            world.Inventory.Add(seeds, 4);
            world.Inventory.Add(cornItem, 2);
            world.Bin.Ship(world.Inventory, cornItem, 2);

            var reopened = SaveAndReload(world, TestData.Database(new ItemDefinition[] { turnipItem, cornItem, seeds }, new[] { seeds.Crop }));

            Assert.AreEqual(5, reopened.Inventory.CountOf(turnipItem));
            Assert.AreEqual(3, reopened.Inventory.Slots[0].Count);
            Assert.AreEqual(2, reopened.Inventory.Slots[1].Count);
            Assert.AreEqual(4, reopened.Inventory.CountOf(seeds));
            Assert.AreEqual(200, reopened.Bin.PendingValue);

            reopened.DayCycle.EndDay();
            Assert.AreEqual(700, reopened.Wallet.Money, "the bin still sells after loading");
        }

        // Today's weather, tomorrow's forecast and the water in the can come back too.
        [Test]
        public void Weather_and_watering_can_survive_a_reload()
        {
            var world = new TestWorld(random: new FixedRandom(0.0)); // rains whenever it can
            world.DayCycle.EndDay();
            world.DayCycle.EndDay();
            Assume.That(world.Weather.Today, Is.EqualTo(WeatherType.Rain));
            var can = TestData.WateringCan(capacity: 30);
            world.Tools.Use(can, 1, 1);
            world.Tools.Use(can, 2, 1);

            var reopened = SaveAndReload(world, EmptyDatabase());

            Assert.AreEqual(WeatherType.Rain, reopened.Weather.Today);
            Assert.AreEqual(world.Weather.Tomorrow, reopened.Weather.Tomorrow);
            Assert.AreEqual(28, reopened.Tools.WaterLeft);
        }

        // The game saves when a day ends, so the day cycle announces it every time, passing out included.
        [Test]
        public void Ending_the_day_announces_it_for_saving()
        {
            var world = new TestWorld(maxStamina: 2);
            int dayEnds = 0;
            world.DayCycle.DayEnded += () => dayEnds++;

            world.DayCycle.EndDay();
            world.Tools.Use(TestData.Tool(ToolType.Hoe, staminaCost: 2), 1, 1); // passes out
            world.DayCycle.PassTime(20 * 60);                                    // stays up until 2 AM

            Assert.AreEqual(3, dayEnds);
        }
    }
}
