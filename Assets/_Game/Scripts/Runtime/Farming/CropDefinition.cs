using System;
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

        public string Id => id;
        public Season[] Seasons => seasons;
        public int[] DaysPerStage => daysPerStage;

        public void Configure(string id, Season[] seasons, int[] daysPerStage)
        {
            this.id = id;
            this.seasons = seasons;
            this.daysPerStage = daysPerStage;
        }

        public bool GrowsIn(Season season) => Array.IndexOf(seasons, season) >= 0;
    }
}
