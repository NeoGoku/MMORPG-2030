# Architecture v0.2 — multiplayer foundation

Godot 4.3/GDScript Compatibility client, .NET 8 authoritative server and JSON WebSocket transport. Engine selection remains provisional until Android device, Windows and browser runtime checks meet M1. PostgreSQL remains planned; no persistence is implemented.

## Implemented boundary
- `/health`: readiness metadata; explicitly reports `persistenceReady: false`.
- `/ws`: one temporary server-issued identity per connection; one shared in-memory world, maximum 100 sessions. This is a resource cap, not evidence of 100-player performance.
- `World` advances movement at 20 Hz, speed 5 units/second and bounds ±19 on X/Z. These are provisional slice constants, not ratified gameplay rules. Input expires after 250 ms.
- Each session has one receive loop and one independent bounded send loop. Slow sends time out after 3 seconds; no client can stall the world tick. Receive inactivity times out after 10 seconds. Disconnect removes the session.
- Client only sends direction and a monotonically increasing sequence. Server normalizes direction, ignores stale sequence numbers and never accepts client positions. Text frames may be fragmented but messages are limited to 4096 bytes, 60 messages/second. Invalid messages end the session.
- Godot polls WebSocketPeer, sends inputs at most 20 Hz, interpolates snapshot positions and renders local/remote capsules. Reconnection creates a new temporary identity. Keyboard and basic touch arrow buttons are available.

## Protocol v1
Server first: `{"type":"welcome","version":1,"id":"server-generated","tickRate":20}`.
Client: `{"type":"input","seq":1,"x":0.5,"z":-1}`. Finite numeric direction components must be within ±1000; normalized length never exceeds 1. Sequence is a non-negative signed 64-bit integer.
Server: `{"type":"snapshot","tick":1,"players":[{"id":"…","x":0,"z":0}]}`.
All players currently receive all session positions. No spatial interest management yet.

## Deployment and next boundaries
No production server is deployed. Public use requires authentication, authorization, TLS (`wss://`), connection/abuse protection, origin policy and operational monitoring. Browser HTTPS pages require WSS; client refuses mixed-content addresses. Do not expose the unauthenticated foundation as a production service.

PostgreSQL will own accounts, three character slots, inventory, quests, housing, pets and economy. Future item transfers require transactions. Persistence, combat, NPCs, character creation and reconnect identity are not implemented. Interest management and load tests must precede claims of 50–100-player capacity. The GDD remains authoritative; this foundation does not redefine its mechanics.

## Cloud builds
GitHub Actions checks PRs and main using Godot 4.3 and matching templates. Separate Android-debug APK, Windows EXE/PCK and single-thread Web artifacts have 14-day retention. Android uses Java 17, SDK 34/build-tools 34.0.0 and a temporary debug keystore. Each CI run gets a new debug key; installation over an older debug APK may require uninstalling it. No release signing keys or hosting accounts are required. Web artifacts must be served via HTTP(S), not opened as local files. No production deployments or paid services.
