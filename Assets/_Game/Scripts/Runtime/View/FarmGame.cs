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

        private string message = "";
        private float messageTime = float.NegativeInfinity;

        public GameCalendar Calendar { get; private set; }
        public GameClock Clock { get; private set; }
        public FarmGrid Grid { get; private set; }
        public Stamina Stamina { get; private set; }
        public Wallet Wallet { get; private set; }
        public ShippingBin Bin { get; private set; }
        public Inventory Inventory { get; private set; }
        public WeatherSystem Weather { get; private set; }
        public DayCycleService DayCycle { get; private set; }
        public ToolUser Tools { get; private set; }
        public ToolDefinition[] ToolDefinitions { get; private set; }

        public string CurrentMessage =>
            UnityEngine.Time.unscaledTime - messageTime < MessageSeconds ? message : "";

        public static Vector3 CellToWorld(Vector2Int cell) =>
            new Vector3(cell.x - (GridSize - 1) * 0.5f, 0f, cell.y - (GridSize - 1) * 0.5f);

        public static Vector2Int WorldToCell(Vector3 world) =>
            new Vector2Int(Mathf.FloorToInt(world.x + GridSize * 0.5f), Mathf.FloorToInt(world.z + GridSize * 0.5f));

        private void Awake()
        {
            Calendar = new GameCalendar();
            Clock = new GameClock();
            Grid = new FarmGrid(GridSize, GridSize);
            Stamina = new Stamina();
            Wallet = new Wallet();
            Bin = new ShippingBin();
            Inventory = new Inventory();
            var random = new System.Random();
            Weather = new WeatherSystem(random);
            DayCycle = new DayCycleService(Calendar, Clock, Grid, Stamina, Bin, Wallet, Weather, random);
            Tools = new ToolUser(Grid, Stamina, DayCycle, Calendar, Inventory);
            ToolDefinitions = new[] { hoe, wateringCan, sickle, hammer, axe };

            PlaceFeatures();
            if (turnipSeeds != null) Inventory.Add(turnipSeeds, 5);

            gridView.Build(GridSize, GridSize);
            gridView.Redraw(Grid);
            CreateBlock("Bed", BedCell, new Color(0.85f, 0.2f, 0.2f));
            CreateBlock("Shipping Bin", BinCell, new Color(0.2f, 0.4f, 0.9f));
        }

        private void OnEnable() => actions.FindActionMap("Player").Enable();

        private void OnDisable() => actions.FindActionMap("Player").Disable();

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

        private void CreateBlock(string blockName, Vector2Int cell, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = blockName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellToWorld(cell) + new Vector3(0f, 0.4f, 0f);
            go.transform.localScale = new Vector3(0.9f, 0.8f, 0.9f);
            PlaceholderTint.Apply(go.GetComponent<Renderer>(), color);
        }
    }
}
