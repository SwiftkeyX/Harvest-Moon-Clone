using NUnit.Framework;

namespace Farm.Tests
{
    public class TimeTests
    {
        private const int MinutesFromSixAmToTwoAm = 20 * 60;

        // Time passing during the day doesn't end it.
        [Test]
        public void Time_before_two_am_keeps_the_day_going()
        {
            var world = new TestWorld();

            bool passedOut = world.DayCycle.PassTime(MinutesFromSixAmToTwoAm - 1);

            Assert.IsFalse(passedOut);
            Assert.AreEqual(1, world.Calendar.Day);
            Assert.AreEqual("1:59 AM", world.Clock.ToDisplayString());
        }

        // Staying up until 2:00 AM makes the player pass out: the day ends, they wake at 6:00 AM
        // with half stamina.
        [Test]
        public void Staying_up_until_two_am_passes_out()
        {
            var world = new TestWorld();

            bool passedOut = world.DayCycle.PassTime(MinutesFromSixAmToTwoAm);

            Assert.IsTrue(passedOut);
            Assert.AreEqual(2, world.Calendar.Day);
            Assert.AreEqual(GameClockStart, world.Clock.MinuteOfDay);
            Assert.AreEqual(50, world.Stamina.Current, "half of 100");
        }

        // However much time is skipped at once, only one day ends.
        [Test]
        public void Skipping_far_past_two_am_ends_only_one_day()
        {
            var world = new TestWorld();

            world.DayCycle.PassTime(MinutesFromSixAmToTwoAm * 3);

            Assert.AreEqual(2, world.Calendar.Day);
            Assert.AreEqual(GameClockStart, world.Clock.MinuteOfDay);
        }

        // Passing out at 2 AM still sells what's in the shipping bin.
        [Test]
        public void Passing_out_at_two_am_still_sells_the_bin()
        {
            var world = new TestWorld();
            var turnip = TestData.Item("turnip", sellPrice: 60);
            world.Inventory.Add(turnip, 1);
            world.Bin.Ship(world.Inventory, turnip, 1);

            world.DayCycle.PassTime(MinutesFromSixAmToTwoAm);

            Assert.AreEqual(560, world.Wallet.Money);
        }

        // The clock reads like a normal 12-hour clock, past midnight included.
        [TestCase(0, "6:00 AM")]
        [TestCase(330, "11:30 AM")]
        [TestCase(360, "12:00 PM")]
        [TestCase(465, "1:45 PM")]
        [TestCase(1080, "12:00 AM")]
        [TestCase(1170, "1:30 AM")]
        public void Clock_reads_as_twelve_hour_time(int minutesAfterSixAm, string expected)
        {
            var world = new TestWorld();

            world.Clock.Advance(minutesAfterSixAm);

            Assert.AreEqual(expected, world.Clock.ToDisplayString());
        }

        private static int GameClockStart => Farm.Time.GameClock.DayStartMinute;
    }
}
