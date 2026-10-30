namespace Farm.Time
{
    public class GameClock
    {
        public const int DayStartMinute = 360;

        public int MinuteOfDay { get; private set; } = DayStartMinute;

        public void Advance(int minutes) => MinuteOfDay += minutes;

        public void ResetForNewDay() => MinuteOfDay = DayStartMinute;
    }
}
