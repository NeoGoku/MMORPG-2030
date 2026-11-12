# MMORPG-2030 — Game Design Document v1.1
Status: ratified concept. Implementation is partial and tracked in docs/IMPLEMENTATION_STATUS.md; the current city slice does not replace or finalize these requirements. Unresolved mechanics remain TBD.

## Vision
3D third-person semi-realistic MMORPG set in an alternate 2030 where magic is publicly known. Several large cities in a seamless open world. Shared persistent world without channels, initial 50–100 concurrent players. Android APK, Windows and browser crossplay.

## Character and combat
Three character slots; nickname, gender, face/body, hairstyles, tattoos. Level cap 100. Eight fixed classes: Dark Mage, Light Mage, Archer, Shield-and-Sword, Dual Swords, Healer, Creature Summoner, Tank. Classic FlyFF-like tab-target, stationary normal attacks, auto-attack, six visible skills, approximately 8–12 active skills per class. At least eight attributes (names/formulas not finalized); one freely allocatable point per level. Unlimited additional farmable stat points from professions/quests with exponentially increasing EXP costs. Rare reset item. EXP from monsters, quests, dungeons, professions, exploration and PvP; death loses EXP (amount TBD).

## World and content
Dynamic day/night and weather with gameplay effects, limited destructible objects, teleportation and mounts. Fixed monster levels by region; mutated animals, demons, hostile humans, robots, magical creatures, world bosses. Classic simple monster AI. Open-world dungeons without instances, personal group loot, groups up to ten.

## Pets
One active companion pet; combat, healing, tanking, buffs, loot gathering and resource finding. Breeding by crossing, levels, evolution, automatic recovery after defeat, tradeable pets, offline training. Relationship to summoner summons TBD.

## Professions and economy
All players can learn all ten: mining, herbalism, woodcutting, smithing, tailoring, alchemy, engineering, cooking, building/furniture, animal breeding. Recipes and workbenches/NPCs, rarity Normal through Legendary, repairs. Auction house, direct trades, stalls, NPC vendors, commissions; almost everything tradeable.

## Housing
Intro quest grants housing access. Physical apartments in city buildings, not private instances. Expandable rooms/floors, free furniture placement, storage, workshops, gardens, production, breeding, visitors/permissions, recurring rent. Property scarcity and city expansion rules TBD.

## Quests and social
Optional main story and sandbox play; main, side, daily, class, profession, world event and hidden quests. Brief NPC dialogues, minimap navigation, fixed specialist NPCs. Decisions affect story and faction standing. Guild levels, guildhouses, guild quests/wars, guild storage and skills. Text, private, party and guild chat, emotes.

## Automation and PvP
Auto-attack, potions, quest turn-in, loot, navigation, limited guarded auto-farming with radius, resource and safety constraints; exact limits TBD. PvP only in arenas and designated PvP zones, with stat normalization; infinite farmed stats remain effective in PvE.

## Monetization and endgame
Free game, no real-money shop, premium currency or paid convenience. All cosmetics and conveniences earned in game. Funding TBD. Prestige, stat farming, profession mastery, pet breeding, housing, guild progression and endgame gear. No seasons.

## Technical intent
Android-first touch controls, landscape, customizable joystick and skill UI; Windows keyboard/mouse; browser performance adaptations. Server-authoritative security, streaming, persistence. Engine not irrevocably selected; Godot 4/GDScript is the current candidate.
