# Multiplayer test plan — Stonewards 0.2.3

The checks below are **not completed tests**. They should be performed before publishing a binary release.

## Environment

- Host: Stonewards 0.2.3 + BepInEx 5 + `StonewardsBellControl.dll`.
- Guest: unmodded Stonewards 0.2.3, ideally another PC/account.
- Enable or unlock the bell normally in-game.
- Keep a copy of `BepInEx/LogOutput.log` from the host if something fails.

## Checklist

- [ ] Host boots without a plugin load or Harmony patch error.
- [ ] Native Canvas panel is visible only to host; F8 changes `BLOCKED` / `ALLOWED`.
- [ ] Panel uses Party HUD's stepped border, gold button outline and native Jersey10 typeface.
- [ ] Fresh profile: Bell Control appears immediately in a fixed BottomLeft position (horizontal 26, vertical 80) above Emote, without any delayed repositioning.
- [ ] Existing default v0.2.5 profile migrates to fixed placement on first startup; intentionally customized layouts are unchanged.
- [ ] With FollowEmote explicitly enabled, Bell Control dynamically tracks the Emote shortcut, preserving the configured EmoteGap.
- [ ] Single-row 248×42 reference-unit panel reads BELL, BLOCKED/ALLOWED, ALLOW/BLOCK and F8 without wrapping or clipping at 1920×1080 and 2560×1440.
- [ ] New shorter panel is still positioned directly above Emote and does not obstruct the game shortcut.
- [ ] Changing game resolution or UI scale keeps the fixed BottomLeft placement on-screen, near Emote; follow mode additionally tracks the actual button.
- [ ] With FollowEmote disabled (default), Bell Control uses BottomLeft (26 reference units from left, 80 physical pixels from bottom).
- [ ] If the Emote element is not found, the fallback works without crashes or frequent UI rescanning.
- [ ] Previous default BottomLeft (14, 146) and older default TopRight offsets migrate to the new fallback without manual editing.
- [ ] User-customized anchor or offsets are preserved during the one-time migration.
- [ ] With `Anchor = TopRight`, `VerticalOffset = 80`, and `AvoidGoldCounter = true`, the gold count and its full backing panel remain visible, with a clear gap below it.
- [ ] With `AvoidGoldCounter = false`, the panel can be positioned closer to the top with VerticalOffset.
- [ ] With the cursor locked, the new Canvas does not intercept mouse-looking or gameplay interactions.
- [ ] Button changes state with F8.
- [ ] On-screen button toggles state with cursor visible, without intercepting other UI clicks.
- [ ] For TopRight anchor, lowering VerticalOffset moves the panel up.
- [ ] Panel remains on-screen when changing resolution, anchor and scale.
- [ ] Entering/leaving hosted sessions creates/destroys the panel without duplicates.
- [ ] With guests blocked, guest holds the bell: wave timer is not shortened.
- [ ] With guests blocked, host rings: bell works as in the unmodded game.
- [ ] After F8 allows guests, a guest can ring and start the wave normally.
- [ ] After F8 blocks guests again, a new guest attempt is rejected.
- [ ] Blocking while guest is mid-hold does not start a wave early.
- [ ] Guest disconnects and rejoins: permission is unchanged.
- [ ] Host returns to menu and hosts a new session: guests are blocked again.
- [ ] Natural end of countdown and normal wave progression are unaffected.
- [ ] No unhandled exceptions in BepInEx logs.

## Bug report information

For a bug, include game version, mod version, BepInEx version, whether you hosted, reproduction steps, expected/actual behavior, and relevant log excerpts (remove private account/session identifiers).
