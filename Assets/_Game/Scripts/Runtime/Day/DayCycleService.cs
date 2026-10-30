using Farm.Farming;
using Farm.Time;

namespace Farm.Day
{
    public class DayCycleService
    {
        private readonly GameCalendar calendar;
        private readonly FarmGrid grid;

        public DayCycleService(GameCalendar calendar, FarmGrid grid)
        {
            this.calendar = calendar;
            this.grid = grid;
        }

        public void EndDay()
        {
            grid.AdvanceDay();
            if (calendar.AdvanceDay())
                grid.WitherOutOfSeasonCrops(calendar.Season);
        }
    }
}
