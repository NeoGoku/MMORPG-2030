# Architecture v0.7 — integrated Nordtor slice

Godot 4.3/GDScript Compatibility client, standalone .NET 8 authoritative server, optional PostgreSQL 16. Matching JSON WebSocket protocol **v6** required. No production deployment or paid service.

## Modules and shared rules
`Client/data/district.json` contains walkability/spawns/combat constants; `game.json` contains classes/skills/items/recipes/quests/professions and provisional balance. Both are exported/copied with builds. Server `World` advances at 20 ticks/sec under a lock. `Characters.cs`, `PetRules.cs`, `World.Progression.cs`, `World.Effects.cs`, `World.Social.cs`, `World.Trade.cs` and `World.Farming.cs` implement integrated character, combat, pets, social/economic and automation behavior. Account/ticket ownership lives in `Accounts.cs`/`Session.cs`; `Persistence/GameStore.cs` owns versioned storage.

Client `main.gd` switches explicit practice/online modes and reconciles entity presentation. `practice.gd`/`progression.gd` implement temporary local training; no fabricated peers, account uploads or silent substitution of server rewards. `lobby.gd`, `character_menu.gd`, `social_panel.gd`, `hud.gd` and touch controls expose actual server intents. Client/server are separate simulations: targeted cases cover shared behaviors, full numerical lockstep parity is not claimed.

## Authority and integration
Only the server mutates online outcomes. Input contains direction/actions, never damage/items/rewards. Bounded normalized directions, sequence rejection, expiring input, building collision, target/range/cooldown/resource and ownership checks remain. Normal attacks require stationary movement. Monster AI, death/respawn, quest and personal loot remain shared simulation. Death loses a configurable provisional percentage of current-level EXP once per death (M6 default 5%, no delevel); the GDD's final amount remains TBD.

`RewardKill` serves normal attacks, skills, status damage and summons. The killer's eligible nearby group shares one EXP pool, receives individual drops and quest credit. Pets/summons have distinct server-owned entities/state. Taunt, DoT, HoT, slow, armor break and buffs expire on server time; directional skills use collision-aware movement.

Trade clones both participants, revalidates offers, and commits both revisions with `SaveBatch` before updating live state. Manual skills affecting group rewards also save their participants atomically and roll back combat/loot/character changes on storage failure. Routine simulation uses the existing five-second checkpoint/disconnect boundary. Groups/chat/summons/drops are runtime state; pet training uses a persisted UTC deadline.

## Protocol v6
Welcome: `{"type":"welcome","version":6,"id":"character-id","tickRate":20}`.
Movement: `{"type":"input","seq":1,"x":0,"z":-1}`.
Action: `{"type":"action","seq":2,"kind":"chat_private","target":"character-id","text":"Hallo"}`.

Input/actions share an increasing nonnegative 64-bit sequence. Target is at most 64 characters; optional text at most 256 (chat further limited to 240, strips control characters). Full accepted kind list is in `Session.cs`; it includes combat, progression, social, trade, pet and farming intents. Malformed/unknown messages close the connection. Existing 4096-byte text limit, 60 messages/sec, inactivity/send deadlines and 100-session ceiling are transport guards, not capacity evidence.

Snapshots include own `self`, distance-filtered `players`, `monsters`, personal `loot`, `pets`, `summons`, plus shared `environment` and recipient-specific `social`. Social includes party roster/HP, pending invites, authorized chat history, own trade and public online ID/name/level list; distant coordinates are omitted by entity interest filtering. Event counters can coalesce and are not a reliable combat event log. Interest filtering is not world asset streaming: static city geometry is still loaded together.

## Operations and platforms
See [operations](M3_OPERATIONS.md) for registration/tickets, migrations, TLS/origins and durability limits. Online requires a reachable configured server; practice is immediately playable without one. One server process per DB; DB operations under world lock remain a scaling limitation.

CI builds debug APK, Windows EXE/PCK and Web artifacts for 14 days; no deployment. Android uses Java 17/SDK 34 and ephemeral debug signing, so reinstall may be required. Software-rendered scene/touch/UI checks supplement simulation/HTTP/WebSocket/PostgreSQL tests; they do not replace physical Android, Windows runtime or 50–100-player qualification. Exact implemented/partial/missing scope is in [status](IMPLEMENTATION_STATUS.md).

## M5 shared-world module
`SharedWorld.cs` defines versioned housing/guild/market/reward state. `World.Community.cs` dispatches validated intents against cloned world/character state; `World.Housing.cs`, `World.Guilds.cs` and `World.Market.cs` enforce authority and conservation. `GameStore.SaveWorld` commits world CAS and character revisions together. `002_shared_world.sql` is additive. `World.Arena.cs` owns runtime normalized consensual matches and atomic daily account rewards; PvE stats/resources remain separate. New `community`/`arena` snapshot fields are recipient-aware (private storage/visitor lists, own proceeds/invites) alongside public physical ownership/furniture.

`District.Homes/Walls/Encounters` add shared coordinates and collision without instances. AUR-IX warning state is published before blast resolution; boss/drop mapping is shared by manual/pet/farm pickup. `community_city.gd` streams new housing shells within 60/75 m hysteresis and presents actual furniture/telegraphs; old city content is still static. `community_panel.gd` binds touch forms to bounded v5 intents. See M5_OPERATIONS.md for exact constraints and remaining scaling/streaming boundaries.

## M6 evolution — protocol v6
Quest rules now validate prerequisites/level/class and grant items/points atomically with character saves; UTC daily counters persist in the aggregate. Reset/prestige consume actual inventory items. Auction pages filter expired offers without deleting escrow; sellers reclaim through the existing atomic cancel path. Guild chat snapshots select only online guild recipients. Additive fields require no destructive migration. World/process/checkpoint limitations remain unchanged. See DEVELOPMENT_HANDOFF.md.

## Seven-package continuation — protocol v7
GearState/EquippedGear add stable per-item identities alongside preserved legacy pristine stacks. Explicit unpack is reversible at the data-backup level and consumes exactly one stack item; no silent destructive migration. Condition moves with gear through trade/auction/home storage. ClassRules derives passive bonuses and resource regeneration from catalogue data. Shared snapshots include appearance/equipment, profession/resource and normalized arena effect state. SpatialIndex builds 24 m cells once after world mutation and exact distance checks remain authoritative. Arena disconnect/zone/expiry guards run before effects/rewards. One process per DB, synchronous DB saves, runtime monsters/matches and routine checkpoint boundaries remain unchanged.

## Issue #8 durable-play extension
Creation carries validated appearance through the owned-character API; a shared preview widget renders the selected class/appearance in a local UI SubViewport (not a world/housing instance). Guild GearStorage uses the same world+character CAS as owner storage. StallState publishes nearby physical props; stall purchases route privately into MarketAction only after ownership/range/expiry checks, sharing escrow/fee/proceeds logic. Closing converts remaining stock to ordinary exchange offers without reconstructing items. Only Vendor-marked item definitions can be purchased from NPCs. Quest snapshots provide navigation targets; the client tracks them without submitting progression outcomes. All additions remain within the one-server/DB and checkpoint boundaries.

Issue #8 release boundary: protocol v8 matches the creation/target/stall/guild-instance contract. Existing JSONB records gain additive fields; migrations 001/002 remain valid. Deployment must pair matching client/server and retain backups of legacy stacks/instances.
