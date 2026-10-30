using System.Collections.Generic;
using System.Linq;
using Farm.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>Placeholder seed shop menu. While it is open the player stands still and game time stops.</summary>
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private FarmGame game;
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerActions playerActions;

        private InputAction move;
        private InputAction attack;
        private InputAction interact;
        private float lastVertical;
        private string message = "";

        public int Selected { get; set; }

        private void Awake()
        {
            move = actions.FindAction("Player/Move");
            attack = actions.FindAction("Player/Attack");
            interact = actions.FindAction("Player/Interact");
        }

        private void OnEnable()
        {
            attack.performed += OnAttack;
            interact.performed += OnInteract;
        }

        private void OnDisable()
        {
            attack.performed -= OnAttack;
            interact.performed -= OnInteract;
        }

        private void Update()
        {
            if (!game.ShopOpen) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) game.ShopOpen = false;

            // Step the selection once per push of the stick or key.
            float vertical = move.ReadValue<Vector2>().y;
            if (Mathf.Abs(vertical) > 0.5f && Mathf.Abs(lastVertical) <= 0.5f)
                Selected = Mathf.Clamp(Selected + (vertical > 0 ? -1 : 1), 0, Mathf.Max(0, Available().Count - 1));
            lastVertical = vertical;
        }

        private void OnInteract(InputAction.CallbackContext ctx) => Interact(player.TargetCell);

        private void OnAttack(InputAction.CallbackContext ctx)
        {
            if (game.ShopOpen) BuySelected();
        }

        /// <summary>E pressed while targeting a cell: closes the shop if it is open, opens it at the counter.</summary>
        public void Interact(Vector2Int target)
        {
            if (game.ShopOpen)
            {
                game.ShopOpen = false;
            }
            else if (target == FarmGame.ShopCell)
            {
                game.ShopOpen = true;
                Selected = 0;
                message = game.Shop.IsOpen ? "" : "Shop is closed (9 AM-5 PM)";
            }
        }

        /// <summary>Buys one packet of the selected seeds, or says why that isn't possible.</summary>
        public void BuySelected()
        {
            var list = Available();
            if (list.Count == 0) { message = "Nothing for sale"; return; }
            SeedDefinition seeds = list[Mathf.Clamp(Selected, 0, list.Count - 1)];

            if (!game.Shop.IsOpen) message = "Shop is closed (9 AM-5 PM)";
            else if (game.Wallet.Money < seeds.BuyPrice) message = "Not enough money";
            else if (!game.Inventory.CanAdd(seeds, 1)) message = "No room";
            else if (game.Shop.Buy(seeds, 1))
            {
                message = "Bought " + FarmLabels.Item(seeds).ToLower();
                playerActions.Refresh();
            }
            else message = "Can't buy that";
        }

        private List<SeedDefinition> Available() => game.Shop.AvailableNow().ToList();

        private void OnGUI()
        {
            if (game == null || !game.ShopOpen || game.Shop == null) return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            style.normal.textColor = Color.white;
            var list = Available();
            float height = 120 + list.Count * 30;
            GUI.Box(new Rect(380, 170, 560, height), "Seed shop");

            string status = game.Shop.IsOpen ? $"Money: {game.Wallet.Money}" : "Shop is closed (9 AM-5 PM)";
            GUI.Label(new Rect(395, 200, 530, 30), status, style);
            for (int i = 0; i < list.Count; i++)
            {
                var seeds = list[i];
                string line = $"{(i == Selected ? ">" : " ")} {FarmLabels.Item(seeds)}  {seeds.BuyPrice}g  (carry {game.Inventory.CountOf(seeds)})";
                GUI.Label(new Rect(395, 230 + i * 30, 530, 30), line, style);
            }
            GUI.Label(new Rect(395, 230 + list.Count * 30, 530, 30), message, style);
            GUI.Label(new Rect(395, 260 + list.Count * 30, 530, 30), "W/S select  Space buy  E/Esc close", style);
        }
    }
}
