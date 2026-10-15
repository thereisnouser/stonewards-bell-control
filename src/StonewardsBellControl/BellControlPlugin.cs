using BepInEx;
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
        public const string PluginVersion = "0.1.0";

        // A new host session starts with guest ringing disabled.
        internal static bool GuestsMayRing { get; private set; }

        private Harmony _harmony;
        private bool _wasHosting;

        private static bool IsHost => NetworkServer.active && NetworkClient.active;

        private void Awake()
        {
            GuestsMayRing = false;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(BellControlPlugin).Assembly);
            Logger.LogInfo("Bell Control loaded. Host: F8 toggles guest access to the bell.");
        }

        private void Update()
        {
            bool hosting = IsHost;

            // Permissions never leak into the next hosted session.
            if (hosting != _wasHosting)
            {
                GuestsMayRing = false;
                _wasHosting = hosting;
            }

            if (hosting && Keyboard.current != null &&
                Keyboard.current.f8Key.wasPressedThisFrame)
            {
                ToggleGuests();
            }
        }

        private void OnGUI()
        {
            if (!IsHost)
                return;

            // Compact host-only control. F8 also works while the cursor is locked.
            const float width = 275f;
            float x = Screen.width - width - 12f;
            GUI.Box(new Rect(x, 12f, width, 67f), "BELL CONTROL (HOST)");

            string state = GuestsMayRing ? "Guests: ALLOWED" : "Guests: BLOCKED";
            GUI.Label(new Rect(x + 10f, 33f, 135f, 22f), state);

            string action = GuestsMayRing ? "Block (F8)" : "Allow (F8)";
            if (GUI.Button(new Rect(x + 147f, 32f, 116f, 27f), action))
                ToggleGuests();
        }

        private void ToggleGuests()
        {
            if (!IsHost)
                return;

            GuestsMayRing = !GuestsMayRing;
            Logger.LogInfo(GuestsMayRing
                ? "Guest access to the bell ENABLED."
                : "Guest access to the bell DISABLED.");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }

        // In Stonewards 0.2.3 Bell.CmdTryRing and Bell.CmdStartAnimation
        // execute UserCode_... directly for the host (isServer && isClient).
        // Networked guest commands instead pass through InvokeUserCode_... .
        // Blocking the dispatchers therefore blocks guests, not the host.

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
