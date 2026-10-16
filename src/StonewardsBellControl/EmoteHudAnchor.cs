using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace StonewardsBellControl
{
    /// <summary>
    /// Finds the game's Emote shortcut in its live UI Toolkit HUD and caches
    /// the element. The panel reads its current screen position every frame,
    /// rather than assuming fixed coordinates or UI scale.
    /// </summary>
    internal sealed class EmoteHudAnchor
    {
        // Check promptly while the HUD initializes, then throttle discovery.
        // Avoid scanning the UI tree every frame (it can be quite large).
        private const float InitialRetrySeconds = 0.35f;
        private const float LaterRetrySeconds = 1f;
        private const int MaxProbesPerHostSession = 16;
        private const int MaxVisitedElements = 4000;

        private readonly ManualLogSource _log;
        private TextElement _emoteLabel;
        private VisualElement _emoteBadge;
        private int _probeCount;
        private float _nextProbe;
        private bool _loggedFallback;
        private bool _everLocated;

        public bool WasLocated => _everLocated;

        public EmoteHudAnchor(ManualLogSource log)
        {
            _log = log;
        }

        public void Reset()
        {
            _emoteLabel = null;
            _emoteBadge = null;
            _probeCount = 0;
            _nextProbe = 0f;
            _loggedFallback = false;
            _everLocated = false;
        }

        public void RefreshIfNeeded()
        {
            if (_emoteLabel != null && _emoteLabel.panel != null)
                return;

            _emoteLabel = null;
            _emoteBadge = null;
            if (_probeCount >= MaxProbesPerHostSession ||
                Time.unscaledTime < _nextProbe)
                return;

            _probeCount++;
            _nextProbe = Time.unscaledTime +
                (_probeCount <= 6 ? InitialRetrySeconds : LaterRetrySeconds);
            Discover();
        }

        public bool TryGetBadgeBounds(out Rect bounds)
        {
            bounds = default(Rect);
            if (_emoteLabel == null || _emoteLabel.panel == null)
                return false;

            try
            {
                if (!IsVisible(_emoteLabel))
                    return false;

                var badge = _emoteBadge != null &&
                    _emoteBadge.panel == _emoteLabel.panel
                    ? _emoteBadge : _emoteLabel;
                if (!TryGetScreenRect(badge, out bounds))
                    return false;

                return bounds.width > 1f && bounds.height > 1f;
            }
            catch (Exception)
            {
                _emoteLabel = null;
                _emoteBadge = null;
                return false;
            }
        }

        private void Discover()
        {
            try
            {
                var docs = Resources.FindObjectsOfTypeAll<UIDocument>();
                TextElement best = null;
                VisualElement bestBadge = null;
                float highestScore = float.NegativeInfinity;
                int remaining = MaxVisitedElements;

                foreach (var doc in docs)
                {
                    if (remaining <= 0)
                        break;
                    if (doc == null || !doc.gameObject.scene.IsValid() ||
                        !doc.gameObject.activeInHierarchy)
                        continue;

                    VisualElement root = doc.rootVisualElement;
                    if (root == null || root.panel == null)
                        continue;

                    var stack = new Stack<VisualElement>();
                    stack.Push(root);
                    while (stack.Count != 0 && remaining-- > 0)
                    {
                        var element = stack.Pop();
                        var text = element as TextElement;
                        if (text != null && IsEmoteLabel(text) && IsVisible(text))
                        {
                            Rect textRect;
                            if (TryGetScreenRect(text, out textRect) &&
                                textRect.xMin >= 0f &&
                                textRect.xMin < Screen.width * 0.36f &&
                                textRect.yMin > Screen.height * 0.62f)
                            {
                                float score = (1f - textRect.xMin / Screen.width) +
                                    textRect.yMin / Screen.height;
                                if (HasEmoteNameNearby(text))
                                    score += 2f;
                                if (score > highestScore)
                                {
                                    highestScore = score;
                                    best = text;
                                    bestBadge = FindShortcutBadge(text, textRect);
                                }
                            }
                        }

                        for (int i = element.childCount - 1; i >= 0; i--)
                            stack.Push(element.ElementAt(i));
                    }
                }

                if (best != null)
                {
                    _emoteLabel = best;
                    _emoteBadge = bestBadge ?? best;
                    _everLocated = true;
                    _log.LogInfo("Located Emote HUD. Bell Control will follow its live position.");
                    return;
                }

                if (!_loggedFallback)
                {
                    _loggedFallback = true;
                    _log.LogInfo("Emote HUD is not ready yet; retrying before displaying Bell Control.");
                }
            }
            catch (Exception ex)
            {
                if (!_loggedFallback)
                {
                    _loggedFallback = true;
                    _log.LogWarning("Emote HUD discovery attempt failed: " + ex.Message);
                }
            }
        }

        private static bool IsEmoteLabel(TextElement label)
        {
            string value = label.text;
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
                return false;

            value = value.Trim();
            return value.Equals("Emote", StringComparison.OrdinalIgnoreCase) ||
                value.EndsWith(" Emote", StringComparison.OrdinalIgnoreCase) ||
                (HasEmoteNameNearby(label) &&
                    value.IndexOf("Emote", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool HasEmoteNameNearby(VisualElement element)
        {
            int depth = 0;
            for (var node = element; node != null && depth++ < 4; node = node.parent)
            {
                if (HasEmoteKeyword(node.name))
                    return true;
                foreach (var className in node.GetClasses())
                    if (HasEmoteKeyword(className))
                        return true;
            }
            return false;
        }

        private static bool HasEmoteKeyword(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf("emote", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static VisualElement FindShortcutBadge(TextElement label, Rect labelRect)
        {
            VisualElement best = label;
            int depth = 0;
            for (var parent = label.parent; parent != null && depth++ < 5;
                 parent = parent.parent)
            {
                Rect rect;
                if (!TryGetScreenRect(parent, out rect) ||
                    rect.width > 225f || rect.height > 85f ||
                    rect.width < labelRect.width || rect.height < labelRect.height ||
                    rect.xMin > labelRect.xMin || rect.yMin > labelRect.yMin)
                    break;

                best = parent;
            }
            return best;
        }

        private static bool IsVisible(VisualElement element)
        {
            for (var node = element; node != null; node = node.parent)
            {
                var style = node.resolvedStyle;
                if (style.display == DisplayStyle.None ||
                    style.visibility == UnityEngine.UIElements.Visibility.Hidden ||
                    style.opacity <= 0.01f)
                    return false;
            }
            return element.panel != null;
        }

        private static bool TryGetScreenRect(VisualElement element, out Rect rect)
        {
            rect = default(Rect);
            if (element == null || element.panel == null)
                return false;

            Rect root = element.panel.visualTree.worldBound;
            Rect box = element.worldBound;
            if (root.width <= 0f || root.height <= 0f ||
                box.width <= 0f || box.height <= 0f)
                return false;

            // UI Toolkit uses panel coordinates, while Screen.width/height are
            // physical pixels. Use the same conversion as Daily Quest Tracker.
            float sx = Screen.width / root.width;
            float sy = Screen.height / root.height;
            rect = new Rect((box.xMin - root.xMin) * sx,
                            (box.yMin - root.yMin) * sy,
                            box.width * sx, box.height * sy);
            return rect.xMin >= -1f && rect.yMin >= -1f &&
                rect.xMax <= Screen.width + 1f &&
                rect.yMax <= Screen.height + 1f;
        }
    }
}
