using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>
    /// The lettered grid laid over the test map (columns A–K west to east, rows 1–12 north to south),
    /// so a place can be named like "D5". Shared by the pins and the map picture.
    /// </summary>
    public static class MapGrid
    {
        public const float Left = -20f, Top = 20f, Cell = 25f;
        public const int Columns = 11, Rows = 12;

        public static string CellAt(Vector3 p)
        {
            int column = Mathf.FloorToInt((p.x - Left) / Cell);
            int row = Mathf.FloorToInt((Top - p.z) / Cell);
            if (column < 0 || column >= Columns || row < 0 || row >= Rows) return "off map";
            return ColumnName(column) + (row + 1);
        }

        public static string ColumnName(int column) => ((char)('A' + column)).ToString();
    }

    [Serializable]
    public class MapPin
    {
        public int number;
        public Vector3 position;
        public string cell;
        public string piece;
        public string screenshot;
        public string time;
    }

    [Serializable]
    public class MapPinFile
    {
        public List<MapPin> pins = new List<MapPin>();
    }

    /// <summary>
    /// Drops numbered pins while walking a test map, to point at places that need work.
    /// N drops a pin where the player stands (and saves a screenshot). X (or Backspace/Delete) removes
    /// the pin nearest the player, or the last pin if none is close.
    /// Pins are kept in MapNotes/pins.json next to the Assets folder and come back on the next Play.
    /// </summary>
    public class MapPins : MonoBehaviour
    {
        [SerializeField] private float poleHeight = 3f;
        [SerializeField] private Color color = new Color(0.9f, 0.15f, 0.2f);
        [SerializeField] private float removeRadius = 6f; // how close a pin must be to be "the one you're standing at"

        public static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "MapNotes"));
        public static string FilePath => Path.Combine(Folder, "pins.json");

        private MapPinFile file = new MapPinFile();
        private readonly List<GameObject> markers = new List<GameObject>();
        private Material material;

        /// <summary>How many pins are on the map.</summary>
        public int Count => file.pins.Count;

        public static MapPinFile Load() =>
            File.Exists(FilePath) ? JsonUtility.FromJson<MapPinFile>(File.ReadAllText(FilePath)) ?? new MapPinFile() : new MapPinFile();

        private void Start()
        {
            // A copy of the pipeline's default material, tinted, so it always matches the active render pipeline.
            var probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            material = new Material(probe.GetComponent<Renderer>().sharedMaterial) { color = color };
            Destroy(probe);
            file = Load();
            foreach (var pin in file.pins) markers.Add(Marker(pin));
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.nKey.wasPressedThisFrame) Drop();
            bool remove = keys.xKey.wasPressedThisFrame || keys.backspaceKey.wasPressedThisFrame || keys.deleteKey.wasPressedThisFrame;
            if (remove && file.pins.Count > 0) Remove(NearestOrLast());
        }

        private void LateUpdate()
        {
            // Keep pins readable at any zoom: face the camera and grow with distance.
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var marker in markers)
            {
                float scale = Mathf.Max(1f, Vector3.Distance(cam.transform.position, marker.transform.position) / 25f);
                marker.transform.localScale = Vector3.one * scale;
                marker.transform.GetChild(2).rotation = cam.transform.rotation;
            }
        }

        private void Drop()
        {
            int number = file.pins.Count == 0 ? 1 : file.pins[file.pins.Count - 1].number + 1;
            var pin = new MapPin
            {
                number = number,
                position = transform.position,
                cell = MapGrid.CellAt(transform.position),
                piece = PieceUnderfoot(),
                screenshot = $"pin-{number:00}.png",
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            };
            file.pins.Add(pin);
            Save();
            markers.Add(Marker(pin));
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, pin.screenshot));
            Debug.Log($"Pin {pin.number} at {pin.cell} on {pin.piece}. Saved to {Folder}");
        }

        // The pin closest to the player if one is within reach, otherwise the newest pin.
        private int NearestOrLast()
        {
            int best = file.pins.Count - 1;
            float bestDistance = removeRadius;
            for (int i = 0; i < file.pins.Count; i++)
            {
                float d = Vector3.Distance(file.pins[i].position, transform.position);
                if (d <= bestDistance) { best = i; bestDistance = d; }
            }
            return best;
        }

        private void Remove(int index)
        {
            var pin = file.pins[index];
            file.pins.RemoveAt(index);
            Save();
            Destroy(markers[index]);
            markers.RemoveAt(index);
            string shot = Path.Combine(Folder, pin.screenshot);
            if (File.Exists(shot)) File.Delete(shot);
            Debug.Log($"Removed pin {pin.number}.");
        }

        private void Save()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonUtility.ToJson(file, true));
        }

        // The named map piece (Peak, Mid Shelf…) under the player: the parent of whatever collider is hit.
        private string PieceUnderfoot()
        {
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 50f) && hit.collider.transform.parent != null)
                return hit.collider.transform.parent.name;
            return "unknown";
        }

        private GameObject Marker(MapPin pin)
        {
            var root = new GameObject($"Pin {pin.number}");
            root.transform.position = pin.position;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(pole.GetComponent<Collider>());
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = Vector3.up * poleHeight * 0.5f;
            pole.transform.localScale = new Vector3(0.12f, poleHeight * 0.5f, 0.12f);
            pole.GetComponent<Renderer>().sharedMaterial = material;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = Vector3.up * poleHeight;
            head.transform.localScale = Vector3.one * 0.6f;
            head.GetComponent<Renderer>().sharedMaterial = material;

            var label = new GameObject("Label").AddComponent<TextMeshPro>();
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = Vector3.up * (poleHeight + 1.1f);
            label.text = pin.number.ToString();
            label.fontSize = 10f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.rectTransform.sizeDelta = new Vector2(4f, 2f);
            return root;
        }
    }
}
