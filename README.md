# Stonewards Bell Control 🔔

**Host-only bell permissions for Stonewards.** A small [BepInEx 5](https://github.com/BepInEx/BepInEx) + [Harmony](https://github.com/pardeike/Harmony) plugin that lets the host decide when other players are allowed to ring the wave bell.

> [!IMPORTANT]
> **Experimental — not yet tested in a live game.** The implementation was developed against the supplied `Assembly-CSharp.dll` from **Stonewards 0.2.3**. It has not been compiled against a full game installation or multiplayer-tested yet. Please test it in a private session before relying on it.

## Features

- 🔒 **Guests blocked by default** when a host session starts.
- 👑 **The host can always ring** the bell.
- 🔓 **F8** toggles guest permissions; a host-only **Allow / Block** button appears in the upper-right corner.
- 🛡️ **Server-side enforcement:** guest ring commands are ignored when blocked, even if guests do not have the plugin.
- 🔄 **Resets on each hosted session:** returning to the menu / rehosting restores the blocked setting.
- ⏱️ The regular countdown is not intentionally modified.

**Compatibility:** designed for Stonewards **0.2.3**. New game versions may require new patches.

## Requirements

- Stonewards 0.2.3 on Windows
- BepInEx **5** installed in the game directory **or in the active r2modman profile**
- .NET SDK supporting `netstandard2.1` (required to *build*; not required to run a built DLL)
- Locally installed game assemblies (`Stonewards_Data/Managed`) and BepInEx core DLLs (referenced during compilation)

This repository does **not** contain or distribute copyrighted game DLLs.

## Build and install (r2modman)

Run PowerShell from the cloned repository directory:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-and-install.ps1 `
  -GameDir "F:\Steam\steamapps\common\Stonewards" `
  -ProfileDir "C:\Users\user\AppData\Roaming\r2modmanPlus-local\Stonewards\profiles\stonewards"
```

**Adjust both paths to your installation and active r2modman profile.** The script finds Unity game DLLs in `GameDir\Stonewards_Data\Managed`, BepInEx assemblies in `ProfileDir\BepInEx\core`, builds the plugin, and installs it to:

```text
<ProfileDir>/BepInEx/plugins/StonewardsBellControl/StonewardsBellControl.dll
```

Then launch the game **through the same r2modman profile**.

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
- If guests began an animation while allowed, locking them out mid-interaction may leave a brief visual mismatch. The final ring request remains blocked.
- This does **not** prevent other modded clients or game mechanics from invoking unrelated wave-start logic.
- BepInEx may be detected by the game; modding can affect leaderboard eligibility. Review the game's current rules before using mods for online play.
- Only the supplied managed game assembly was available during development; runtime behavior has **not** been verified.

See [multiplayer test plan](docs/TESTING.md) before publishing a release.

## Project status

**v0.1.0 — experimental source release.** No compiled release asset is included. If the game updates, inspect the new assembly before claiming compatibility.

## License

[MIT](LICENSE) — applies to the mod source code only. Stonewards, Unity, Mirror, BepInEx and Harmony belong to their respective owners.
