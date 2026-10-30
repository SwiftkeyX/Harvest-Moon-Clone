using Farm.Items;
using NUnit.Framework;

namespace Farm.Tests
{
    public class InventoryTests
    {
        // Items of the same kind stack in one slot up to their stack limit, then start a new slot.
        [Test]
        public void Items_stack_up_to_their_limit_then_use_the_next_slot()
        {
            var inventory = new Inventory(slotCount: 3);
            var turnip = TestData.Item("turnip", maxStack: 5);

            int leftover = inventory.Add(turnip, 7);

            Assert.AreEqual(0, leftover);
            Assert.AreEqual(7, inventory.CountOf(turnip));
            Assert.AreEqual(5, inventory.Slots[0].Count);
            Assert.AreEqual(2, inventory.Slots[1].Count);
            Assert.IsNull(inventory.Slots[2], "third slot still empty");
        }

        // When the inventory is full, whatever doesn't fit is handed back.
        [Test]
        public void Adding_more_than_fits_returns_the_leftover()
        {
            var inventory = new Inventory(slotCount: 2);
            var turnip = TestData.Item("turnip", maxStack: 5);

            int leftover = inventory.Add(turnip, 12);

            Assert.AreEqual(2, leftover);
            Assert.AreEqual(10, inventory.CountOf(turnip));
        }

        // Different items never share a slot.
        [Test]
        public void Different_items_use_separate_slots()
        {
            var inventory = new Inventory(slotCount: 2);
            var turnip = TestData.Item("turnip");
            var corn = TestData.Item("corn");

            inventory.Add(turnip, 1);
            int leftover = inventory.Add(corn, 1);

            Assert.AreEqual(0, leftover);
            Assert.AreEqual(1, inventory.CountOf(turnip));
            Assert.AreEqual(1, inventory.CountOf(corn));
            Assert.AreEqual(0, inventory.Add(TestData.Item("potato"), 0), "adding nothing always fits");
        }

        // Removing takes all of the requested amount or nothing at all, and empties used-up slots.
        [Test]
        public void Remove_is_all_or_nothing()
        {
            var inventory = new Inventory(slotCount: 3);
            var turnip = TestData.Item("turnip", maxStack: 5);
            inventory.Add(turnip, 7);

            Assert.IsFalse(inventory.Remove(turnip, 8), "only 7 held");
            Assert.AreEqual(7, inventory.CountOf(turnip));

            Assert.IsTrue(inventory.Remove(turnip, 6));
            Assert.AreEqual(1, inventory.CountOf(turnip));

            Assert.IsTrue(inventory.Remove(turnip, 1));
            Assert.AreEqual(0, inventory.CountOf(turnip));
            Assert.IsNull(inventory.Slots[0]);
            Assert.IsNull(inventory.Slots[1]);
        }
    }
}
