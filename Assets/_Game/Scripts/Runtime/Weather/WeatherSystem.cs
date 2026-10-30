using System;
using Farm.Time;

namespace Farm.Weather
{
    public class WeatherSystem
    {
        private readonly Random random;

        public WeatherType Today { get; private set; } = WeatherType.Sunny;
        /// <summary>The forecast. The first one is always sunny; every night rolls the next.</summary>
        public WeatherType Tomorrow { get; private set; } = WeatherType.Sunny;

        public WeatherSystem(Random random)
        {
            this.random = random;
        }

        public static double RainChance(Season season)
        {
            switch (season)
            {
                case Season.Spring: return 0.25;
                case Season.Summer: return 0.20;
                case Season.Fall: return 0.25;
                default: return 0.0;
            }
        }

        /// <summary>A night passes: the forecast comes true and a new one is rolled for the new day's season.</summary>
        public void Advance(Season season)
        {
            Today = Tomorrow;
            Tomorrow = random.NextDouble() < RainChance(season) ? WeatherType.Rain : WeatherType.Sunny;
        }
    }
}
