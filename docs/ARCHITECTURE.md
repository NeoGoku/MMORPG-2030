# Architecture v0.5 — integrated Nordtor slice

Godot 4.3/GDScript Compatibility client, standalone .NET 8 authoritative server, optional PostgreSQL 16. Matching JSON WebSocket protocol **v4** required. No production deployment or paid service.

## Modules and shared rules
`Client/data/district.json` contains walkability/spawns/combat constants; `game.json` contains classes/skills/items/recipes/quests/professions and provisional balance. Both are exported/copied with builds. Server `World` advances at 20 ticks/sec under a lock. `Characters.cs`, `PetRules.cs`, `World.Progression.cs`, `World.Effects.cs`, `World.Social.cs`, `World.Trade.cs` and `World.Farming.cs` implement integrated character, combat, pets, social/economic and automation behavior. Account/ticket ownership lives in `Accounts.cs`/`Session.cs`; `Persistence/GameStore.cs` owns versioned storage.

Client `main.gd` switches explicit practice/online modes and reconciles entity presentation. `practice.gd`/`progression.gd` implement temporary local training; no fabricated peers, account uploads or silent substitution of server rewards. `lobby.gd`, `character_menu.gd`, `social_panel.gd`, `hud.gd` and touch controls expose actual server intents. Client/server are separate simulations: targeted cases cover shared behaviors, full numerical lockstep parity is not claimed.

## Authority and integration
Only the server mutates online outcomes. Input contains direction/actions, never damage/items/rewards. Bounded normalized directions, sequence rejection, expiring input, building collision, target/range/cooldown/resource and ownership checks remain. Normal attacks require stationary movement. Monster AI, death/respawn, quest and personal loot remain shared simulation. Death EXP penalty is still an unresolved GDD amount and not applied by this slice.

`RewardKill` serves normal attacks, skills, status damage and summons. The killer's eligible nearby group shares one EXP pool, receives individual drops and quest credit. Pets/summons have distinct server-owned entities/state. Taunt, DoT, HoT, slow, armor break and buffs expire on server time; directional skills use collision-aware movement.

Trade clones both participants, revalidates offers, and commits both revisions with `SaveBatch` before updating live state. Manual skills affecting group rewards also save their participants atomically and roll back combat/loot/character changes on storage failure. Routine simulation uses the existing five-second checkpoint/disconnect boundary. Groups/chat/summons/drops are runtime state; pet training uses a persisted UTC deadline.

## Protocol v4
Welcome: `{"type":"welcome","version":4,"id":"character-id","tickRate":20}`.
Movement: `{"type":"input","seq":1,"x":0,"z":-1}`.
Action: `{"type":"action","seq":2,"kind":"chat_private","target":"character-id","text":"Hallo"}`.

Input/actions share an increasing nonnegative 64-bit sequence. Target is at most 64 characters; optional text at most 256 (chat further limited to 240, strips control characters). Full accepted kind list is in `Session.cs`; it includes combat, progression, social, trade, pet and farming intents. Malformed/unknown messages close the connection. Existing 4096-byte text limit, 60 messages/sec, inactivity/send deadlines and 100-session ceiling are transport guards, not capacity evidence.

Snapshots include own `self`, distance-filtered `players`, `monsters`, personal `loot`, `pets`, `summons`, plus shared `environment` and recipient-specific `social`. Social includes party roster/HP, pending invites, authorized chat history, own trade and public online ID/name/level list; distant coordinates are omitted by entity interest filtering. Event counters can coalesce and are not a reliable combat event log. Interest filtering is not world asset streaming: static city geometry is still loaded together.

## Operations and platforms
See [operations](M3_OPERATIONS.md) for registration/tickets, migrations, TLS/origins and durability limits. Online requires a reachable configured server; practice is immediately playable without one. One server process per DB; DB operations under world lock remain a scaling limitation.

CI builds debug APK, Windows EXE/PCK and Web artifacts for 14 days; no deployment. Android uses Java 17/SDK 34 and ephemeral debug signing, so reinstall may be required. Software-rendered scene/touch/UI checks supplement simulation/HTTP/WebSocket/PostgreSQL tests; they do not replace physical Android, Windows runtime or 50–100-player qualification. Exact implemented/partial/missing scope is in [status](IMPLEMENTATION_STATUS.md).
