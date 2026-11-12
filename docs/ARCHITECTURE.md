# Architecture v0.3 — Nordtor city slice

Godot 4.3/GDScript Compatibility client and .NET 8 server. JSON WebSocket protocol **v2** intentionally requires matching clients/server. PostgreSQL remains planned; sessions and progression are not persisted. No production deployment or paid service is configured.

## Modules and shared data
- `Client/data/district.json`: single source for bounds, building footprints, player/NPC/monster spawns and provisional combat/quest constants. Explicitly included in Godot exports and copied into server build output.
- `Server/District.cs`: typed data and walkability. `World.cs`: locked, server-owned state and deterministic `Step(dt)`; hosted service advances 20 ticks/second. `Session.cs`: bounded transport and intent parsing.
- `Client/scripts/practice.gd`: explicit local training simulation using the same district/rules and snapshot shape. No fabricated other players. This keeps an APK playable without hosting; it is never trusted by an online server.
- `network.gd`: WebSocket lifecycle and v2 intents. `main.gd`: switches session modes and synchronizes presentation. `city.gd`: original batched geometry/camera collision. `actor.gd`: humanoid/drone presentation and damage numbers. `hud.gd`, `joystick.gd`, `touch_action.gd`: Android-first inputs and status.

Local and server simulation are separate implementations. Scenario tests cover matching core rules; full numeric lockstep parity is not claimed. Balance edits belong in the shared JSON. Invalid JSON is a startup error, not a runtime player input.

## Authoritative gameplay
Only the server mutates online HP, damage, kills, quest, XP, levels, currency and loot. Clients submit direction and action intents, never outcomes. Direction is finite, bounded and normalized; stale input expires after 250 ms. Server movement slides along expanded building rectangles and world bounds.

A valid target remains selected while idle. Normal attacks execute only when stationary, in range, target alive and cooldown elapsed. Monsters patrol, chase the closest living player within aggro/home leash, return/heal when disengaged, die and respawn. Players have HP and a timed safe respawn; this slice applies **no death EXP penalty**, pending the GDD's unresolved amount. This is a temporary test rule, not a GDD change.

Last-hit kill credit grants 20 EXP and one personal drop. Only its owning session can collect nearby loot; pickup atomically removes it. No groups/party kill sharing yet. Mira's three-kill quest counts after acceptance and grants the turn-in reward once. Disconnect destroys owned loot and session progression. Safe-side recovery and all balance constants are provisional.

## Protocol v2
Welcome: `{"type":"welcome","version":2,"id":"server-id","tickRate":20}`.
Movement: `{"type":"input","seq":1,"x":0,"z":-1}`.
Action: `{"type":"action","seq":2,"kind":"target","target":"drone-1"}`.
Kinds: `target`, `attack` (toggle), `interact` (Mira), `loot` (specified owned drop or nearest own nearby drop).

Input and actions share one increasing non-negative 64-bit sequence per connection; duplicate/stale commands are ignored. Action targets are strings up to 64 characters; unknown kinds/types and malformed numeric inputs close the session. Extra outcome fields (e.g. coins/damage) have no effect.

Snapshots contain `type`, `tick`, `players`, `monsters`, `loot`, `self`. Public player states include id/x/z/yaw/HP/maxHP/level/target/attacking/dead and attack/hurt serials/lastDamage. Monster states include id/name/x/z/HP/maxHP/dead and attack/hurt counters. Only the receiver's own drops and quest/XP/stat-point/coin/scrap/message state are sent. Counters trigger presentation effects and may coalesce under network delay; they are not a reliable combat-event log.

Each connection has one receive and one independent bounded send loop. 4096-byte text-message limit (fragmentation supported), 60 messages/second, 10-second input inactivity and 3-second send deadlines; 100 concurrent session cap. This cap is not load-test evidence. Slow peers cannot block the world tick. All players currently receive the full small district state; spatial interest management is future work.

## Platform and operations boundary
APK starts in clearly labelled temporary local training. Online opens a configurable endpoint; failed/disconnected online sessions stay offline from the server and never silently substitute training rewards. The remembered endpoint is a local preference, not a credential. HTTPS browser builds require WSS. Public deployment requires auth, TLS, origin/abuse policy, monitoring and persistence; the unauthenticated development server is not production-ready.

CI creates debug APK, Windows EXE/PCK and Web artifacts for 14 days. Android uses Java 17, SDK 34/build-tools 34.0.0 and an ephemeral debug key; updating a prior APK may require uninstalling it. Rendered scene/input tests run under Xvfb/OpenGL, while simulation/transport tests are headless. No automatic deployments or paid hosting.
