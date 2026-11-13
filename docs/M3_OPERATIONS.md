# M3 identity and persistence operations

Historical M3/M4 details below. Current M5 requires protocol v5 and adds migration 002; see [M5 operations](M5_OPERATIONS.md).

No public server, paid service, external account permission or production database was provisioned. Tests use an isolated local PostgreSQL 16 container and CI PostgreSQL service, with deliberately disposable test credentials.

## Reproduce locally or in an existing cloud workspace

1. Copy `.env.example` to the ignored `.env`, replace `POSTGRES_PASSWORD` with a unique password and set exact `MMORPG_WEB_ORIGINS` for the Web frontend.
2. Run `docker compose up --build`. `compose.yml` binds database/server ports to loopback and keeps data in the named `mmorpg-data` volume. Do not delete the volume to update the server.
3. Alternatively run PostgreSQL 16 yourself and launch `.NET 8` with `MMORPG_DATABASE` set to the Npgsql connection string and `dotnet run --project Server --urls http://127.0.0.1:5080`.
4. Migration scripts in `Server/Migrations` run transactionally once, recorded in `schema_migrations`, with an advisory migration lock. Current migration creates accounts and a versioned character aggregate. Back up an existing database before future schema upgrades; no automatic down/drop migration is provided.
5. Without `MMORPG_DATABASE`, the server explicitly reports `persistenceReady:false`; accounts and characters are RAM-only. This is a test backend, not a durable alternative.

The PostgreSQL migration/store and reconnect path were exercised against a real database. The Dockerfile/Compose configuration is supplied for reproducibility; a full image deployment is not a production qualification.

## Android / Web connection

Open **Online**, enter the HTTPS server root (no `/ws`), register/login, select one of three slots, create nickname/class, then choose **Online: Spielen**. Passwords are cleared after requests and never written to client preferences. Only the nonsecret server URL is remembered. The six skill buttons show equipped skills; casting is gated by the skill unlock level. Training remains separate and temporary, with three in-session local slots.

A phone needs a reachable HTTPS/WSS endpoint; loopback URLs point at that phone. This change does not create hosting or a TLS certificate. For browser requests configure the exact frontend origin in `MMORPG_WEB_ORIGINS`; use an approved reverse proxy/TLS setup before exposing the service. Plain HTTP credentials are permitted by the client only on `localhost`/`127.0.0.1` for local development. Do not forward raw HTTP database or account endpoints publicly.

## API / protocol v3

- `POST /api/register`, `/api/login`: username + password, PBKDF2-SHA256 (600,000 iterations, per-account random salt), constant-time hash comparison, shared per-IP auth request limit. Session token is 256 random bits, expires in 12 hours, only its hash remains on server, and sessions expire on restart.
- Bearer-authenticated `GET/POST /api/characters`: account-scoped list/create; slots 0–2, fixed class IDs, validated nickname. Appearance is currently a shared preset, not the full GDD editor.
- `POST /api/ticket`: owned character ID → one-use 30-second ticket. WebSocket `/ws` expects an initial text `{"type":"auth","ticket":"…"}` before welcome. No token in URL/log query strings. Active or pending-save characters cannot join twice. Re-login restores the saved identity.
- Explicit `MMORPG_ALLOW_GUEST=true` enables isolated legacy regression/development connections. It is disabled by default and must never be enabled on a public authenticated service. Guest characters are not durable accounts.
- Matching client/server v3 required. Existing bounded input, message size, sequencing, speed and collision checks remain. Outcomes and character documents are never accepted from clients.

## Transaction/durability boundary

Inventory, equipment, quests, EXP, attributes, pet state and currency live in one server-generated JSONB character aggregate. PostgreSQL save updates that aggregate, level/coin constraints and optimistic revision together in a transaction. Stale writes fail; owned slot uniqueness is enforced by SQL. Economic/action saves are synchronous; failures roll back character/loot/monster mutations. Snapshots copy their data before leaving the world lock. Character state is reloaded under that lock at join to avoid stale-ticket/disconnect races.

Movement, normal combat and automatic pet outcomes are checkpointed every five simulated seconds and at orderly disconnect. A process crash may lose that last checkpoint interval; durable world-monster/drop persistence is not implemented. Cooldown/resource/HP state is saved to prevent reconnect-reset exploits. Failed disconnect saves stay pending and block rejoin until successful retry. Operate **one authoritative world-server process per database**: multi-process active-character leasing/partitioning is not implemented.

Database operations currently run under the world lock with five-second connection/command limits. This preserves initial transaction ordering but is a performance/outage bottleneck, not a claim of 50–100-player readiness. A queued persistence/command architecture and crash/load testing remain required before production. No production security certification is claimed.

## M4 extension — protocol v4 and batch transactions

M4 requires **v4**, replacing the historical v3 references above. Each class has eight active skills unlocked by level, six equipped visible slots adjustable in **Skills**. **Gruppe / Chat / Handel** opens real social/trade controls. Select an online character, invite or open trade; exchange requires both confirmations of unchanged offers within 5 m. A modified offer clears confirmations; trade expires after 120 simulated seconds and cancels on disconnect. Only authenticated characters can trade.

`IGameStore.SaveBatch` updates up to ten distinct owned aggregates in deterministic ID order inside one PostgreSQL transaction. Optimistic revision failure on any row rolls back all rows. In-memory mode prevalidates all rows but is still nondurable. No auction escrow, stalls or repair service exists. Manual skill group-reward persistence uses this batch boundary; normal simulation remains checkpointed.

M4 state additions are backward-compatible JSON fields, not a destructive migration. Old companion IDs normalize from character identity; skill bar defaults to the first six entries. Pet training is persisted as a UTC deadline, completed on login/tick, awards once and needs an inactive selected pet/feed/workbench to start. Server restart removes temporary summons/groups/chat/trade/farm runtime state. Group members can reconnect with the same saved identity within 120 simulated seconds; leadership may have transferred. Account sessions themselves still expire on restart.

Guarded autofarm is opt-in at the safe city edge (for example around 0,3), requires 60% HP and distance from monster homes, caps radius at 14 and combat at 60 seconds using simulation plus real UTC time. Return can outlast that phase; no further attacks then. Manual movement cancels. Stuck return aborts safely. Farming is not offline gameplay; do not describe it as a background reward service.

Provisional defaults are in `game.json`: profession +10 gather/+25 craft, level per 100 EXP, conserved group kill EXP within 40 m, chat radius 30 m, entity interest 52 m, pet breeding level 2/evolution level 5/training 300 s for 50 EXP, day 600 s/weather 120 s. Weather is visual only. Existing M3 profession counters retain their value as initial EXP. These choices do not ratify unresolved GDD balance.
