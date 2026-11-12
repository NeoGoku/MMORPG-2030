# Implementation status
Updated: 2026-11-12

## Implemented and locally verified
- Main scene child-parent declarations repaired. Scene hierarchy regression check demonstrated failing before and passing after the fix.
- .NET 8 server Release build: zero warnings/errors.
- Protocol v1: shared ephemeral sessions, server-owned movement at 20 Hz, normalized speed, bounds, stale input stop, message limits, independent snapshot sends and disconnect cleanup.
- Real WebSocket integration: two clients observe movement; excessive direction cannot increase speed; stale input stops movement; disconnect removes player; malformed/oversized messages close; reconnect works.
- Two actual Godot 4.3 WebSocket clients observe shared movement.
- Godot 4.3 scene import and short headless client startup passed.
- Windows debug EXE/PCK exported locally with Godot 4.3. Web single-thread export verified locally after installing matching templates.

## Cloud export and interactive validation
- Windows/Web CI exports and the server CI integration passed in [run 43174302980](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43174302980) and [run 43174303139](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43174303139).
- Android first CI run exposed removed SDK package `tools` in setup action defaults; configuration now explicitly requests `platform-tools`. The following export exposed Godot 4.3's required ETC2/ASTC texture import setting (confirmed against the engine validation source); it is now enabled. The next run built, signed and verified an APK, but the strict log gate caught a missing project icon; an explicit SVG icon is now configured. Final clean export evidence follows in the PR.
- Chromium loaded the exported single-thread Web client without JavaScript/console errors. No physical device checks yet.
- Godot endpoint/status UI, interpolated avatars, camera follow, keyboard and touch arrow controls. Interactive device/browser checks pending.
- Further CI evidence and downloadable artifacts are linked from the PR; this file preserves the evidence available when committed.

## Not implemented / not validated
- PostgreSQL persistence, accounts, character creation and slots, NPC, monster, combat, quest, loot and XP.
- Authentication, production TLS hosting, spatial streaming/interest management and production abuse protection.
- 50–100 concurrent player performance, real Android/Windows device play, browser crossplay and touch usability.
- Full GDD progression, economy, professions, housing, pets, guilds and PvP.
- Paid infrastructure: not provisioned.

M1 and M2 remain partial. Successful exports alone do not establish cross-platform playability. GDD v1.1 remains the ratified concept.
