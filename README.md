# AI Orchestrator for ServUO

An **AI-powered framework** for ServUO (Ultima Online emulator) that replaces static NPCs with intelligent, goal-driven agents. Hirelings can hold conversations, follow complex commands, fight, harvest resources, crew boats, and react emotionally — all powered by a pluggable LLM backend.

---What was changed:
1. 🧠 AI ORCHESTRATOR CORE (10 files)
File	Lines	What it does
AIOrchestratorInit.cs	108	Central startup — registers ALL 18+ systems with delay calls, reports init status
AIConfig.cs	247	Global config: LLM backends (7 types), model overrides per sub-agent (Combat, Dialogue, Environment, Economy, Faction, Spawner, Dungeon, Narrator — 8 separate models), heartbeat interval, max NPCs per player, max connections, rate limiting, admin commands [aiconfig / [aicfg to view/edit at runtime
LLMClient.cs	241	Universal LLM gateway — wraps Ollama (generate + chat), OpenAI, vLLM, LM Studio, KoboldCpp, TGI, LlamaCpp; concurrent slot gating (max 4 simultaneous), output sanitization (strips truncation artifacts, control chars, partial JSON, non-ASCII pollution), HTTP error handling, streaming timeout
AIHeartbeat.cs	79	Global pulse timer — configurable interval (default 3000ms), dispatches OnHeartbeat to all AI creatures in range of each player, defines IAIOrchestrator interface, SubagentType enum (Combat, Dialogue, Environment, Hireling)
AIEventIntegration.cs	139	Hooks into 17 vanilla ServUO events: CreatureDeath, MobileKilled, CreatureKilled, MobileCreated, ItemCreated, PlayerDeath, Login, Logout, Speech, CraftSuccess, ResourceHarvest, SkillGain, QuestComplete, ItemSold, ItemBought, ContainerLoot, MobileDamaged — all feed into orchestrated AI responses
AIBaseCreature.cs	183	New base class AIBaseCreature : BaseCreature, IAIOrchestrator — creatures that natively carry Memory + 4 sub-agents (Combat, Dialogue, Environment, Hireling); dynamic sub-agent switching based on context; serialization of identity, loyalty, skill gains
AIComponentSystem.cs	287	Attachable AI for ANY NPC — AIComponent wraps sub-agents for non-subclassed creatures; AIComponentRegistry is a global world-wide registry with auto-attach on MobileCreated event; persistence to Saves/AIOrchestrator/ via WorldSave/WorldLoad; RegisterAll() scans entire world, RegisterNear() range-based; [globalai command; 30-second auto-refresh timer
AIComponentCommands.cs	162	Admin commands: [aiglobal toggle, [ainear, [aistatus, [aiinfo (per-NPC status gump with identity, memory, loyalty, active sub-agent), [aidebug
AIMemory.cs	400	Persistent NPC memory — PlayerMemory (per-player relationship, conversation count, last interaction, gifts, favors); AIMemory (identity, persistent data dictionary, serialization); ConversationMemory (recent turns for context); NpcIdentity (name, vocation, homeland, temperament, backstory, speech style, private drive, mood — 8 identity facets); PersonalityTemplates (14 predefined personalities: Brave, Cowardly, Wise, Greedy, Kind, Cunning, Proud, Humble, Mysterious, Fierce, Gentle, Lazy, Energetic, Playful — each with stat modifiers)
NpcIdentityGenerator.cs	90	Generates unique identities per NPC: random name, vocation from 40+ roles (Blacksmith, Tailor, Mage, Warrior, Bard, Farmer, Fisher, Chef, Alchemist, Ranger, Priest, Thief, Miner, Lumberjack, Sailor, Tinker, Baker, Butcher, Veterinarian, Herbalist, Jeweler, Cobbler, Carpenter, Mapmaker, Artist, Scribe, Herder, Gypsy, Merchant, Noble, Knight, Guard, Sailor, Pirate, Hunter, Woodworker, Stonemason, Glassblower, Fletcher, Bowyer, Rope Maker), homeland from 12+ cities, backstory variety
2. 🎭 SUB-AGENT SYSTEM (6 files)
File	Lines	What it does
CombatSubagent.cs	464	AI-driven combat brain — evaluates threat level, selects targets by priority (aggressor > attacker of master > nearest hostile), coordinates with faction system for ally detection, auto-casts heals/buffs, flees at low HP, calls for help, loots corpses, reports battle results
DialogueSubagent.cs	386	Conversation engine — processes player speech, generates contextual NPC responses via LLM, maintains conversation memory (last 5 turns), mood-aware dialogue, personality-influenced tone, faction-aware responses, romantic interaction detection, gossip propagation
EnvironmentSubagent.cs	161	Ambient behavior — idle animations, random movement (wander, patrol, explore), object inspection, weather reactions, day/night awareness, socializing with nearby NPCs, sitting/resting
HirelingSubagent.cs	135	Hireling commands — follow, stay, guard, attack commands, loyalty tracking, skill gain tracking, command gump integration
AmbientGossipSubagent.cs	160	NPC-to-NPC gossip network — random conversations between nearby NPCs, gossip propagation across the world, player can overhear, rumor spreading (faction conflicts, recent events, quest hints), configurable interval
HarvestSubagent.cs	171	Autonomous resource gathering — mine/woodcut at assigned location, 5s tick + ~15% chance, produces all ore types (Iron→Valorite) and wood types (Log→Frostwood), tool requirement check, mood speed factor, stop command
3. ⚔️ HERO HIRELING SYSTEM (5 files)
File	Lines	What it does
HeroHireling.cs	983	18 hero classes (enum + giant switch with per-class stats, skills, gear, appearance): Warrior, Archer, Mage, Paladin, Ranger, Ninja, AnimalTamer, Necromancer, Bard, Alchemist, Assassin, Berserker, Warlock, Spellblade, Crusader, Shadowmage, Beast Rider, Avatar; SetupClass() assigns per-class stats, skills (custom ranges per class), equipment sets, appearance overrides via ApplyClassAppearance(); All Skills Book system (any skill, 0→120); HeroHirelingSpawner placed in Britain for random class spawning; full serialization with hire cost, level, bond status
AvatarSpawner.cs	92	Places male + female Avatar in Trinsic center (Felucca + Trammel = 4 NPCs); white/silver hair, pale skin, blessed
HirelingCommandGump.cs	605	Full command interface — Guard Post system (set, patrol, auto-engage hostiles, heartbeat), Vendor Mode (setup gump, price prompts, customer shop gump, buy/sell, restock timer, item price persistence), Harvest commands (mine/chop targeting), mood reporting, /Report command, speech command routing
HirelingSubagent.cs	135	Hireling-specific AI: loyalty system, skill tracking, command processing
HirelingMarketGump.cs	92	Market-style UI for browsing available hirelings
4. 🏛️ FACTION SYSTEM (10 files)
File	Lines	What it does
NpcFaction.cs	370	8 factions with full preset data: Britannian Crown, Minax, Shadowlords, Council of Mages, True Britannians, Orcish Horde, Undead Legion, Outlaw (players only); each has description, base hue, city affiliation, playable status, alignment data; VirtueAlignment (8 classical virtues), ViceAlignment (matching vices)
FactionReputationSystem.cs	236	Per-player, per-faction reputation tracking (±5000 range); GetReputation(), SetReputation(), ModifyReputation(), GetFactionRank(), GetFactionTitle(); death hook auto-rewards rep on kills; +15 rep per faction creature kill, +50 for captains; access blocking ≤ -500 rep
FactionQuartermaster.cs	231	Specialized vendor per faction: 9 placed in Britain (Felucca + Trammel); faction-hued items, reputation gating, speech commands ("buy", "faction", "standing"), 20+ item stock per faction with unique items
FactionRewards.cs	144	Faction-specific reward tables: weapons, armor, scrolls, reagents, unique faction-dyed gear, faction artifacts
FactionStatusGump.cs	105	Full-screen gump: per-faction reputation bars, rank, title, recent changes, faction description
FactionVisibility.cs	198	Faction NPC visual indicators: hue-based identification, title display, faction name display, detection via GetNpcFaction()
FactionDiplomatSubagent.cs	227	AI faction diplomat — auto-negotiates faction relations, sends emissaries, declares vendettas, forms alliances, trade agreements, configurable interval
Npc.cs (NpcAllianceBehavior)	206	Faction NPC combat behavior: allied factions don't fight, enemy factions auto-engage, faction reinforcement calls, faction territory patrol
LootTableIntegration.cs	269	Faction loot drops: faction-appropriate items on faction creature kills; FactionLootItem, DragonScale, DaemonSword; faction loot tables per faction type
5. 🎯 BOUNTY SYSTEM (3 files)
File	Lines	What it does
BountySystem.cs	234	Full bounty system: Bounty class with target name, crime, bounty amount, issuer, date, status (Open/Claimed/Expired); BountyDeathHook auto-checks kills against open bounties; auto-generates bounties on notorious player kills; persistent bounty board
BountyBoardItem.cs	89	BountyBoardItem (deed) — placeable in houses/guildhalls; BountyBoardGump lists open bounties with details
BountyHunterBadge.cs	73	BountyHunterBadge — wearable item tracking bounty count by tier (Bronze/Silver/Gold/Platinum/Mythic); property bonuses per tier (+hit points, +damage, +luck); serialization of kill counts
PlayerOutlawSystem.cs	203	Outlaw/notoriety system: tracks murder count, bounty value, outlaw status; Guard attack on sight if outlaw; Outlaw faction for players; broadcast on outlaw status change; [outlawstatus command
6. 🧙 QUEST & PROGRESSION SYSTEM (5 files)
File	Lines	What it does
AIQuestSystem.cs	956	3 quest types: FactionKillTargets (kill N of faction X), FactionCollectTargets (collect N of item Y), FactionQuestTitles (earn rank/title); AIQuest class with objective tracking, rewards, expiration; AIQuestManager generates/assigns/completes quests; QuestCombatHook tracks kill progress; QuestExploreHook discovery rewards; FactionQuestIntegration pools per faction; QuestReputationHook awards rep on completion
QuestProgressHook.cs	34	Hooks craft and gather events for quest credit
BardDeedIntegration.cs	100	Bard deeds track performance-based quests, fame gain, music skill rewards
PlayerDeedTracker.cs	134	Deed tracking system: per-player deed log (date, type, description, value), [mytitles command, world save persistence, completed count
UniversalTamingSystem.cs	121	Universal pet taming: tame ANY creature with skill check + chance formula, bond-on-tame option, loyalty tracking for tamed creatures, skill progression, rare taming bonuses
7. 🌊 SEA & NAVAL SYSTEMS (5 files)
File	Lines	What it does
SeaBattleSystem.cs	430	11 coastal city spawn points: Trinsic, Britain, Vesper, Skara Brae, Moonglow, Magincia, Ocllo, Jhelom, Buccaneer's Den, Serpent's Hold, Cove; 10-min spawn check (20% chance); full battle lifecycle (Announced → Delayed → Active → Completed/Failed → Cleanup); 3–7 crew + captain; faction-aligned enemy themes; loot chests (gold 500–5000, items, treasure maps 25%); global broadcasts at each phase; faction rep rewards
SailorHireling.cs	498	SailorHireling : BaseHire — fishing 60–100, cartography 30–70; 16-port navigation (Britain, Trinsic, Vesper, Skara Brae, Moonglow, Magincia, Ocllo, Jhelom, Buccaneer's Den, Serpent's Hold, Cove, Papua, Delucia, Nujel'm, Wind, Minoc); commands: "Take me to X", "Go fish"/"Stop fishing", "Stop"/"Anchor", "Crew my boat"/"Pilot my boat", "Report", "Disembark"; auto-navigation via LockPilot() + StartMove() + NavigationTick; auto-fishing via Fishing.System.StartHarvesting() + FindFishingSpot() water detection
BoatAutomationSystem.cs	192	Boat registry system: 16 port destinations with multiple aliases, RegisterBoat()/UnregisterBoat()/TryGetDestination(), UpkeepTick() cleanup of orphaned boats, OnBoatPlaced() event
QoLIntegration.cs	274	Quality of Life improvements for boats: recall-from-boat, house-placement-from-boat, auto-dock, boat insurance check
WorldEventEncounters.cs	280	Dynamic sea/world events: random encounters while traveling (merchant ships, pirate raids, sea monsters, ghost ships, treasure flotsam), difficulty-scaled rewards, event notification system
8. 🧟 CREATURE & MONSTER SYSTEMS (3 files)
File	Lines	What it does
CreatureVariants.cs	1368	22 new creature variants with unique abilities: OrcShaman, OrcArcher, OrcKnight, OrcBeastmaster, OrcWarlord, LizardmanShaman, LizardmanSniper, LizardmanHighPriest, TrollWitchdoctor, TrollChieftain, SkeletalMage, SkeletalArcher, SkeletalLich, LesserDragon, LesserDaemon, LesserOrc, GreaterOrc, GreaterTroll, GreaterSkeleton; VariantSpawner with 8 variant themes (OrcishHorde, UndeadLegion, LizardmanEmpire, TrollWarband, DaemonCult, DragonBrood, MixedMonster, UndeadHoarde; VariantLoot — per-variant loot tables with unique items
RegionalThreatSystem.cs	337	Dynamic regional threats: 6 threat types (Orcish Invasion, Undead Rising, Monster Migration, Elemental Surge, Bandit Activity, Daemon Incursion); threat levels I–IV with increasing spawn counts and difficulty; passive threat escalation over time; player-driven resolution; threat-specific loot and faction reputation rewards
SpawnerIntegration.cs	522	Spawner overhaul: piggybacks on vanilla Spawner system, adds AI-aware spawn filtering, hero hireling spawn points, variant creature spawns, quest item spawns, faction-aligned spawns, dynamic spawn scaling based on player count nearby
9. 💖 RELATIONSHIP & SOCIAL SYSTEMS (8 files)
File	Lines	What it does
NPCRelationshipSystem.cs	670	Full relationship framework: per-NPC/player relationship tracking; NPCRole enum (Stranger, Acquaintance, Friend, CloseFriend, Confidant, RomanticInterest, Partner, Spouse, Mentor, Protege, Rival, Enemy, Nemesis, Ally); relationship score ±100; gift tracking and favor; progression events; 5 relationship tiers with unlockable interactions; RelationshipGiveHook (gift giving); RelationshipContextMenu (context menu entries for relationship actions)
RomanceInteractions.cs	224	Romance system: flirt, compliment, gift, date, propose; mood-dependent success; relationship score requirements; special dialogue; marriage support; LoveLetter item
RomanceQuestGiver.cs	133	RomanceQuestGiver : BaseVendor — NPC that gives romance-related quests (fetch flowers, deliver love letters, arrange meetings)
LoveLetter.cs	85	LoveLetter item — writable, sendable, readable; romantic text, can be delivered as quest item
RelationshipGump.cs	70	Relationship status gump — shows relationship tier, score, history, recent interactions
TameRelationshipIntegration.cs	81	Pets gain relationship score with owner over time; affects loyalty, obedience, special behaviors
AnimalHirelingService.cs	64	Service that pairs animal taming with hireling system — tamed creatures can be registered as hirelings
MiscItems.cs	230	9 new themed items: HeartyStew (HP regen), DragonBreathWhiskey (fire resist), ManaBerryPie (mana regen), GoblinAle (stam debuff/buff), LichBoneStaff (undead focus), OrcishWarAxe (orc damage), TrollSkinBoots (regen), PhoenixFeather, VampireFang (quest/drop items)
10. 🏘️ TOWN & ECONOMY SYSTEMS (8 files)
File	Lines	What it does
LivingEconomy.cs	272	Dynamic economy engine: tracks item prices across all vendors; supply/demand modeling per item type; price fluctuations based on recent sales; 24-hour price window; automatic price adjustments; inflation/deflation curve; [economy admin command; data persistence across restarts
EconomySubagent.cs	281	AI economy agent: analyzes market trends, recommends price changes, generates economic reports, predicts shortages, creates trade opportunities; EconomyDataHook2 hooks buy/sell events for data collection
HouseholdTaskSubagent.cs	628	NPC chore system: 20+ task types (Cook, Clean, Repair, Farm, Guard, Teach, Craft, Hunt, Gather, Fish, Scout, Patrol, TendAnimals, Harvest, Log, Build, Decorate, Organize, Research, Train); task scheduling based on time of day; household management for player-owned NPCs; task priority system; event-driven task completion
HouseholdStatusGump.cs	154	Household management gump: task list, NPC assignments, completion status, household statistics
TownPlotDeed.cs	65	TownPlotDeed — allows players to claim plots in towns for housing or vendor stalls
TownHousingConfig.cs	12	Town housing configuration: plot sizes, costs, availability per city
SentientItem.cs	495	Sentient items: weapons/armor that level up, learn, develop personality, speak to owner, gain special properties; SentientSystem manages all sentient items; SentientItemComponent attachable to any item; XP system via kills/crafting; SentientDebugCommand for admin
MiscItems.cs	230	(see section 9 — 9 new items with stat/combat effects)
11. 🎲 GAME MASTER & NARRATIVE SYSTEMS (5 files)
File	Lines	What it does
AIGameMaster.cs	517	AI Game Master: multi-phase narrative engine; NarrativePhase (Dawn, RisingAction, Climax, FallingAction, Resolution); arc lifecycle (Setup→Complication→Crisis→Climax→Aftermath); story events auto-generated based on world state; configurable interval; broadcasts narrative events to players; player choices influence narrative direction
DungeonMasterSubagent.cs	393	AI Dungeon Master: per-dungeon AI overseer; dynamic encounter generation; loot balancing based on party size/level; trap placement; boss mechanics; difficulty scaling; puzzle generation; Dungeon Region hooks for entrance/exit tracking
DungeonRegionHook.cs	58	Tracks player dungeon entry/exit, party composition, time spent, mobs killed
SpawnControllerSubagent.cs	247	AI Spawn Controller: dynamic respawn rates based on player activity; creature difficulty scaling; regional spawn balancing; boss spawn conditions; event-triggered spawn waves
WorldEventEncounters.cs	280	Dynamic encounters across the world (see section 7 also)
12. 🎭 NEMESIS & THREAT SYSTEMS (2 files)
File	Lines	What it does
NemesisSystem.cs	359	Nemesis/arch-enemy system: tracks player-nemesis pairs; NemesisLieutenant (5 lieutenant tiers with scaling power); nemesis grudge system (revenge mechanic on multiple kills); nemesis progression (gains power each time it escapes); nemesis-specific loot and titles; world-save persistence; NemesisHook for kill detection
RegionalThreatSystem.cs	337	(see section 8 — dynamic threats with escalations)
13. 🌐 COMMUNITY & SOCIAL FEATURES (2 files)
File	Lines	What it does
CommunityBoardItem.cs	224	Community bulletin board: CommunityBoardItem (placeable deed); CommunityPost (title, body, author, date); CommunityBoardGump with post listing; PostTitlePrompt / PostBodyPrompt for creating posts; PostDetailGump for reading; persistent storage
NpcActionAllowlist.cs	63	Whitelist of NPC actions; configurable restrictions for AI behavior; safety guardrails
14. 🔧 CHANGES TO VANILLA SERVUO FILES
File	Change
Scripts/Multis/Boats/BaseBoat.cs	Patched with LockPilot(), UnlockPilot(), StartMove(dir, bool), StopMove() methods + Mobile Pilot property + OnPilotCommand() hook for sailor hireling integration; OnDamage() hook for sea battle integration
15. 📦 PERSISTENCE & DATA MANAGEMENT
WorldSave/WorldLoad hooks integrated into: AIComponentRegistry, AIQuestManager, NPC Relationship System, Bounty System, PlayerDeedTracker, LivingEconomy, NemesisSystem, AIMemory, BoatAutomationSystem
Save path: Saves/AIOrchestrator/ directory
Custom binary serialization for each system
16. 🖥️ ADMIN COMMANDS
Command	Function
[aiconfig / [aicfg	View/edit all AI config (backend, models, heartbeat, limits)
[aiinfo	Toggle AI status gump on target NPC
[aiglobal	Toggle global AI system on/off
[ainear	Register nearby NPCs for AI
[aistatus	Show orchestrator status (counts, active components)
[aidebug	Debug output for AI decisions
[globalai	Global orchestrator management gump
[spawnhero	Spawn random hero hireling
[factionrep	Faction reputation management
[economy	Economy data and controls
[outlawstatus	Outlaw/bounty status
[mytitles	Player deed/title log
[ai-model set	Change model per sub-agent at runtime
17. 🎯 SUMMARY: EVERYTHING CHANGED
Category	Count	Description
New base classes	2	AIBaseCreature, HeroHireling
New creature types	22	Orc/Lizardman/Troll/Skeleton/Daemon/Dragon variants
New NPC types	3	SailorHireling, FactionQuartermaster, RomanceQuestGiver
New item types	20+	Sentient items, faction loot, bounty items, misc items, community board, love letters, deeds
New gumps	12	HirelingMarketGump, HirelingCommandGump, VendorShopGump, VendorSetupGump, FactionStatusGump, RelationshipGump, HouseholdStatusGump, BountyBoardGump, CommunityBoardGump, CommunityPostDetailGump, PostTitlePrompt, PostBodyPrompt
Faction system	8 factions + Outlaw	Full reputation, quartermasters, rewards, visibility, diplomacy, quests
Bounty system	3 files	Creation, claiming, badges, death hooks
AI Sub-agents	6	Combat, Dialogue, Environment, Hireling, Gossip, Harvest
Dungeon/Game Master	5 files	Narrative engine, dungeon AI, spawn control, region hooks
Economy	2 files	Supply/demand, price fluctuations, market analysis
Social systems	8 files	Relationships, romance, pets, gifts, community boards
Sea systems	5 files	Sea battles, sailor hirelings, boat automation, encounters
Threat systems	2 files	Regional threats, nemesis/arch-enemy
Vanilla edits	1 file	BaseBoat.cs — pilot methods for automation
Admin commands	14	Config, debug, spawn, faction, economy, outlaw
Total new code	~12,500 lines	66 files


## Multi-Agent AI Orchestrator

The core innovation: every hireling has an AI-driven "brain" that dispatches specialized sub-agents.

### Architecture

```
┌─────────────────────────────────────────────────┐
│                AI Orchestrator                   │
│  (OrchestratorBrain — one per NPC)              │
│                                                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐ │
│  │ Commander  │  │  Vendor    │  │  Harvester │ │
│  │ (combat,   │  │  (shop,    │  │  (mine,    │ │
│  │  follow,   │  │  pricing,  │  │  chop)     │ │
│  │  guard)    │  │  trade)    │  │            │ │
│  └────────────┘  └────────────┘  └────────────┘ │
│  ┌────────────┐  ┌────────────┐                  │
│  │ Sailor     │  │GuardPost   │                  │
│  │ (nav,      │  │Controller  │                  │
│  │  fish)     │  │(patrol,    │                  │
│  │            │  │ engage)    │                  │
│  └────────────┘  └────────────┘                  │
└─────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────┐
│              LLM Client (sanitized output)        │
│  Ollama · vLLM · OpenAI · LM Studio · KoboldCpp │
│  TGI · LlamaCpp                                   │
└─────────────────────────────────────────────────┘
```

### Per-NPC Sub-Agents

- **CommanderSubagent** — Handles combat, movement, following, guarding. Responds to attack commands, sets AI type.
- **GuardPostSubagent** — Patrols a guard post zone. Heartbeat checks for enemies. Auto-engages hostiles.
- **HarvestSubagent** — Mines ore (Iron→Valorite) or chops wood (Log→Frostwood) at a designated tile. Mood speed factor.
- **SailorSubagent** — Navigates to 16 ports, operates boat locks, auto-fishes.
- **VendorSubagent** — Manages shop inventory, pricing, customer buy UI.

### All Skills Book

Any hireling can learn any skill by reading a special skill book. Skills progress from 0 to 120. This enables multi-classing — turn a Warrior into a Mage by feeding them spell books.

---

## NPC Mood & Personality System

Every hireling has a **mood** and a **personality** that affect their behavior, combat effectiveness, work speed, and loyalty.

### 10 Mood States

| Mood | Hue | Combat | Work Speed | Loyalty | Trigger |
|---|---|---|---|---|---|
| Ecstatic | Gold (0x047) | +20% | ×1.8 | +30 | Frequent praise, gifts |
| Happy | Bright green | +10% | ×1.5 | +20 | Praise, gifts |
| Pleased | Light green | +5% | ×1.3 | +10 | Friendly interaction |
| Content | Default | 0 | ×1.0 | 0 | Default state |
| Bored | Gray | -5% | ×0.85 | -5 | Idle too long |
| Irritated | Orange | -10% | ×0.7 | -10 | Repeated scolding |
| Angry | Red (0x01) | +15% (wild) | ×0.5 | -20 | Scolding, damage |
| Frightened | Pale | -20% | ×1.2 (flee) | -5 | Near-death |
| Lonely | Blue-gray | -10% | ×0.8 | -15 | No nearby master |
| Betrayed | Dark red | +25% (hostile) | — | -50 | Attacked by master |

### 10 Personalities

| Personality | Aggression | Obedience | Social | Speed |
|---|---|---|---|---|
| Brave | +2 | 0 | 0 | 0 |
| Cowardly | -2 | +1 | 0 | +1 |
| Aggressive | +3 | -1 | -1 | 0 |
| Loyal | 0 | +3 | 0 | 0 |
| Independent | -1 | -2 | 0 | 0 |
| Lazy | 0 | 0 | 0 | -2 |
| Energetic | 0 | 0 | 0 | +2 |
| Friendly | -1 | 0 | +3 | 0 |
| Stoic | 0 | +1 | -2 | 0 |
| Playful | 0 | -1 | +2 | +1 |

### Mood Barks

NPCs automatically emote a bark when their mood changes significantly (e.g., "*The Avatar looks pleased.*", "*The Avatar is furious!*").

---

## Hireling Command System

All commands use the `HirelingCommandGump` interface. The hireling delegates to the appropriate sub-agent.

### Combat Commands
- `"All kill"` / `"Attack [target]"` — Commander engages
- `"All stop"` / `"Stop"` — Cease combat
- `"Follow me"` — Stick to master
- `"Stay"` / `"Guard me"` — Hold position / protect master
- `"[target]"` (single-click NPC) — Attack via speech

### Vendor Mode Commands
- `"Set up shop"` — Opens `VendorSetupGump` to configure shop name, restock delay, markup
- `"Vendor prices"` — Opens `VendorPricePrompt` to set price on held item
- `"Open shop"` / `"Buy"` — Opens `VendorShopGump` for customers
- `"Stop selling"` — Disables vendor mode
- Players can browse and buy from vendor-mode hirelings

### Harvest Commands
- `"Mine here"` — Assigns HarvestSubagent to mine at current location
- `"Chop here"` / `"Lumber here"` — Assigns HarvestSubagent to chop wood
- `"Stop harvesting"` — Stops all harvest activity

### Guard Post Commands
- `"Guard post"` / `"Guard post here"` — Creates a GuardPost instance at NPC location
- `"Dismiss guard post"` — Removes the guard post assignment
- NPC patrols around the post and engages hostiles automatically

### Mood Commands
- Praise: `"Good job!"`, `"Well done"`, `"Thank you"`, etc. → mood improves
- Scold: `"Bad!"`, `"Stop that"`, `"No!"`, etc. → mood worsens
- Gift: giving gold or items → mood improves
- `/Report` — NPC reports current mood, personality, and status

---

## Harvest Subagent (Mining / Lumberjacking)

Hirelings can autonomously harvest resources in the wilderness.

### Mining

| Ore | Min Skill | Max Skill |
|---|---|---|
| Iron | 0 | 30 |
| Dull Copper | 25 | 60 |
| Shadow Iron | 45 | 85 |
| Copper | 55 | 100 |
| Bronze | 65 | 110 |
| Gold | 75 | 115 |
| Agapite | 85 | 120 |
| Verite | 95 | 120 |
| Valorite | 105 | 120 |

### Lumberjacking

| Wood | Min Skill |
|---|---|
| Log | 0 |
| Oak | 30 |
| Ash | 50 |
| Yew | 70 |
| Heartwood | 85 |
| Bloodwood | 95 |
| Frostwood | 105 |

### Features
- ~15% tick chance every 5 seconds
- Mood speed factor (Ecstatic = ×1.8, Angry = ×0.5)
- Tool check (requires pickaxe or axe in backpack)
- Produces randomized ore/wood based on skill
- `"Stop harvesting"` to halt

---

## Faction System Overhaul

### 8 Factions (& 1 Outlaw)

| ID | Faction |
|---|---|
| 1 | Britannian Crown |
| 2 | Minax |
| 3 | Shadowlords |
| 4 | Council of Mages |
| 5 | True Britannians |
| 6 | Orcish Horde |
| 7 | Undead Legion |
| 8 | Outlaw (no quartermaster) |

### Faction Quartermasters

- **9 NPC vendors** placed in Felucca + Trammel Britain
- Each sells **faction-colored** items (weapons, armor, scrolls, reagents)
- Stock varies by faction (e.g., Orcish Horde sells orc-themed gear)
- `CheckVendorAccess()` — players with ≤ -500 reputation are **blocked**
- `OnSpeech("faction"|"standing"|"status")` — reports reputation
- `OnSpeech("buy"|"shop")` — opens faction shop

### Reputation System

- Players start at 0 reputation with each faction
- Killing faction creatures: **+15 rep** per kill (captain: **+50**)
- Reputation ranges: **±5000** (min/max)
- Faction allegiance tracked via `NpcFaction.Presets`

---

## Sea Battles & Naval Encounters

### 11 Coastal Cities

Trinsic, Britain, Vesper, Skara Brae, Moonglow, Magincia, Ocllo, Jhelom, Buccaneer's Den, Serpent's Hold, Cove

### Battle Mechanics

| Property | Value |
|---|---|
| Spawn Check Interval | 10 minutes |
| Spawn Chance | 20% per check |
| Crew Size | 3–7 + Captain |
| Enemy Types | Pirates, orc pirates, undead crew, faction raiders |
| Battle Lifecycle | Announced → Active (5-min) → Completed / Failed → Cleanup |
| Faction Rep Reward | +50 per kill, +15 per crew |
| Loot | Gold (500–5000), random items, treasure maps |

### Loot Chest

Drops after victory with:
- 500–5000 gold
- 3 random items (weapons, armor, scrolls, gems)
- 25% chance of a treasure map

---

## Sailor Hirelings & Boat Automation

### Sailor Hireling

A specialized `BaseHire` subclass with:

| Skill | Range |
|---|---|
| Fishing | 60–100 |
| Cartography | 30–70 |

### Navigation Commands

- `"Take me to [port]"` — Auto-navigate to any of 16 ports
- `"Go fish"` / `"Stop fishing"` — Automated fishing
- `"Stop"` / `"Anchor"` — Halt boat movement
- `"Crew my boat"` / `"Pilot my boat"` — Take control of vessel
- `"Report"` — Current status, heading, destination
- `"Disembark"` — Leave the boat

### 16 Port Destinations

Britain, Trinsic, Vesper, Skara Brae, Moonglow, Magincia, Ocllo, Jhelom, Buccaneer's Den, Serpent's Hold, Cove, Papua, Delucia, Nujel'm, Wind, Minoc

### Boat Automation System

- `BoatAutomationSystem.RegisterBoat()` / `UnregisterBoat()` — Boat registry similar to BaseHouse
- `TryGetDestination(name)` — Fuzzy port name matching
- `UpkeepTick()` — Cleanup of orphaned boats
- `OnBoatPlaced()` — Event hook

---

## AI Quest & Reputation Integration

### Faction Quests (`AIQuestSystem`)

Three quest types:

1. **FactionKillTargets** — "Kill 5 Orcish Horde members for the Crown"
2. **FactionCollectTargets** — "Bring 10 leather hides to the Council of Mages"
3. **FactionQuestTitles** — Earn titles like "Champion of the Crown"

### Reputation Hooks

- `FactionReputationSystem.GetCreatureFaction(mob)` — Detect faction by creature type
- `FactionReputationSystem.RewardFactionReputation(mob, killer, gold)` — 10% of gold reward as reputation
- `FactionReputationSystem.OnKilledBy(mob, killer)` — Auto-hook for all kills

### Quest Rewards
- Gold proportional to difficulty
- Faction reputation (10% of gold reward)
- Faction titles (earned at high reputation)

---

## All AI Inference Backends Supported

The AI Orchestrator communicates with LLMs through `LLMClient.cs`, which normalizes output across all major backends.

### Configuration (`AIConfig.cs`)

```csharp
public static class AIConfig
{
    // Pick ONE backend by setting its base URL:
    public static string BaseUrl = "http://localhost:11434";  // Ollama
    // public static string BaseUrl = "http://localhost:8000"; // vLLM / TGI
    // public static string BaseUrl = "http://localhost:1234"; // LM Studio
    // public static string BaseUrl = "http://localhost:5000"; // KoboldCpp
    // public static string BaseUrl = "http://localhost:8080"; // LlamaCpp

    // OpenAI-compatible:
    // public static string BaseUrl = "https://api.openai.com/v1";
    // public static string BaseUrl = "https://api.groq.com/openai/v1";

    public static string Model = "llama3.2";         // Model name for the backend
    public static string ApiKey = "";                  // Required for OpenAI, optional for others
    public static string ApiChatEndpoint = "/v1/chat/completions";
    public static int MaxTokens = 2048;
    public static double Temperature = 0.7;
}
```

### Backend Details

| Backend | Port | Auth | Notes |
|---|---|---|---|
| **Ollama** | 11434 | None | `ollama pull llama3.2` |
| **vLLM** | 8000 | Optional | `--api-key` flag |
| **LM Studio** | 1234 | None | Enable CORS in settings |
| **OpenAI** | — | API key | `gpt-4o-mini`, etc. |
| **KoboldCpp** | 5000 | None | `--api` flag |
| **TGI** | 8000 | Optional | Text Generation Inference |
| **LlamaCpp** | 8080 | None | `./server` binary |

### Output Sanitization

`LLMClient` strips raw inference artifacts:
- Trailing partial JSON
- Control characters (\\n, \\t) outside valid positions
- Non-ASCII artifacts
- Cut-off sentences
- Leading/trailing quote noise

---

## Installation Guide

### Prerequisites

- Windows (or Linux/macOS with Mono/.NET 8)
- [ServUO](https://github.com/ServUO/ServUO) (latest)
- .NET 8 SDK
- An LLM backend (Ollama recommended for local)

### 1. Set Up ServUO

```bash
git clone https://github.com/ServUO/ServUO.git
cd ServUO
dotnet build ServUO.sln -c Debug
```

### 2. Add AI Orchestrator Files

Copy the files from this repository into your ServUO installation:

```bash
# From this repo root, copy to your ServUO Scripts directory:
xcopy /e /y "Scripts" "D:\uo\ServUO\Scripts\"
```

All files go under `Scripts/Custom/AIOrchestrator/`:
- All `.cs` files in the `AIOrchestrator` folder
- Including `Factions/` subfolder
- Note: `BaseBoat.cs` is a **patch file** for boat automation (apply manually if needed)

### 3. Set Up Your AI Backend

#### Option A: Ollama (recommended)

```bash
# Install Ollama from https://ollama.com
ollama pull llama3.2
ollama serve  # Starts on port 11434
```

#### Option B: LM Studio

1. Download from [lmstudio.ai](https://lmstudio.ai)
2. Load a model (e.g., Llama 3.2 3B)
3. Start server: Settings → Local Inference Server → Start
4. Enable CORS in server settings

#### Option C: OpenAI

Get an API key and set `BaseUrl = "https://api.openai.com/v1"` and `ApiKey` in `AIConfig.cs`.

### 4. Configure `AIConfig.cs`

Open `AIConfig.cs` and set:

```csharp
public static string BaseUrl = "http://localhost:11434";  // Your backend URL
public static string Model = "llama3.2";                   // Your model name
public static string ApiKey = "";                          // If required
```

### 5. Rebuild & Run

```bash
dotnet build ServUO.sln -c Debug
cd Server/bin/Debug/net8.0
ServUO.exe
```

You should see log output like:
```
[AIOrchestrator] Initializing AI Orchestrator...
[AIOrchestrator] Initialized: LLMClient
[AIOrchestrator] Initialized: AIQuestSystem
[FactionQuartermasterSpawner] 9 Quartermasters placed.
[AvatarSpawner] The Avatar placed in Trinsic (Felucca + Trammel).
[SeaBattleSystem] Monitoring 11 coastal cities.
```

### 6. (Optional) Unzip Custom Maps

If your server uses custom map sizes, ensure `Data/` directory includes the correct map files. The Orchestrator does not modify map data.

---

## Configuration

### AIConfig.cs Settings

See [All AI Inference Backends Supported](#all-ai-inference-backends-supported) above for full documentation.

### Spawner Control

- **HeroHirelingSpawner**: `Utility.Random(17)` picks a random class from 0–16 (Avatar excluded from random pool). Modify the Random value to adjust available classes.
- **AvatarSpawner**: Places exactly 4 Avatars (Male Felucca, Male Trammel, Female Felucca, Female Trammel). Modify coordinates in `AvatarSpawner.cs` to change location.
- **FactionQuartermasterSpawner**: Places 9 QMs at Britain (Felucca + Trammel). Modify `PlaceAt()` calls to move or add QMs.
- **SeaBattleSystem**: Configure `CoastalCities`, spawn interval, crew counts, and loot in `SeaBattleConfig.cs`.

### Mood System Tuning

- Mood drift tick: `AIOrchestratorInit.cs` thread timer interval (default 30s)
- Mood thresholds, effects, and barks: `NpcMoodSystem.cs` constants section

---

## Commands Reference

### Player Commands

| Command | Target | Effect |
|---|---|---|
| `[aiinfo` | Self | Toggle AI status gump |
| `[aiconfig` | Self | Open AI configuration gump |
| `All kill` | Hireling | Attack target |
| `All stop` | Hireling | Cease combat |
| `Follow me` | Hireling | Follow master |
| `Stay` / `Guard me` | Hireling | Hold position |
| `Set up shop` | Hireling | Open vendor setup |
| `Open shop` / `Buy` | Vendor NPC | Browse inventory |
| `Mine here` / `Chop here` | Hireling | Start harvesting |
| `Stop harvesting` | Hireling | Stop harvesting |
| `Guard post here` | Hireling | Set guard post |
| `Take me to [port]` | Sailor | Navigate to port |
| `Go fish` / `Stop fishing` | Sailor | Auto-fish |
| `Crew my boat` | Sailor | Board and pilot |
| `Disembark` | Sailor | Leave boat |
| `/Report` | Hireling | Show mood, personality, status |

### Admin Commands

| Command | Target | Effect |
|---|---|---|
| `[globalai` | Self | Toggle global orchestrator gump |
| `[spawnhero` | Self | Spawn random hero hireling |
| `[factionrep` | Self | Faction reputation management |

---

## Frequently Asked Questions

**Q: Do I need an AI backend to use this?**
A: No. Without an AI backend, hirelings still function as enhanced ServUO NPCs with all combat, vendor, harvest, guard, and sailor commands. The AI backend adds conversational intelligence, proactive behavior, and dynamic quest generation.

**Q: Which model should I use?**
A: For local: Llama 3.2 3B (fast) or Llama 3.1 8B (smarter). For cloud: GPT-4o-mini or Claude 3 Haiku.

**Q: Can I add my own hero class?**
A: Yes. Add an entry to the `HeroClass` enum, add a `case` in `SetupClass()` with stats/gear/appearance, and update `Utility.Random(X)` in the constructor and `HeroHirelingSpawner`.

**Q: Can I turn existing NPCs into AI NPCs?**
A: Yes. Use `[aiinfo` on any NPC to toggle AI integration. The NPC must have a valid `BaseCreature` subclass.

**Q: How do I add a new port for sailors?**
A: Add it to `BoatAutomationSystem.cs` in the ports dictionary with coordinates, facings, and all aliases.

**Q: How do I add a new faction?**
A: Add it to `NpcFaction.Presets`, create a `FactionQuartermaster` instance, and add kill targets/collect items to `AIQuestSystem`.

**Q: Will these changes break my existing world?**
A: No. All systems use unique serialization namespaces. Existing vendor/shop data is untouched. New systems are additive.

**Q: Can I use this on a production shard?**
A: Yes. All systems are designed for live servers. Mood effects, rep caps, and spawn rates are tuned for balance.

---

## License

This project is provided as a modification for ServUO. ServUO itself is licensed under the GNU General Public License v2. See [ServUO License](https://github.com/ServUO/ServUO?tab=GPL-2.0-1-ov-file).

---

*Built for ServUO. Powered by LLMs. Inspired by the Virtues.*
