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
        public const string PluginVersion = "0.2.2";

        internal static bool GuestsMayRing { get; private set; }

        private ConfigEntry<PanelCorner> _anchor;
        private ConfigEntry<int> _horizontalOffset;
        private ConfigEntry<int> _verticalOffset;
        private ConfigEntry<float> _scale;
        private ConfigEntry<bool> _avoidGoldCounter;

        private Harmony _harmony;
        private BellControlPanel _panel;
        private bool _wasHosting;
        private bool _uiUnavailable;

        private static bool IsHost => NetworkServer.active && NetworkClient.active;

        private void Awake()
        {
            _anchor = Config.Bind("Appearance", "Anchor", PanelCorner.TopRight,
                "Corner used to position the Bell Control panel.");
            _horizontalOffset = Config.Bind("Appearance", "HorizontalOffset", 14,
                "Distance inward from the selected left/right screen edge, in reference-resolution units.");
            _verticalOffset = Config.Bind("Appearance", "VerticalOffset", 200,
                "Distance inward from the selected top/bottom screen edge, in actual screen pixels.");
            _avoidGoldCounter = Config.Bind("Appearance", "AvoidGoldCounter", true,
                "Keep the Bell Control panel below the native gold counter when anchored TopRight.");
            _scale = Config.Bind("Appearance", "Scale", 1.0f,
                new ConfigDescription("Panel scale (0.65 - 1.60).",
                    new AcceptableValueRange<float>(0.65f, 1.60f)));

            GuestsMayRing = false;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(BellControlPlugin).Assembly);
            Logger.LogInfo("Bell Control loaded. Host: F8 toggles guest access to the bell.");
        }

        private void Update()
        {
            bool hosting = IsHost;

            if (hosting != _wasHosting)
            {
                GuestsMayRing = false;
                _wasHosting = hosting;
                _uiUnavailable = false;
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

            _panel.UpdateLayout(_anchor.Value, _horizontalOffset.Value,
                _verticalOffset.Value, _scale.Value, _avoidGoldCounter.Value);
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
