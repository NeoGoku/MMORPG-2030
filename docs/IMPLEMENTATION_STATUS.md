# Implementation status — M3 integration package
Updated: 2026-11-12. Based on merged PR #2. GDD v1.1 remains binding; M3 is partial.

## Playable integrations
- Existing Nordtor third-person city, joystick/camera, Mira hunt, stationary auto-attacks, HP/damage/death/respawn, loot and multiplayer remain.
- Three owned online character slots; nickname and eight fixed GDD classes. Character appearance uses the existing common humanoid preset. Three separate temporary training slots can select classes locally.
- Eight configurable attributes; server allocation spends earned points and calculates class attack, vitality HP, wisdom mana/healing, armor/equipment defense and arcane resistance. Level cap 100 and one earned stat point per level remain.
- Additional stat points: profession actions earn farm-EXP; server costs grow exponentially using arbitrary-precision integers. No real-money purchase or artificial stat-point cap. Local training uses ordinary numeric values and is not an authoritative progression store.
- Touch layout menu moves joystick, action and skill controls in bounded 12-pixel steps, saves positions locally and restores defaults.
- Six simultaneously visible skill buttons. **Two working skills per class**, with damage/heal/buff variants, mana and cooldowns; the other four positions are visibly locked. This is not the GDD's full 8–12 active skills per class. Summoner's starter buff does not implement a separate summon system.
- Slot/stack-limited inventory (24 configurable slots), normal/uncommon starter items, weapon/chest equipment, visible derived values, healing potions, merchant buy/sell and atomic recipe material consumption.
- Three gathering professions (ore/herbs/wood), two crafting professions (smithing/alchemy) and two recipes. Catalogue/state covers all ten GDD professions; the remaining five have no playable recipes/services yet.
- Data-driven side/class/profession objectives and prerequisites, quest log and one-time NPC rewards. Miras original main hunt remains its tested legacy adapter; a general main-story/branching engine is incomplete.
- One active following spirit pet with selectable combat assistance, periodic healing or personal-loot collection, EXP/levels and synchronized online position/mode. Combat assistance leaves final kill credit to the player. Parent/evolution fields are stored, but breeding/evolution are **not playable**.
- Expanded continuous South Park/residential area, two added buildings, services/resource markers and a two-way teleporter. Zone metadata exists; actual world streaming/interest management is **not implemented**.
- Local training remains instantly playable and clearly separate from accounts. Online displays real characters and pets; no simulated remote players.

## Accounts / persistence
- Password-hashed accounts, expiring opaque sessions, account-owned slots, one-use WebSocket tickets, default-denied anonymous login, basic request limits and HTTPS-only remote account UI.
- PostgreSQL 16 versioned migration and optimistic aggregate storage: character, EXP/points, inventory/equipment, quests/currency, HP/mana/cooldowns and pet state. Re-login/reconnect restores saved state; duplicate active joins and stale writes are rejected.
- Economic/actions save atomically; simulation checkpoints every five seconds and disconnect saves. Crash may lose the latest checkpoint interval. Monsters/drops are ephemeral; no durable world persistence. One world-server process per DB only, DB work under world lock is not a scaled persistence architecture.
- No DB setting means explicitly flagged RAM-only test mode. Reproducible Docker/Compose configuration supplied. No production server or external database booked; see [operations](M3_OPERATIONS.md).

## Evidence and limits
- Existing city/gameplay/transport suites retained; new tests cover all eight starter class kits, mana/cooldown rejection, stats, inventory/crafting/equipment, account ownership and three slots.
- Real PostgreSQL tests exercise migrations, password login, optimistic conflict rejection, aggregate roundtrip and authenticated reconnect. Regression cases cover detached snapshots, stale ticket/disconnect joins and skill-save rollback.
- Rendered mobile tests cover six skill slots, joystick/action behavior and the account lobby → character creation → authenticated connection → visible pet → separate local slot.
- CI/build results and download links are recorded in the PR. Actual physical Android testing of this new package and multi-device crossplay, performance/battery and Windows runtime qualification remain outstanding. Owner confirmed the prior PR #2 Android build works.

## Explicitly still missing / partial
- Parties, invitations, local/group chat and group loot/combat attribution: not implemented.
- Full character appearance editor, full class skill trees/buffs/resistances, gear variety, generalized loot tables and all rarity tiers in actual drops: incomplete.
- Full ten professions, workbench variety, broad main/side/class/profession quest content, repeatable profession/quest progression, separate summoner summons, pet breeding/evolution/offline training: incomplete.
- Persistent housing/economy, auction house/trading, guilds, PvP, automation, weather, seamless world streaming, production operations and 50–100-player load validation: not implemented.
- Production art/audio and extensive animation/equipment silhouettes: incomplete. Current original stylized assets remain provisional.

No confirmed GDD feature is removed or declared complete based only on a data definition/menu. No paid services or production deployment.
