using Farm.Farming;
using Farm.Time;
using NUnit.Framework;

namespace Farm.Tests
{
    public class SeasonTests
    {
        // A season is 30 days: sleeping on Spring 30 wakes you on Summer 1 of the same year.
        [Test]
        public void Sleeping_on_day_30_starts_the_next_season()
        {
            var world = new TestWorld(day: 30, season: Season.Spring);

            world.DayCycle.EndDay();

            Assert.AreEqual(1, world.Calendar.Day);
            Assert.AreEqual(Season.Summer, world.Calendar.Season);
            Assert.AreEqual(1, world.Calendar.Year);
        }

        // Winter is the last season: after Winter 30 comes Spring 1 of the next year.
        [Test]
        public void Sleeping_on_winter_30_starts_spring_of_the_next_year()
        {
            var world = new TestWorld(day: 30, season: Season.Winter, year: 1);

            world.DayCycle.EndDay();

            Assert.AreEqual(1, world.Calendar.Day);
            Assert.AreEqual(Season.Spring, world.Calendar.Season);
            Assert.AreEqual(2, world.Calendar.Year);
        }

        // When the season changes, a crop that can't grow in the new season dies: the crop is
        // removed and the tile shows a withered plant.
        [Test]
        public void Spring_crop_withers_when_summer_starts()
        {
            var world = new TestWorld(day: 30, season: Season.Spring);
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(), world.Calendar.Season);

            world.WaterAndSleep(1, 1);

            Assert.IsNull(world.Grid.Get(1, 1).Crop, "turnip is gone");
            Assert.AreEqual(TileState.Withered, world.Grid.Get(1, 1).State);
        }

        // A crop that grows in both seasons keeps growing across the change.
        [Test]
        public void Crop_that_grows_in_spring_and_summer_survives_the_change()
        {
            var world = new TestWorld(day: 30, season: Season.Spring);
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Strawberry(), world.Calendar.Season);

            world.WaterAndSleep(1, 1);

            Assert.IsNotNull(world.Grid.Get(1, 1).Crop, "strawberry is still planted");
            Assert.AreEqual(TileState.Tilled, world.Grid.Get(1, 1).State);
        }

        // Crops only die at a season change, not on ordinary nights mid-season.
        [Test]
        public void Spring_crop_does_not_wither_on_an_ordinary_night()
        {
            var world = new TestWorld(day: 15, season: Season.Spring);
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(), world.Calendar.Season);

            world.WaterAndSleep(1, 1);

            Assert.IsNotNull(world.Grid.Get(1, 1).Crop);
            Assert.AreEqual(TileState.Tilled, world.Grid.Get(1, 1).State);
        }

        // Seeds can only be planted in a season their crop grows in.
        [Test]
        public void Summer_crop_cannot_be_planted_in_spring()
        {
            var world = new TestWorld(day: 1, season: Season.Spring);
            world.Grid.Till(1, 1);

            Assert.IsFalse(world.Grid.Plant(1, 1, TestData.Tomato(), world.Calendar.Season));
            Assert.IsNull(world.Grid.Get(1, 1).Crop);
        }

        // A withered tile has to be cleared before anything can be planted there again.
        [Test]
        public void Withered_tile_cannot_be_planted_on()
        {
            var world = new TestWorld(day: 30, season: Season.Spring);
            world.Grid.Till(1, 1);
            world.Grid.Plant(1, 1, TestData.Turnip(), world.Calendar.Season);
            world.WaterAndSleep(1, 1);

            Assert.IsFalse(world.Grid.Plant(1, 1, TestData.Strawberry(), world.Calendar.Season));
        }
    }
}
