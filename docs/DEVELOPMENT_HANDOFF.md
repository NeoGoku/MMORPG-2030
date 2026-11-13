# Current M8 handoff — 2026-11-13

Branch `codex/m8-recovery-housing`, [open PR #12](https://github.com/NeoGoku/MMORPG-2030/pull/12), base PR #11 / `d3a251f3ac19f2a1f1ce5cf9947ec894abfcefed`; no merges. Production source `66fbd5fa3a389ee41e2a1fc2b512d608432cbb57`. Commits: `dc5fc03` receipts, `0a927ed` floors, `719bd24` stations, `66fbd5f` compatibility/operations guards. Three sequential production packages: canonical durable receipts/migration 003, shared persistent floors with stair/access guards, and six-slot physical workshop production. Matching protocol **v10**; back up the entire database and do not run v9 writers against new housing documents. [Actual scope and controls](M8_CONTENT.md).

Local targeted actual-PostgreSQL cases passed; actual SIGKILL/re-login also preserves owned receipt. Headless client assertions pass with dummy-renderer mesh warnings; real-renderer/platform evidence is deferred to final CI, whose exact source/results/artifacts will be recorded here and in the new PR.

Next exact implementation: bounded asynchronous acknowledged save queue, durable pending-operation recovery and prolonged-outage handling without replaying economic effects or overwriting CAS. Current receipts resolve exact writes/immediate uncertain commits, not all gameplay command retries. Add receipt archival/size budgeting before load qualification. Then expand city buildings/housing using shared Floor/HomeId and WorkOrders, without losing old Production jobs. No continuous stair physics, full alpha, production art, measured 50–100-player/mobile qualification or automatic future run claimed. Earlier sections below are historical.

# Current M7 handoff — 2026-11-13

Branch: `codex/m7-durable-checkpoints`. Open PR: https://github.com/NeoGoku/MMORPG-2030/pull/11 (base PR #10 → #9 → previous open gameplay chain; no merges). Latest production source: `d9db175383007e2a3eebcb7706ebe1af30f94a84`. Packages: `0287d15` atomic party/confirmed saves, `d310773` pending reconnect/refundable production/actual SIGKILL drill, `d9db175` two shared-world content regions and server distribution. Protocol **v9**, retain SQL migrations 001/002 and existing JSONB aggregates.

Build evidence: server/PostgreSQL/crash/restore/package run https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128518 ; Android/Windows/Web run https://github.com/NeoGoku/MMORPG-2030/actions/runs/43414128497 . Final outcomes/downloads are recorded below and in PR #11. A documentation-only follow-up may be newer than the tested source; do not trigger duplicate platform builds for it.

Next concrete implementation: introduce a bounded durable simulation/save queue preserving character/shared-world CAS, with explicit acknowledgement semantics. First address uncertain commit outcomes (DB commit succeeds but acknowledgement is lost): reconcile against persisted operation identity rather than blindly replaying economic actions or overwriting newer versions. Current conservative version conflicts can require operational recovery; the pending-disconnect dictionary itself is RAM-only. Add controlled transaction-boundary crash/outage scenarios. Then implement a shared elevation/stair/floor model before adding upper housing floors, richer production and larger cities. Keep existing regional quests/loot/transit and Workshop Inputs compatibility.

Verified slices and limitations: see [M7_CONTENT.md](M7_CONTENT.md). Remaining full-GDD requirements are still partial; no full alpha, zero-loss unconfirmed simulation, multiple-server-per-DB or 50–100-player/device qualification. No paid services/deployment or automatic continuation scheduler. Earlier sections below are historical evidence, superseded by this header where applicable.

# Issue #8 durable development handoff — 2026-11-13

Durable task: https://github.com/NeoGoku/MMORPG-2030/issues/8. This issue is an entrypoint, not a scheduler. Read AUTONOMOUS_DEVELOPMENT.md and DEVELOPMENT_QUEUE.md each task. No automatic future Codex run is promised.

Actual reconciliation: PR #1/#2 merged; #3–#7 open at task start. #8 is now this issue, not a PR. PR #9 did not exist at start. Base gameplay: PR #7 / 79c6f4d31b892d4230d019f6abed022c8b6ca03b. Main 85e18e9 adds only the autonomous protocol/queue; both files are preserved on codex/issue8-durable-play without reverting unmerged gameplay. New batch PR, commit chain, exact final source SHA and CI/artifact links are recorded in its PR.

Implemented adjacent packages:
1. Guild gear storage with member deposits/officer-leader withdrawals, proximity/capacity/equipped checks, touch controls and shared-world/character transaction. Reopen tests preserve rank, instance ID and condition; duplicate withdrawal/full-inventory purchase/repeated purchase do not duplicate or lose assets. Both memory and real PostgreSQL exercised.
2. Character creation accepts bounded appearance values, persists them in the owned character and uses a live class/appearance 3D preview in the lobby. The local preview SubViewport is UI-only, not a world/housing instance.
3. Quest snapshots expose objective/reward-NPC targets. Quest log drives private world marker, distance and minimap direction; claimed/locked markers hide, completed objectives switch to reward NPC. Mira adapter is also trackable. This is guidance, not auto-navigation/pathfinding.
4. Shared physical player stalls reuse auction escrow/fees/proceeds, with naming/zone/spacing/range checks, offline sales, safe expiry, capacity/replay protection and close-to-exchange conservation. Active cap excludes expired stands. Test reopen scenarios exercise offline seller and recovery; rendered touch journey opens/lists/closes real stock and world prop.
5. Table/shelf/lamp building recipes and placeable original models, stored item identity, footprint/door/occupant guards and exact removal. NPC vendor stock is explicitly allowlisted, closing forged zero-price reward/boss-material purchase paths.

Release contract: matching protocol v8 required. New aggregate fields are additive; existing PostgreSQL migrations 001/002 suffice. Catalogue adds Vendor/FurnitureKind/footprints and stall limits/duration. Default stand lifetime 86400 seconds, cap 100 active; these remain provisional configuration. Old expired stall metadata does not consume active capacity; escrow can be recovered. Closing converts unsold stand listings into ordinary exchange listings. Pet sales remain at exchange. No paid services, merge, public deployment or permission changes.

Verification: focused .NET suites, actual PostgreSQL reopen tests, Godot import/local regression and rendered creation/navigation/stall touch flow. Final fresh PostgreSQL/network + Android/Windows/Web run is linked in PR. Backup/restore crash drills, all failure timings, physical-device/crossplay/battery/load qualification are not claimed. Owner acceptance remains deferred until a meaningful release milestone.

Next concrete tasks (P0/P1/P2):
- Isolated restore drill from ops/backup.sh and process-crash/restart acceptance, then queued/outage-resilient persistence without weakening CAS/ownership.
- Improve stand offer expiry/condition visibility and market recovery UI, including guild-house instance administration and long-lived stock cleanup.
- Add a shared elevation/floor/stair model before promising upper housing floors; extend furniture production/workstation effects and persistent production chains.
- Rich NPC dialogue/quest navigation targets, story/faction decisions; expand class PvP balance, arena consent/latency scenarios and controlled auto-navigation.
- Then mobile profiling, legal production rigs/textures/animation and measured load. No 50–100-player capacity or complete alpha claim.

Preserved limits: one world server per DB, synchronous DB operations under world lock, routine five-second/disconnect checkpoint and possible last-interval crash loss. Monsters/drops/summons/parties/chat/matches remain runtime state. Housing still four finite homes, one ground floor/two rooms and one guildhall. Full city/world streaming, mounts/destruction, territorial wars, broad production/factions and final semi-realistic art remain partial/missing. No percentages.

Reproduce with existing .NET8 Dockerfile/Compose, PostgreSQL16, ignored .env password, exact browser origin and TLS proxy. Back up before deployment; restore into an isolated DB, validate counts/migrations/ownership/trade before switching. No production restore was performed. Next task must start from this PR's exact head, not assume open PRs are already on main.

## Cycle 2 continuation
Base now PR #9 / ab8ef49bcce05a79130fac25cc4d2d1b41128758; open PR chain #3–#7/#9 preserved, no merges. Branch codex/issue8-cycle2. Added graceful-stop checkpoint; isolated restore script refusing production/existing DBs plus CI whole-document fingerprint drill; one-slot persisted table-based workshop production; authoritative dungeon/forest region-level movement/join gates. New JSON fields are additive; migrations 001/002 remain sufficient. Protocol v8 is retained (additive fields/actions); clients need this batch for production UI.

Focused tests: graceful-stop state, low-level reconnect rejection/qualified region entry, recipe input escrow/premature collect and once-only output after recreated world. Godot region smoke passed. CI fresh PostgreSQL/archive restore/platform results and exact code commit are recorded in batch PR. Hard crashes still lose the last routine checkpoint interval; queued saves, actual kill-at-transaction timing, device/capacity qualification are not complete. Next: explicit crash/replay/restore process harness with controlled disposable server, pending-save queue design preserving CAS, then shared elevation/stairs/floors and richer production. Do not describe graceful shutdown as zero-loss abrupt-crash persistence.

## M7 continuation after cycle 2
Base PR #10 / f8abe3c40d98e3ca1f9ade6e5649e456b763e55c. Cycle-2 server/restore CI 43398685897 and client CI 43398685885 passed. Branch codex/m7-durable-checkpoints adds party-wide SaveBatch checkpoints, capped retry delay, forced shutdown retry and explicit save-now/confirmed tick UI. Matching protocol v9. Test proves both party revisions remain unchanged on failure and advance together on forced retry. Latest PR records exact commit/build evidence. Remains synchronous DB work under lock, routine unconfirmed tick loss and runtime-only monsters/matches. Next exact task is acknowledged simulation/save queue + real process-kill replay harness, before claiming full persistence complete; then elevation/floors and larger production/content. Do not repeat cycle-2 restore/production/region foundations.

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

## M8 client build evidence
Verified source `66fbd5fa3a389ee41e2a1fc2b512d608432cbb57`: [Android/Windows/Web and rendered housing checks all passed](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152208). Actual software-GL preview downloaded and visually inspected: shared upper-floor cutaway, furniture and station models render. The earlier local dummy-renderer warnings are not a physical-device qualification. No final semi-realistic art claim.

Artifacts expire 2026-11-27, matching protocol v10:
- [Android debug APK inside ZIP](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152208/artifacts/14043341599)
- [Windows](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152208/artifacts/14043636278)
- [Web](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152208/artifacts/14042951859)
- [Rendered housing/station preview](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152208/artifacts/14042951863)
Server/PostgreSQL/crash/restore/package run: https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152295 ; final outcome follows below/in PR #12.

## Final M8 server / integration evidence
All relevant final jobs succeeded for source `66fbd5fa3a389ee41e2a1fc2b512d608432cbb57`: [server/PostgreSQL/network/crash/receipt restore/package](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152295) and [Android/Windows/Web/rendered housing](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152208). [Server with content, migrations 001–003 and SOURCE_COMMIT](https://github.com/NeoGoku/MMORPG-2030/actions/runs/43435152295/artifacts/14041923624), expires 2026-11-27. This final evidence commit changes documentation only; no duplicate platform builds.

PR #12 remains open; no merges/deployment/paid resources. Next exact task: bounded asynchronous acknowledged save queue, durable handling of pending/uncertain outcomes through prolonged outages, then receipt retention/load qualification and broader city/housing content. Current exact-write receipts are verified; full sustained-outage and unconfirmed-tick durability remain partial. Physical Android/crossplay-device and capacity qualification remain untested.
