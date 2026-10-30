using Farm.Time;

namespace Farm.Core
{
    /// <summary>What a new game starts with.</summary>
    public class GameSetup
    {
        public int Day = 1;
        public Season Season = Season.Spring;
        public int Year = 1;
        public int GridWidth = 8;
        public int GridHeight = 8;
        public int MaxStamina = 100;
        public int InventorySlots = 12;
        public int StartingMoney = 500;
        /// <summary>Dice for weather and weeds. Null means a new time-seeded random.</summary>
        public System.Random Random;
    }
}
