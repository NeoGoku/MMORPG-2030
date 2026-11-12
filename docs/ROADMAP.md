# Roadmap
## M0 — Foundation
Versioned GDD, AGENTS.md, architecture, Godot project, .NET server skeleton, reproducible CI exports.
## M1 — Cross-platform feasibility
Android debug APK, Windows and browser artifact. Verify actual exports and test at least one Android device and browser.
## M2 — Multiplayer vertical slice
Third-person touch movement, two connected characters, persistence, NPC, monster, tab-target combat, loot, XP, quest, one small urban district.
## M3 — Core MMORPG
Eight classes, attributes, skill system, quests, professions, trading, parties, pet foundations, navigation and guarded auto-farming.
## M4 — Persistent world
Multiple cities, spatial streaming, non-instanced dungeons, physical housing, guilds, pet breeding/evolution, economy.
## M5 — Content, balancing and release
Cross-platform optimization, content production, server security, PvP normalization, operations and release.

Milestone completion requires implemented code and verifiable build/runtime evidence; not merely files or plans.

## Current milestone boundaries (2026-11-12)
M0: source foundation and scene-load check implemented. Export pipelines now include Android setup and PR checks; export evidence tracked in PR/status.
M1: pending successful cloud artifacts and real Android, Windows and browser runtime checks. Keep Godot provisional.
M2: ephemeral two-player movement foundation verified with real WebSocket and Godot clients. Next: PostgreSQL-backed accounts/character slots, city content, NPC/monster, combat, quest, loot and XP. No persistence or production hosting yet.

## Nordtor slice (2026-11-12)
M2 now has a playable temporary district: third-person humanoid, Android analog/multi-touch controls, buildings/roads, NPC hunt quest, hostile drone AI, tab-target stationary auto-attacks, HP/damage/death/respawn, personal loot and XP/level rewards. The same district is immediately playable in explicit local training and shared through a real authoritative online server. Current art is original procedural placeholder art.

Next M2 work: accounts and three character slots, PostgreSQL-backed progression/inventory, deliberate reconnect identity, full combat/class foundations and physical-device/crossplay qualification. Current quest and balance values are slice proposals; no GDD features are dropped. M2 remains partial until persistence and character creation are implemented.

## M3 package handoff (2026-11-12)
Integrated identity/three character slots, PostgreSQL saves/reconnect, eight class starter kits, eight allocated attributes, six-slot HUD, equipment/inventory/trade/gather/craft and initial quest/pet progression. See implementation status for exact limits. Next coherent work: queued durable persistence and multi-process character leases; full appearance/skills and profession content; group invitations/chat with personal attribution; real spatial streaming. Preserve all later GDD milestones. Physical Android qualification must precede declaring this package device-ready.
