using Farm.Economy;
using Farm.Farming;
using Farm.Player;
using Farm.Time;

namespace Farm.Day
{
    public class DayCycleService
    {
        private readonly GameCalendar calendar;
        private readonly FarmGrid grid;
        private readonly Stamina stamina;
        private readonly ShippingBin bin;
        private readonly Wallet wallet;

        public DayCycleService(GameCalendar calendar, FarmGrid grid, Stamina stamina, ShippingBin bin, Wallet wallet)
        {
            this.calendar = calendar;
            this.grid = grid;
            this.stamina = stamina;
            this.bin = bin;
            this.wallet = wallet;
        }

        public void EndDay(bool passedOut = false)
        {
            bin.SellAll(wallet);
            bool changed = calendar.AdvanceDay();
            grid.PassNight(new Night(calendar.Season, changed));
            stamina.RestoreForNewDay(passedOut);
        }
    }
}
