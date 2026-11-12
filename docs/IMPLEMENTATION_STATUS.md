# Implementation status — Nordtor city slice
Updated: 2026-11-12. GDD v1.1 remains authoritative; this is a small playable vertical slice, not a finished MMORPG.

## Implemented
| Area | Actual scope |
| --- | --- |
| City | Original procedural 3D district, six solid buildings, roads/crosswalk, sidewalks, plaza, gate, lamps, trees, benches, skyline and directional lighting. Buildings have matching server/local collision footprints and camera collision bodies. No accessible interiors. |
| Character/camera | Visible articulated sword/shield humanoid, walking/swing/death animation, third-person follow and right-side touch orbit/right-mouse drag. This appearance is not an implementation of all classes. |
| Android controls | Analog left joystick, independent action touch, target cycle, attack toggle, NPC interaction and loot buttons. Focus loss releases movement. Camera and action UI are separate from the joystick. |
| Immediate play | Explicit **local training** starts on launch without a server/account. One local player, three simulated enemies. No invented remote players. All progress is temporary. |
| Online | Real Godot WebSocket connections to the .NET server; shared player positions/yaw/HP/levels/attack and hurt counters, shared monster positions/HP/deaths/respawns. Blue remote humanoids and green local character. Online requires a reachable user-hosted server. |
| Combat | Three hostile security drones with patrol/chase/leash, range checks, stationary auto-attacks, player/monster HP, visible damage numbers, death and respawn. No PvP. Numbers are provisional slice balancing. |
| Quest | Mira offers one three-drone hunt; kills count after acceptance, then return to NPC in range. One-time 60 EXP / 25 coin reward and heal. No branching story or quest catalogue. |
| EXP/loot | 20 EXP per kill; level threshold 100 × current level, cap 100, one earned stat point per level (allocation UI pending). Personal nearby loot: 4 coins + 1 scrap per kill. Expiring drops, no duplicate pickup/reward. Coins/scrap are counters, not a full inventory/economy. |
| Build delivery | Android debug APK, Windows EXE/PCK and single-thread Web GitHub Actions artifacts. Existing signing/SDK/texture/icon fixes preserved. No paid services or deployment. |

## Verification evidence
- Deterministic .NET scenario program covers NPC distance/acceptance, persistent target selection between ticks, moving versus stationary attacks, collision, kill credit, personal/double loot, quest/reward once, level/stat-point progression, monster damage/death/player respawn and replay rejection.
- Local Godot training scenario checks the same quest/combat/loot/progression loop and collision.
- Real two-client WebSocket test covers movement limits, stale/replayed input, malformed/oversized/binary/fragmented messages, disconnect/reconnect, quest actions, shared attack/monster HP, personal state and ignored client reward spoof fields.
- Two actual Godot clients exercise protocol v2 movement over the real server. A rendered main-scene test also verifies a real remote humanoid moves, disappears on disconnect and never silently becomes an offline substitute.
- Rendered Godot/OpenGL scene test checks startup, visible humanoid and camera view, HUD buttons in viewport, analog touch, simultaneous joystick/action, neutral release and exactly one action despite touch-to-mouse emulation.
- Exported Web scene loaded in Chromium; screenshots/runtime evidence and final CI build results are recorded in the PR. Scene tests use a real renderer because Godot 4.3's dummy renderer reports a primitive-mesh cleanup error, reproduced independently with a single capsule.

## Partial / untested / not implemented
- Actual Android phone gameplay for this new build, multi-device crossplay, performance/battery/accessibility and native Windows runtime are not certified by cloud export success. Android APK has not been manually operated on a physical device here.
- No accounts, three-slot character creation, reconnect identity or PostgreSQL persistence. Offline training and online progress are separate and lost on reset/disconnect/server restart.
- No full eight-class skill/attribute system, stat allocation/reset, durable inventory, equipment or trading. A displayed earned stat point does not yet change attributes.
- No full quests, NPC catalogue, professions, pets, housing, guilds, parties, economy, PvP, weather or seamless streaming. All GDD requirements remain in the roadmap.
- No production auth/TLS deployment, spatial interest management, load-test evidence or 50–100-player capacity claim. The 100-session cap is only a resource limit.
- Current art is original stylized placeholder geometry; semi-realistic final production art remains future work. No sound or imported character animations.

M1: cloud exports established; physical device and crossplay qualification pending. M2: playable city/combat/quest/loot slice exists; persistence and character creation remain incomplete.
