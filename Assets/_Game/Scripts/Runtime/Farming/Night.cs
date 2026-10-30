using System;
using Farm.Time;

namespace Farm.Farming
{
    /// <summary>The facts about a night that the farm needs: the season of the new day, whether it just started, whether it rains and the dice for chance events (none means no weeds).</summary>
    public readonly struct Night
    {
        public Season Season { get; }
        public bool SeasonChanged { get; }
        public bool IsRaining { get; }
        public Random Random { get; }

        public Night(Season season, bool seasonChanged, bool isRaining = false, Random random = null)
        {
            Season = season;
            SeasonChanged = seasonChanged;
            IsRaining = isRaining;
            Random = random;
        }
    }
}
