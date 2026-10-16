using System;
using System.Collections.Generic;
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
    /// Party HUD-inspired single-row Unity Canvas: stepped border, burgundy
    /// title accent, graphite action button and the game's Jersey10 font.
    /// It remains independent
    /// of Party HUD and does not distribute game textures or font files.
    /// </summary>
    internal sealed class BellControlPanel : IDisposable
    {
        private const float Width = 248f;
        private const float Height = 42f;
        private const float EdgePadding = 6f;
        // The vanilla gold display is taller than its visible numeric row:
        // reserve enough space for its whole backing panel and an extra gap.
        // This is a minimum independent of old saved VerticalOffset values.
        private const float GoldSafeTopPixels = 200f;

        // Same base colors and button treatments as PartyHudPlugin.cs.
        private static readonly Color PanelFrame = new Color(0.72f, 0.64f, 0.48f, 1f);
        private static readonly Color PanelBackground = new Color(19f / 255f, 23f / 255f, 32f / 255f, 0.97f);
        private static readonly Color PanelHeader = new Color(76f / 255f, 46f / 255f, 48f / 255f, 1f);
        private static readonly Color Divider = new Color(0.71f, 0.63f, 0.48f, 0.38f);
        private static readonly Color Heading = new Color(0.98f, 0.79f, 0.51f, 1f);
        private static readonly Color TextColor = new Color(0.98f, 0.90f, 0.76f, 1f);
        private static readonly Color Green = new Color(0.47f, 0.87f, 0.56f, 1f);
        private static readonly Color Red = new Color(1f, 0.52f, 0.50f, 1f);
        private static readonly Color ButtonFill = new Color(55f / 255f, 59f / 255f, 69f / 255f, 1f);
        private static readonly Color ButtonActive = new Color(79f / 255f, 57f / 255f, 61f / 255f, 1f);
        private static readonly Color ButtonOutline = new Color(178f / 255f, 153f / 255f, 95f / 255f, 1f);

        private readonly Action _onToggle;
        private readonly GameObject _canvasObject;
        private readonly Canvas _canvas;
        private readonly GraphicRaycaster _raycaster;
        private readonly RectTransform _panelRoot;
        private readonly List<Text> _labels = new List<Text>();
        private readonly Text _statusText;
        private readonly Text _actionText;
        private readonly Text _shortcutText;
        private readonly Image _statusBar;
        private readonly Image _buttonFill;
        private readonly Image _buttonFillCenter;
        private float _nextFontCheck;
        private bool _gameFontApplied;

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

            _raycaster = _canvasObject.AddComponent<GraphicRaycaster>();
            _raycaster.enabled = false;

            Font font = FindGameFont();
            _gameFontApplied = font != null;
            if (font == null)
                font = GetFallbackFont();
            if (font == null)
                throw new InvalidOperationException("Neither a Stonewards font nor a fallback UI font was found.");

            _panelRoot = CreateRect("Bell Control Window", _canvasObject.transform,
                0f, 0f, Width, Height);

            // Compact single-row panel. Keep Party HUD's pixel-stepped border,
            // a burgundy title accent and the graphite/gold action control.
            AddSolid("Background Top", _panelRoot, 7, 0, Width - 14, Height, PanelBackground);
            AddSolid("Background Center", _panelRoot, 0, 7, Width, Height - 14, PanelBackground);
            AddSolid("Burgundy Title Top", _panelRoot, 7, 2, 51, Height - 4, PanelHeader);
            AddSolid("Burgundy Title Center", _panelRoot, 2, 7, 56, Height - 14, PanelHeader);
            DrawWindowFrame(_panelRoot, Width, Height);

            AddText("Title", _panelRoot, font, "BELL", 17, true,
                Heading, TextAnchor.MiddleLeft, 12, 5, 42, 31);
            AddSolid("Divider", _panelRoot, 59, 10, 1, 22, Divider);

            _statusBar = AddSolid("Status Accent", _panelRoot, 65, 13, 3, 17, Red);
            _statusText = AddText("Status", _panelRoot, font, "BLOCKED", 16, true,
                Red, TextAnchor.MiddleCenter, 71, 5, 69, 31);

            // Allow/Block is the only clickable part of the bar.
            var action = CreateRect("Guest Permission Button", _panelRoot,
                147, 7, 92, 28);
            _buttonFill = AddSolid("Button Top", action, 3, 0, 86, 28, ButtonFill);
            _buttonFillCenter = AddSolid("Button Center", action, 0, 3, 92, 22, ButtonFill);
            DrawButtonFrame(action, 92, 28);
            _buttonFill.raycastTarget = true;

            var button = action.gameObject.AddComponent<Button>();
            button.targetGraphic = _buttonFill;
            // Game-style fill: no Unity ColorTint state changes.
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnButtonClicked);

            _actionText = AddText("Action", action, font, "ALLOW", 16, true,
                TextColor, TextAnchor.MiddleCenter, 5, 0, 57, 28);
            _shortcutText = AddText("Shortcut", action, font, "F8", 12, true,
                Heading, TextAnchor.MiddleCenter, 64, 0, 22, 28);
        }

        public void SetAllowed(bool allowed)
        {
            if (!IsAlive)
                return;

            _statusText.text = allowed ? "ALLOWED" : "BLOCKED";
            _statusText.color = allowed ? Green : Red;
            _statusBar.color = allowed ? Green : Red;
            _actionText.text = allowed ? "BLOCK" : "ALLOW";
            _shortcutText.text = "F8";
            _buttonFill.color = allowed ? ButtonActive : ButtonFill;
            _buttonFillCenter.color = allowed ? ButtonActive : ButtonFill;
        }

        public void UpdateInteraction()
        {
            if (!IsAlive)
                return;

            // Do not capture game mouse input while the first-person cursor is locked.
            _raycaster.enabled = Cursor.visible && Cursor.lockState == CursorLockMode.None;

            // The native UI font may load after the plugin or scene initializes.
            if (!_gameFontApplied && Time.unscaledTime >= _nextFontCheck)
            {
                _nextFontCheck = Time.unscaledTime + 4f;
                Font font = FindGameFont();
                if (font != null)
                {
                    for (int i = 0; i < _labels.Count; i++)
                        if (_labels[i] != null)
                        {
                            _labels[i].font = font;
                            _labels[i].fontStyle = FontStyle.Normal;
                        }
                    _gameFontApplied = true;
                }
            }
        }

        /// <summary>Margins are measured from the selected physical screen edge.</summary>
        public void UpdateLayout(PanelCorner corner, int horizontalOffset,
            int verticalOffset, float requestedScale, bool avoidGoldCounter,
            Rect? emoteBadge, int emoteGapPixels, int emoteHorizontalAdjustPixels)
        {
            if (!IsAlive)
                return;

            float canvasScale = Mathf.Max(0.01f, _canvas.scaleFactor);
            float canvasWidth = Screen.width / canvasScale;
            float canvasHeight = Screen.height / canvasScale;
            float panelScale = Mathf.Clamp(requestedScale, 0.65f, 1.6f);
            panelScale = Mathf.Min(panelScale,
                Mathf.Max(0.1f, (canvasWidth - EdgePadding * 2f) / Width),
                Mathf.Max(0.1f, (canvasHeight - EdgePadding * 2f) / Height));
            _panelRoot.localScale = Vector3.one * panelScale;

            bool right = corner == PanelCorner.TopRight ||
                corner == PanelCorner.BottomRight;
            bool top = corner == PanelCorner.TopRight ||
                corner == PanelCorner.TopLeft;
            Vector2 anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
            _panelRoot.anchorMin = anchor;
            _panelRoot.anchorMax = anchor;
            _panelRoot.pivot = anchor;

            float maxX = Mathf.Max(EdgePadding, canvasWidth -
                Width * panelScale - EdgePadding);
            float maxY = Mathf.Max(EdgePadding, canvasHeight -
                Height * panelScale - EdgePadding);

            // Follow the live Emote shortcut and preserve a physical-pixel gap.
            // This does not assume the HUD's default scale or a fixed resolution.
            // The old bottom-left edge offsets remain a safe fallback.
            if (corner == PanelCorner.BottomLeft && emoteBadge.HasValue)
            {
                Rect badge = emoteBadge.Value;
                float physicalX = badge.xMin + emoteHorizontalAdjustPixels;
                float physicalBottom = Screen.height - badge.yMin +
                    Mathf.Max(0, emoteGapPixels);
                float followX = Mathf.Clamp(physicalX / canvasScale,
                    EdgePadding, maxX);
                float followY = Mathf.Clamp(physicalBottom / canvasScale,
                    EdgePadding, maxY);
                _panelRoot.anchoredPosition = new Vector2(followX, followY);
                return;
            }

            // Manual offset remains measured from the selected edge.
            float x = Mathf.Clamp(Mathf.Max(0, horizontalOffset),
                EdgePadding, maxX);
            float minimumTop = top && right && avoidGoldCounter
                ? GoldSafeTopPixels : EdgePadding * canvasScale;
            float yPixels = Mathf.Max(Mathf.Max(0, verticalOffset), minimumTop);
            float y = Mathf.Clamp(yPixels / canvasScale, EdgePadding, maxY);
            _panelRoot.anchoredPosition = new Vector2(
                right ? -x : x, top ? -y : y);
        }

        private static void DrawWindowFrame(Transform parent, float w, float h)
        {
            const float e = 2f;
            AddSolid("Frame Top", parent, 8, 0, w - 16, e, PanelFrame);
            AddSolid("Top Left Step 1", parent, 5, 2, 3, e, PanelFrame);
            AddSolid("Top Left Step 2", parent, 2, 5, 3, e, PanelFrame);
            AddSolid("Left Border", parent, 0, 7, e, h - 14, PanelFrame);
            AddSolid("Top Right Step 1", parent, w - 8, 2, 3, e, PanelFrame);
            AddSolid("Top Right Step 2", parent, w - 5, 5, 3, e, PanelFrame);
            AddSolid("Right Border", parent, w - e, 7, e, h - 14, PanelFrame);
            AddSolid("Bottom Border", parent, 8, h - e, w - 16, e, PanelFrame);
            AddSolid("Bottom Left Step 1", parent, 5, h - 4, 3, e, PanelFrame);
            AddSolid("Bottom Left Step 2", parent, 2, h - 7, 3, e, PanelFrame);
            AddSolid("Bottom Right Step 1", parent, w - 8, h - 4, 3, e, PanelFrame);
            AddSolid("Bottom Right Step 2", parent, w - 5, h - 7, 3, e, PanelFrame);
        }

        private static void DrawButtonFrame(Transform parent, float w, float h)
        {
            const float e = 2f;
            AddSolid("Button Top Frame", parent, 4, 0, w - 8, e, ButtonOutline);
            AddSolid("Button Bottom Frame", parent, 4, h - e, w - 8, e, ButtonOutline);
            AddSolid("Button Left Frame", parent, 0, 4, e, h - 8, ButtonOutline);
            AddSolid("Button Right Frame", parent, w - e, 4, e, h - 8, ButtonOutline);
            AddSolid("Button TL", parent, 2, 2, 2, e, ButtonOutline);
            AddSolid("Button TR", parent, w - 4, 2, 2, e, ButtonOutline);
            AddSolid("Button BL", parent, 2, h - 4, 2, e, ButtonOutline);
            AddSolid("Button BR", parent, w - 4, h - 4, 2, e, ButtonOutline);
        }

        private static RectTransform CreateRect(string name, Transform parent,
            float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static Image AddSolid(string name, Transform parent,
            float x, float y, float w, float h, Color color)
        {
            var img = CreateRect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Text AddText(string name, Transform parent, Font font, string value,
            int size, bool bold, Color color, TextAnchor align,
            float x, float y, float w, float h)
        {
            var text = CreateRect(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            // Native Jersey10 fonts carry the intended weight in the font asset.
            text.fontStyle = bold && !font.name.StartsWith("Jersey10", StringComparison.OrdinalIgnoreCase)
                ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            _labels.Add(text);
            return text;
        }

        private static Font FindGameFont()
        {
            Font alternative = null;
            foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font == null)
                    continue;
                if (font.name == "Jersey10-Regular")
                    return font;
                if (font.name == "Jersey10-Regular2")
                    alternative = font;
            }
            return alternative;
        }

        private static Font GetFallbackFont()
        {
            try
            {
                var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (font != null)
                    return font;
            }
            catch (Exception)
            {
                // Old/new Unity runtimes may not include the default resource.
            }

            foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
                if (font != null)
                    return font;

            return null;
        }

        private void OnButtonClicked() => _onToggle();

        public void Dispose()
        {
            if (_canvasObject != null)
                UnityEngine.Object.Destroy(_canvasObject);
        }
    }
}
