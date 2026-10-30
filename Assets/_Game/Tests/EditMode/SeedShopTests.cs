using System.Linq;
using Farm.Time;
using NUnit.Framework;

namespace Farm.Tests
{
    public class SeedShopTests
    {
        private const int NineAm = 540;   // minutes after midnight
        private const int FivePm = 1020;

        private static void OpenAtNine(TestWorld world) => world.Clock.Advance(NineAm - world.Clock.MinuteOfDay);

        // During opening hours, in-season seed packets can be bought: the money goes, the packets
        // go into the inventory.
        [Test]
        public void Buying_seeds_takes_money_and_adds_packets()
        {
            var world = new TestWorld();
            var packet = TestData.SeedPacket(TestData.Turnip(), buyPrice: 20);
            var shop = world.OpenShop(packet);
            OpenAtNine(world);

            Assert.IsTrue(shop.Buy(packet, 3));

            Assert.AreEqual(440, world.Wallet.Money, "500 − 3×20");
            Assert.AreEqual(3, world.Inventory.CountOf(packet));
        }

        // The shop opens at 9:00 AM and closes at 5:00 PM.
        [Test]
        public void Shop_is_closed_before_nine_and_from_five()
        {
            var world = new TestWorld();
            var packet = TestData.SeedPacket(TestData.Turnip());
            var shop = world.OpenShop(packet);

            Assert.IsFalse(shop.Buy(packet, 1), "6:00 AM, not open yet");

            world.Clock.Advance(FivePm - world.Clock.MinuteOfDay - 1);
            Assert.IsTrue(shop.Buy(packet, 1), "4:59 PM, still open");

            world.Clock.Advance(1);
            Assert.IsFalse(shop.Buy(packet, 1), "5:00 PM, closed");
            Assert.AreEqual(1, world.Inventory.CountOf(packet));
        }

        // The shop only sells seeds for crops that grow in the current season.
        [Test]
        public void Only_in_season_seeds_are_sold()
        {
            var world = new TestWorld(season: Season.Spring);
            var turnipSeeds = TestData.SeedPacket(TestData.Turnip());
            var tomatoSeeds = TestData.SeedPacket(TestData.Tomato());
            var shop = world.OpenShop(turnipSeeds, tomatoSeeds);
            OpenAtNine(world);

            CollectionAssert.AreEqual(new[] { turnipSeeds }, shop.AvailableNow().ToArray());
            Assert.IsFalse(shop.Buy(tomatoSeeds, 1), "tomatoes are a summer crop");
            Assert.AreEqual(500, world.Wallet.Money);
        }

        // Without enough money nothing is bought.
        [Test]
        public void Not_enough_money_buys_nothing()
        {
            var world = new TestWorld();
            var packet = TestData.SeedPacket(TestData.Turnip(), buyPrice: 200);
            var shop = world.OpenShop(packet);
            OpenAtNine(world);

            Assert.IsFalse(shop.Buy(packet, 3), "600 needed, 500 held");

            Assert.AreEqual(500, world.Wallet.Money);
            Assert.AreEqual(0, world.Inventory.CountOf(packet));
        }

        // If the packets won't all fit in the inventory, nothing is bought and no money is taken.
        [Test]
        public void Full_inventory_buys_nothing()
        {
            var world = new TestWorld(inventorySlots: 1);
            world.Inventory.Add(TestData.Item("rock"), 1);
            var packet = TestData.SeedPacket(TestData.Turnip());
            var shop = world.OpenShop(packet);
            OpenAtNine(world);

            Assert.IsFalse(shop.Buy(packet, 1));

            Assert.AreEqual(500, world.Wallet.Money);
        }

        // Seeds the shop doesn't stock can't be bought there.
        [Test]
        public void Seeds_not_in_stock_cannot_be_bought()
        {
            var world = new TestWorld();
            var shop = world.OpenShop(TestData.SeedPacket(TestData.Turnip()));
            var strawberrySeeds = TestData.SeedPacket(TestData.Strawberry());
            OpenAtNine(world);

            Assert.IsFalse(shop.Buy(strawberrySeeds, 1));
            Assert.AreEqual(500, world.Wallet.Money);
        }
    }
}
