using System;
using UnityEngine;
using UnityEngine.UI;

namespace StonewardsBellControl
{
    internal enum PanelCorner
    {
        TopRight,
        TopLeft,
        BottomRight,
        BottomLeft
    }

    /// <summary>
    /// Native Unity uGUI panel with a Canvas, RectTransforms, Images, Text and Button.
    /// Colors mirror the visual palette used by Stonewards Party HUD.
    /// No OnGUI or manually calculated pixel-based GUI.Rect positions.
    /// </summary>
    internal sealed class BellControlPanel : IDisposable
    {
        private const float PanelWidth = 284f;
        private const float PanelHeight = 124f;
        private const float EdgePadding = 6f;

        private static readonly Color FrameColor = new Color(0.72f, 0.64f, 0.48f, 1f);
        private static readonly Color BackgroundColor = new Color(19f / 255f, 23f / 255f, 32f / 255f, 0.97f);
        private static readonly Color HeaderColor = new Color(76f / 255f, 46f / 255f, 48f / 255f, 1f);
        private static readonly Color DividerColor = new Color(0.71f, 0.63f, 0.48f, 0.38f);
        private static readonly Color PrimaryText = new Color(0.94f, 0.91f, 0.83f, 1f);
        private static readonly Color SecondaryText = new Color(0.71f, 0.72f, 0.70f, 1f);
        private static readonly Color LockedText = new Color(1f, 0.66f, 0.55f, 1f);
        private static readonly Color AllowedText = new Color(0.59f, 0.89f, 0.68f, 1f);
        private static readonly Color LockedBackground = new Color(0.34f, 0.16f, 0.19f, 1f);
        private static readonly Color AllowedBackground = new Color(0.16f, 0.31f, 0.25f, 1f);
        private static readonly Color ButtonBackground = new Color(0.25f, 0.30f, 0.35f, 1f);

        private readonly Action _onToggle;
        private readonly GameObject _canvasObject;
        private readonly Canvas _canvas;
        private readonly RectTransform _panelRoot;
        private readonly Text _statusText;
        private readonly Text _buttonText;
        private readonly Image _statusBackground;
        private readonly Image _buttonBackground;

        public bool IsAlive => _canvasObject != null && _panelRoot != null;

        public BellControlPanel(Action onToggle)
        {
            _onToggle = onToggle ?? throw new ArgumentNullException(nameof(onToggle));

            _canvasObject = new GameObject("Stonewards Bell Control Canvas", typeof(RectTransform));
            UnityEngine.Object.DontDestroyOnLoad(_canvasObject);

            _canvas = _canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 110;

            var scaler = _canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasObject.AddComponent<GraphicRaycaster>();

            var font = GetUiFont();
            if (font == null)
                throw new InvalidOperationException("Cannot find a Unity UI font.");

            _panelRoot = CreateRect("Bell Control", _canvasObject.transform,
                0f, 0f, PanelWidth, PanelHeight);
            AddImage(_panelRoot.gameObject, FrameColor, false);

            var background = CreateRect("Background", _panelRoot, 2f, 2f,
                PanelWidth - 4f, PanelHeight - 4f);
            AddImage(background.gameObject, BackgroundColor, false);

            var header = CreateRect("Header", _panelRoot, 2f, 2f, PanelWidth - 4f, 29f);
            AddImage(header.gameObject, HeaderColor, false);

            CreateText("Title", _panelRoot, font, "BELL CONTROL", 13, FontStyle.Bold,
                PrimaryText, TextAnchor.MiddleLeft, 13f, 4f, 190f, 25f);
            CreateText("Host Label", _panelRoot, font, "HOST", 10, FontStyle.Normal,
                SecondaryText, TextAnchor.MiddleRight, 222f, 4f, 48f, 25f);

            var divider = CreateRect("Divider", _panelRoot, 3f, 32f,
                PanelWidth - 6f, 1f);
            AddImage(divider.gameObject, DividerColor, false);

            CreateText("Guests Label", _panelRoot, font, "Guests", 15, FontStyle.Normal,
                PrimaryText, TextAnchor.MiddleLeft, 13f, 42f, 115f, 26f);

            var state = CreateRect("Status", _panelRoot, 150f, 42f, 121f, 26f);
            _statusBackground = AddImage(state.gameObject, LockedBackground, false);
            _statusText = CreateText("Status Text", state, font, "BLOCKED",
                12, FontStyle.Bold, LockedText, TextAnchor.MiddleCenter,
                0f, 0f, 121f, 26f);

            var action = CreateRect("Toggle Button", _panelRoot, 12f, 80f, 260f, 33f);
            _buttonBackground = AddImage(action.gameObject, ButtonBackground, true);
            var button = action.gameObject.AddComponent<Button>();
            button.targetGraphic = _buttonBackground;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(HandleButtonClick);

            _buttonText = CreateText("Button Text", action, font, "ALLOW GUESTS  [F8]",
                12, FontStyle.Bold, PrimaryText, TextAnchor.MiddleCenter,
                0f, 0f, 260f, 33f);
        }

        public void SetAllowed(bool allowed)
        {
            if (!IsAlive)
                return;

            _statusText.text = allowed ? "ALLOWED" : "BLOCKED";
            _statusText.color = allowed ? AllowedText : LockedText;
            _statusBackground.color = allowed ? AllowedBackground : LockedBackground;
            _buttonText.text = allowed ? "BLOCK GUESTS  [F8]" : "ALLOW GUESTS  [F8]";
            _buttonBackground.color = ButtonBackground;
        }

        /// <summary>
        /// Offsets are distances inward from the configured corner.
        /// Decreasing VerticalOffset from a top anchor moves the panel up.
        /// Keep the whole panel within the visible screen on resize.
        /// </summary>
        public void UpdateLayout(PanelCorner corner, int horizontalOffset, int verticalOffset,
            float requestedScale)
        {
            if (!IsAlive)
                return;

            float canvasScale = Mathf.Max(0.01f, _canvas.scaleFactor);
            float canvasWidth = Screen.width / canvasScale;
            float canvasHeight = Screen.height / canvasScale;

            float scale = Mathf.Clamp(requestedScale, 0.65f, 1.60f);
            float widthFit = (canvasWidth - 2f * EdgePadding) / PanelWidth;
            float heightFit = (canvasHeight - 2f * EdgePadding) / PanelHeight;
            scale = Mathf.Min(scale, Mathf.Max(0.1f, Mathf.Min(widthFit, heightFit)));
            _panelRoot.localScale = Vector3.one * scale;

            bool right = corner == PanelCorner.TopRight || corner == PanelCorner.BottomRight;
            bool top = corner == PanelCorner.TopRight || corner == PanelCorner.TopLeft;

            var anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
            _panelRoot.anchorMin = anchor;
            _panelRoot.anchorMax = anchor;
            _panelRoot.pivot = anchor;

            float xMargin = Mathf.Clamp(horizontalOffset, EdgePadding,
                Mathf.Max(EdgePadding, canvasWidth - PanelWidth * scale - EdgePadding));
            float yMargin = Mathf.Clamp(verticalOffset, EdgePadding,
                Mathf.Max(EdgePadding, canvasHeight - PanelHeight * scale - EdgePadding));

            _panelRoot.anchoredPosition = new Vector2(
                right ? -xMargin : xMargin,
                top ? -yMargin : yMargin);
        }

        private void HandleButtonClick()
        {
            _onToggle();
        }

        private static RectTransform CreateRect(string name, Transform parent,
            float left, float top, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Image AddImage(GameObject go, Color color, bool raycast)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Font font,
            string value, int fontSize, FontStyle fontStyle, Color color,
            TextAnchor alignment, float x, float y, float width, float height)
        {
            var rect = CreateRect(name, parent, x, y, width, height);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private static Font GetUiFont()
        {
            try
            {
                var builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (builtin != null)
                    return builtin;
            }
            catch (Exception)
            {
                // Some Unity versions no longer ship the built-in Arial resource.
            }

            var loaded = Resources.FindObjectsOfTypeAll<Font>();
            for (int i = 0; i < loaded.Length; i++)
                if (loaded[i] != null)
                    return loaded[i];

            return null;
        }

        public void Dispose()
        {
            if (_canvasObject != null)
                UnityEngine.Object.Destroy(_canvasObject);
        }
    }
}
