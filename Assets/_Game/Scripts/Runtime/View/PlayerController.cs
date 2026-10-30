using Farm.Farming;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>Moves the player on the XZ plane, snaps facing to a cardinal direction and tracks the target cell.</summary>
    public class PlayerController : MonoBehaviour
    {
        private const float Speed = 4f;
        private const float BodyRadius = 0.25f;
        private const float Limit = FarmGame.GridSize * 0.5f + 1.9f; // farm plus a 2-tile margin

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private FarmGame game;

        private InputAction move;
        private Transform highlight;
        private Vector2Int facing = Vector2Int.up;

        public Vector2Int TargetCell { get; private set; }

        private void Awake()
        {
            move = actions.FindAction("Player/Move");
            PlaceholderTint.Apply(GetComponent<Renderer>(), new Color(0.95f, 0.6f, 0.15f));
            highlight = BuildHighlight();
            transform.position = FarmGame.CellToWorld(FarmGame.PlayerStartCell) + new Vector3(0f, transform.localScale.y, 0f);
            UpdateTarget();
        }

        private void OnDestroy()
        {
            if (highlight != null) Destroy(highlight.gameObject);
        }

        private void Update()
        {
            Vector2 input = game.ShopOpen ? Vector2.zero : Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 from = transform.position;
                Vector3 to = from + new Vector3(input.x, 0f, input.y) * (Speed * UnityEngine.Time.deltaTime);
                to.x = Mathf.Clamp(to.x, -Limit, Limit);
                to.z = Mathf.Clamp(to.z, -Limit, Limit);

                // Slide along a free axis when the diagonal step is blocked.
                var slideX = new Vector3(to.x, from.y, from.z);
                var slideZ = new Vector3(from.x, from.y, to.z);
                transform.position = CanEnter(to) ? to : CanEnter(slideX) ? slideX : CanEnter(slideZ) ? slideZ : from;

                facing = Mathf.Abs(input.x) > Mathf.Abs(input.y)
                    ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                    : new Vector2Int(0, input.y > 0 ? 1 : -1);
                transform.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.y));
            }
            UpdateTarget();
        }

        /// <summary>False if the player standing here would overlap a rock, stump, pond, bed, bin or shop counter.</summary>
        public bool CanEnter(Vector3 position)
        {
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dz = -1; dz <= 1; dz += 2)
                    if (IsBlocked(FarmGame.WorldToCell(position + new Vector3(dx, 0f, dz) * BodyRadius))) return false;
            return true;
        }

        private bool IsBlocked(Vector2Int cell)
        {
            if (cell == FarmGame.BedCell || cell == FarmGame.BinCell || cell == FarmGame.ShopCell) return true;
            var tile = game.Grid.Get(cell.x, cell.y);
            return tile != null && (tile.State == TileState.Rock || tile.State == TileState.Stump || tile.State == TileState.Water);
        }

        private void UpdateTarget()
        {
            TargetCell = FarmGame.WorldToCell(transform.position) + facing;
            highlight.position = FarmGame.CellToWorld(TargetCell) + new Vector3(0f, 0.12f, 0f);
        }

        // Four thin yellow bars around a tile.
        private Transform BuildHighlight()
        {
            var root = new GameObject("Target Highlight").transform;
            AddBar(root, new Vector3(0f, 0f, 0.47f), new Vector3(1f, 0.04f, 0.06f));
            AddBar(root, new Vector3(0f, 0f, -0.47f), new Vector3(1f, 0.04f, 0.06f));
            AddBar(root, new Vector3(0.47f, 0f, 0f), new Vector3(0.06f, 0.04f, 1f));
            AddBar(root, new Vector3(-0.47f, 0f, 0f), new Vector3(0.06f, 0.04f, 1f));
            return root;
        }

        private static void AddBar(Transform parent, Vector3 position, Vector3 scale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            PlaceholderTint.Apply(bar.GetComponent<Renderer>(), new Color(1f, 0.95f, 0.2f));
        }
    }
}
