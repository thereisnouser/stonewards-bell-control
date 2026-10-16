# Changelog

## [0.2.5] - Unreleased (experimental)

- Replaced the three-row 222×88 panel with a single-row 248×42 control: BELL, colored guest permission, and an outlined ALLOW/BLOCK button with F8 hint.
- Kept Party HUD-style stepped frame, game font and compact gold/graphite styling.
- Preserved the Emote-relative anchor, fallback position, scaling, and host-only server permissions.

> Source-only update; verify appearance and clickable behavior in-game.

## [0.2.4] - Unreleased (experimental)

- Shrunk the Bell Control Canvas from 278×112 to 222×88 reference UI units, retaining the Party HUD-inspired visuals, status and action button.
- Added an Emote UI Toolkit locator: follow the in-game shortcut's live bounds, including with different resolutions and UI scales.
- Added FollowEmote, EmoteGap and EmoteHorizontalAdjust settings, with a fixed BottomLeft fallback if the native shortcut is unavailable.
- Updated previous default layout positions to a 92px fallback bottom offset while preserving custom positions.
- Added the UIElementsModule reference needed for HUD discovery; bell permission patches and F8 remain unchanged.

> Requires local compilation and gameplay verification before publishing a release.

## [0.2.3] - Unreleased (experimental)

- Relocated the default host panel to BottomLeft (14 horizontal, 146px bottom margin) to avoid the top-right Daily Quest Tracker, gold counter and the top-left Party HUD.
- Added a one-time migration for previous default TopRight coordinates while preserving custom positions.
- Kept all four configurable anchors, old gold-counter protection, visual style and the F8/server enforcement logic unchanged.

> Layout change based on user screenshots; verify the new location in game before release.

## [0.2.2] - Unreleased (experimental)

- Increased the minimum protected top spacing for the native gold HUD from 132px to 200px after a 2048x1152 screenshot still showed overlap.
- Updated the default vertical offset to 200px without overriding custom offsets; protection remains active whenever AvoidGoldCounter is true.

> Screenshot-driven layout correction; requires in-game verification.

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
