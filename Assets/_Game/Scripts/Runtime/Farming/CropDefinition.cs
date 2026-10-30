using System;
using Farm.Items;
using Farm.Time;
using UnityEngine;

namespace Farm.Farming
{
    [CreateAssetMenu(menuName = "Farm/Crop")]
    public class CropDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private Season[] seasons = Array.Empty<Season>();
        [SerializeField] private int[] daysPerStage = Array.Empty<int>();
        [SerializeField] private ItemDefinition harvestItem;
        [SerializeField] private int harvestYield = 1;
        [SerializeField] private int regrowDays;

        public string Id => id;
        public Season[] Seasons => seasons;
        public int[] DaysPerStage => daysPerStage;
        public ItemDefinition HarvestItem => harvestItem;
        public int HarvestYield => harvestYield;
        public int RegrowDays => regrowDays;

        public void Configure(string id, Season[] seasons, int[] daysPerStage,
            ItemDefinition harvestItem = null, int harvestYield = 1, int regrowDays = 0)
        {
            this.id = id;
            this.seasons = seasons;
            this.daysPerStage = daysPerStage;
            this.harvestItem = harvestItem;
            this.harvestYield = harvestYield;
            this.regrowDays = regrowDays;
        }

        public bool GrowsIn(Season season) => Array.IndexOf(seasons, season) >= 0;
    }
}
