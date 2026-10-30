using UnityEngine;

namespace Farm.Core
{
    public static class SaveSerializer
    {
        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        public static SaveData FromJson(string json) => JsonUtility.FromJson<SaveData>(json);
    }
}
