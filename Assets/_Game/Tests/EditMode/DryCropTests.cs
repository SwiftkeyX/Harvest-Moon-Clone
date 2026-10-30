using NUnit.Framework;

namespace Farm.Tests
{
    public class DryCropTests
    {
        // A crop that is never watered stays planted but doesn't grow. It must not die from
        // drought: once watering starts, it still needs the full 4 watered nights.
        [Test]
        public void Unwatered_turnip_pauses_growth_and_does_not_die()
        {
            var world = new TestWorld();
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(), world.Calendar.Season);

            for (int night = 1; night <= 5; night++)
                world.DayCycle.EndDay();

            Assert.IsNotNull(world.Grid.Get(1, 1).Crop, "crop survives 5 dry nights");
            Assert.IsFalse(world.Grid.Get(1, 1).Crop.IsMature, "no growth without water");

            for (int night = 1; night <= 3; night++)
                world.WaterAndSleep(1, 1);
            Assert.IsFalse(world.Grid.Get(1, 1).Crop.IsMature, "dry nights did not count towards growth");

            world.WaterAndSleep(1, 1);
            Assert.IsTrue(world.Grid.Get(1, 1).Crop.IsMature, "ready after 4 watered nights");
        }

        // Watering lasts one night only: the next morning the tile is dry again.
        [Test]
        public void Watered_tile_is_dry_again_the_next_morning()
        {
            var world = new TestWorld();
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(), world.Calendar.Season);

            world.WaterAndSleep(1, 1);

            Assert.IsFalse(world.Grid.Get(1, 1).IsWatered);
        }

        // Skipped days only pause growth: watering on nights 1, 3, 5 and 7 (4 watered nights)
        // makes the turnip ready, and the dry nights in between change nothing.
        [Test]
        public void Watering_every_other_day_still_finishes_after_four_watered_nights()
        {
            var world = new TestWorld();
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(), world.Calendar.Season);

            for (int i = 1; i <= 3; i++)
            {
                world.WaterAndSleep(1, 1);
                world.DayCycle.EndDay();
            }
            Assert.IsFalse(world.Grid.Get(1, 1).Crop.IsMature, "3 watered nights is not enough");

            world.WaterAndSleep(1, 1);
            Assert.IsTrue(world.Grid.Get(1, 1).Crop.IsMature, "4th watered night finishes it");
        }
    }
}
