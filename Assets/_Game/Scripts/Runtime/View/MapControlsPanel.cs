using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>
    /// Lists the test map's controls in the top-left corner. H hides or shows it.
    /// </summary>
    public class MapControlsPanel : MonoBehaviour
    {
        private static readonly (string key, string action)[] Controls =
        {
            ("W / A / S / D", "walk"),
            ("Shift", "run"),
            ("Space", "jump"),
            ("F", "fly on / off"),
            ("Space / Ctrl", "fly up / down"),
            ("Q / E", "turn view"),
            ("Right mouse drag", "turn and tilt view"),
            ("Mouse wheel", "zoom"),
            ("N", "drop pin"),
            ("X", "remove nearest pin"),
            ("R", "back to start"),
            ("H", "hide this panel"),
        };

        [SerializeField] private bool visible = true;

        private MapPins pins;
        private GUIStyle box, key, action;

        private void Awake() => pins = GetComponent<MapPins>();

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys != null && keys.hKey.wasPressedThisFrame) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            if (box == null)
            {
                box = new GUIStyle(GUI.skin.box);
                key = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
                action = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            }

            const float line = 22f, keyWidth = 150f, actionWidth = 160f, pad = 10f;
            int rows = Controls.Length + (pins != null ? 1 : 0);
            GUI.Box(new Rect(pad, pad, keyWidth + actionWidth + pad * 2f, rows * line + pad * 2f), GUIContent.none, box);

            float y = pad * 2f;
            foreach (var (k, a) in Controls)
            {
                GUI.Label(new Rect(pad * 2f, y, keyWidth, line), k, key);
                GUI.Label(new Rect(pad * 2f + keyWidth, y, actionWidth, line), "- " + a, action);
                y += line;
            }
            if (pins != null)
                GUI.Label(new Rect(pad * 2f, y, keyWidth + actionWidth, line), $"Pins dropped: {pins.Count}", key);
        }
    }
}
