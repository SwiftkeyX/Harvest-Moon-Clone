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

        public DayCycleService(GameCalendar calendar, FarmGrid grid, Stamina stamina)
        {
            this.calendar = calendar;
            this.grid = grid;
            this.stamina = stamina;
        }

        public void EndDay(bool passedOut = false)
        {
            bool changed = calendar.AdvanceDay();
            grid.PassNight(new Night(calendar.Season, changed));
            stamina.RestoreForNewDay(passedOut);
        }
    }
}
