using Farm.Farming;
using Farm.Time;
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
