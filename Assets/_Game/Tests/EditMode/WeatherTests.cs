using Farm.Farming;
using Farm.Time;
using Farm.Weather;
using NUnit.Framework;

namespace Farm.Tests
{
    public class WeatherTests
    {
        // A rainy day waters every tilled tile, planted or not, so the player doesn't have to.
        [Test]
        public void Rainy_night_waters_every_tilled_tile()
        {
            var grid = new FarmGrid(4, 4);
            grid.Till(1, 1);
            grid.Till(2, 2);
            grid.Plant(2, 2, TestData.Turnip(), Season.Spring);

            grid.PassNight(new Night(Season.Spring, seasonChanged: false, isRaining: true, random: new FixedRandom(0.99)));

            Assert.IsTrue(grid.Get(1, 1).IsWatered);
            Assert.IsTrue(grid.Get(2, 2).IsWatered);
            Assert.IsFalse(grid.Get(0, 0).IsWatered, "grass isn't watered");
        }

        // Rain waters the new day: it doesn't count as watering for the night that just passed.
        [Test]
        public void Rain_does_not_grow_a_crop_that_was_dry_last_night()
        {
            var grid = new FarmGrid(4, 4);
            grid.Till(1, 1);
            grid.Plant(1, 1, TestData.Turnip(), Season.Spring);

            grid.PassNight(new Night(Season.Spring, seasonChanged: false, isRaining: true, random: new FixedRandom(0.99)));

            Assert.AreEqual(0, grid.Get(1, 1).Crop.Stage, "no growth from a dry night");
            Assert.IsTrue(grid.Get(1, 1).IsWatered, "but today is watered by the rain");
        }

        // Tomorrow's weather is known a day ahead and becomes today's after sleeping.
        [Test]
        public void Tomorrows_forecast_becomes_todays_weather()
        {
            var world = new TestWorld(random: new FixedRandom(0.0));
            Assume.That(world.Weather.Today, Is.EqualTo(WeatherType.Sunny), "a new game starts sunny");
            WeatherType forecast = world.Weather.Tomorrow;

            world.DayCycle.EndDay();

            Assert.AreEqual(forecast, world.Weather.Today);
        }

        // When it rains, the player wakes up to watered fields.
        [Test]
        public void Waking_up_to_rain_finds_the_fields_watered()
        {
            var world = new TestWorld(random: new FixedRandom(0.0)); // every chance happens: rain every spring day
            world.Grid.Till(1, 1);
            world.DayCycle.EndDay();   // the first forecast comes true
            world.DayCycle.EndDay();   // a rainy day for sure

            Assert.AreEqual(WeatherType.Rain, world.Weather.Today);
            Assert.IsTrue(world.Grid.Get(1, 1).IsWatered);
        }

        // It never rains in winter, however the dice fall.
        [Test]
        public void It_never_rains_in_winter()
        {
            var world = new TestWorld(day: 1, season: Season.Winter, random: new FixedRandom(0.0));

            for (int night = 0; night < 10; night++)
            {
                world.DayCycle.EndDay();
                Assert.AreEqual(WeatherType.Sunny, world.Weather.Today, $"night {night + 1}");
            }
        }

        // Each season has its own chance of rain.
        [Test]
        public void Rain_chance_depends_on_the_season()
        {
            Assert.AreEqual(0.25, WeatherSystem.RainChance(Season.Spring), 1e-9);
            Assert.AreEqual(0.20, WeatherSystem.RainChance(Season.Summer), 1e-9);
            Assert.AreEqual(0.25, WeatherSystem.RainChance(Season.Fall), 1e-9);
            Assert.AreEqual(0.0, WeatherSystem.RainChance(Season.Winter), 1e-9);
        }

        // Weeds can sprout overnight on empty grass, never on tilled soil or debris.
        [Test]
        public void Weeds_sprout_only_on_grass()
        {
            var grid = new FarmGrid(3, 1);
            grid.Till(1, 0);
            grid.Place(2, 0, TileState.Rock);

            grid.PassNight(new Night(Season.Spring, seasonChanged: false, isRaining: false, random: new FixedRandom(0.0)));

            Assert.AreEqual(TileState.Weed, grid.Get(0, 0).State, "grass sprouted a weed");
            Assert.AreEqual(TileState.Tilled, grid.Get(1, 0).State);
            Assert.AreEqual(TileState.Rock, grid.Get(2, 0).State);
        }

        // Weeds are rare: a roll above the 1% chance leaves the grass alone.
        [Test]
        public void Weeds_do_not_sprout_when_the_roll_misses()
        {
            var grid = new FarmGrid(3, 3);

            grid.PassNight(new Night(Season.Spring, seasonChanged: false, isRaining: false, random: new FixedRandom(0.02)));

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    Assert.AreEqual(TileState.Grass, grid.Get(x, y).State);
        }
    }
}
