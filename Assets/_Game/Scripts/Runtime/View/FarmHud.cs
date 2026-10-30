using System.Text;
using Farm.Items;
using Farm.Tools;
using UnityEngine;

namespace Farm.View
{
    /// <summary>Placeholder text overlay.</summary>
    public class FarmHud : MonoBehaviour
    {
        [SerializeField] private FarmGame game;
        [SerializeField] private PlayerActions playerActions;

        private void OnGUI()
        {
            if (game == null || game.Calendar == null || game.Clock == null || game.Stamina == null
                || game.Wallet == null || playerActions == null || playerActions.Hotbar == null) return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            style.normal.textColor = Color.white;
            var calendar = game.Calendar;

            GUI.Label(new Rect(10, 10, 1400, 30),
                $"{calendar.Season} {calendar.Day}, Year {calendar.Year}  {game.Clock.ToDisplayString()}   Stamina: {game.Stamina.Current}/{game.Stamina.Max}   Money: {game.Wallet.Money}   {game.Weather.Today} (tomorrow: {game.Weather.Tomorrow})", style);
            GUI.Label(new Rect(10, 40, 1400, 30), HotbarText(), style);
            GUI.Label(new Rect(10, 70, 1400, 30), InventoryText(), style);
            GUI.Label(new Rect(10, 100, 1400, 30), "WASD move  Space/Click use  1/2 select  E interact  N sleep", style);
            string message = game.CurrentMessage;
            if (message.Length > 0) GUI.Label(new Rect(10, 130, 1400, 30), message, style);
            if (game.SavedNoticeVisible) GUI.Label(new Rect(10, 160, 1400, 30), "Game saved", style);
        }

        private string HotbarText()
        {
            var hotbar = playerActions.Hotbar;
            var sb = new StringBuilder();
            for (int i = 0; i < hotbar.Entries.Count; i++)
            {
                var entry = hotbar.Entries[i];
                string label = entry.Seeds != null
                    ? $"{FarmLabels.Item(entry.Seeds)} x{game.Inventory.CountOf(entry.Seeds)}"
                    : ToolLabel(entry.Tool);
                sb.Append(i == hotbar.Selected ? $"[{label}]" : label).Append("  ");
            }
            return sb.ToString();
        }

        private string ToolLabel(ToolDefinition tool)
        {
            if (tool.Type != ToolType.WateringCan) return FarmLabels.Tool(tool);
            int left = game.Tools.WateringCanStarted ? game.Tools.WaterLeft : tool.Capacity;
            return $"{FarmLabels.Tool(tool)} {left}/{tool.Capacity}";
        }

        private string InventoryText()
        {
            var sb = new StringBuilder("Inventory: ");
            bool any = false;
            foreach (var stack in game.Inventory.Slots)
            {
                if (stack == null) continue;
                sb.Append($"{FarmLabels.Item(stack.Item)} x{stack.Count}  ");
                any = true;
            }
            if (!any) sb.Append("empty");
            return sb.ToString();
        }
    }
}
