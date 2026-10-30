using Farm.Day;
using Farm.Farming;
using Farm.Time;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    public class FarmViewController : MonoBehaviour
    {
        private enum FarmAction { Till, Plant, Water }

        private const int GridSize = 8;
        private const float MessageSeconds = 2f;

        [SerializeField] private CropDefinition turnip;
        [SerializeField] private FarmGridView gridView;

        private GameCalendar calendar;
        private FarmGrid grid;
        private DayCycleService dayCycle;
        private Camera cam;
        private FarmAction action = FarmAction.Till;
        private string message = "";
        private float messageTime = float.NegativeInfinity;

        private void Awake()
        {
            cam = Camera.main;
            calendar = new GameCalendar();
            grid = new FarmGrid(GridSize, GridSize);
            dayCycle = new DayCycleService(calendar, grid);
            gridView.Build(GridSize, GridSize);
            gridView.Redraw(grid);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) action = FarmAction.Till;
                if (keyboard.digit2Key.wasPressedThisFrame) action = FarmAction.Plant;
                if (keyboard.digit3Key.wasPressedThisFrame) action = FarmAction.Water;
                if (keyboard.nKey.wasPressedThisFrame)
                {
                    dayCycle.EndDay();
                    Say("Slept");
                    gridView.Redraw(grid);
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && cam != null && mouse.leftButton.wasPressedThisFrame)
                HandleClick(mouse.position.ReadValue());
        }

        private void HandleClick(Vector2 screenPosition)
        {
            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return;

            Vector3 point = ray.GetPoint(distance);
            int x = Mathf.FloorToInt(point.x + GridSize * 0.5f);
            int y = Mathf.FloorToInt(point.z + GridSize * 0.5f);
            if (x < 0 || y < 0 || x >= GridSize || y >= GridSize) return;

            bool ok;
            switch (action)
            {
                case FarmAction.Till:
                    ok = grid.Till(x, y);
                    Say(ok ? "Tilled" : "Can't till here");
                    break;
                case FarmAction.Plant:
                    ok = grid.Plant(x, y, turnip, calendar.Season);
                    Say(ok ? "Planted turnip" : "Can't plant here");
                    break;
                default:
                    ok = grid.Water(x, y);
                    Say(ok ? "Watered" : "Can't water here");
                    break;
            }
            gridView.Redraw(grid);
        }

        private void Say(string text)
        {
            message = text;
            messageTime = UnityEngine.Time.unscaledTime;
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            style.normal.textColor = Color.white;
            string actionLabel = $"[{(int)action + 1}] {action}";
            GUI.Label(new Rect(10, 10, 900, 30), $"{calendar.Season} {calendar.Day}, Year {calendar.Year}   Action: {actionLabel}", style);
            GUI.Label(new Rect(10, 40, 900, 30), "1 Till  2 Plant  3 Water  Click tile  N Sleep", style);
            if (UnityEngine.Time.unscaledTime - messageTime < MessageSeconds)
                GUI.Label(new Rect(10, 70, 900, 30), message, style);
        }
    }
}
