namespace Farm.Farming
{
    public class CropInstance
    {
        public CropDefinition Definition { get; }
        public int Stage { get; private set; }
        public int DaysInStage { get; private set; }

        public CropInstance(CropDefinition definition)
        {
            Definition = definition;
        }

        public bool IsMature => Stage >= Definition.DaysPerStage.Length;

        public void GrowOneDay()
        {
            if (IsMature) return;
            DaysInStage++;
            if (DaysInStage >= Definition.DaysPerStage[Stage])
            {
                Stage++;
                DaysInStage = 0;
            }
        }
    }
}
