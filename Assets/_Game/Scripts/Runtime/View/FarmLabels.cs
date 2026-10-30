using Farm.Items;
using Farm.Tools;

namespace Farm.View
{
    /// <summary>Short display names for the placeholder UI.</summary>
    public static class FarmLabels
    {
        /// <summary>"turnip-seeds" becomes "Turnip seeds".</summary>
        public static string Item(ItemDefinition item)
        {
            string text = item.Id.Replace('-', ' ');
            return text.Length == 0 ? text : char.ToUpper(text[0]) + text.Substring(1);
        }

        public static string Tool(ToolDefinition tool) =>
            tool.Type == ToolType.WateringCan ? "Can" : tool.Type.ToString();
    }
}
