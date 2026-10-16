# Stonewards Bell Control 🔔

**Host-only bell permissions for Stonewards.** A small [BepInEx 5](https://github.com/BepInEx/BepInEx) + [Harmony](https://github.com/pardeike/Harmony) plugin that lets the host decide when other players are allowed to ring the wave bell.

> [!IMPORTANT]
> **Experimental.** Developed for **Stonewards 0.2.3**. The previous Canvas UI was shown in user gameplay screenshots. The new Emote-relative position and compact size still need in-game confirmation, as does multiplayer regression testing.

## Features

- 🔒 **Guests blocked by default** when a host session starts.
- 👑 **The host can always ring** the bell.
- 🔓 **F8** toggles guest permissions; a host-only **Allow / Block** button appears in a native Unity Canvas.
- 🎨 Party HUD-style stepped frame, graphite action button, muted gold and burgundy palette, and Stonewards' native Jersey10 font where available.
- 🧭 **Follows the in-game Emote prompt** near the lower-left corner using its live UI Toolkit bounds, with a small configurable gap. Falls back to BottomLeft when Emote cannot be detected. This stays clear of Party HUD and the top-right gold/daily quest panels.
- 📏 Compact control: **222 × 88** reference UI units instead of 278 × 112.
- 📐 Configurable **FollowEmote**, **EmoteGap**, **EmoteHorizontalAdjust**, **Anchor**, edge offsets, and **Scale** in the BepInEx config.
- 🛡️ **Server-side enforcement:** guest ring commands are ignored when blocked, even if guests do not have the plugin.
- 🔄 **Resets on each hosted session:** returning to the menu / rehosting restores the blocked setting.
- ⏱️ The regular countdown is not intentionally modified.

**Compatibility:** designed for Stonewards **0.2.3**. New game versions may require new patches.

## Requirements

- Stonewards 0.2.3 on Windows
- BepInEx **5** installed in the game directory **or in the active r2modman profile**
- .NET SDK supporting `netstandard2.1` (required to *build*; not required to run a built DLL)
- Locally installed game assemblies (`Stonewards_Data/Managed`) including `UnityEngine.UI.dll`, `UnityEngine.UIModule.dll`, `UnityEngine.UIElementsModule.dll` and `UnityEngine.TextRenderingModule.dll`, plus BepInEx core DLLs

This repository does **not** contain or distribute copyrighted game DLLs.

## Build and install (r2modman)

Run PowerShell from the cloned repository directory:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-and-install.ps1 `
  -GameDir "F:\Steam\steamapps\common\Stonewards" `
  -ProfileDir "C:\Users\user\AppData\Roaming\r2modmanPlus-local\Stonewards\profiles\stonewards-bell-control"
```

**Adjust both paths to your installation and active r2modman profile.** The script finds Unity game DLLs in `GameDir\Stonewards_Data\Managed`, BepInEx assemblies in `ProfileDir\BepInEx\core`, builds the plugin, and installs it to:

```text
<ProfileDir>/BepInEx/plugins/StonewardsBellControl/StonewardsBellControl.dll
```

Then launch the game **through the same r2modman profile**.

## Panel appearance and positioning

The UI uses a real Unity Canvas (uGUI), not `OnGUI`. It is visible only while hosting a session. On the default BottomLeft anchor it looks up the **actual Emote shortcut** in the live UI Toolkit hierarchy and positions itself immediately above that element, with a gap. When the game rescales or moves the HUD, Bell Control follows it. The upper-left remains available to Party HUD, and the upper-right to gold and Daily Quest Tracker. If Emote is not found (e.g., UI changed after an update), the panel uses the safe BottomLeft offsets instead.

Open the generated `BepInEx/config/dev.stonewards.bellcontrol.cfg` in your active r2modman profile:

```ini
[Appearance]
Anchor = BottomLeft
FollowEmote = true
EmoteGap = 12
EmoteHorizontalAdjust = 0
HorizontalOffset = 14
VerticalOffset = 92
AvoidGoldCounter = true
Scale = 1
```

- `FollowEmote`: when `true` and the anchor is `BottomLeft`, follow the actual Emote HUD shortcut, using its screen coordinates.
- `EmoteGap`: physical pixel gap between the top of Emote and the bottom of Bell Control.
- `EmoteHorizontalAdjust`: physical pixel adjustment relative to the Emote button's left edge, if needed.
- `Anchor`: `TopRight`, `TopLeft`, `BottomRight` or `BottomLeft`. Other corners always use manual edge offsets.
- `HorizontalOffset`: distance **inward** from the selected left/right edge when Emote following is disabled or unavailable.
- `VerticalOffset`: fallback physical pixel distance **inward** from the selected top/bottom edge. For top anchors, **smaller moves the panel up**; for bottom anchors, **larger moves it up**.
- `AvoidGoldCounter`: when `true` and `Anchor = TopRight`, the panel's top edge is never closer than **200 physical pixels** to the screen's top, so the native gold counter remains visible. Older profiles with `VerticalOffset = 80` are automatically protected. Disable to position the panel closer to the top edge.
- `Scale`: from `0.65` to `1.60`. The panel is also reduced if needed to stay on-screen.

Existing old default TopRight positions (offset 14 with vertical offset 80, 132 or 200) and the previous default BottomLeft position (offset 14, vertical 146) are migrated once to the new BottomLeft fallback offset (92). Custom anchors and nonstandard offsets are preserved. The internal `LayoutRevision` option tracks this migration; do not edit it. Changes are picked up while the game is running when BepInEx updates its configuration; restarting the game reliably applies manual file edits. Offsets are clamped only to keep the panel on-screen.

**Controls:** F8 always works for the host. The on-screen button can be clicked when the game cursor is available.

To compile without installing, omit `-ProfileDir` when BepInEx lives inside the game directory, or use `-NoInstall` with `-ProfileDir`. You can also set `-BepInExDir` explicitly when dependencies are in a different directory.

## Build only (BepInEx installed into game directory)

```powershell
.\scripts\build.ps1 -GameDir "F:\Steam\steamapps\common\Stonewards"
```

The build output is `src/StonewardsBellControl/bin/Release/netstandard2.1/StonewardsBellControl.dll`.

## Manual installation

Copy the built `StonewardsBellControl.dll` into the **host's active** `BepInEx/plugins` directory (the r2modman profile if you use r2modman). Launch the game, host a session and press **F8** to toggle guest bell permissions. The panel is only displayed for the host.

**Guests do not need the plugin.** Remove the DLL to uninstall. Do not deploy a build into both the game directory and a mod manager profile at the same time.

## How it works

The game sends guest bell interactions to the host through Mirror commands. For version 0.2.3, the plugin patches these server-side dispatch methods:

- `Bell.InvokeUserCode_CmdTryRing__FirstPersonController`
- `Bell.InvokeUserCode_CmdStartAnimation__FirstPersonController`

With guest access blocked, those calls are ignored. Host-local calls use the game's local `UserCode_...` path, so they are unaffected. The mod does **not** change the countdown or grant network authority to guests.

## Known limitations

- Guests still see vanilla bell interaction text and progress UI, but their ring request will be denied while blocked.
- The host's on-screen button requires an active UI cursor (F8 works regardless of cursor lock). While the cursor is locked, the mod disables its UI raycaster to avoid interfering with mouse look.
- Native uGUI dependencies must be present in the installed game.
- If guests began an animation while allowed, locking them out mid-interaction may leave a brief visual mismatch. The final ring request remains blocked.
- This does **not** prevent other modded clients or game mechanics from invoking unrelated wave-start logic.
- BepInEx may be detected by the game; modding can affect leaderboard eligibility. Review the game's current rules before using mods for online play.
- The Emote UI lookup is heuristic and needs testing on the current game build; if it cannot identify the element, fixed BottomLeft fallback positioning remains available.

See [multiplayer test plan](docs/TESTING.md) before publishing a release.

## Project status

**v0.2.4 — experimental compact Emote-following layout.** No compiled release asset is included. If the game updates, inspect the new assembly before claiming compatibility.

## License

[MIT](LICENSE) — applies to the mod source code only. Stonewards, Unity, Mirror, BepInEx and Harmony belong to their respective owners.
