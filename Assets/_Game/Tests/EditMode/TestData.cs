using Farm.Farming;
using Farm.Time;
using Farm.Tools;
using UnityEngine;

namespace Farm.Tests
{
    /// <summary>Creates throwaway ScriptableObject data for tests.</summary>
    public static class TestData
    {
        /// <summary>Spring crop, 4 stages of 1 day: ready after 4 watered nights.</summary>
        public static CropDefinition Turnip()
        {
            var crop = ScriptableObject.CreateInstance<CropDefinition>();
            crop.Configure("turnip", new[] { Season.Spring }, new[] { 1, 1, 1, 1 });
            return crop;
        }

        /// <summary>A tool of the given type that costs the given stamina per swing.</summary>
        public static ToolDefinition Tool(ToolType type, int staminaCost = 2)
        {
            var tool = ScriptableObject.CreateInstance<ToolDefinition>();
            tool.Configure(type, staminaCost);
            return tool;
        }

        /// <summary>Watering can holding <paramref name="capacity"/> uses of water.</summary>
        public static ToolDefinition WateringCan(int capacity = 30, int staminaCost = 1)
        {
            var tool = ScriptableObject.CreateInstance<ToolDefinition>();
            tool.Configure(ToolType.WateringCan, staminaCost, capacity: capacity);
            return tool;
        }

        /// <summary>Seed bag that plants <paramref name="crop"/> in the 3×3 square around the target tile.</summary>
        public static ToolDefinition SeedBag(CropDefinition crop, int staminaCost = 2)
        {
            var tool = ScriptableObject.CreateInstance<ToolDefinition>();
            tool.Configure(ToolType.SeedBag, staminaCost, crop: crop);
            return tool;
        }

        /// <summary>Summer-only crop, 4 stages of 2 days.</summary>
        public static CropDefinition Tomato()
        {
            var crop = ScriptableObject.CreateInstance<CropDefinition>();
            crop.Configure("tomato", new[] { Season.Summer }, new[] { 2, 2, 2, 2 });
            return crop;
        }

        /// <summary>Grows in both Spring and Summer, 3 stages of 2 days.</summary>
        public static CropDefinition Strawberry()
        {
            var crop = ScriptableObject.CreateInstance<CropDefinition>();
            crop.Configure("strawberry", new[] { Season.Spring, Season.Summer }, new[] { 2, 2, 2 });
            return crop;
        }
    }
}
