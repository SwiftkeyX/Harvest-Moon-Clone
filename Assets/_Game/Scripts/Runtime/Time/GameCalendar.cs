namespace Farm.Time
{
    public class GameCalendar
    {
        public int Day { get; private set; } = 1;
        public Season Season { get; private set; } = Season.Spring;
        public int Year { get; private set; } = 1;

        public void AdvanceDay()
        {
            Day++;
        }
    }
}
