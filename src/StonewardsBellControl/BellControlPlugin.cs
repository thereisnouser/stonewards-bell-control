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
        public const string PluginVersion = "0.2.4";

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

        private static bool IsHost => NetworkServer.active && NetworkClient.active;

        private void Awake()
        {
            _anchor = Config.Bind("Appearance", "Anchor", PanelCorner.BottomLeft,
                "Corner used to position the Bell Control panel.");
            _horizontalOffset = Config.Bind("Appearance", "HorizontalOffset", 14,
                "Distance inward from the selected left/right screen edge, in reference-resolution units.");
            _verticalOffset = Config.Bind("Appearance", "VerticalOffset", 92,
                "Distance inward from the selected top/bottom screen edge, in actual screen pixels.");
            _avoidGoldCounter = Config.Bind("Appearance", "AvoidGoldCounter", true,
                "Keep the Bell Control panel below the native gold counter when anchored TopRight.");
            _followEmote = Config.Bind("Appearance", "FollowEmote", true,
                "Position the panel above the actual Emote HUD shortcut while anchored BottomLeft.");
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
            if (_layoutRevision.Value >= 2)
                return;

            // Migrate only historical defaults. User-selected anchors and
            // custom offsets are preserved; FollowEmote can be disabled.
            if (_anchor.Value == PanelCorner.TopRight &&
                _horizontalOffset.Value == 14 &&
                (_verticalOffset.Value == 80 ||
                 _verticalOffset.Value == 132 ||
                 _verticalOffset.Value == 200))
            {
                _anchor.Value = PanelCorner.BottomLeft;
                _verticalOffset.Value = 92;
                Logger.LogInfo("Moved default Bell Control layout above Emote.");
            }
            else if (_anchor.Value == PanelCorner.BottomLeft &&
                _horizontalOffset.Value == 14 && _verticalOffset.Value == 146)
            {
                _verticalOffset.Value = 92;
                Logger.LogInfo("Updated bottom-left fallback offset for compact panel.");
            }

            _layoutRevision.Value = 2;
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

            // The Emote badge is found once and then tracked by its live
            // UI Toolkit bounds, including when the screen resolution changes.
            Rect? emoteBadge = null;
            if (_followEmote.Value && _anchor.Value == PanelCorner.BottomLeft)
            {
                _emoteHud.RefreshIfNeeded();
                Rect bounds;
                if (_emoteHud.TryGetBadgeBounds(out bounds))
                    emoteBadge = bounds;
            }

            _panel.UpdateLayout(_anchor.Value, _horizontalOffset.Value,
                _verticalOffset.Value, _scale.Value, _avoidGoldCounter.Value,
                emoteBadge, _emoteGap.Value, _emoteHorizontalAdjust.Value);
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
