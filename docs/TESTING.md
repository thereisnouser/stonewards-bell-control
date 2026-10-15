# Multiplayer test plan — Stonewards 0.2.3

The checks below are **not completed tests**. They should be performed before publishing a binary release.

## Environment

- Host: Stonewards 0.2.3 + BepInEx 5 + `StonewardsBellControl.dll`.
- Guest: unmodded Stonewards 0.2.3, ideally another PC/account.
- Enable or unlock the bell normally in-game.
- Keep a copy of `BepInEx/LogOutput.log` from the host if something fails.

## Checklist

- [ ] Host boots without a plugin load or Harmony patch error.
- [ ] UI panel is visible only to host; F8 changes `BLOCKED` / `ALLOWED`.
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
