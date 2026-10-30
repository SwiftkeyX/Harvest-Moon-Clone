using System;
using Farm.Economy;
using Farm.Farming;
using Farm.Player;
using Farm.Time;
using Farm.Weather;

namespace Farm.Day
{
    public class DayCycleService
    {
        private readonly GameCalendar calendar;
        private readonly GameClock clock;
        private readonly FarmGrid grid;
        private readonly Stamina stamina;
        private readonly ShippingBin bin;
        private readonly Wallet wallet;
        private readonly WeatherSystem weather;
        private readonly Random random;

        /// <summary>Raised at the end of every night: sleeping, passing out from exhaustion or at 2 AM.</summary>
        public event Action DayEnded;

        public DayCycleService(GameCalendar calendar, GameClock clock, FarmGrid grid, Stamina stamina,
            ShippingBin bin, Wallet wallet, WeatherSystem weather, Random random)
        {
            this.calendar = calendar;
            this.clock = clock;
            this.grid = grid;
            this.stamina = stamina;
            this.bin = bin;
            this.wallet = wallet;
            this.weather = weather;
            this.random = random;
        }

        /// <summary>Moves the clock on. Reaching 2:00 AM passes the player out (one day at most). Returns true if that happened.</summary>
        public bool PassTime(int minutes)
        {
            clock.Advance(minutes);
            if (clock.MinuteOfDay < GameClock.PassOutMinute) return false;
            EndDay(passedOut: true);
            return true;
        }

        public void EndDay(bool passedOut = false)
        {
            bin.SellAll(wallet);
            bool changed = calendar.AdvanceDay();
            clock.ResetForNewDay();
            weather.Advance(calendar.Season);
            grid.PassNight(new Night(calendar.Season, changed, weather.Today == WeatherType.Rain, random));
            stamina.RestoreForNewDay(passedOut);
            DayEnded?.Invoke();
        }
    }
}
