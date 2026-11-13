# Development queue — MMORPG-2030

Canonical feature requirements: docs/GDD.md. Verify implementation status in current PRs before claiming any item missing or complete.

## P0 — Durable, end-to-end play
- [ ] Reconcile PR #1 through #9 and later PRs; identify merged versus open changes.
- [ ] Complete account auth, reconnect, character persistence and migration-safe PostgreSQL operations.
- [ ] Complete server-authoritative inventory/equipment, quest rewards and atomic trade flows.
- [ ] Make all eight classes' skills, resource costs and effects genuinely usable in online play.
- [ ] Ensure mobile UI exposes inventory, character stats, skills, quest log, chat, pet and housing.
- [ ] Maintain reproducible Android/Windows/Web exports; Android acceptance only at larger milestones.

## P1 — World and content
- [ ] Expand meaningful playable city districts, wilderness and region traversal.
- [ ] Implement open-world dungeons and multi-stage world bosses with party mechanics.
- [ ] Implement quest chains, class/profession quests and optional story decisions.
- [ ] Finish equipment variety, appearance editor and visible equipment.
- [ ] Implement spatial streaming/interest management without player channels.

## P2 — Persistent social/economy
- [ ] Finish physically situated housing, floors, furniture crafting, production and access control.
- [ ] Finish guild quests, skills, guildhouse permissions and designated-zone wars.
- [ ] Finish auction escrow, player stalls and safe recovery after restarts.
- [ ] Finish pet breeding/evolution, offline training, summoner separation and all six pet roles.
- [ ] Finish ten professions, mastery and connected crafting economy.

## P3 — Quality and endgame
- [ ] Finish normalized PvP, prestige, infinite PvE stat progression and configurable death XP loss.
- [ ] Improve navigation and guarded auto-farm without infinite loops.
- [ ] Improve semi-realistic licensed/original assets, animation, lighting and mobile LOD.
- [ ] Performance and security qualification for 50–100 players; no unsupported capacity claims.
- [ ] Production deployment documentation without provisioning paid services.

## Selection rules
Prioritize vertical end-to-end completion over superficially touching every item. Re-evaluate from actual code, tests and PR state at each new task. Update checkboxes only with evidence and record proof in docs/IMPLEMENTATION_STATUS.md.
