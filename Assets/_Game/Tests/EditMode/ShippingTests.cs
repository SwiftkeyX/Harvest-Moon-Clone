using Farm.Tools;
using NUnit.Framework;

namespace Farm.Tests
{
    public class ShippingTests
    {
        // Every new game starts with 500 money.
        [Test]
        public void New_game_starts_with_500_money()
        {
            var world = new TestWorld();

            Assert.AreEqual(500, world.Wallet.Money);
        }

        // Shipping moves items from the inventory into the bin. Nothing is paid until the night.
        [Test]
        public void Shipping_moves_items_into_the_bin_without_paying_yet()
        {
            var world = new TestWorld();
            var turnip = TestData.Item("turnip", sellPrice: 60);
            world.Inventory.Add(turnip, 5);

            Assert.IsTrue(world.Bin.Ship(world.Inventory, turnip, 3));

            Assert.AreEqual(2, world.Inventory.CountOf(turnip));
            Assert.AreEqual(180, world.Bin.PendingValue);
            Assert.AreEqual(500, world.Wallet.Money, "paid overnight, not now");
        }

        // You can't ship more than you carry; nothing moves.
        [Test]
        public void Shipping_more_than_you_carry_does_nothing()
        {
            var world = new TestWorld();
            var turnip = TestData.Item("turnip", sellPrice: 60);
            world.Inventory.Add(turnip, 2);

            Assert.IsFalse(world.Bin.Ship(world.Inventory, turnip, 3));

            Assert.AreEqual(2, world.Inventory.CountOf(turnip));
            Assert.AreEqual(0, world.Bin.PendingValue);
        }

        // Overnight, everything in the bin sells at its sell price; the money is there in the
        // morning and the bin is empty.
        [Test]
        public void Sleeping_sells_the_bin_and_empties_it()
        {
            var world = new TestWorld();
            var turnip = TestData.Item("turnip", sellPrice: 60);
            var corn = TestData.Item("corn", sellPrice: 100);
            world.Inventory.Add(turnip, 2);
            world.Inventory.Add(corn, 1);
            world.Bin.Ship(world.Inventory, turnip, 2);
            world.Bin.Ship(world.Inventory, corn, 1);

            world.DayCycle.EndDay();

            Assert.AreEqual(720, world.Wallet.Money, "500 + 2×60 + 100");
            Assert.AreEqual(0, world.Bin.PendingValue);
        }

        // A night with an empty bin earns nothing.
        [Test]
        public void Empty_bin_earns_nothing()
        {
            var world = new TestWorld();

            world.DayCycle.EndDay();

            Assert.AreEqual(500, world.Wallet.Money);
        }

        // Passing out still ends the day, so the bin is still sold.
        [Test]
        public void Bin_is_sold_even_when_the_player_passes_out()
        {
            var world = new TestWorld(maxStamina: 2);
            var turnip = TestData.Item("turnip", sellPrice: 60);
            world.Inventory.Add(turnip, 1);
            world.Bin.Ship(world.Inventory, turnip, 1);

            var result = world.Tools.Use(TestData.Tool(ToolType.Hoe, staminaCost: 2), 1, 1);

            Assert.IsTrue(result.PassedOut);
            Assert.AreEqual(560, world.Wallet.Money);
        }

        // A harvested crop can go straight into the bin and becomes money overnight.
        [Test]
        public void Harvest_ship_and_sleep_turns_a_crop_into_money()
        {
            var world = new TestWorld();
            var turnipItem = TestData.Item("turnip", sellPrice: 60);
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(turnipItem), world.Calendar.Season);
            for (int n = 0; n < 4; n++) world.WaterAndSleep(1, 1);
            world.Tools.Harvest(1, 1);

            world.Bin.Ship(world.Inventory, turnipItem, 1);
            world.DayCycle.EndDay();

            Assert.AreEqual(560, world.Wallet.Money);
        }
    }
}
