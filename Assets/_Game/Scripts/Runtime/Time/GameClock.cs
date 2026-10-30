namespace Farm.Time
{
    public class GameClock
    {
        public const int DayStartMinute = 360;
        public const int PassOutMinute = 1560; // 2:00 AM the next day

        public int MinuteOfDay { get; private set; } = DayStartMinute;

        public void Advance(int minutes) => MinuteOfDay += minutes;

        public void ResetForNewDay() => MinuteOfDay = DayStartMinute;

        /// <summary>12-hour time such as "6:00 AM"; minutes past 1440 read as the small hours.</summary>
        public string ToDisplayString()
        {
            int hour24 = MinuteOfDay / 60 % 24;
            int hour12 = hour24 % 12 == 0 ? 12 : hour24 % 12;
            return $"{hour12}:{MinuteOfDay % 60:00} {(hour24 < 12 ? "AM" : "PM")}";
        }
    }
}
