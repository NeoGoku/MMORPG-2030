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

## Implemented; validation pending
- Android, Windows, Web export pipelines: PR/main triggers, strict artifact outputs, error-log checks and failure diagnostics. Android SDK/Java/debug-signing setup included.
- Godot endpoint/status UI, interpolated avatars, camera follow, keyboard and touch arrow controls. Interactive device/browser checks pending.
- Local Windows/Web export results and GitHub Actions evidence are recorded in the PR after execution.

## Not implemented / not validated
- PostgreSQL persistence, accounts, character creation and slots, NPC, monster, combat, quest, loot and XP.
- Authentication, production TLS hosting, spatial streaming/interest management and production abuse protection.
- 50–100 concurrent player performance, real Android/Windows device play, browser crossplay and touch usability.
- Full GDD progression, economy, professions, housing, pets, guilds and PvP.
- Paid infrastructure: not provisioned.

M1 and M2 remain partial. Successful exports alone do not establish cross-platform playability. GDD v1.1 remains the ratified concept.
