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
            bool changed = calendar.AdvanceDay();
            grid.PassNight(new Night(calendar.Season, changed));
        }
    }
}
