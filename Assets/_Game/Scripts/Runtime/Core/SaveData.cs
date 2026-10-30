using System;

namespace Farm.Core
{
    /// <summary>Everything a save file holds. Public fields only, so JsonUtility can read and write it.</summary>
    [Serializable]
    public class SaveData
    {
        public int day;
        public int season;
        public int year;
        public int clockMinute;
        public int weatherToday;
        public int weatherTomorrow;

        public int gridWidth;
        public int gridHeight;
        public TileSave[] tiles = new TileSave[0];   // x-major: index = x * gridHeight + y

        public int staminaMax;
        public int staminaCurrent;
        public int money;

        public ItemSlotSave[] inventory = new ItemSlotSave[0];
        public ItemSlotSave[] bin = new ItemSlotSave[0];

        public bool canStarted;
        public int waterLeft;

        /// <summary>Seed for the next session's random numbers (the random itself can't be saved).</summary>
        public int randomSeed;

        [Serializable]
        public class TileSave
        {
            public int state;
            public bool watered;
            public string crop = "";
            public int stage;
            public int daysInStage;
            public bool regrowing;
            public int regrowNights;
        }

        [Serializable]
        public class ItemSlotSave
        {
            public string item = "";
            public int count;
        }
    }
}
