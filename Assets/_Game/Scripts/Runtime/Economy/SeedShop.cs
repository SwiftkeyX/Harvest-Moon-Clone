using System.Collections.Generic;
using System.Linq;
using Farm.Items;
using Farm.Time;

namespace Farm.Economy
{
    public class SeedShop
    {
        public const int OpensAtMinute = 540;
        public const int ClosesAtMinute = 1020;

        private readonly List<SeedDefinition> stock;
        private readonly Wallet wallet;
        private readonly Inventory inventory;
        private readonly GameClock clock;
        private readonly GameCalendar calendar;

        public SeedShop(IEnumerable<SeedDefinition> stock, Wallet wallet, Inventory inventory, GameClock clock, GameCalendar calendar)
        {
            this.stock = stock.ToList();
            this.wallet = wallet;
            this.inventory = inventory;
            this.clock = clock;
            this.calendar = calendar;
        }

        public bool IsOpen => clock.MinuteOfDay >= OpensAtMinute && clock.MinuteOfDay < ClosesAtMinute;

        /// <summary>The stocked seeds whose crop grows in the current season.</summary>
        public IEnumerable<SeedDefinition> AvailableNow() => stock.Where(s => s.Crop.GrowsIn(calendar.Season));

        /// <summary>Buys the packets if the shop is open, sells them now, and you have the money and room. Otherwise nothing changes.</summary>
        public bool Buy(SeedDefinition seeds, int count)
        {
            if (count <= 0 || !IsOpen || !AvailableNow().Contains(seeds)) return false;
            if (!inventory.CanAdd(seeds, count)) return false;
            if (!wallet.TrySpend(seeds.BuyPrice * count)) return false;
            inventory.Add(seeds, count);
            return true;
        }
    }
}
