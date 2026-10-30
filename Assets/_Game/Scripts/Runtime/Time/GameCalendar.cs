namespace Farm.Time
{
    public class GameCalendar
    {
        public const int DaysPerSeason = 30;

        public int Day { get; private set; }
        public Season Season { get; private set; }
        public int Year { get; private set; }

        public GameCalendar(int day = 1, Season season = Season.Spring, int year = 1)
        {
            Day = day;
            Season = season;
            Year = year;
        }

        public bool AdvanceDay()
        {
            Day++;
            if (Day <= DaysPerSeason) return false;

            Day = 1;
            if (Season == Season.Winter)
            {
                Season = Season.Spring;
                Year++;
            }
            else
            {
                Season++;
            }
            return true;
        }
    }
}
