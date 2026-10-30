using Farm.Economy;
using Farm.Farming;
using Farm.Player;
using Farm.Time;

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

        public DayCycleService(GameCalendar calendar, GameClock clock, FarmGrid grid, Stamina stamina, ShippingBin bin, Wallet wallet)
        {
            this.calendar = calendar;
            this.clock = clock;
            this.grid = grid;
            this.stamina = stamina;
            this.bin = bin;
            this.wallet = wallet;
        }

        public void EndDay(bool passedOut = false)
        {
            bin.SellAll(wallet);
            bool changed = calendar.AdvanceDay();
            clock.ResetForNewDay();
            grid.PassNight(new Night(calendar.Season, changed));
            stamina.RestoreForNewDay(passedOut);
        }
    }
}
