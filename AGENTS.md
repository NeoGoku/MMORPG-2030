# MMORPG-2030 Codex instructions

The owner develops exclusively from Android. Use cloud-based development and CI; do not require a local PC.

## Approved game direction (GDD v1.1)
- Semi-realistic alternate-2030 3D MMORPG with openly known magic; seamless shared world without channels, initially 50–100 concurrent players; Android, Windows and browser crossplay.
- Third-person, FlyFF-style tab-target, eight fixed classes: Dark Mage, Light Mage, Archer, Shield Sword, Dual Swords, Healer, Creature Summoner and Tank. Level cap 100; one distributable stat point per level, additional unlimited increasingly costly stat points via quests and professions.
- Ten professions, crafting, trade, auction house, guilds, quests, pets with breeding/evolution, physically located non-instanced persistent housing, guarded auto-farming, PvP only in arenas/designated zones with normalized attributes, no real-money store.
- Preserve all ratified GDD v1.1 details; record unresolved mechanics as proposals, not facts.

## Technical direction
- Provisional Godot 4/GDScript client, standalone authoritative C#/.NET server, PostgreSQL, GitHub Actions. Validate Android, Windows and web export feasibility before finalizing engine.
- Maintain docs/GDD.md, docs/ARCHITECTURE.md, docs/ROADMAP.md and docs/IMPLEMENTATION_STATUS.md. Record implemented, partial, blocked, and untested features truthfully.
- Implement coherent, production-oriented modules, not isolated demos. Automate cloud builds and downloadable APK, Windows and web artifacts.
- Never commit secrets, credentials or signing keys. Request owner approval for paid services and account permissions.
- Conserve compute and Codex usage: avoid redundant audits, runtime loops and repeated full builds; run targeted checks and at most one necessary build per coherent integration batch unless fixing a specific failure.
- Continue autonomously within available task duration; ask only for essential missing authorizations or product decisions.

## Initial milestones
1. Set up versioned GDD, architecture, roadmap, client/server repository structure and CI.
2. Validate cross-platform Godot exports and deliver Android APK, Windows and browser test artifacts.
3. Build a persistent multiplayer city slice: two players, movement, character creation, NPC, tab-target combat, monster, quest, loot and XP.
4. Expand systematically toward the complete GDD without silently dropping features.
