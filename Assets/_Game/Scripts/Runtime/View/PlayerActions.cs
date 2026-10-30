using Farm.Farming;
using Farm.Items;
using Farm.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>Turns the Attack, Interact, Previous and Next actions into tool use, shipping, sleeping and hotbar changes.</summary>
    public class PlayerActions : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private FarmGame game;
        [SerializeField] private PlayerController player;

        private InputAction attack;
        private InputAction interact;
        private InputAction previous;
        private InputAction next;

        public Hotbar Hotbar { get; private set; }

        private void Awake()
        {
            attack = actions.FindAction("Player/Attack");
            interact = actions.FindAction("Player/Interact");
            previous = actions.FindAction("Player/Previous");
            next = actions.FindAction("Player/Next");
        }

        private void Start()
        {
            Hotbar = new Hotbar(game.ToolDefinitions);
            Hotbar.Refresh(game.Inventory);
        }

        private void OnEnable()
        {
            attack.performed += OnAttack;
            interact.performed += OnInteract;
            previous.performed += OnPrevious;
            next.performed += OnNext;
            game.GameReset += Refresh;
        }

        private void OnDisable()
        {
            attack.performed -= OnAttack;
            interact.performed -= OnInteract;
            previous.performed -= OnPrevious;
            next.performed -= OnNext;
            game.GameReset -= Refresh;
        }

        private void Update()
        {
            // Debug shortcut: sleep anywhere.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.nKey.wasPressedThisFrame) Sleep();
        }

        private void OnPrevious(InputAction.CallbackContext ctx) => Hotbar.Previous();

        private void OnNext(InputAction.CallbackContext ctx) => Hotbar.Next();

        private void OnAttack(InputAction.CallbackContext ctx)
        {
            Vector2Int target = player.TargetCell;
            var entry = Hotbar.Current;
            ToolResult result;
            string success;
            string failure;

            if (entry.Seeds != null)
            {
                if (game.Inventory.CountOf(entry.Seeds) < 1) { game.Say("No seeds"); return; }
                result = game.Tools.Sow(entry.Seeds, target.x, target.y);
                success = "Sowed " + FarmLabels.Item(entry.Seeds).ToLower();
                failure = "Can't plant here";
            }
            else
            {
                var tool = entry.Tool;
                bool refill = tool.Type == ToolType.WateringCan && game.Grid.Get(target.x, target.y)?.State == TileState.Water;
                result = game.Tools.Use(tool, target.x, target.y);
                (success, failure) = Messages(tool, refill);
            }

            game.Say(result.PassedOut ? "Passed out!" : result.Success ? success : failure);
            Refresh();
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            Vector2Int target = player.TargetCell;

            if (target == FarmGame.BinCell)
            {
                ShipFirstItem();
            }
            else if (target == FarmGame.BedCell)
            {
                Sleep();
                return;
            }
            else
            {
                var crop = game.Grid.Get(target.x, target.y)?.Crop;
                var item = crop?.Definition.HarvestItem;
                game.Say(game.Tools.Harvest(target.x, target.y)
                    ? "Harvested " + FarmLabels.Item(item).ToLower()
                    : "Nothing to harvest");
            }
            Refresh();
        }

        private void ShipFirstItem()
        {
            foreach (var stack in game.Inventory.Slots)
            {
                if (stack == null || stack.Item is SeedDefinition) continue;
                ItemDefinition item = stack.Item;
                int count = stack.Count;
                game.Bin.Ship(game.Inventory, item, count);
                game.Say($"Shipped {FarmLabels.Item(item).ToLower()} x{count}");
                return;
            }
            game.Say("Nothing to ship");
        }

        private void Sleep()
        {
            game.DayCycle.EndDay();
            game.Say("Slept");
            Refresh();
        }

        public void Refresh()
        {
            game.Redraw();
            Hotbar.Refresh(game.Inventory);
        }

        private static (string success, string failure) Messages(ToolDefinition tool, bool refill)
        {
            switch (tool.Type)
            {
                case ToolType.Hoe: return ("Tilled", "Can't till here");
                case ToolType.WateringCan: return (refill ? "Refilled" : "Watered", "Nothing to water");
                default: return ("Cleared", "Nothing to clear");
            }
        }
    }
}
