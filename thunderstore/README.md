# Bell Control 🔔

**Control who can ring the wave bell in Stonewards.** The host can lock or unlock the bell for other players without affecting their own ability to ring it.

## Features

- **Host-only controls:** press **F8** or use the compact **ALLOW / BLOCK** button.
- **Guests are blocked by default** whenever a new host session starts.
- **Server-side enforcement:** guests cannot start the bell while blocked, even if they do not have this mod.
- **Compact native-style HUD:** a one-line panel that automatically positions itself above the in-game **Emote** prompt.
- **Customizable:** adjust the panel's scale, position and Emote gap using BepInEx settings.

## How to use

1. Install the package using r2modman or Thunderstore Mod Manager.
2. **The host** starts Stonewards through the mod manager and hosts a session.
3. Press **F8** to allow guests to ring the bell; press it again to block them.

The host can always ring. **Guests do not need to install Bell Control.** The panel is visible only to the host.

## Position and configuration

By default, the small panel follows Emote at the lower left. It stays hidden until Emote loads so it does not jump between positions. If Emote cannot be detected, it uses a fixed fallback. To use manual placement:

```ini
[Appearance]
Anchor = BottomLeft
FollowEmote = false
HorizontalOffset = 26
VerticalOffset = 80
Scale = 1
```

Configuration file: `BepInEx/config/dev.stonewards.bellcontrol.cfg`. Other available settings include `EmoteGap` and `EmoteHorizontalAdjust`.

## Compatibility

Designed for **Stonewards 0.2.3** and **BepInEx 5**. This is a **host-side mod**; it does not alter the normal countdown. Guests may still see the vanilla bell interaction even while the host blocks their request.

**Note:** the default **F8** shortcut may overlap with hotkeys used by other mods, including Party HUD's build inspector. Rebind the other mod's shortcut if needed.

[Source code](https://github.com/thereisnouser/stonewards-bell-control) · [Report an issue](https://github.com/thereisnouser/stonewards-bell-control/issues)
