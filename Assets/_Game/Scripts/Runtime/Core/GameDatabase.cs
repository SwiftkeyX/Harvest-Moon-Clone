using System.Collections.Generic;
using System.Linq;
using Farm.Farming;
using Farm.Items;
using UnityEngine;

namespace Farm.Core
{
    /// <summary>Every item and crop by id, so a save file can find its definitions again.</summary>
    [CreateAssetMenu(menuName = "Farm/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
        [SerializeField] private List<CropDefinition> crops = new List<CropDefinition>();

        public void SetContents(IEnumerable<ItemDefinition> items, IEnumerable<CropDefinition> crops)
        {
            this.items = new List<ItemDefinition>(items);
            this.crops = new List<CropDefinition>(crops);
        }

        /// <summary>Every seed packet, in database order.</summary>
        public IEnumerable<SeedDefinition> Seeds => items.OfType<SeedDefinition>();

        /// <summary>The item with this id, or null if unknown.</summary>
        public ItemDefinition GetItem(string id) => items.Find(i => i != null && i.Id == id);

        /// <summary>The crop with this id, or null if unknown.</summary>
        public CropDefinition GetCrop(string id) => crops.Find(c => c != null && c.Id == id);
    }
}
