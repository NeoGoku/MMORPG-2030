# Playable city implementation plan

Goal: turn the foundation into a complete small playable city/combat/quest loop.
Spec: docs/superpowers/specs/2026-11-12-city-slice.md; AGENTS.md; docs/GDD.md.
Architecture: shared district data; .NET authoritative simulation; explicit local Godot training; protocol v2; modular city/actor/input/HUD code.

- [x] Define district data and write failing gameplay scenario checks: accept quest, reject far interaction, normalize movement/collisions, target/range/stationary attacks, kill credit, loot ownership/double collection, rewards once, death/respawn.
- [x] Implement .NET world model and session intents/snapshots; keep bounded transport and independent sends.
- [x] Implement Godot training from shared data, network v2 and actor/city presentation; joystick, third-person camera and target/combat/quest HUD.
- [x] Run one coherent integration batch: server build/scenarios + transport, Godot parse/gameplay/scene/network tests, rendered browser interaction/screenshot. Correct specific observed failures only.
- [ ] Update architecture, status, roadmap, asset provenance and Android usage; focused review; commit and PR; verify CI Android/Windows/Web artifacts.

Review focus: expired/replayed intents, client-supplied reward spoofing, double kill/loot/reward, movement through buildings, targeting dead/far enemies, local/online separation, multi-touch release and UI inside landscape viewport. No automatic production deployment or paid services.

Execution notes: target-selection regression found by independent read-only review, reproduced RED then fixed; new check separates selection and attack by several ticks. Real touch-event test reproduced synthetic mouse double-action, fixed by accepting emulated mouse GUI events before BaseButton. Godot dummy-renderer cleanup error reproduced with one primitive mesh; rendered checks use Xvfb/OpenGL instead of hiding errors. Local/server scenario suites and real transport passed before final fixes; final integrated proof and CI follow.

Final local proof: .NET full scenarios, local Godot gameplay, Python real transport, two Godot client transport, OpenGL scene/multi-touch and rendered remote-avatar tests pass. Exported Web starts in Chromium with no JS/console errors. Fresh reviewer found one blocking online-target issue; fixed with RED→GREEN regression. No paid services. Documentation and original-asset provenance updated. CI/PR completion is the final step.
