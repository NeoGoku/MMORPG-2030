# Cross-platform multiplayer foundation implementation plan

Goal: repair the scene, deliver CI exports, then connect Godot to an authoritative .NET server.
Architecture: Godot 4.3 Compatibility and JSON WebSocket protocol v1; server-owned session IDs and 20 Hz movement snapshots. In-memory sessions only; no production deployment.
Spec: AGENTS.md and docs/GDD.md. Autonomous execution requested by owner.

- [x] Scene: reproduce missing-parent error, add a load/instantiate regression check, fix child parents, verify once.
- [x] Exports: PR triggers, pinned engine/templates, Java 17 + Android SDK 34/build-tools 34.0.0 and ephemeral debug key; single-thread Web; separate strict artifacts and logs.
- [x] Multiplayer: versioned welcome/input/snapshot protocol, bounded messages, validated finite input, normalized speed and bounds, 100-session cap, disconnect cleanup. Godot endpoint UI, connection state, remote avatars, keyboard/touch input.
- [x] Evidence: real two-client integration test including malformed messages, movement limits, disconnect and reconnect; scene check and CI exports. Update architecture, roadmap, status and usage docs.
- [x] Review diff, commit, publish feature branch and open GitHub PR. No merge or paid hosting.

Review focus: fragmented/oversized frames, invalid JSON/numbers, slow peers, stale input, HTTPS browser transport. Production auth, persistence, TLS hosting and load validation remain explicit follow-up work.

Execution evidence: scene and touch regressions RED→GREEN; .NET Release 0 warnings/errors; real Python and Godot two-client tests passed; Windows/Web CI exports and server CI passed. Independent review found touch layout issue, fixed and covered. Android SDK action default requested removed `tools` package; corrected to explicit `platform-tools`. Browser rendered Web export successfully; labels changed to plain text after screenshot showed missing arrow glyphs. No paid hosting, merge, account permission changes or persistence.

Android second run: SDK configuration passed; export validation failed with empty error. Godot 4.3 has_valid_project_configuration explicitly rejects disabled ETC2/ASTC imports without adding text; enabled rendering/textures/vram_compression/import_etc2_astc for cross-platform builds.
