using Farm.View;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Farm.EditorTools
{
    /// <summary>Builds the placeholder uGUI: HUD panels, the seed shop panel, the canvas and the event system.</summary>
    internal static class FarmUiBuilder
    {
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.6f);

        public static void Build(FarmHud hud, ShopPanel shop)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            BuildHud(canvasGo.transform, hud);
            BuildShop(canvasGo.transform, shop);
        }

        private static void BuildHud(Transform canvas, FarmHud hud)
        {
            // Top-left: the status line.
            var top = NewPanel("Status Panel", canvas, new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(1760f, 52f));
            var status = NewText("Status", top, "", 26f, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)status.transform, 14f, 4f);

            // Bottom-left: message, hotbar, inventory and controls.
            var bottom = NewPanel("Bottom Panel", canvas, new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(1500f, 240f));
            var column = bottom.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(14, 14, 10, 10);
            column.spacing = 6f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var message = NewText("Message", bottom, "", 28f, TextAlignmentOptions.MidlineLeft);
            message.color = new Color(1f, 0.9f, 0.4f);
            SetHeight(message, 36f);

            var hotbarRow = NewRect("Hotbar", bottom);
            var row = hotbarRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            hotbarRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

            var inventory = NewText("Inventory", bottom, "", 24f, TextAlignmentOptions.MidlineLeft);
            SetHeight(inventory, 34f);

            var controls = NewText("Controls", bottom, "WASD move   Space/Click use   1/2 select   E interact   N sleep", 22f, TextAlignmentOptions.MidlineLeft);
            controls.color = new Color(0.8f, 0.8f, 0.8f);
            SetHeight(controls, 30f);

            EditorAssetUtil.Wire(hud, ("statusText", status), ("hotbarRow", hotbarRow),
                ("inventoryText", inventory), ("messageText", message));
        }

        private static void BuildShop(Transform canvas, ShopPanel shop)
        {
            var panel = NewPanel("Shop Panel", canvas, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f));
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(24, 24, 24, 24);
            column.spacing = 8f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var title = NewText("Title", panel, "Seed shop", 38f, TextAlignmentOptions.Center);
            SetHeight(title, 50f);
            var money = NewText("Money", panel, "", 26f, TextAlignmentOptions.MidlineLeft);
            SetHeight(money, 36f);

            var rows = NewRect("Rows", panel);
            var rowsLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 4f;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;
            rows.gameObject.AddComponent<LayoutElement>().preferredHeight = 264f;

            var message = NewText("Message", panel, "", 24f, TextAlignmentOptions.MidlineLeft);
            message.color = new Color(1f, 0.9f, 0.4f);
            SetHeight(message, 36f);
            var footer = NewText("Footer", panel, "W/S select   Space buy   E/Esc close", 22f, TextAlignmentOptions.MidlineLeft);
            footer.color = new Color(0.8f, 0.8f, 0.8f);
            SetHeight(footer, 36f);

            EditorAssetUtil.Wire(shop, ("panel", panel.gameObject), ("moneyText", money),
                ("rows", rows), ("messageText", message));
            panel.gameObject.SetActive(false);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform NewPanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = NewRect(name, parent);
            rect.gameObject.AddComponent<Image>().color = PanelColor;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
        {
            var rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void SetHeight(Component target, float height) =>
            target.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

        private static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
