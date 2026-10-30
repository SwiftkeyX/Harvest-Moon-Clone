using Farm.Time;
using NUnit.Framework;

namespace Farm.Tests
{
    public class CropGrowthTests
    {
        // It creates a new world and checks that the calendar reads Day 1, Spring, Year 1. \
        // That catches a game that starts on the wrong date.
        [Test]
        public void New_game_starts_on_spring_1_of_year_1()
        {
            var world = new TestWorld();

            Assert.AreEqual(1, world.Calendar.Day);
            Assert.AreEqual(Season.Spring, world.Calendar.Season);
            Assert.AreEqual(1, world.Calendar.Year);
        }

        // It ends the day once (as if the player slept) and checks that it's now Day 2, still Spring. 
        // That catches sleeping not advancing time.
        [Test]
        public void Ending_the_day_moves_the_calendar_to_the_next_day()
        {
            var world = new TestWorld();

            world.DayCycle.EndDay();

            Assert.AreEqual(2, world.Calendar.Day);
            Assert.AreEqual(Season.Spring, world.Calendar.Season);
        }

        // This is the main farming rule, played out like a player would:
        // 1. Till a grass tile at position (2, 3), and check that it worked.
        // 2. Plant a turnip there, and check that it worked. The turnip is a Spring crop that needs 4 watered nights.
        // 3. For nights 1, 2 and 3: water the tile, sleep, and check that the turnip is not ready yet.
        // 4. Night 4: water, sleep, and check that the turnip is ready.
        [Test]
        public void Watered_turnip_is_ready_to_harvest_after_four_nights()
        {
            var world = new TestWorld();
            var turnip = TestData.Turnip();

            Assert.IsTrue(world.Grid.Till(2, 3), "grass can be tilled");
            Assert.IsTrue(world.Grid.Plant(2, 3, turnip, world.Calendar.Season), "spring crop plants on tilled soil in spring");

            for (int night = 1; night <= 3; night++)
            {
                world.WaterAndSleep(2, 3);
                Assert.IsFalse(world.Grid.Get(2, 3).Crop.IsMature, $"not ready after night {night}");
            }

            world.WaterAndSleep(2, 3);

            Assert.IsTrue(world.Grid.Get(2, 3).Crop.IsMature, "ready after night 4");
        }
    }
}
