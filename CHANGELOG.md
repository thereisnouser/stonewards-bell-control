# Changelog

## [0.2.1] - Unreleased (experimental)

- Replaced the flat rectangular Canvas skin with pixel-stepped game-style frames and outlined button matching Party HUD.
- Prefer Stonewards' built-in Jersey10 font, with a fallback until that font is available.
- Added an `AvoidGoldCounter` setting (enabled by default): protect the gold HUD when anchored TopRight, even if the saved vertical offset is small.
- Reduced the panel height, kept host-only F8 and server-side bell enforcement untouched.
- Disabled UI raycasting while the mouse cursor is locked to prevent gameplay input interference.

> Source-only change. Compile and test in the game before releasing.

## [0.2.0] - Unreleased (experimental)

- Replaced the original `OnGUI` panel with a native Unity Canvas / uGUI panel.
- Matched Party HUD's dark, burgundy and gold visual palette.
- Added BepInEx appearance settings: anchor, horizontal/vertical offsets, and scale.
- Made offsets relative to the chosen screen edge and kept the entire panel on-screen.
- Kept F8, host-only controls and server-side guest restrictions unchanged.
- Added native UI dependencies to the build scripts.

> Requires build and in-game verification with Stonewards 0.2.3. No binary release yet.

## [0.1.0] - Unreleased (experimental)

- Added host-only F8 and on-screen controls for guest bell access.
- Blocked remote bell ringing and start-animation commands when guest access is disabled.
- Reset guest permissions to blocked when starting a new host session.
- Added build and multiplayer-test instructions.

> Initial source implementation only. Compilation and in-game testing are still pending.
