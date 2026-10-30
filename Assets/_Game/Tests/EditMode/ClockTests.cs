using NUnit.Framework;

namespace Farm.Tests
{
    public class ClockTests
    {
        // Every day starts at 6:00 AM (360 minutes after midnight).
        [Test]
        public void Day_starts_at_six_in_the_morning()
        {
            var world = new TestWorld();

            Assert.AreEqual(360, world.Clock.MinuteOfDay);
        }

        // Time moves forward by the minutes passed.
        [Test]
        public void Clock_moves_forward()
        {
            var world = new TestWorld();

            world.Clock.Advance(150);

            Assert.AreEqual(510, world.Clock.MinuteOfDay, "8:30 AM");
        }

        // Sleeping starts the next day at 6:00 AM again.
        [Test]
        public void Sleeping_resets_the_clock_to_six()
        {
            var world = new TestWorld();
            world.Clock.Advance(600);

            world.DayCycle.EndDay();

            Assert.AreEqual(360, world.Clock.MinuteOfDay);
        }
    }
}
