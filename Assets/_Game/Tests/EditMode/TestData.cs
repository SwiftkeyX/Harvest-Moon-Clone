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
    }
}
