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
