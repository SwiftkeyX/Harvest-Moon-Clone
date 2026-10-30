using Farm.Farming;
using UnityEngine;

namespace Farm.Tools
{
    [CreateAssetMenu(menuName = "Farm/Tool")]
    public class ToolDefinition : ScriptableObject
    {
        [SerializeField] private ToolType type;
        [SerializeField] private int staminaCost;
        [SerializeField] private int capacity;
        [SerializeField] private CropDefinition crop;

        public ToolType Type => type;
        public int StaminaCost => staminaCost;
        public int Capacity => capacity;
        public CropDefinition Crop => crop;

        public void Configure(ToolType type, int staminaCost, int capacity = 0, CropDefinition crop = null)
        {
            this.type = type;
            this.staminaCost = staminaCost;
            this.capacity = capacity;
            this.crop = crop;
        }
    }
}
