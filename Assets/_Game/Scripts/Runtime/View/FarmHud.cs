using System.Collections.Generic;
using System.Text;
using Farm.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.View
{
    /// <summary>Fills the placeholder HUD panels. Texts are rebuilt only when what they show has changed.</summary>
    public class FarmHud : MonoBehaviour
    {
        private static readonly Color SlotColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        private static readonly Color SelectedSlotColor = new Color(0.2f, 0.5f, 0.9f, 0.95f);
        private static readonly Color InteractionColor = new Color(1f, 0.88f, 0.2f);

        [SerializeField] private FarmGame game;
        [SerializeField] private PlayerActions playerActions;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private RectTransform hotbarRow;
        [SerializeField] private TMP_Text inventoryText;
        [SerializeField] private TMP_Text messageText;

        private class Slot
        {
            public GameObject Root;
            public Image Background;
            public TMP_Text Label;
        }

        private readonly List<Slot> slots = new List<Slot>();
        private readonly StringBuilder builder = new StringBuilder();
        private int statusKey;
        private int hotbarKey;
        private int inventoryKey;
        private string shownMessage = "";
        private TMP_Text interactionText;
        private string shownInteraction = "";

        private void Awake() => interactionText = CreateInteractionText();

        private void Update()
        {
            if (game == null || game.Calendar == null || playerActions == null || playerActions.Hotbar == null) return;
            UpdateStatus();
            UpdateHotbar();
            UpdateInventory();
            UpdateMessage();
            UpdateInteraction();
        }

        private void UpdateStatus()
        {
            var calendar = game.Calendar;
            var weather = game.Weather;
            var key = new System.HashCode();
            key.Add((int)calendar.Season); key.Add(calendar.Day); key.Add(calendar.Year);
            key.Add(game.Clock.MinuteOfDay);
            key.Add((int)weather.Today); key.Add((int)weather.Tomorrow);
            key.Add(game.Stamina.Current); key.Add(game.Stamina.Max); key.Add(game.Wallet.Money);
            int hash = key.ToHashCode();
            if (hash == statusKey) return;
            statusKey = hash;

            statusText.text = $"{calendar.Season} {calendar.Day}, Year {calendar.Year}   {game.Clock.ToDisplayString()}   " +
                $"{weather.Today} (tomorrow: {weather.Tomorrow})   Stamina: {game.Stamina.Current}/{game.Stamina.Max}   Money: {game.Wallet.Money}";
        }

        private void UpdateHotbar()
        {
            var hotbar = playerActions.Hotbar;
            var entries = hotbar.Entries;
            var key = new System.HashCode();
            key.Add(hotbar.Selected); key.Add(entries.Count);
            key.Add(game.Tools.WateringCanStarted); key.Add(game.Tools.WaterLeft);
            foreach (var entry in entries)
                if (entry.Seeds != null) key.Add(game.Inventory.CountOf(entry.Seeds));
            int hash = key.ToHashCode();
            if (hash == hotbarKey) return;
            hotbarKey = hash;

            while (slots.Count < entries.Count) slots.Add(CreateSlot());
            for (int i = 0; i < slots.Count; i++)
            {
                bool used = i < entries.Count;
                slots[i].Root.SetActive(used);
                if (!used) continue;

                var entry = entries[i];
                slots[i].Label.text = entry.Seeds != null
                    ? $"{FarmLabels.Item(entry.Seeds)} x{game.Inventory.CountOf(entry.Seeds)}"
                    : ToolLabel(entry.Tool);
                slots[i].Background.color = i == hotbar.Selected ? SelectedSlotColor : SlotColor;
            }
        }

        private string ToolLabel(ToolDefinition tool)
        {
            if (tool.Type != ToolType.WateringCan) return FarmLabels.Tool(tool);
            int left = game.Tools.WateringCanStarted ? game.Tools.WaterLeft : tool.Capacity;
            return $"{FarmLabels.Tool(tool)} {left}/{tool.Capacity}";
        }

        private void UpdateInventory()
        {
            var key = new System.HashCode();
            foreach (var stack in game.Inventory.Slots)
            {
                if (stack == null) continue;
                key.Add(stack.Item.Id); key.Add(stack.Count);
            }
            int hash = key.ToHashCode();
            if (hash == inventoryKey) return;
            inventoryKey = hash;

            builder.Clear().Append("Inventory: ");
            bool any = false;
            foreach (var stack in game.Inventory.Slots)
            {
                if (stack == null) continue;
                builder.Append(FarmLabels.Item(stack.Item)).Append(" x").Append(stack.Count).Append("   ");
                any = true;
            }
            if (!any) builder.Append("empty");
            inventoryText.text = builder.ToString();
        }

        private void UpdateMessage()
        {
            string message = game.CurrentMessage;
            if (message.Length == 0 && game.SavedNoticeVisible) message = "Game saved";
            if (message == shownMessage) return;
            shownMessage = message;
            messageText.text = message;
        }

        private void UpdateInteraction()
        {
            string text = game.CurrentInteraction;
            if (text == shownInteraction) return;
            shownInteraction = text;
            interactionText.text = text;
        }

        // A large yellow line above the hotbar panel for the result of each E press.
        private TMP_Text CreateInteractionText()
        {
            var canvas = messageText.canvas.rootCanvas.transform;
            var go = new GameObject("Interaction Text", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.3f);
            rect.anchorMax = new Vector2(0.5f, 0.3f);
            rect.sizeDelta = new Vector2(1400f, 70f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = messageText.font;
            text.fontSize = 44f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = InteractionColor;
            text.raycastTarget = false;
            text.text = "";
            return text;
        }

        private Slot CreateSlot()
        {
            var root = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            root.transform.SetParent(hotbarRow, false);
            var layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = 170f;
            layout.preferredHeight = 66f;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(root.transform, false);
            var rect = (RectTransform)labelGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 4f);
            rect.offsetMax = new Vector2(-6f, -4f);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            return new Slot { Root = root, Background = root.GetComponent<Image>(), Label = label };
        }
    }
}
