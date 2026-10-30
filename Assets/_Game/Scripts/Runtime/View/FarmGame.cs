using System.IO;
using Farm.Core;
using Farm.Day;
using Farm.Economy;
using Farm.Farming;
using Farm.Items;
using Farm.Player;
using Farm.Time;
using Farm.Tools;
using Farm.Weather;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>Owns the rule objects and the placeholder world; the other view scripts reach the rules through it.</summary>
    public class FarmGame : MonoBehaviour
    {
        public const int GridSize = 8;
        public static readonly Vector2Int BedCell = new Vector2Int(3, -1);
        public static readonly Vector2Int BinCell = new Vector2Int(4, -1);
        public static readonly Vector2Int ShopCell = new Vector2Int(6, -1);
        public static readonly Vector2Int PlayerStartCell = new Vector2Int(3, -2);

        private const float MessageSeconds = 3f;

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private FarmGridView gridView;
        [SerializeField] private ToolDefinition hoe;
        [SerializeField] private ToolDefinition wateringCan;
        [SerializeField] private ToolDefinition sickle;
        [SerializeField] private ToolDefinition hammer;
        [SerializeField] private ToolDefinition axe;
        [SerializeField] private SeedDefinition turnipSeeds;
        [SerializeField] private GameDatabase database;

        private string message = "";
        private float messageTime = float.NegativeInfinity;

        /// <summary>Raised after F9 replaced the game with a new one.</summary>
        public event System.Action GameReset;

        private GameState state;
        private float savedNoticeTime = float.NegativeInfinity;

        public GameCalendar Calendar => state.Calendar;
        public GameClock Clock => state.Clock;
        public FarmGrid Grid => state.Grid;
        public Stamina Stamina => state.Stamina;
        public Wallet Wallet => state.Wallet;
        public ShippingBin Bin => state.Bin;
        public Inventory Inventory => state.Inventory;
        public WeatherSystem Weather => state.Weather;
        public DayCycleService DayCycle => state.DayCycle;
        public ToolUser Tools => state.Tools;
        public ToolDefinition[] ToolDefinitions { get; private set; }
        public SeedShop Shop { get; private set; }

        /// <summary>True while the shop panel is open: the player stands still and game time is paused.</summary>
        public bool ShopOpen { get; set; }

        public bool SavedNoticeVisible => UnityEngine.Time.unscaledTime - savedNoticeTime < MessageSeconds;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "save_slot1.json");

        public string CurrentMessage =>
            UnityEngine.Time.unscaledTime - messageTime < MessageSeconds ? message : "";

        public static Vector3 CellToWorld(Vector2Int cell) =>
            new Vector3(cell.x - (GridSize - 1) * 0.5f, 0f, cell.y - (GridSize - 1) * 0.5f);

        public static Vector2Int WorldToCell(Vector3 world) =>
            new Vector2Int(Mathf.FloorToInt(world.x + GridSize * 0.5f), Mathf.FloorToInt(world.z + GridSize * 0.5f));

        private void Awake()
        {
            ToolDefinitions = new[] { hoe, wateringCan, sickle, hammer, axe };
            StartGame(loadSave: true);

            gridView.Build(GridSize, GridSize);
            gridView.Redraw(Grid);
            AddDetail(CreateBlock("Bed", BedCell, new Color(0.85f, 0.2f, 0.2f)), new Vector3(0f, 0.55f, 0.3f), new Vector3(0.8f, 0.2f, 0.3f), new Color(0.97f, 0.95f, 0.9f)); // pillow
            AddDetail(CreateBlock("Shipping Bin", BinCell, new Color(0.2f, 0.4f, 0.9f)), new Vector3(0f, 0.55f, 0f), new Vector3(1.06f, 0.14f, 1.06f), new Color(0.1f, 0.22f, 0.6f)); // lid
            CreateBlock("Seed Shop", ShopCell, new Color(0.95f, 0.85f, 0.35f));
        }

        // Loads the save if there is one, otherwise sets up a new farm.
        private void StartGame(bool loadSave)
        {
            state = new GameState(new GameSetup { GridWidth = GridSize, GridHeight = GridSize });
            if (!(loadSave && TryLoad()))
            {
                PlaceFeatures();
                if (turnipSeeds != null) Inventory.Add(turnipSeeds, 5);
            }
            // After loading: a restore rebuilds the systems, so listen only now.
            state.DayCycle.DayEnded += Save;
            Shop = new SeedShop(database.Seeds, Wallet, Inventory, Clock, Calendar);
        }

        private void OnEnable() => actions.FindActionMap("Player").Enable();

        private void OnDisable() => actions.FindActionMap("Player").Disable();

        private void Update()
        {
            // Debug: forget the save and start a new game.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                StartGame(loadSave: false);
                Redraw();
                Say("New game");
                GameReset?.Invoke();
            }
        }

        private bool TryLoad()
        {
            if (!File.Exists(SavePath)) return false;
            try
            {
                state.Restore(SaveSerializer.FromJson(File.ReadAllText(SavePath)), database);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Could not load the save, starting a new game: {e.Message}");
                state = new GameState(new GameSetup { GridWidth = GridSize, GridHeight = GridSize });
                return false;
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(SavePath, SaveSerializer.ToJson(state.Capture()));
                savedNoticeTime = UnityEngine.Time.unscaledTime;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Could not save the game: {e.Message}");
            }
        }

        public void Redraw() => gridView.Redraw(Grid);

        public void Say(string text)
        {
            message = text;
            messageTime = UnityEngine.Time.unscaledTime;
        }

        // Fixed positions so every new game starts the same.
        private void PlaceFeatures()
        {
            Grid.Place(7, 7, TileState.Water);
            Grid.Place(6, 7, TileState.Water);
            foreach (var c in new[] { new Vector2Int(1, 5), new Vector2Int(2, 6), new Vector2Int(5, 2) })
                Grid.Place(c.x, c.y, TileState.Weed);
            foreach (var c in new[] { new Vector2Int(0, 3), new Vector2Int(6, 1) })
                Grid.Place(c.x, c.y, TileState.Rock);
            foreach (var c in new[] { new Vector2Int(4, 5), new Vector2Int(2, 1) })
                Grid.Place(c.x, c.y, TileState.Stump);
        }

        private GameObject CreateBlock(string blockName, Vector2Int cell, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = blockName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellToWorld(cell) + new Vector3(0f, 0.4f, 0f);
            go.transform.localScale = new Vector3(0.9f, 0.8f, 0.9f);
            PlaceholderTint.Apply(go.GetComponent<Renderer>(), color);
            return go;
        }

        // A small part on top of a block, positioned and scaled relative to the block.
        private static void AddDetail(GameObject block, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(block.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            PlaceholderTint.Apply(part.GetComponent<Renderer>(), color);
        }
    }
}
