using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StonewardsBellControl
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class BellControlPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dev.stonewards.bellcontrol";
        public const string PluginName = "Stonewards Bell Control";
        public const string PluginVersion = "0.2.7";

        internal static bool GuestsMayRing { get; private set; }

        private ConfigEntry<PanelCorner> _anchor;
        private ConfigEntry<int> _horizontalOffset;
        private ConfigEntry<int> _verticalOffset;
        private ConfigEntry<float> _scale;
        private ConfigEntry<bool> _avoidGoldCounter;
        private ConfigEntry<int> _layoutRevision;
        private ConfigEntry<bool> _followEmote;
        private ConfigEntry<int> _emoteGap;
        private ConfigEntry<int> _emoteHorizontalAdjust;
        private EmoteHudAnchor _emoteHud;

        private Harmony _harmony;
        private BellControlPanel _panel;
        private bool _wasHosting;
        private bool _uiUnavailable;
        private bool _anchorFallbackLocked;
        private bool _anchorSearchStarted;
        private float _anchorDeadline;

        // Give the in-game Emote HUD time to appear; never show a temporary
        // fixed-position panel while the tracker is still resolving it.
        private const float AnchorResolutionTimeout = 8f;

        private static bool IsHost => NetworkServer.active && NetworkClient.active;

        private void Awake()
        {
            _anchor = Config.Bind("Appearance", "Anchor", PanelCorner.BottomLeft,
                "Corner used to position the Bell Control panel.");
            _horizontalOffset = Config.Bind("Appearance", "HorizontalOffset", 26,
                "Distance inward from the selected left/right screen edge, in reference-resolution units.");
            _verticalOffset = Config.Bind("Appearance", "VerticalOffset", 80,
                "Distance inward from the selected top/bottom screen edge, in actual screen pixels.");
            _avoidGoldCounter = Config.Bind("Appearance", "AvoidGoldCounter", true,
                "Keep the Bell Control panel below the native gold counter when anchored TopRight.");
            _followEmote = Config.Bind("Appearance", "FollowEmote", true,
                "Follow the in-game Emote shortcut. Panel stays hidden until positioned, avoiding startup jumps.");
            _emoteGap = Config.Bind("Appearance", "EmoteGap", 12,
                "Gap in physical pixels between Emote and the Bell Control panel.");
            _emoteHorizontalAdjust = Config.Bind("Appearance", "EmoteHorizontalAdjust", 0,
                "Horizontal adjustment in physical pixels relative to the Emote shortcut.");
            _emoteHud = new EmoteHudAnchor(Logger);

            _scale = Config.Bind("Appearance", "Scale", 1.0f,
                new ConfigDescription("Panel scale (0.65 - 1.60).",
                    new AcceptableValueRange<float>(0.65f, 1.60f)));

            // Upgrade only the historical untouched TopRight defaults. Preserve
            // manual positions so a user-selected layout is never overwritten.
            _layoutRevision = Config.Bind("Appearance", "LayoutRevision", 0,
                "Internal layout migration version. Leave unchanged.");
            MigrateLayoutIfNeeded();

            GuestsMayRing = false;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(BellControlPlugin).Assembly);
            Logger.LogInfo("Bell Control loaded. Host: F8 toggles guest access to the bell.");
        }

        private void MigrateLayoutIfNeeded()
        {
            if (_layoutRevision.Value >= 4)
                return;

            // Historical default positions are migrated, not custom layouts.
            if (_layoutRevision.Value < 2)
            {
                if (_anchor.Value == PanelCorner.TopRight &&
                    _horizontalOffset.Value == 14 &&
                    (_verticalOffset.Value == 80 ||
                     _verticalOffset.Value == 132 ||
                     _verticalOffset.Value == 200))
                {
                    _anchor.Value = PanelCorner.BottomLeft;
                    _horizontalOffset.Value = 26;
                    _verticalOffset.Value = 80;
                }
                else if (_anchor.Value == PanelCorner.BottomLeft &&
                    _horizontalOffset.Value == 14 &&
                    _verticalOffset.Value == 146)
                {
                    _horizontalOffset.Value = 26;
                    _verticalOffset.Value = 80;
                }
            }

            if (_anchor.Value == PanelCorner.BottomLeft &&
                _emoteGap.Value == 12 &&
                _emoteHorizontalAdjust.Value == 0)
            {
                // Convert the unmodified 0.2.4/0.2.5 default or the v0.2.6
                // fixed preset back to Emote-following. Do not alter custom
                // horizontal or vertical offsets.
                bool oldAutoPreset =
                    _horizontalOffset.Value == 14 &&
                    _verticalOffset.Value == 92;
                bool oldFixedPreset =
                    _horizontalOffset.Value == 26 &&
                    _verticalOffset.Value == 80;

                if (oldAutoPreset || oldFixedPreset)
                {
                    _followEmote.Value = true;
                    Logger.LogInfo("Restored automatic Emote positioning with no visible startup jump.");
                }
            }

            _layoutRevision.Value = 4;
            Config.Save();
        }

        private void Update()
        {
            bool hosting = IsHost;

            if (hosting != _wasHosting)
            {
                GuestsMayRing = false;
                _wasHosting = hosting;
                _uiUnavailable = false;
                _anchorFallbackLocked = false;
                _anchorSearchStarted = false;
                _anchorDeadline = 0f;
                if (_emoteHud != null)
                    _emoteHud.Reset();
                DisposePanel();
            }

            if (!hosting)
                return;

            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
                ToggleGuests();

            if (_panel == null || !_panel.IsAlive)
            {
                if (_uiUnavailable)
                    return;

                try
                {
                    _panel = new BellControlPanel(ToggleGuests);
                    _panel.SetAllowed(GuestsMayRing);
                }
                catch (Exception ex)
                {
                    _uiUnavailable = true;
                    DisposePanel();
                    Logger.LogError("Failed to create Bell Control Canvas; F8 still works: " + ex);
                    return;
                }
            }

            bool follow = _followEmote.Value &&
                _anchor.Value == PanelCorner.BottomLeft;
            Rect? emoteBadge = null;
            bool show = true;

            if (follow && !_anchorFallbackLocked)
            {
                // Hosting can begin in a pre-game lobby long before Emote
                // exists. Start the timeout only after gameplay input is live.
                var input = InputManager.Instance;
                bool gameReady = input != null &&
                    (input.CurrentInputState == InputManager.InputState.Gameplay ||
                     input.CurrentInputState == InputManager.InputState.Dead);
                if (!gameReady && !_anchorSearchStarted)
                {
                    show = false;
                }
                else
                {
                    if (!_anchorSearchStarted)
                    {
                        _anchorSearchStarted = true;
                        _anchorDeadline = Time.unscaledTime + AnchorResolutionTimeout;
                    }
                    _emoteHud.RefreshIfNeeded();
                    Rect bounds;
                    if (_emoteHud.TryGetBadgeBounds(out bounds))
                    {
                        emoteBadge = bounds;
                    }
                    else if (_emoteHud.WasLocated)
                    {
                        // The shortcut can disappear in menus. Hide the
                        // panel, rather than snapping back to manual offsets.
                        show = false;
                    }
                    else if (Time.unscaledTime < _anchorDeadline)
                    {
                        show = false;
                    }
                    else
                    {
                        // If the Emote UI is missing entirely, show the fixed
                        // fallback and never jump to the tracker mid-session.
                        _anchorFallbackLocked = true;
                        Logger.LogWarning("Emote HUD unavailable; using fixed layout for this session.");
                    }
                }
            }

            if (show)
            {
                _panel.UpdateLayout(_anchor.Value, _horizontalOffset.Value,
                    _verticalOffset.Value, _scale.Value, _avoidGoldCounter.Value,
                    emoteBadge, _emoteGap.Value, _emoteHorizontalAdjust.Value);
            }
            _panel.SetVisible(show);
            _panel.UpdateInteraction();
        }

        private void ToggleGuests()
        {
            if (!IsHost)
                return;

            GuestsMayRing = !GuestsMayRing;
            if (_panel != null && _panel.IsAlive)
                _panel.SetAllowed(GuestsMayRing);

            Logger.LogInfo(GuestsMayRing
                ? "Guest access to the bell ENABLED."
                : "Guest access to the bell DISABLED.");
        }

        private void DisposePanel()
        {
            if (_panel == null)
                return;

            _panel.Dispose();
            _panel = null;
        }

        private void OnDestroy()
        {
            DisposePanel();
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }

        // Preserve the previously tested host-only enforcement: remote Mirror
        // dispatchers are rejected while locked; host-local calls remain unchanged.
        [HarmonyPatch(typeof(Bell), "InvokeUserCode_CmdTryRing__FirstPersonController")]
        private static class GuestBellRingPatch
        {
            [HarmonyPrefix]
            private static bool Prefix() => !NetworkServer.active || GuestsMayRing;
        }

        [HarmonyPatch(typeof(Bell), "InvokeUserCode_CmdStartAnimation__FirstPersonController")]
        private static class GuestBellAnimationPatch
        {
            [HarmonyPrefix]
            private static bool Prefix() => !NetworkServer.active || GuestsMayRing;
        }
    }
}
