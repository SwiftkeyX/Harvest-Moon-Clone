namespace Farm.Farming
{
    public class CropInstance
    {
        private bool regrowing;
        private int regrowNights;

        public CropDefinition Definition { get; }
        public int Stage { get; private set; }
        public int DaysInStage { get; private set; }

        public CropInstance(CropDefinition definition)
        {
            Definition = definition;
        }

        public bool IsMature => regrowing
            ? regrowNights >= Definition.RegrowDays
            : Stage >= Definition.DaysPerStage.Length;

        /// <summary>How far the crop is to being mature, 0 to 1 (1 only when mature).</summary>
        public float GrowthProgress
        {
            get
            {
                if (IsMature) return 1f;
                return regrowing
                    ? (float)regrowNights / Definition.RegrowDays
                    : (float)Stage / Definition.DaysPerStage.Length;
            }
        }

        public bool RegrowsAfterHarvest => Definition.RegrowDays > 0;

        public void GrowOneDay()
        {
            if (IsMature) return;
            if (regrowing)
            {
                regrowNights++;
                return;
            }
            DaysInStage++;
            if (DaysInStage >= Definition.DaysPerStage[Stage])
            {
                Stage++;
                DaysInStage = 0;
            }
        }

        /// <summary>Starts the regrow countdown after the crop was picked.</summary>
        internal void StartRegrow()
        {
            regrowing = true;
            regrowNights = 0;
        }
    }
}
