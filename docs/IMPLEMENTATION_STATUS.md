# Latest M8 status

Implemented and verified on actual PostgreSQL: durable exact-write receipts/owned lookup/store reopen, housing floors/access/snapshots/placement/restart, parallel station escrow and capacity/replay recovery. Actual SIGKILL preserves acknowledged character and receipt. Client floor/elevation assertions passed; final rendered/platform CI evidence follows in handoff/PR. Asynchronous durable save queue and prolonged-outage reconciliation remain partially implemented; full GDD/physical-device/load approval is not claimed. [Actual M8 scope](M8_CONTENT.md).

# Latest M7 evidence

Production source `d9db175383007e2a3eebcb7706ebe1af30f94a84`, [PR #11](https://github.com/NeoGoku/MMORPG-2030/pull/11), matching protocol v9. Locally verified: atomic party checkpoint/failure retry, safe pending-disconnect reconnect, exact cancellable production escrow, real PostgreSQL SIGKILL-after-confirmation restoration and fresh login; two regional content loops with server gates/loot/craft/quests and Godot geometry streaming. CI source runs: server `43414128518`, clients `43414128497`; final outcomes below/PR. Full GDD and unconfirmed crash-loss/queued persistence remain partial. See [actual M7 content](M7_CONTENT.md).

# Implementation status — M5 shared-world integration
Updated 2026-11-13. Based on the verified M4 branch, with open M3/M4 PRs preserved. GDD v1.1 remains binding. M5 is **partial**, not a production-complete MMORPG.

## Preserved M1–M4
Nordtor/South Park, third-person joystick/camera, stationary auto-attack, HP/damage/death/respawn, original hunt/side objectives, personal loot/EXP, hashed accounts/three owned slots, PostgreSQL identity/reconnect, eight attributes and farmable stat points, eight classes with eight actual skills/six bindings, ten gathering/crafting profession paths, equipment/inventory/merchant/direct trade, groups/chat/shared rewards, pet lifecycle/offline training/six roles, guarded farming, visual weather/day-night and entity interest remain integrated. Local practice stays temporary and has no invented peers.

## M5 playable additions
- **Physical housing:** four Lindenhof apartments with real doorways/walls/rooms in the shared coordinates. Elin intro quest at (30,24): craft an oak chair and supply three wood, then claim a free unit. One apartment per character; first rent week included. One ground-floor room expands into an adjacent second room.
- **Furniture:** owned inventory chairs can be freely positioned in X/Z, rotated, moved and removed through touch controls. Finite positions, unlocked room bounds, doorway clearance and overlap checks; placement/removal and character inventory commit together. All nearby players see the same furniture. Only chairs currently have a placeable model.
- **Housing rights/production:** character-ID and guild visitor permissions, entry and revocation checks, owner-only storage/workshop/pet breeding, one timed herb planter. Seven-day rent obligation and manual payment; arrears disable visitors/productive actions while preserving owner retrieval/items. No auto-debit or destructive eviction. Persisted UTC garden/training/rent deadlines survive restart.
- **Guilds:** creation/invitation/accept/leave, persistent leader/officer/member ranks and permissions, guild EXP/levels, repeatable material delivery task/reward, officer-authorized storage withdrawals, earned energy funding an actual timed PvE attack buff. One physical guildhall can be acquired using shared materials; membership governs access.
- **Auction exchange:** real item escrow, name search, listing/purchase/cancellation and offline seller proceeds, five-percent fee/minimum one coin. CAS world revision and character transaction reject conflicting/repeated transfers and full inventory without losing items. Up to 200 world listings/ten per seller; newest 40 search results plus own-offer list. No expiry/bids/stalls yet.
- **Normalized arena:** mutually accepted duels and ready party teams (2–5 per side) only inside the marked square; distinct accounts, fixed 100 match HP/15 damage/range/cooldown independent of PvE stats. Team damage rejected. Group-vs-group guild-team mode is available for separate guilds in the same arena. Forfeit/disconnect/zone exit/time limit ends without reward. Knockout reward is one gameplay token per account per UTC day, stored with its guard; eligible guild-team win also adds guild EXP. PvE HP/resources remain separate. This is a common normalized strike, not complete class-skill PvP balancing or territorial guild wars.
- **Connected content:** Lindenhof residences, exchange and register, western arena, industrial buildings/constructs and open Tiefwerke ruins. Five added level-3–8 encounters with real HP/combat, two world quest objectives, optional level-5/Mira-gated transit. AUR-IX has 900 HP, warned area blast/shared damage and cooperative armor requiring two recent attackers to remove solo damage reduction. Boss cores feed a real smithing weapon recipe; pet/farm/manual pickup preserve the same drop item.
- **Presentation:** new original cutaway housing interiors/workstations/planters, windows/facades/roads/lamps, construct/boss silhouettes and visible warning circles. New housing shells/camera colliders load within 60 m and unload beyond 75 m; original city/industrial/dungeon geometry remains static. Server entities still use 52 m interest. Touch menus expose housing, guilds, auctions and arena; six skill controls remain.

## Persistence and authority
Protocol **v5** requires matching client/server. Additive migration 002 creates a versioned world JSONB aggregate for ownership/furniture/storage/guilds/auctions/proceeds/reward guards. World and affected character records save in one transaction with optimistic revisions. Restart/reconnect restores durable state. RAM mode is explicitly nondurable.

One authoritative server process per DB remains required. Shared state is bounded for this slice, cached and saved synchronously under the world lock. It is not a normalized high-volume market, queued persistence or sharded world. Existing routine simulation uses five-second/disconnect checkpoints; crash can lose that interval. Monsters/drops/summons/parties/chat and active arena matches remain ephemeral. No public server, paid service or production deployment.

## Evidence
- Existing gameplay/account/ownership/class/pet/farm regressions retained. Added housing quest/furniture conservation/permissions/revocation/storage/expansion/garden tests, guild ranks/storage/buff, escrow/replay/offline seller, stale world and stale character rollback, recreated-world housing/guild recovery.
- Actual PostgreSQL migration and shared transaction scenarios exercised. Normalized duel cases use huge PvE attributes, resource/cooldown/outside-zone/reward guards; ready two-party teams reject friendly damage and rewardless forfeit.
- Two real accounts through public HTTP/WebSocket actions: gather/craft/intro/ownership, shared furniture pose, reconnect, guild join and auction purchase/proceeds. Test clients send normal liveness input; no production test-grant endpoint.
- Rendered account/skill/chat/pet and M5 housing/guild/auction/arena menus, real housing chunk creation/unload, original city/multitouch and local dungeon/boss warning/collision cases.
- Independent review findings reproduced then fixed: boss loot pet item loss, permanently full expired guild invitation list and missing housing-craft quest progress. Final exact CI/artifact evidence is attached to the PR.

## Still incomplete / unqualified
- Housing has four finite units, chairs, two rooms and one ground floor; no additional floors/stairs, broad furniture catalogue, production chains or final scarcity/city-expansion policy. Visitor allowlists are not a complete friends system. Guildhall transfer/release/disband-with-house remains open.
- Guild quest variety, multiple guild skills/halls, persistent scheduled/territorial wars, guild chat and advanced administration remain incomplete. Guild-team mode lacks full multi-device war acceptance testing.
- PvP supports the common normalized attack; class-specific PvP skill balance, rankings, broad reward content and stronger anti-collusion remain open.
- Repairs/durability/stalls/commissions, auction expiry/bidding/pagination and tradeable pets remain missing. All rarity tiers and broad profession/equipment/quest content are incomplete.
- Added connected content is a small populated extension, not several production-scale cities. Full async streaming of every building/NPC/world asset, weather gameplay effects, mounts/destruction and broad story/faction systems remain open.
- Art is original stylized procedural geometry. Semi-realistic production character rigs/textures/audio, comprehensive equipment silhouettes, measured mobile LOD/texture compression and Android memory/battery profiling remain open.
- Physical Android/Windows runtime acceptance for this package, browser/multi-device crossplay, sustained team/guild load, 50–100-player capacity and production security/operations are unqualified.

All remaining GDD goals remain on the roadmap. No menu or data definition alone is counted as a finished system.

## M6 integration update (2026-11-13)
Implemented with focused tests: guild-only chat, leader hall release, auction pagination/expiry retaining escrow, gated item/stat-point quest rewards and UTC daily objectives, consumable rare stat reset, level-cap prestige and configurable death EXP loss. Touch controls connect the online actions; local reset/prestige are temporary. Protocol v6. See DEVELOPMENT_HANDOFF.md for rule proposals, exact continuation and qualification categories. This does not declare the full MMORPG alpha complete; all M5 gaps not explicitly replaced remain open.

## Continuation package 1
Appearance editing and public avatar reconciliation (gender/face/body/hair/skin/tattoo choices), distinct gear instances with equip/unequip/condition/repair and actual damage contributions. Existing pristine item stacks stay valid; explicit unpack converts one stack item to one unique instance without deleting legacy saves. Instance transfer/storage is addressed separately before claiming full economy integration. Models remain procedural, not final semi-realistic art. PR #8 unavailable (404); continuation uses latest available #6.

## Continuation package 2
Eight configurable class resources with skill expenditure, passive unlocks affecting attack/defense/healing/summons, normal-hit resource gain/regeneration and resource visibility. Monster threat now accumulates damage instead of every hit replacing the target; explicit taunts retain priority. Class-specific PvP skill balancing remains open; arena still isolates normalized match values. Formulas are provisional game.json data.

## Continuation package 3
Connected Grünsaum forest/bridge/open root ruins with four level-10–15 encounters and a second worldboss, gated fast travel with unrestricted return, vegetation/ruin presentation. Boss below half HP uses stronger/faster warned blasts. Player/monster snapshot candidates use a reusable 24 m spatial cell index, retaining exact distance checks and one shared world. This is not a measured 100-player capacity claim or complete async asset streaming.

## Continuation package 4
Recipe mastery requirements/tool hook, advanced resource nodes tied to gathering levels, new class-constrained weapons/head/feet/accessory items, Epic/Legendary recipe outputs with boss materials, eight class practice quests and chained forest/profession objectives. Original recipe paths retained. Broader narrative/factions and final balance remain open.

## Continuation package 5
Distinct equipment condition/identity now survives direct two-confirmation trade, auction escrow and owner housing storage. Equipped instances are rejected for transfer; capacity is revalidated and the existing atomic save boundaries are used. Inactive, unselected pets without training can be auctioned with identity/traits/lineage intact; duplicate purchases cannot mint a second pet. Housing floors/expanded furniture catalogue and stalls remain open.

## Continuation package 6
Arena class skill intents now spend separate normalized energy and use fixed configurable PvP damage, cooldowns, heals/group heals, buffs/guard, DoT/slow/cleanse, movement and temporary summon attacks/explosion. These reuse match ownership/range/team checks, never PvE farm stats. Farming selects affordable class damage/AoE/DoT/drain skills. Territory wars/rankings and comprehensive live balance remain incomplete.

## Continuation package 7
North-up minimap with real player/monster/service positions, equipment overlays and appearance variations, forest/ruin geometry, and persistent low/balanced/far graphics profiles controlling 3D scale, frame cap, shadows and actor sight. Six equipped skill buttons dispatch normalized class skills during arena matches. No measured device-performance or semi-realistic production-art claim.

## Issue #8 — durable-play/creation batch
Actual base is PR #7; #3–#7 remain open, #1/#2 merged. The main-only autonomous protocol/queue are included without reverting gameplay. Guild instance storage now accepts unequipped gear, limits withdrawal to officers/leader and preserves identity/condition through the existing shared-world transaction. New memory/PostgreSQL scenarios reopen the store around guild storage and auction escrow, reject full inventory without money/item loss and reject duplicate withdrawal/purchase. Creation API validates appearance and the online lobby offers the same choices with a live isolated 3D preview. These are character previews, not housing instances. Final DB/render/CI evidence belongs to the batch PR.

## Issue #8 — quest navigation
Data-driven quest navigation coordinates now accompany valid quest snapshots; completed objectives point to the reward NPC. Quest-log tracking drives a private world marker, distance label and minimap direction; Mira's preserved main-hunt adapter can also be tracked. This is guidance, not automatic obstacle-aware navigation. Locked/claimed objectives hide their markers. Local practice mirrors target selection.

## Issue #8 — physical player stalls
Players can place named persistent stalls in the marked trade zone, escrow stack items or unique gear, inspect nearby stock and buy only within range. Purchases/proceeds use the existing atomic auction transaction; equipped gear/capacity/replay protections remain. Closing moves unsold offers back into the ordinary exchange; expiry preserves seller escrow. Stalls are shared world props, not a separate channel or paid convenience. Pet listings remain at the auction exchange.

## Issue #8 — furniture and vendor authority
Building recipes now produce placeable tables, shelves and lamps as well as chairs, with stored item kind, model, footprint/door/occupant checks and conserved removal. NPC purchase is now explicitly catalogue-whitelisted: only listed vendor stock may be bought, preventing crafted/boss/zero-price reward items from being minted through forged buy intents. Expired stalls no longer permanently exhaust the active-stand cap; escrow remains recoverable.

Issue #8 verification update: actual PostgreSQL reopen cases for guild gear/offline auctions passed locally; rendered creation-preview/quest guidance and touch stall open→list→close journey passed. Final source uses protocol v8; platform/network evidence and exact source commit will be recorded in the batch PR. Physical-device acceptance deferred per issue. Full alpha remains partial.

## Cycle 2 — persistence operations
Orderly hosted-server stop now checkpoints connected characters and pending disconnect saves after the tick loop stops. Isolated restore helper refuses existing/non-prefixed targets and never drops production data. CI dumps PostgreSQL after account/economy integration, restores to a new DB and compares complete character/shared-world document fingerprints. Abrupt kill/crash still has the existing five-second checkpoint window; zero-loss crash durability and queued persistence are not claimed.

## Cycle 2 — persistent workshop production
Placed oak table enables one queued recipe job per apartment. Start validates profession/tool/material requirements and atomically binds inputs; real UTC deadline continues offline. Collection validates capacity, grants output/profession/quest progress once and clears the persisted job. This is one workshop slot, not a full production-chain/floor system.

## Cycle 2 — region authority
Data-driven shared dungeon/forest regions now enforce minimum character levels on movement and join/reconnect. Qualified characters remain in the same open world; no channels or private instances. Original low-level city/hunt remains accessible. Region names appear in character UI. Full world streaming/content expansion remains partial.

## M7 persistence continuation
Routine checkpoint writes connected party members together using the existing optimistic SaveBatch transaction (party cap ten), so shared combat progression is not checkpointed one member at a time. Failed routine writes use bounded exponential retry delay; orderly shutdown forces a final attempt. Explicit save-now action reports success only after store commit and exposes last confirmed tick. A hard crash can still lose unconfirmed ticks; no zero-loss or asynchronous save-queue claim.

M7 recovery package: reconnect immediately retries an owned pending disconnect snapshot and refuses stale SQL state during failure. Workshop production stores original input escrow and supports once-only atomic cancellation/refund, including after restart/catalogue edits; old jobs without input metadata remain collectible and cannot be refunded. Focused tests passed. A real disposable PostgreSQL server SIGKILL/restart test preserves the entire explicitly confirmed character document and rejects old bearer sessions/tickets; unconfirmed simulation remains outside this guarantee. CI includes this drill. The first M7 server CI failed on a social-test stale snapshot access, now synchronized on both client trade views.

## M7 world/content integration
Implemented and checked locally: Helixhafen (15–22) and Aschenhain (12–18), two-way data-driven transit, eight enemies including two open-dungeon bosses using existing multi-phase/cooperation mechanics, two public walk-in dungeon shells, four functioning merchant/workbench field NPCs, six chained quests, two mastery-gated gathering nodes and two class-restricted crafted chest pieces. Monster loot is configured per spawn and retains personal/replay protection. Completed quests navigate to the nearest actual reward trader. New scenery streams with distance hysteresis and material-batched meshes; existing Nordtor is preserved. .NET scenarios check gates/transit/input stop, item conservation, quest reward, merchant navigation and configured monster loot. Godot smoke checks area load/unload and local transit/loot parity. This is two compact playable extensions, not a massive world or completed streaming/performance qualification.

## Final M7 build evidence
Verified production source: `d9db175383007e2a3eebcb7706ebe1af30f94a84`. All relevant final jobs succeeded: [Server/PostgreSQL/WebSocket/SIGKILL/restore/package](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128518), [Android/Windows/Web including rendered client checks](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128497). This last commit changes documentation only; source was not rebuilt again.

Downloads expire 2026-11-27; matching protocol v9:
- [Android debug APK inside ZIP](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128497/artifacts/14030912240)
- [Windows EXE/PCK](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128497/artifacts/14031186727)
- [Web folder](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128497/artifacts/14030842378)
- [.NET8 server/content/migrations/source metadata](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128518/artifacts/14031451042)

No physical Android acceptance or full-alpha/load certification; next implementation remains durable acknowledged save queue/uncertain-commit reconciliation, then elevation/floors. PR #11 remains open, no merges.

M8 package 1: transaction-bound canonical payload receipts, account-owned lookup and persisted last-commit IDs; exact write retransmission resolves once, superseded payload is rejected. Memory and actual PostgreSQL reopen/replay checks passed. Migration 003 is additive; back up commit_receipts with the existing database. Immediate post-commit response-loss resolution added; fully asynchronous save queue and unresolved prolonged-outage reconciliation remain partial. No zero-loss unconfirmed-tick claim.

M8 package 2: up to three configurable physical housing floors with interaction-based adjacent stair traversal, saved occupant floor/home and per-floor furniture. Shared player/pet elevation, camera and cutaway shells; same-floor trade/combat/loot guards and immediate visitor ejection. Actual PostgreSQL targeted tests passed for costs, two-player upper-floor snapshots, ownership/placement/restart/recall and bounds. Client state/elevation/load-unload assertions passed; local dummy renderer emitted native mesh warnings, so actual rendered verification is deferred to the existing CI software-renderer job. No continuous stair physics or housing instances.

M8 package 3: forge/loom/distillation furniture, mastery recipes and workshop quest; up to six parallel UTC-deadline jobs bound to physical same-floor stations. Original input escrow, busy-station move/removal lock, recipe/proximity/ownership/capacity validation and once-only item/profession/farm XP collection. Actual PostgreSQL targeted cases passed for parallel start, incompatible recipe rejection, duplicate commands, reopened-world collection/cancellation and full-inventory recovery. Existing single-slot jobs remain collectible/cancellable by original rules. Provisional costs/caps in game.json; protocol v10. Rendered floor/station preview and all platforms deferred to one final CI batch.

M8 integration: three implemented packages described in M8_CONTENT.md. All M8 targeted PostgreSQL cases passed, including overlapping-XZ cross-floor trade rejection and production capacity recovery. Existing M7 world/classes/economy preserved. Source/PR/CI final evidence is recorded in the current handoff header. Fully asynchronous persistence, prolonged-outage reconciliation, receipt archival/load and physical device acceptance remain partial.
