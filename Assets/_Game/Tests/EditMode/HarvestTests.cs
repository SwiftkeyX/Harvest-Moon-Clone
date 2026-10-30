using Farm.Farming;
using NUnit.Framework;

namespace Farm.Tests
{
    public class HarvestTests
    {
        private static void PlantAndGrow(TestWorld world, CropDefinition crop, int x, int y, int nights)
        {
            world.Grid.Till(x, y);
            world.Grid.Plant(x, y, crop, world.Calendar.Season);
            for (int n = 0; n < nights; n++) world.WaterAndSleep(x, y);
        }

        // Picking a mature crop puts its produce in the inventory and leaves tilled, empty soil.
        [Test]
        public void Harvesting_a_mature_turnip_puts_it_in_the_inventory()
        {
            var world = new TestWorld();
            var turnipItem = TestData.Item("turnip");
            PlantAndGrow(world, TestData.Turnip(turnipItem), 1, 1, nights: 4);

            Assert.IsTrue(world.Tools.Harvest(1, 1));

            Assert.AreEqual(1, world.Inventory.CountOf(turnipItem));
            Assert.IsNull(world.Grid.Get(1, 1).Crop);
            Assert.AreEqual(TileState.Tilled, world.Grid.Get(1, 1).State);
        }

        // A crop that isn't mature yet can't be picked.
        [Test]
        public void Unripe_crop_cannot_be_harvested()
        {
            var world = new TestWorld();
            var turnipItem = TestData.Item("turnip");
            PlantAndGrow(world, TestData.Turnip(turnipItem), 1, 1, nights: 3);

            Assert.IsFalse(world.Tools.Harvest(1, 1));

            Assert.AreEqual(0, world.Inventory.CountOf(turnipItem));
            Assert.IsNotNull(world.Grid.Get(1, 1).Crop);
        }

        // Picking is done by hand and costs no stamina.
        [Test]
        public void Harvesting_costs_no_stamina()
        {
            var world = new TestWorld();
            PlantAndGrow(world, TestData.Turnip(), 1, 1, nights: 4);

            world.Tools.Harvest(1, 1);

            Assert.AreEqual(100, world.Stamina.Current);
        }

        // With no room in the inventory the crop is not picked and stays in the ground.
        [Test]
        public void Full_inventory_leaves_the_crop_in_the_ground()
        {
            var world = new TestWorld(inventorySlots: 1);
            var turnipItem = TestData.Item("turnip");
            world.Inventory.Add(TestData.Item("rock"), 1);
            PlantAndGrow(world, TestData.Turnip(turnipItem), 1, 1, nights: 4);

            Assert.IsFalse(world.Tools.Harvest(1, 1));

            Assert.AreEqual(0, world.Inventory.CountOf(turnipItem));
            Assert.IsTrue(world.Grid.Get(1, 1).Crop.IsMature, "still waiting to be picked");
        }

        // A regrowing crop stays planted after picking and is ready again after its regrow nights.
        [Test]
        public void Corn_regrows_after_harvest_and_can_be_picked_again()
        {
            var world = new TestWorld();
            var cornItem = TestData.Item("corn");
            PlantAndGrow(world, TestData.Corn(cornItem, regrowDays: 2), 1, 1, nights: 2);

            Assert.IsTrue(world.Tools.Harvest(1, 1));
            Assert.IsNotNull(world.Grid.Get(1, 1).Crop, "corn plant stays");
            Assert.IsFalse(world.Grid.Get(1, 1).Crop.IsMature, "regrowing");

            world.WaterAndSleep(1, 1);
            Assert.IsFalse(world.Grid.Get(1, 1).Crop.IsMature, "1 of 2 regrow nights");

            world.WaterAndSleep(1, 1);
            Assert.IsTrue(world.Grid.Get(1, 1).Crop.IsMature, "ready again");

            Assert.IsTrue(world.Tools.Harvest(1, 1));
            Assert.AreEqual(2, world.Inventory.CountOf(cornItem));
        }

        // Regrowing pauses on dry nights, just like first-time growth.
        [Test]
        public void Regrowing_corn_needs_watered_nights()
        {
            var world = new TestWorld();
            PlantAndGrow(world, TestData.Corn(regrowDays: 2), 1, 1, nights: 2);
            world.Tools.Harvest(1, 1);

            world.DayCycle.EndDay();
            world.DayCycle.EndDay();

            Assert.IsFalse(world.Grid.Get(1, 1).Crop.IsMature);
        }
    }
}
