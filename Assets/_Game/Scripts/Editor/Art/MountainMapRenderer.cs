using System.IO;
using Farm.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools.Art
{
    /// <summary>
    /// Renders a top-down picture of the decorated mountain with the lettered grid, the piece names
    /// and any pins dropped while walking, to MapNotes/map.png. Use it to point at places like "D5".
    /// </summary>
    public static class MountainMapRenderer
    {
        private const string ScenePath = "Assets/_Game/Scenes/GrayboxDecorated.unity";
        private const float PixelsPerMetre = 6f;
        private const float Margin = 14f; // room for the column letters and row numbers
        private const float LabelHeight = 150f; // above the summit flag, so labels draw over everything

        [MenuItem("Farm/Render Mountain Map (grid + pins)")]
        public static void RenderAndOpen() => EditorUtility.OpenWithDefaultApp(Render());

        [MenuItem("Farm/Clear Map Pins")]
        public static void ClearPins()
        {
            int count = MapPins.Load().pins.Count;
            if (count == 0)
            {
                EditorUtility.DisplayDialog("Clear map pins", "There are no pins.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Clear map pins", $"Delete all {count} pins and their screenshots?", "Delete", "Cancel"))
                return;
            if (File.Exists(MapPins.FilePath)) File.Delete(MapPins.FilePath);
            foreach (string shot in Directory.GetFiles(MapPins.Folder, "pin-*.png")) File.Delete(shot);
            Debug.Log($"Cleared {count} map pins.");
        }

        public static string Render()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                float width = MapGrid.Columns * MapGrid.Cell, height = MapGrid.Rows * MapGrid.Cell;
                float left = MapGrid.Left - Margin, top = MapGrid.Top + Margin;
                float viewW = width + Margin * 2f, viewH = height + Margin * 2f;
                var centre = new Vector3(left + viewW * 0.5f, 0f, top - viewH * 0.5f);

                var ink = UnlitMaterial(new Color(0.12f, 0.1f, 0.14f));
                var pinMat = UnlitMaterial(new Color(0.9f, 0.15f, 0.2f));

                // Grid lines.
                for (int c = 0; c <= MapGrid.Columns; c++)
                    Bar(scene, ink, new Vector3(MapGrid.Left + c * MapGrid.Cell, LabelHeight, MapGrid.Top - height * 0.5f), new Vector3(0.5f, 0.1f, height));
                for (int r = 0; r <= MapGrid.Rows; r++)
                    Bar(scene, ink, new Vector3(MapGrid.Left + width * 0.5f, LabelHeight, MapGrid.Top - r * MapGrid.Cell), new Vector3(width, 0.1f, 0.5f));

                // Column letters on top, row numbers on the left, a small name in each cell's corner.
                for (int c = 0; c < MapGrid.Columns; c++)
                    Label(scene, MapGrid.ColumnName(c), new Vector3(MapGrid.Left + (c + 0.5f) * MapGrid.Cell, LabelHeight, MapGrid.Top + Margin * 0.5f), 60f, Color.white);
                for (int r = 0; r < MapGrid.Rows; r++)
                    Label(scene, (r + 1).ToString(), new Vector3(MapGrid.Left - Margin * 0.5f, LabelHeight, MapGrid.Top - (r + 0.5f) * MapGrid.Cell), 60f, Color.white);
                for (int c = 0; c < MapGrid.Columns; c++)
                    for (int r = 0; r < MapGrid.Rows; r++)
                        Label(scene, MapGrid.ColumnName(c) + (r + 1), new Vector3(MapGrid.Left + c * MapGrid.Cell + 3.2f, LabelHeight, MapGrid.Top - r * MapGrid.Cell - 2.2f),
                            22f, new Color(0.12f, 0.1f, 0.14f));

                // Piece names on each block.
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.name != "Mountain") continue;
                    foreach (Transform group in go.transform)
                        foreach (Transform piece in group)
                        {
                            var cliff = piece.Find("Cliff");
                            if (cliff == null) continue;
                            Bounds b = cliff.GetComponent<Renderer>().bounds;
                            Vector3 at = new Vector3(b.center.x, LabelHeight, b.center.z);
                            float turn = 0f;
                            // Lowlands are mostly covered by the shelves, so name them in their open east side.
                            if (piece.name.StartsWith("Lowland")) at.x = b.max.x - 40f;
                            // Only a thin west strip of the Ledge shows beside the Peak, so write along it.
                            if (piece.name == "Ledge") { at.x = b.min.x + 3.5f; turn = 90f; }
                            if (piece.name == "South Plateau") at.z = b.min.z + 8f;
                            Label(scene, piece.name, at, 48f, Color.black, turn);
                        }
                }

                // Pins.
                foreach (var pin in MapPins.Load().pins)
                {
                    var dot = Bar(scene, pinMat, new Vector3(pin.position.x, LabelHeight + 1f, pin.position.z), new Vector3(4f, 0.1f, 4f));
                    dot.name = "Pin dot";
                    Label(scene, pin.number.ToString(), new Vector3(pin.position.x + 6f, LabelHeight + 1f, pin.position.z + 3f), 70f, new Color(0.9f, 0.15f, 0.2f));
                }

                // Top-down camera framing the grid.
                var camGo = new GameObject("Map Camera");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                camGo.transform.SetPositionAndRotation(centre + Vector3.up * 400f, Quaternion.Euler(90f, 0f, 0f));
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = viewH * 0.5f;
                cam.farClipPlane = 1000f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.36f, 0.72f, 0.92f);
                cam.scene = scene;
                cam.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

                int pxW = Mathf.RoundToInt(viewW * PixelsPerMetre), pxH = Mathf.RoundToInt(viewH * PixelsPerMetre);
                var rt = new RenderTexture(pxW, pxH, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(pxW, pxH, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, pxW, pxH), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                rt.Release();

                Directory.CreateDirectory(MapPins.Folder);
                string path = Path.Combine(MapPins.Folder, "map.png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                return path;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static Material UnlitMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", color);
            return mat;
        }

        private static GameObject Bar(Scene scene, Material mat, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, scene);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void Label(Scene scene, string text, Vector3 position, float size, Color color, float turn = 0f)
        {
            var go = new GameObject("Label " + text);
            SceneManager.MoveGameObjectToScene(go, scene);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = color;
            tmp.rectTransform.sizeDelta = new Vector2(80f, 20f);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, turn, 0f)); // lie flat, readable from above
            tmp.ForceMeshUpdate();
        }
    }
}
