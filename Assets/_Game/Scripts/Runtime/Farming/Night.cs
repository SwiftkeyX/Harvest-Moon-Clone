using Farm.Time;

namespace Farm.Farming
{
    /// <summary>The facts about a night that the farm needs: the season of the new day and whether it just started.</summary>
    public readonly struct Night
    {
        public Season Season { get; }
        public bool SeasonChanged { get; }

        public Night(Season season, bool seasonChanged)
        {
            Season = season;
            SeasonChanged = seasonChanged;
        }
    }
}
