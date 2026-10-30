using System.Collections.Generic;
using System.Linq;
using Farm.Items;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Farm.View
{
    /// <summary>Placeholder seed shop menu. While it is open the player stands still and game time stops.</summary>
    public class ShopPanel : MonoBehaviour
    {
        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0f);
        private static readonly Color SelectedRowColor = new Color(0.2f, 0.5f, 0.9f, 0.9f);

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private FarmGame game;
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerActions playerActions;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private RectTransform rows;
        [SerializeField] private TMP_Text messageText;

        private class Row
        {
            public GameObject Root;
            public Image Background;
            public TMP_Text Name;
            public TMP_Text Price;
            public TMP_Text Carried;
        }

        private readonly List<Row> rowPool = new List<Row>();
        private InputAction move;
        private InputAction attack;
        private InputAction interact;
        private float lastVertical;
        private string message = "";
        private int shownKey;

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
            if (panel.activeSelf != game.ShopOpen) panel.SetActive(game.ShopOpen);
            if (!game.ShopOpen) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                game.ShopOpen = false;
                panel.SetActive(false);
                return;
            }

            // Step the selection once per push of the stick or key.
            float vertical = move.ReadValue<Vector2>().y;
            if (Mathf.Abs(vertical) > 0.5f && Mathf.Abs(lastVertical) <= 0.5f)
                Selected = Mathf.Clamp(Selected + (vertical > 0 ? -1 : 1), 0, Mathf.Max(0, Available().Count - 1));
            lastVertical = vertical;

            Refresh();
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

        // Rewrites the texts only when something they show has changed.
        private void Refresh()
        {
            var list = Available();
            var key = new System.HashCode();
            key.Add(Selected); key.Add(game.Wallet.Money); key.Add(game.Shop.IsOpen); key.Add(message);
            foreach (var seeds in list) key.Add(game.Inventory.CountOf(seeds));
            int hash = key.ToHashCode();
            if (hash == shownKey) return;
            shownKey = hash;

            moneyText.text = game.Shop.IsOpen ? $"Money: {game.Wallet.Money}" : "Shop is closed (9 AM-5 PM)";
            messageText.text = message;

            while (rowPool.Count < list.Count) rowPool.Add(CreateRow());
            for (int i = 0; i < rowPool.Count; i++)
            {
                bool used = i < list.Count;
                rowPool[i].Root.SetActive(used);
                if (!used) continue;

                rowPool[i].Name.text = FarmLabels.Item(list[i]);
                rowPool[i].Price.text = list[i].BuyPrice.ToString();
                rowPool[i].Carried.text = "carry " + game.Inventory.CountOf(list[i]);
                rowPool[i].Background.color = i == Selected ? SelectedRowColor : RowColor;
            }
        }

        private Row CreateRow()
        {
            var root = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            root.transform.SetParent(rows, false);
            root.GetComponent<LayoutElement>().preferredHeight = 40f;
            var layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            return new Row
            {
                Root = root,
                Background = root.GetComponent<Image>(),
                Name = CreateCell(root.transform, 340f, TextAlignmentOptions.Left),
                Price = CreateCell(root.transform, 110f, TextAlignmentOptions.Right),
                Carried = CreateCell(root.transform, 160f, TextAlignmentOptions.Right),
            };
        }

        private static TMP_Text CreateCell(Transform parent, float width, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Cell", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredWidth = width;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 26f;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }
    }
}
