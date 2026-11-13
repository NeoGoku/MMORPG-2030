# M5 shared world operations

M5 builds on the unmerged M4/M3 branches; integrate those PRs first, then retarget the new PR to main. Matching client/server **protocol v5** is required. No public host, paid service or production deployment is created.

## Durable shared state

The additive `002_shared_world.sql` migration creates one versioned JSONB world row. `LoadWorld` reads housing, guilds, escrow auctions, pending seller proceeds and arena reward guards. `SaveWorld(world, characters)` checks the world revision and updates that row plus up to ten distinct character aggregates in one SQL transaction. A stale world/character aborts the entire transfer. The in-memory backend has the same CAS boundary but remains explicitly nondurable.

Before upgrading an existing database, back it up. Run the existing server/Compose setup from M3_OPERATIONS.md; migrations execute transactionally under the existing advisory lock. This migration does not delete old characters or change account ownership. A rollback to old code must preserve the new table; old clients cannot use v5. Do not delete the named production volume to update.

One authoritative process per database remains required. The shared aggregate is cached in that process and serialized under the world lock; it is not a multi-process lease, shard or scaling architecture. DB work is synchronous. Successful economic/housing/guild commands persist immediately. Routine PvE movement/combat/pets still use five-second/disconnect checkpoints. Monsters/drops/summons/group chat and arena matches are runtime state and reset after restart. Saved housing/guild/auction/pet-training identity survives.

## Housing and guilds

Elin's register is at (30,24). Accept the housing intro; gather three spare wood and craft an oak chair (chair recipe itself needs three wood + one ore). Claim one of four free Lindenhof apartments. The first rent week is included. One apartment per character; finite stock is a deliberate slice capacity, not the final GDD scarcity policy.

The physical entrance is at (home.x-4, home.z+6), with an actual doorway and walls in the shared world. The initial west room opens an adjacent east room for eight wood and 20 coins. No private instance or hidden duplicate interior. Only one ground floor is implemented. Owner, explicit character-ID visitors or permitted guild members can enter. Revocation immediately ejects visitors; periodic checks handle rent expiry and guild changes. Owner access/removal/storage retrieval remain possible during arrears. Furniture uses world X/Z and yaw; input is finite, bounded by unlocked rooms, overlap-checked and kept away from the doorway. Place consumes an inventory chair; removal returns it only if inventory has space. Other furniture types are pending.

Weekly rent is ten coins with an explicit pay action; arrears suspend visitor access and productive actions. No auto-debit, eviction or item destruction. Housing storage (999 per item), existing recipes, pet breeding and a single five-minute herb planter work inside the apartment. Timers use UTC and survive restart. All rates/costs remain provisional slice balance.

Guilds cost 20 coins to register, have up to 40 members and leader/officer/member roles. Invitations last 24 real hours and expire out of the invitation cap. Storage withdrawal is officer/leader-only; deposits are open to members, at Elin or the acquired physical hall. Material delivery quests consume three wood/two ore, grant 25 guild EXP and energy plus five coins; guild level is `1 + EXP / 100`. Energy funds the five-minute +2 PvE attack blessing without reducing earned level. One hall is currently available for 20 wood/ten ore from guild storage. Guild membership, ranks, quest counts, buff, hall and inventory persist. Leader must transfer leadership before leaving a populated guild. A guild holding a hall cannot currently dissolve; release/transfer UI is future work.

## Auctions

The exchange is at (30,12). Search by name from anywhere; list/buy/cancel/claim proceeds at the board. Listings escrow actual unequipped items, up to 99 units and 100,000 total coins, ten offers per seller/200 globally. Search returns the newest 40 matches; own offers have a separate list. Offers stay until purchase/cancellation (no expiration yet). Successful buy deducts buyer coins, transfers items and records offline seller proceeds together; a five-percent rounded-up fee (minimum one coin) leaves circulation. Claim and cancel are single-use operations. A full buyer/canceller inventory rejects without losing escrow. These limits are not production marketplace load evidence.

## Arena

Only the marked square x=-62..-38, z=23..47 permits arena actions. Both sides explicitly invite/accept; duels and two-to-five-member party teams are implemented. Every team member must toggle ready, be in-zone, have a distinct account and be outside another match. Guild teams must belong to two different guilds. No territory war or damage outside the arena is enabled.

Match HP=100, hit=15, reach=3.2 and cooldown=0.8s, regardless of PvE farmed stats/gear. PvE HP/mana remain separate; PvE skills are unavailable within a match. The current normalized arena has one common attack, not all 64 skills balanced for PvP. Leaving/disconnecting/timeout/forfeit ends the match without rewards. A genuine knockout win can grant one arena token per account per UTC day; duplicate wins cannot repeat payout. Guild-team wins can grant guild EXP alongside an eligible reward. Reward and daily guard persist atomically. No combat-season, ranking or advanced anti-collusion claim.

## World and client

Connected Lindenhof housing, exchange, arena, industrial constructs and open Tiefwerke ruins use the same coordinates. Five new encounters have fixed levels 3–8, including AUR-IX with a 2.5-second warned radius-five blast and a cooperative armor mechanic (two recent attackers remove the solo damage reduction). Boss cores feed a smithing recipe. Original three-drone hunt stays unchanged. Two new world objectives extend the quest log. The optional transit at (0,-40) requires level five and Mira's completed hunt; exit at (0,-59) is unrestricted. Walking to the ruins remains possible; this is a gate on fast travel, not on the entire zone.

New housing geometry streams in within 60 m and unloads beyond 75 m, including its camera collision bodies. Original Nordtor geometry, industrial buildings and dungeon scenery still load together. Server entity interest remains 52 m. This is partial spatial streaming, not a complete async city/NPC asset pipeline. Weather remains visual. Art is original procedural geometry, not production semi-realistic rigs/textures or a measured Android LOD/texture-compression programme.

## Proof and remaining qualification

CI runs the original server/client scenarios, real PostgreSQL transaction/reconnect checks, M5 housing/guild/auction/arena/boss regressions and a two-client HTTP/WebSocket journey using only public game actions (gather/craft/claim/place/observe/reconnect/auction). The journey sends normal idle inputs so its second client follows the existing session liveness contract; no test-only grant endpoint exists. Run only against a disposable test DB: the housing test deliberately acquires finite world property.

APK, Windows and Web artifacts and exact check links are attached to the M5 PR. Physical Android/Windows acceptance, multi-device crossplay, battery/memory profiling, sustained guild-team load, production security/operations and 50–100-player qualification remain open.
