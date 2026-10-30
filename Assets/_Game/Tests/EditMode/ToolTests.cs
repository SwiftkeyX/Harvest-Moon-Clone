using Farm.Farming;
using Farm.Time;
using Farm.Tools;
using NUnit.Framework;

namespace Farm.Tests
{
    public class ToolTests
    {
        // The hoe digs grass into tilled soil, and every swing costs the tool's stamina.
        [Test]
        public void Hoe_tills_grass_and_costs_stamina()
        {
            var world = new TestWorld();

            var result = world.Tools.Use(TestData.Tool(ToolType.Hoe, staminaCost: 2), 1, 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(TileState.Tilled, world.Grid.Get(1, 1).State);
            Assert.AreEqual(98, world.Stamina.Current);
        }

        // A swing that does nothing still costs stamina.
        [Test]
        public void Hoe_on_a_rock_does_nothing_but_still_costs_stamina()
        {
            var world = new TestWorld();
            world.Grid.Place(1, 1, TileState.Rock);

            var result = world.Tools.Use(TestData.Tool(ToolType.Hoe, staminaCost: 2), 1, 1);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(TileState.Rock, world.Grid.Get(1, 1).State);
            Assert.AreEqual(98, world.Stamina.Current);
        }

        // Weeds, rocks and stumps can only be put on grass when the farm is set up.
        [Test]
        public void Debris_can_only_be_placed_on_grass()
        {
            var world = new TestWorld();
            world.Grid.Till(1, 1);

            Assert.IsTrue(world.Grid.Place(2, 2, TileState.Weed));
            Assert.IsFalse(world.Grid.Place(1, 1, TileState.Stump), "tilled soil");
            Assert.IsFalse(world.Grid.Place(2, 2, TileState.Rock), "already has a weed");
        }

        // Each kind of debris needs its own tool: sickle for weeds, hammer for rocks, axe for stumps.
        [Test]
        public void Each_debris_is_cleared_to_grass_only_by_its_own_tool()
        {
            var world = new TestWorld();
            world.Grid.Place(1, 1, TileState.Weed);
            world.Grid.Place(2, 1, TileState.Rock);
            world.Grid.Place(3, 1, TileState.Stump);

            Assert.IsFalse(world.Tools.Use(TestData.Tool(ToolType.Hammer), 3, 1).Success, "hammer can't clear a stump");
            Assert.AreEqual(TileState.Stump, world.Grid.Get(3, 1).State);

            Assert.IsTrue(world.Tools.Use(TestData.Tool(ToolType.Sickle), 1, 1).Success);
            Assert.IsTrue(world.Tools.Use(TestData.Tool(ToolType.Hammer), 2, 1).Success);
            Assert.IsTrue(world.Tools.Use(TestData.Tool(ToolType.Axe), 3, 1).Success);

            Assert.AreEqual(TileState.Grass, world.Grid.Get(1, 1).State);
            Assert.AreEqual(TileState.Grass, world.Grid.Get(2, 1).State);
            Assert.AreEqual(TileState.Grass, world.Grid.Get(3, 1).State);
        }

        // The sickle also clears a withered crop, leaving the soil tilled and ready to replant.
        [Test]
        public void Sickle_clears_a_withered_tile_back_to_tilled_soil()
        {
            var world = new TestWorld(day: 30, season: Season.Spring);
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(), world.Calendar.Season);
            world.DayCycle.EndDay();
            Assume.That(world.Grid.Get(1, 1).State, Is.EqualTo(TileState.Withered));

            Assert.IsTrue(world.Tools.Use(TestData.Tool(ToolType.Sickle), 1, 1).Success);

            Assert.AreEqual(TileState.Tilled, world.Grid.Get(1, 1).State);
            Assert.IsTrue(world.Grid.Plant(1, 1, TestData.Tomato(), world.Calendar.Season), "summer crop plants there now");
        }

        // The watering can starts full, waters until it's empty, and refills at a water tile.
        [Test]
        public void Watering_can_runs_dry_and_refills_at_water()
        {
            var world = new TestWorld();
            var can = TestData.WateringCan(capacity: 2);
            world.Grid.Place(0, 0, TileState.Water);
            for (int x = 1; x <= 3; x++) world.Grid.Till(x, 1);

            Assert.IsTrue(world.Tools.Use(can, 1, 1).Success);
            Assert.IsTrue(world.Tools.Use(can, 2, 1).Success);
            Assert.IsFalse(world.Tools.Use(can, 3, 1).Success, "can is empty");
            Assert.IsFalse(world.Grid.Get(3, 1).IsWatered);

            Assert.IsTrue(world.Tools.Use(can, 0, 0).Success, "refill at the pond");
            Assert.AreEqual(2, world.Tools.WaterLeft);

            Assert.IsTrue(world.Tools.Use(can, 3, 1).Success);
            Assert.IsTrue(world.Grid.Get(3, 1).IsWatered);
        }

        // Sowing a seed packet plants the 3×3 square around the target, on tilled empty tiles only,
        // uses up one packet and costs stamina once.
        [Test]
        public void Sowing_a_packet_plants_the_3x3_square_and_uses_one_packet()
        {
            var world = new TestWorld();
            var turnip = TestData.Turnip();
            var packet = TestData.SeedPacket(turnip);
            world.Inventory.Add(packet, 2);
            for (int x = 1; x <= 3; x++)
                for (int y = 1; y <= 3; y++)
                    world.Grid.Till(x, y);
            world.Grid.Plant(1, 1, TestData.Strawberry(), world.Calendar.Season); // already planted

            var result = world.Tools.Sow(packet, 2, 2);

            Assert.IsTrue(result.Success);
            int turnips = 0;
            for (int x = 1; x <= 3; x++)
                for (int y = 1; y <= 3; y++)
                    if (world.Grid.Get(x, y).Crop?.Definition == turnip) turnips++;
            Assert.AreEqual(8, turnips, "every tilled empty tile in the square except the strawberry");
            Assert.AreEqual(1, world.Inventory.CountOf(packet), "one packet used");
            Assert.AreEqual(98, world.Stamina.Current, "one swing, one cost");
        }

        // Without a packet in the inventory there's nothing to sow: nothing happens.
        [Test]
        public void Sowing_without_a_packet_does_nothing()
        {
            var world = new TestWorld();
            var packet = TestData.SeedPacket(TestData.Turnip());
            world.Grid.Till(2, 2);

            var result = world.Tools.Sow(packet, 2, 2);

            Assert.IsFalse(result.Success);
            Assert.IsNull(world.Grid.Get(2, 2).Crop);
            Assert.AreEqual(100, world.Stamina.Current, "no packet, no swing");
        }

        // Scattering seeds where nothing can grow wastes the swing but keeps the packet.
        [Test]
        public void Sowing_on_grass_keeps_the_packet_but_costs_stamina()
        {
            var world = new TestWorld();
            var packet = TestData.SeedPacket(TestData.Turnip());
            world.Inventory.Add(packet, 1);

            var result = world.Tools.Sow(packet, 4, 4);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(1, world.Inventory.CountOf(packet));
            Assert.AreEqual(98, world.Stamina.Current);
        }

        // Using tools until stamina hits 0 makes the player pass out: the day ends at once and
        // they wake with half stamina.
        [Test]
        public void Running_out_of_stamina_passes_out_and_wakes_with_half()
        {
            var world = new TestWorld(maxStamina: 4);
            var hoe = TestData.Tool(ToolType.Hoe, staminaCost: 2);

            Assert.IsFalse(world.Tools.Use(hoe, 1, 1).PassedOut);
            var result = world.Tools.Use(hoe, 2, 1);

            Assert.IsTrue(result.PassedOut);
            Assert.AreEqual(2, world.Calendar.Day, "the day ended");
            Assert.AreEqual(2, world.Stamina.Current, "half of 4");
        }

        // A normal night's sleep restores stamina to full.
        [Test]
        public void Sleeping_restores_full_stamina()
        {
            var world = new TestWorld();
            world.Tools.Use(TestData.Tool(ToolType.Hoe, staminaCost: 30), 1, 1);

            world.DayCycle.EndDay();

            Assert.AreEqual(100, world.Stamina.Current);
        }
    }
}
