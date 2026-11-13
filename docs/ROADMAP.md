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

## Current boundaries (2026-11-12, M4 package)
M0 foundation and M1 export pipelines exist; earlier packages produced APK/Windows/Web artifacts and PR #2 APK was owner-tested. New M4 device/runtime qualification remains open. Godot is still provisional until cross-platform acceptance.

M2 city gameplay and persistent account/character reconnect are implemented; hosting, device crossplay and production scaling remain open. M3 core systems are expanded with real ten-player groups/chat, shared rewards, eight skills per class/six bindings, all ten professions, direct atomic trade, pet lifecycle and guarded farming. M4 adds entity distance filtering and visual day/night/weather; these do not complete world streaming.

This package is stacked on unmerged PR #3 to preserve its verified M3 source. Exact evidence and platform artifacts are recorded in its PR and [status](IMPLEMENTATION_STATUS.md).

Next coherent integrations, still required by GDD:
- Durable/queued persistence and multi-process character leases; scale/outage/load qualification.
- Shared-world building interiors, ownership/storage/rent/permissions and persisted furniture placement.
- Direct trade expansion into transactional stalls/auction escrow, repair/durability and fuller profession/loot content.
- New region/dungeon/worldboss/quest chains, level-gated teleporters and actual asynchronous world streaming.
- Guilds/permissions/storage/quests and designated-zone normalized PvP.
- Full appearance editor, semi-realistic rigs/audio, Android performance/battery and Windows/browser/crossplay acceptance.

No later GDD feature is dropped. Housing, guilds, PvP and dungeon/boss remain unimplemented rather than placeholder menus.

## M5 package handoff (2026-11-13)
Integrated finite physical apartments/rooms/chairs/rights/storage/workshop/garden/rent, durable guilds/storage/tasks/buff/hall, transactional escrow auctions, normalized consensual arena duels/ready teams, connected industrial/dungeon/boss content and partial housing geometry streaming. Stack on verified PR #4; M3/M4 remain unmerged at task start. Exact evidence/artifacts belong to the new PR.

Next integration: housing floors/stairs and furniture catalogue/production; guildhall release/transfer and guild chat/quest variety; complete class-skill PvP balance and multi-device team/war qualification; repairs/stalls and auction pagination/expiry; full async city/NPC streaming and semi-realistic assets. Preserve priority on queued persistence/leases/outage/load testing, physical Android/Windows/Web acceptance and production operations. No milestone is declared production-complete by this package.

## M6 integration continuation
Guild chat/hall lifecycle, quest/reward/endgame and paged expiring auctions integrated. Next: unique equipment instances and reversible legacy migration → repair/class equipment → appearance/passives/resources/PvP skills. Full Phase A–G alpha remains partial; detailed next tasks, restore strategy and evidence are in DEVELOPMENT_HANDOFF.md and M6 PR.

## Seven-package continuation
Completed consecutive code integrations: appearance/instanced gear → class resources/passives/threat → connected forest/open ruins/phased boss/cells → mastery recipes/class+profession quest chains → gear/pet economy → normalized class arena skills/farming → minimap/quality profiles/monster silhouettes. PR #8 could not be retrieved (404); latest accessible base was PR #6. Next priority: full end-to-end instance economy PostgreSQL/restart acceptance, appearance creation-screen presets, legacy gear conversion UX and class-specific PvP balance. Housing floors, production catalogue/stalls, NPC navigation/factions, broad city content/streaming and measured device/load qualification remain open.

## Issue #8 queue batch
Reconciled actual PRs/main docs, added guild instance storage and actual store-reopen evidence, creation preview, quest guidance, physical escrow stalls and conservative furniture catalogue. Hardened vendor purchase authority. Next: crash/restore drill + queued persistence, physical floors/production, richer class/PvP and device qualification. Full alpha remains partial. DEVELOPMENT_QUEUE.md is now the current task source; Issue #8 is a resumable entrypoint, not a scheduler.
