Latest M8 branch `codex/m8-recovery-housing`, based on PR #11. Completed concrete slices: transaction receipts/owned lookup and replay checks; bounded shared housing floors and furniture; parallel physical stations and once-only UTC jobs. Full asynchronous/prolonged-outage persistence, larger cities, final art and device/load qualification remain open. Exact final code/PR/build evidence in DEVELOPMENT_HANDOFF.md.

Latest M7 source: `d9db175383007e2a3eebcb7706ebe1af30f94a84`; [open PR #11](https://github.com/NeoGoku/MMORPG-2030/pull/11). Continue from this stack, not main. Highest next task: bounded durable save queue and uncertain-commit reconciliation before broader housing floors. Completed concrete slices are listed below; aggregate goals remain open.

# Development queue — MMORPG-2030

Canonical feature requirements: docs/GDD.md. Verify implementation status in current PRs before claiming any item missing or complete.

## P0 — Durable, end-to-end play
- [x] Reconcile available PRs: #1/#2 merged; #3–#7 open at start of Issue #8 batch; #8 is the durable issue, #9 did not yet exist. Latest gameplay base is PR #7 (79c6f4d); main adds autonomous protocol/queue only.
- [ ] Complete account auth, reconnect, character persistence and migration-safe PostgreSQL operations.
- [ ] Complete server-authoritative inventory/equipment, quest rewards and atomic trade flows.
- [ ] Make all eight classes' skills, resource costs and effects genuinely usable in online play.
- [ ] Ensure mobile UI exposes inventory, character stats, skills, quest log, chat, pet and housing.
- [ ] Maintain reproducible Android/Windows/Web exports; Android acceptance only at larger milestones.

## P1 — World and content
- [ ] Expand meaningful playable city districts, wilderness and region traversal.
- [ ] Implement open-world dungeons and multi-stage world bosses with party mechanics.
- [ ] Implement quest chains, class/profession quests and optional story decisions.
- [ ] Finish equipment variety, appearance editor and visible equipment.
- [ ] Implement spatial streaming/interest management without player channels.

## P2 — Persistent social/economy
- [ ] Finish physically situated housing, floors, furniture crafting, production and access control.
- [ ] Finish guild quests, skills, guildhouse permissions and designated-zone wars.
- [ ] Finish auction escrow, player stalls and safe recovery after restarts.
- [ ] Finish pet breeding/evolution, offline training, summoner separation and all six pet roles.
- [ ] Finish ten professions, mastery and connected crafting economy.

## P3 — Quality and endgame
- [ ] Finish normalized PvP, prestige, infinite PvE stat progression and configurable death XP loss.
- [ ] Improve navigation and guarded auto-farm without infinite loops.
- [ ] Improve semi-realistic licensed/original assets, animation, lighting and mobile LOD.
- [ ] Performance and security qualification for 50–100 players; no unsupported capacity claims.
- [ ] Production deployment documentation without provisioning paid services.

## Selection rules
Prioritize vertical end-to-end completion over superficially touching every item. Re-evaluate from actual code, tests and PR state at each new task. Update checkboxes only with evidence and record proof in docs/IMPLEMENTATION_STATUS.md.

## Issue #8 batch — 2026-11-13
Completed adjacent slices (aggregate P0/P1/P2 goals above remain open where broader qualification/content is missing):
- [x] Officer/leader-only guild instance withdrawals, member deposits and touch UI, preserving gear identity/condition.
- [x] Store-reopen scenarios for guild rank/storage and offline auction gear; full-inventory rollback and repeated command rejection. Memory and actual PostgreSQL exercised.
- [x] Validated appearance at account-owned character creation, live 3D selection preview and persisted selection.
- [x] Quest-log tracking with authoritative objective/reward coordinates, private world marker and minimap direction.
- [x] Physical player stalls reusing persistent escrow; proximity purchases, offline seller proceeds, safe close/return, active cap ignoring expired entries.
- [x] Placeable table/shelf/lamp recipes and item-specific conservation/footprint/occupant checks.
- [x] NPC vendor allowlist prevents forged purchase of reward/boss materials or zero-price reward currency.

Evidence/CI/artifact commit links are recorded in the batch PR and IMPLEMENTATION_STATUS.md. No whole-feature percentage or 50–100-player qualification implied. Physical Android acceptance remains deferred until a release milestone.

Next highest priorities: process-crash/backup-restore acceptance and queued persistence; guildhouse/physical housing floor model; production/stall listing UX with expiry visibility; class PvP balance and multi-device cases; richer NPC/story navigation. Do not repeat completed appearance/guild-storage implementation as missing work.

## Cycle 2 — persistence/workshop/region batch
- [x] Graceful hosted-server stop checkpoints last connected-character state; focused scenario verifies pre-interval movement is stored.
- [x] Isolated restore helper and CI archive restore/document fingerprint comparison added (final CI outcome in batch PR).
- [x] Persistent one-slot housing workshop job binds materials, respects recipe mastery/tool checks and grants output once after UTC deadline/recreated world.
- [x] Shared dungeon/forest minimum-level gates apply to movement and reconnect, with region UI.
- [ ] Abrupt-process-kill zero-loss durability, queued persistence and full restore/outage operational qualification remain open.

## M7 next persistence slice
- [x] Party-wide atomic routine checkpoint batches, bounded retry backoff and forced final shutdown attempt.
- [x] Explicit save-now commit confirmation; RAM backend clearly remains nondurable.
- [ ] Crash-proof acknowledged simulation, asynchronous durable save queue and multi-process leases remain open. Do not infer zero-loss crash persistence from checkpoint confirmation.

- [x] Reconnect flushes pending owned disconnect state; blocks stale reconnect while save fails.
- [x] Production cancellation refunds original persisted inputs once; legacy jobs remain collectible.
- [x] Local actual PostgreSQL SIGKILL/restart confirms whole acknowledged character document, fresh login and old-ticket rejection; CI drill added.

## M7 adjacent world slice
- [x] Two connected level-gated content areas, public dungeon shells, eight configured encounters/two phase bosses and personal configured loot.
- [x] Six linked quests, field trader/workbench functions and two regional profession recipes.
- [x] Data-driven two-way transit, interaction/minimap, distance-streamed material batches for new area scenery; local headless and authority checks passed.
- [ ] Broader cities, actual upper housing floors, asynchronous durable saves, measured device/server streaming and complete GDD remain open.

Next: durable acknowledged simulation queue preserving character/shared-world CAS; transaction-cut process crash/replay cases; shared floor/elevation model before adding upper housing rooms. Do not repeat completed SIGKILL-after-confirmation, workshop cancellation or region foundations.

M7 final verification: all server/PostgreSQL/crash/restore and Android/Windows/Web jobs passed for `d9db175383007e2a3eebcb7706ebe1af30f94a84`; artifacts/next task in DEVELOPMENT_HANDOFF.md and open PR #11. Documentation-only evidence follow-up does not repeat builds.

M8 package 1: transaction-bound canonical payload receipts, account-owned lookup and persisted last-commit IDs; exact write retransmission resolves once, superseded payload is rejected. Memory and actual PostgreSQL reopen/replay checks passed. Migration 003 is additive; back up commit_receipts with the existing database. Immediate post-commit response-loss resolution added; fully asynchronous save queue and unresolved prolonged-outage reconciliation remain partial. No zero-loss unconfirmed-tick claim.

M8 package 2: up to three configurable physical housing floors with interaction-based adjacent stair traversal, saved occupant floor/home and per-floor furniture. Shared player/pet elevation, camera and cutaway shells; same-floor trade/combat/loot guards and immediate visitor ejection. Actual PostgreSQL targeted tests passed for costs, two-player upper-floor snapshots, ownership/placement/restart/recall and bounds. Client state/elevation/load-unload assertions passed; local dummy renderer emitted native mesh warnings, so actual rendered verification is deferred to the existing CI software-renderer job. No continuous stair physics or housing instances.

M8 package 3: forge/loom/distillation furniture, mastery recipes and workshop quest; up to six parallel UTC-deadline jobs bound to physical same-floor stations. Original input escrow, busy-station move/removal lock, recipe/proximity/ownership/capacity validation and once-only item/profession/farm XP collection. Actual PostgreSQL targeted cases passed for parallel start, incompatible recipe rejection, duplicate commands, reopened-world collection/cancellation and full-inventory recovery. Existing single-slot jobs remain collectible/cancellable by original rules. Provisional costs/caps in game.json; protocol v10. Rendered floor/station preview and all platforms deferred to one final CI batch.

M8 integration: three implemented packages described in M8_CONTENT.md. All M8 targeted PostgreSQL cases passed, including overlapping-XZ cross-floor trade rejection and production capacity recovery. Existing M7 world/classes/economy preserved. Source/PR/CI final evidence is recorded in the current handoff header. Fully asynchronous persistence, prolonged-outage reconciliation, receipt archival/load and physical device acceptance remain partial.
