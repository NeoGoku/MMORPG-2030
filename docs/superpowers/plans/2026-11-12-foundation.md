# Cross-platform multiplayer foundation implementation plan

Goal: repair the scene, deliver CI exports, then connect Godot to an authoritative .NET server.
Architecture: Godot 4.3 Compatibility and JSON WebSocket protocol v1; server-owned session IDs and 20 Hz movement snapshots. In-memory sessions only; no production deployment.
Spec: AGENTS.md and docs/GDD.md. Autonomous execution requested by owner.

- [x] Scene: reproduce missing-parent error, add a load/instantiate regression check, fix child parents, verify once.
- [x] Exports: PR triggers, pinned engine/templates, Java 17 + Android SDK 34/build-tools 34.0.0 and ephemeral debug key; single-thread Web; separate strict artifacts and logs.
- [x] Multiplayer: versioned welcome/input/snapshot protocol, bounded messages, validated finite input, normalized speed and bounds, 100-session cap, disconnect cleanup. Godot endpoint UI, connection state, remote avatars, keyboard/touch input.
- [ ] Evidence: real two-client integration test including malformed messages, movement limits, disconnect and reconnect; scene check and CI exports. Update architecture, roadmap, status and usage docs.
- [ ] Review diff, commit, publish feature branch and open GitHub PR. No merge or paid hosting.

Review focus: fragmented/oversized frames, invalid JSON/numbers, slow peers, stale input, HTTPS browser transport. Production auth, persistence, TLS hosting and load validation remain explicit follow-up work.
