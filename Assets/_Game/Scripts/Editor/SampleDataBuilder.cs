using System.Collections.Generic;
using Farm.Core;
using Farm.Farming;
using Farm.Items;
using Farm.Time;
using UnityEditor;

namespace Farm.EditorTools
{
    /// <summary>Creates or updates the six crops with their produce and seed packets, and lists them in the game database.</summary>
    public static class SampleDataBuilder
    {
        private const string DataFolder = "Assets/_Game/Data";
        public const string DatabasePath = DataFolder + "/GameDatabase.asset";

        private struct CropSpec
        {
            public string Id, FileName;
            public Season Season;
            public int[] DaysPerStage;
            public int RegrowDays, SeedPrice, SellPrice;
        }

        private static readonly CropSpec[] Specs =
        {
            new CropSpec { Id = "turnip", FileName = "Turnip", Season = Season.Spring, DaysPerStage = new[] { 1, 1, 1, 1 }, RegrowDays = 0, SeedPrice = 20, SellPrice = 60 },
            new CropSpec { Id = "potato", FileName = "Potato", Season = Season.Spring, DaysPerStage = new[] { 2, 2, 2 }, RegrowDays = 0, SeedPrice = 30, SellPrice = 80 },
            new CropSpec { Id = "tomato", FileName = "Tomato", Season = Season.Summer, DaysPerStage = new[] { 2, 2, 3, 2 }, RegrowDays = 3, SeedPrice = 40, SellPrice = 60 },
            new CropSpec { Id = "corn", FileName = "Corn", Season = Season.Summer, DaysPerStage = new[] { 3, 3, 3, 3 }, RegrowDays = 3, SeedPrice = 50, SellPrice = 100 },
            new CropSpec { Id = "eggplant", FileName = "Eggplant", Season = Season.Fall, DaysPerStage = new[] { 2, 2, 2, 2 }, RegrowDays = 3, SeedPrice = 30, SellPrice = 80 },
            new CropSpec { Id = "sweet-potato", FileName = "SweetPotato", Season = Season.Fall, DaysPerStage = new[] { 2, 2, 2 }, RegrowDays = 0, SeedPrice = 30, SellPrice = 90 },
        };

        [MenuItem("Farm/Create Sample Data")]
        public static void Create()
        {
            var items = new List<ItemDefinition>();
            var seedPackets = new List<ItemDefinition>();
            var crops = new List<CropDefinition>();

            foreach (var spec in Specs)
            {
                var item = EditorAssetUtil.LoadOrCreate<ItemDefinition>($"{DataFolder}/Items/{spec.FileName}.asset", out _);
                item.Configure(spec.Id, spec.SellPrice);

                var crop = EditorAssetUtil.LoadOrCreate<CropDefinition>($"{DataFolder}/Crops/{spec.FileName}.asset", out _);
                crop.Configure(spec.Id, new[] { spec.Season }, spec.DaysPerStage, item, 1, spec.RegrowDays);

                var seeds = EditorAssetUtil.LoadOrCreate<SeedDefinition>($"{DataFolder}/Seeds/{spec.FileName}Seeds.asset", out _);
                seeds.Configure(spec.Id + "-seeds", crop, spec.SeedPrice);

                items.Add(item);
                seedPackets.Add(seeds);
                crops.Add(crop);
            }

            items.AddRange(seedPackets);
            var database = EditorAssetUtil.LoadOrCreate<GameDatabase>(DatabasePath, out _);
            database.SetContents(items, crops);

            AssetDatabase.SaveAssets();
        }
    }
}
