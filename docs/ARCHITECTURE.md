# Architecture v0.1 — proposed, not validated
- Godot 4 GDScript client: Android, Windows, Web (Compatibility renderer). Test export and performance before locking engine.
- Authoritative .NET 8 server with WebSocket transport initially; evaluate latency and browser constraints.
- PostgreSQL for accounts, characters, world objects, housing, pets, quests, inventory and trades. Transactions for item transfer.
- Modular monolith initially; interest management by spatial cells; no player channels. 50–100 concurrent players initial target.
- Client renders and predicts/interpolates; server owns movement validation, combat, items, currency, progression and auto-farm rules.
- Housing interiors exist at physical locations; persistence and visibility must be optimized.
- GitHub Actions generates Android debug APK, Windows and Web artifacts. CI does not automatically deploy production servers.
- Do not store tokens, DB passwords or signing keys in repository.
- No cloud VPS is needed for first local/network simulation milestone. Hosting requires explicit owner approval.
