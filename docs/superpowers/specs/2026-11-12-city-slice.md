# Nordtor playable city slice

Owner requested autonomous implementation, no paid services and bounded verification. Preserve GDD v1.1; constants below are provisional slice balancing, not final class mechanics.

Build on Godot 4.3 + authoritative .NET 8 WebSocket server. A self-created procedural district has roads, sidewalks, collidable buildings, plaza, watch NPC, industrial drone yard, lamps and trees. Art is original mesh geometry/materials, no external assets. An articulated humanoid with sword/shield, third-person orbit camera, analog multi-touch joystick and target/action HUD replaces debug capsules/UI.

APK starts immediately in explicitly labelled offline training. This uses a local simulation with the same world data and protocol-shaped snapshots. Online is an explicit switch to a real WebSocket server; no fake remote players. Connection failures never silently replace online state with offline progress. Offline and online sessions are ephemeral and separate.

Shared district JSON defines bounds, obstacles, NPC/monster spawn positions and combat/quest values. Online server owns movement/collision, monster AI, target validation, stationary auto-attack cooldown/range, HP, damage, deaths/respawns, kill credit, quest state, XP/level and personally owned loot. Client sends direction/action intents only. Snapshots v2 show actual players, monsters, owned loot and combat sequence counters; personal progression returned only to its owner. One NPC offers a three-drone hunt, tracks kills after acceptance and grants one turn-in reward. Kills grant XP and personal scrap/coin drops. Loot must be collected nearby and cannot be claimed twice. Players respawn after death; monsters respawn. No PvP.

Alternatives considered: server-only would leave APK unplayable without reachable hosting; client-only combat would fail visible authoritative multiplayer. Chosen: explicit local practice plus authoritative online, shared data, scenario tests in both runtimes. Cost: simulation rules exist in two languages; parity checks and documented limitations required.

Non-goals: accounts/character creation, PostgreSQL persistence, full eight-class system, skills, full MMO scale, economy, paid hosting, deployment. Protocol v2 intentionally requires matching clients/server. Physical Android gameplay and large-scale load validation remain separate from successful builds.
