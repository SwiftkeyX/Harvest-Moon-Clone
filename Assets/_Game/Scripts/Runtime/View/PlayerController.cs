using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>Moves the player on the XZ plane, snaps facing to a cardinal direction and tracks the target cell.</summary>
    public class PlayerController : MonoBehaviour
    {
        private const float Speed = 4f;
        private const float Limit = FarmGame.GridSize * 0.5f + 1.9f; // farm plus a 2-tile margin

        [SerializeField] private InputActionAsset actions;

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
            Vector2 input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 pos = transform.position + new Vector3(input.x, 0f, input.y) * (Speed * UnityEngine.Time.deltaTime);
                pos.x = Mathf.Clamp(pos.x, -Limit, Limit);
                pos.z = Mathf.Clamp(pos.z, -Limit, Limit);
                transform.position = pos;

                facing = Mathf.Abs(input.x) > Mathf.Abs(input.y)
                    ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                    : new Vector2Int(0, input.y > 0 ? 1 : -1);
                transform.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.y));
            }
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            TargetCell = FarmGame.WorldToCell(transform.position) + facing;
            highlight.position = FarmGame.CellToWorld(TargetCell) + new Vector3(0f, 0.03f, 0f);
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
