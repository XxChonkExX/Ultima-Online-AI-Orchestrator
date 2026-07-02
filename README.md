# AIOrchestrator for ServUO

**AIOrchestrator** is a comprehensive LLM-powered framework for [ServUO](https://www.servuo.com) that brings emergent, AI-driven gameplay to Ultima Online shards. It wraps every aspect of the game — NPCs, economy, factions, dungeons, quests, pets, hirelings, romance, territory control, sea battles, item crafting, and more — under a unified LLM subagent orchestration layer.

---

## Table of Contents

1. [Feature Overview](#feature-overview)
2. [Commands](#commands)
3. [New Creatures & NPCs](#new-creatures--npcs)
4. [New Items](#new-items)
5. [New Dungeons](#new-dungeons)
6. [Installation](#installation)
   - [ServUO Setup](#servuo-setup)
   - [LLM Backend Setup](#llm-backend-setup)
7. [Running Without an LLM](#running-without-an-llm)
8. [AI Orchestration Architecture](#ai-orchestration-architecture)
9. [Configuration](#configuration)
10. [License & Attribution](#license--attribution)

---

## Feature Overview

### Core AI Framework
- **LLM Subagent Architecture** — 8 specialized subagents (Combat, Dialogue, Environment, Economy, Faction, Spawner, Dungeon, Narrator), each independently model-configurable
- **Attachable AI Component** — any `BaseCreature` gains AI without subclassing; auto-attach on spawn
- **Persistent NPC Memory** — identity, backstory, mood, temperament, relationship history, speech style, private drive
- **Configurable LLM Backends** — Ollama, vLLM, OpenAI-compatible, LM Studio, KoboldCPP, TGI, LlamaCpp
- **Heartbeat System** — 500ms AI tick driving NPC behavior, dialogue, combat decisions
- **Profiling & Debugging** — per-subagent latency tracking, AI status introspection

### NPC & Social Systems
- **NPC Identity Generator** — procedurally generates vocation, homeland, temperament, backstory, speech style for every AI-enabled creature
- **Ambient NPC-to-NPC Gossip** — creatures converse amongst themselves, spreading rumors and news
- **Dialogue Subagent** — LLM-driven contextual speech with player memory, mood awareness, faction alignment
- **Combat Subagent** — morale-driven combat lines, death cries, battle taunts, surrender behavior
- **NPC Mood System** — mood (0-100) affects work speed, dialogue depth, combat effectiveness
- **NpcActionAllowlist** — restricts which NPC actions require LLM approval vs. execute locally
- **NpcFaction Alignment** — runtime faction assignment per NPC, visible overhead titles and name colors

### Faction & Territory Systems
- **Faction Manager** — configurable factions with Virtue/Vice alignment, colors, enemy/allies
- **Faction Reputation** — per-player reputation tracking, rank titles, guard hostility, NPC discounts
- **Faction Capitals** — capital towns grant zone-wide buffs to aligned players
- **Faction Rank System** — 5 ranks (Recruit → Lord), each with unique rewards and titles
- **Faction Diplomacy** — alliances, war declarations, war score, border skirmishes, peace treaties
- **Faction Missions Board** — daily/weekly missions rewarding rep, gold, rank points
- **Faction Events** — dynamic events linking factions and towns (caravans, invasions, supply runs)
- **Faction Quartermasters** — faction vendors spawn in aligned towns, sell rank-gated equipment
- **Faction Rewards** — concrete gameplay impact: discounts, titles, guard hostility, vendor access
- **Territory Grid** — 10×10 grid over Britannia; factions contest tiles, gain bonuses
- **War Manager** — declarations, campaigns, morale, battle resolution

### Town & Economy Systems
- **Town Manager** — 12 default towns (Britain, Trinsic, Minoc, Yew, Moonglow, etc.) with faction affiliations
- **Town Market** — per-town commodity prices, dynamic trade route generation
- **Living Economy** — supply/demand tracking across all towns, price fluctuation driven by player activity
- **Economy Subagent** — AI-driven price manipulation, shortage events, boom/bust cycles
- **Harvest Subagent** — NPCs auto-mine, chop wood, and gather resources in the world
- **Vendor Mode System** — offline vendor restock timer, persistent vendor inventory
- **Town Housing Config** — config-controlled town plot deed placement rules
- **Town Plot Deeds** — purchasable town housing via TownHousingConfig

### Dungeon & PvE Systems
- **Instanced Dungeon System** — 20 instanced dungeons across 4 tiers (Normal/Heroic/Epic), 4 rooms each, with scaling difficulty
- **Dungeon Boss Special Abilities** — all 20 bosses have unique WeaponAbility assignments (BleedAttack, ShadowStrike, ColdWind, WhirlwindAttack, etc.)
- **Dungeon Portal System** — physical entrance portals spawned at Britain, Trinsic, Minoc
- **DungeonMaster Subagent** — AI-driven dungeon encounter narration, dynamic room description
- **Dungeon Room Hooks** — region entry/exit triggers subagent narration
- **Instance Party System** — group formation, shared loot distribution, boss defeat rewards
- **Dungeon Reward Items** — ~80 unique, thematic loot items per dungeon tier (see [New Items](#new-items))
- **Item Sets** — 6 item sets (Shadow Vault, Frost Citadel, Crystal Cavern, Coral Scale, Dwarven Plate, Void Rift) with ISetItem set bonuses
- **Crafting Recipes** — 26+ new recipes across DefCooking, DefTinkering, DefBlacksmithy, DefBowFletching
- **Elemental Arrow/Bolt Effects** — 14 enhanced ammunition types with real OnHit effects via MysticBow / MysticCrossbow (Fire, Frost, Poison, Lightning, Holy, Void, Explosive)
- **Sentient Items** — very rare AI-driven items with personality, dialogue, and LLM-generated responses
- **Loot Table Integration** — faction-themed drops on creature death, reward mapping for dungeon items
- **Regional Threat System** — dynamic threat zones that spawn escalating encounters
- **Spawner Integration** — creature variant spawners and hero hireling spawners placed in-world
- **SpawnController Subagent** — AI-driven spawn rate adjustment based on player density and threat level
- **Creature Variants** — 20+ variant creature classes (OrcBeastmaster, SkeletalMage, TrollChieftain, etc.)

### Quest Systems
- **AI Quest Manager** — dynamic quest generation via LLM
- **Quest Chains** — multi-step quest chains (Mage Tower, Merchant Escort, Guardian Trials)
- **Combat & Explore Quest Hooks** — kill-count and explore-location quest progression
- **Quest Progress Tracker** — Gump-based quest log with status updates
- **Faction Quests** — earn reputation through quest completion
- **Bounty System** — procedural bounties on elite creatures via Bounty Board
- **Player Outlaw System** — criminal flag tracking, player bounties posted by NPCs

### Hireling & Pet Systems
- **Hero Hirelings** — 5 specializations (Warrior, Ranger, Mage, Healer, Tinker), level 1-20, equipment upgrades at milestones, bonding/retainer system
- **Hireling Market** — Gump-based hireling browsing, hiring, and management
- **Hireling Command Gump** — behavior profile control (Aggressive, Defensive, Passive, Scout, Guard)
- **Squad System** — form hireling squads with roles (Tank, DPS, Healer, Support)
- **Stable System** — per-town hireling/pet storage
- **Talent System** — 10 talents (2 per spec) with Apply/Remove stat and skill effects
- **Hireling Affinity System** — hireling affinity bonuses based on time together and shared combat
- **Hireling Loadout System** — equipment templates per hireling type
- **Pet Evolution System** — 3-tier pet evolution with stat growth
- **Pet Skill Tree** — per-tier passive abilities with Gump interface
- **Pet Breeding System** — cross-breed pets for hybrid traits
- **Universal Taming System** — taming integration with creature relationship tracking
- **Tame → Relationship Integration** — tamed creatures gain relationship entries
- **Animal Hireling Service** — animal-specific hireling support

### Romance & Relationship Systems
- **NPC Relationship System** — friendship, rivalry, romance, apprenticeship, household membership per NPC
- **Romance Interations** — context menu depth for romantic partners (gifts, dates, marriage)
- **Romance Quest Giver** — procedural romance quest chains
- **Love Letter System** — craftable love letters with romantic outcomes
- **Relationship Gump** — view relationship status, history, and affinity level with any NPC
- **Household System** — form NPC households, shared resource pooling
- **Household Task Subagent** — NPCs autonomously craft and use house addons
- **NPC Favor System** — favor trading, gift-giving, reputation building

### Narrative & World Systems
- **AI Game Master** — emergent multi-phase narrative storyline generator with arc tracking and phase transitions
- **World Event Encounters** — physical spawns tied to narrative phases
- **Narrator Subagent** — zone-entry and event narration
- **Bard Deed Integration** — player deeds narrated by bards
- **Journal Manager** — player-readable journal of significant world events
- **Cliloc Generator** — generates localization string entries for new items and NPCs
- **Avatar Heroes** — legendary hero NPCs (e.g., Dupre, Iolo, Shamino) placed in Trinsic
- **Shadowlord Creatures** — Shadowlord-emissary themed spawn groups in Felucca dungeons
- **Mondain NPC** — Mondain the Wizard placed in Destard with guards
- **Lord British & Lord Blackthorn** — NPCs placed in Britain Castle
- **Nemesis System** — procedural nemesis lieutenants that remember player encounters
- **Boat Automation System** — boat ownership, teleport-to-boat, dry-dock commands
- **Sea Battle System** — faction ship encounters near coastal cities

### Misc Quality of Life
- **QoL Integration** — global gump shortcuts, context menu additions
- **Config Serializer** — JSON import/export of faction configuration
- **AI Persistence** — world-save/load for all in-memory AI state
- **Bounty Board Item & Community Board Item** — placeable world items
- **Whisper / Yell Range Commands** — proximity-based speech distance
- **Test Commands** — IO latency test for backend connectivity

---

## Commands

### Player Commands
| Command | Access | Description |
|---------|--------|-------------|
| `[Hirelings` | Player | Open hireling management menu |
| `[HirelingMarket` | Player | Browse available hirelings |
| `[HirelingCommand` | Player | Control hireling behavior profile |
| `[Squad` | Player | Form and manage hireling squads |
| `[Stable` | Player | Stable/retrieve hirelings and pets |
| `[PetSkills` | Player | Open pet skill tree interface |
| `[GrowthInfo` | Player | View pet/hireling growth status |
| `[BountyList` | Player | View active bounties |
| `[OutlawStatus` | Player | Check your criminal/outlaw status |
| `[FactionStatus` | Player | View faction reputation and rank |
| `[FactionEvents` | Player | Browse active faction events |
| `[Missions` | Player | Open faction missions board |
| `[Market` | Player | View town market prices |
| `[TownInfo` | Player | View town details and affiliation |
| `[TerritoryMap` | Player | View territory control grid |
| `[Household` | Player | Manage NPC household |
| `[ViewRelations` | Player | View NPC relationship status |
| `[skirmish` | Player | Initiate a border skirmish |
| `[FindGuild` | Player | Find nearby faction-aligned NPCs |

### Game Master Commands
| Command | Access | Description |
|---------|--------|-------------|
| `[AIEnable` | GM | Enable AI on targeted creature |
| `[AIDisable` | GM | Disable AI on targeted creature |
| `[AIInfo` | GM | View creature AI identity details |
| `[AIStatus` | GM | View global AI registry stats |
| `[AIToggle` | GM | Toggle AI system on/off |
| `[AIReload` | GM | Reload AIOrchestrator.cfg |
| `[AIDebug` | GM | Display all current config values |
| `[AISetModel` | GM | Set model per subagent type |
| `[AISetBackend` | GM | Switch LLM backend at runtime |
| `[instancedungeon` | GM | List/info/reset instanced dungeons |
| `[SentientDebug` | GM | Debug sentient item state |
| `[AddBountyBoard` | GM | Place a bounty board item |
| `[AddCommunityBoard` | GM | Place a community board item |
| `[AddRomanceGiver` | GM | Place a romance quest giver |
| `[ExportConfig` | GM | Export faction config to JSON |

### Administrator Commands
| Command | Access | Description |
|---------|--------|-------------|
| `[AIEnableAll` | Admin | Enable AI on all BaseCreatures |
| `[AIDisableAll` | Admin | Disable AI on all creatures |
| `[AIAutoAttach` | Admin | Toggle auto-AI on spawn |
| `[GMArcs` | Admin | View AI Game Master story arcs |
| `[GMPhase` | Admin | View/manage current narrative phase |
| `[GMStory` | Admin | View full story state |
| `[GMHistory` | Admin | View narrative event history |
| `[GMThink` | Admin | Force Game Master reasoning tick |
| `[spawndungeonportals` | Admin | Spawn all dungeon entrance portals |
| `[exportconfig` | Admin | Export orchestrator configuration |
| `[importconfig` | Admin | Import orchestrator configuration |
| `[gencliloc` | Admin | Generate cliloc entries |
| `[aiprof` | Admin | View subagent profiling data |
| `[aiprofreset` | Admin | Reset profiling counters |
| `[iotest` | GM | Test LLM backend IO latency |

---

## New Creatures & NPCs

### Variant Creatures (20+)
- **Orcs:** OrcArcher, OrcBeastmaster, OrcKnight, OrcShaman, OrcWarlord
- **Undead:** GreaterSkeleton, SkeletalArcher, SkeletalMage, SkeletalLich
- **Trolls:** GreaterTroll, TrollChieftain, TrollWitchdoctor
- **Daemons:** LesserDaemon, LesserDragon
- **Lizardmen:** LizardmanShaman, LizardmanHighPriest, LizardmanSniper
- **Tactical Archetypes:** TacticalArcherBase, TacticalMageBase (abstract bases for variant spawns)
- **GreaterOrc** — heavy melee variant

### Shadowlord Creatures
- ShadowlordEmissary, ShadowlordKnight, ShadowlordMaster
- AstorothLieutenant, FaerlonLieutenant, NosfentorLieutenant

### Named NPCs
- **LordBritishNPC**, **LordBlackthornNPC** — Britain Castle rulers
- **MondainTheWizard** — Destard encounter with guards
- **Dupre, Iolo, Shamino** — Avatar heroes (spawned in Trinsic via AvatarSpawner)
- **NemesisLieutenant** — procedural nemesis encounters

### Hireling NPCs
- **HeroHireling** — 5 specializations (Warrior, Ranger, Mage, Healer, Tinker), levelable 1-20, squad support, talent system

### Dungeon Bosses (20)
Each with a unique WeaponAbility: Sewer King (BleedAttack), Tidal Lord Aquanis (ConcussionBlow), Crystal Queen (InfectiousStrike), Cutthroat Captain Vex (Disarm), Archmage Spirit Morvain (PsychicAttack), Umbral King (ShadowStrike), Lich Lord Malachar (MortalStrike), King Gribble (CrushingBlow), High Priest Dagon (ParalyzingBlow), Crystal Colossus (ArmorPierce), Frost Lord Jorund (ColdWind), Abbot Malachar (MysticArc), Master Smith Bronzebeard (FrenziedWhirlwind), Elder Root Theradras (NerveStrike), Archfiend Mol'Thar (WhirlwindAttack), Astral Overseer Zy'lan (ForceofNature), Lich King Azrael (MortalStrike), Titan Memory Argus (CrushingBlow), The Librarian (PsychicAttack), Void Tyrant Xul'Gorath (ArmorPierce)

---

## New Items

### Dungeon Reward Items (~80)

**Tier 1 (Level 15-30):** SewerKey, RatmansFang, MuckwalkerBoots, BritainBadgeOfHonor, PotionOfClarity, TrinsicCrest, FloodTreads, HydromancersRing, PurifiedWaterVial, TidecallerStaff, MinersPickOfReturning, CrystalHeart_Small, SpiderSilkBoots, LodeFragment, MinocMiningBadge, BrigandsCutlass, StolenGoldPouch, RouteMapFragment, BanditHood, SmugglersSatchel, SpiritbinderRobe, MoonglowCrystalStaff, AncientTomePage, PhylacteryShard, EctoplasmicResidue

**Tier 2 (Level 30-50):** ShadowbaneSword, UmbralCloak, ShadowWalkerBoots, VaultKeyRing, DarkEssenceOrb, EmeraldStaff, CatacombKey, LichsGrimoire, SoulGem, BoneArmorSet_Chest/Arms/Legs/Helm/Gorget, GoblinTuskBlade, ShamanStick, GoblinGoldHoard, WarrenMap, GoblinToothNecklace, TridentOfTheDepths, CoralScaleArmor_Chest/Arms/Legs, BreathingPearl, AncientShipManifest, SeaGodsBlessing, CrystalLongsword, PrismaticShield, GeodeRing, CrystalHeartFragment, LightbenderCloak

**Tier 3 (Level 45-65):** PermafrostBlade, FrozenHeartShield, ArcticRobe, IceCrystalStaff, SnowstriderBoots, ScarletRobeOfAtonement, CensorOfIncense, BloodstainedTome, MonksPrayerBeads, RedemptionMace, RuneforgedHammer, EverlastingEmber, AncientSchematic, MastercraftGem, LivingBarkShield, DruidsStaffOfWhispers, SeedOfLife, FeySilkCloak, SylvanBow, InfernalGreatsword, VolcanicShield, DemonhideArmor, MoltenCoreAmulet, HellstriderBoots

**Tier 4 (Level 55-95):** StarforgedStaff, CelestialRobe, AstralCompass, ConstellationShield, OrbOfDivination, SoulReaperScythe, BoneArmorOfTheDead_Chest/Arms/Legs/Helm, PhylacteryOfKings, SoulGemPendant, DeathsCloak, TitansFistHammer, PrimordialStoneShield, GiantsToeCharm, MountainHeartCore, ColossusGreaves, InfiniteTome, ReadingGlassesOfClarity, BookmarkOfReturning, LibrariansStamp, TabooKnowledgeScroll, VoidforgedBlade, NullMatterArmor, RiftStabilizer, EmptyGemOfPower, VoidWalkerBoots

### Item Sets (6)
| Set | Pieces | Bonus |
|-----|--------|-------|
| Shadow Vault Set | ShadowbaneSword + UmbralCloak + ShadowWalkerBoots | +10 DefendChance |
| Frost Citadel Set | PermafrostBlade + FrozenHeartShield + ArcticRobe + SnowstriderBoots | +10 ColdResist, +5 PhysResist, +5 DefendChance |
| Crystal Cavern Set | CrystalLongsword + PrismaticShield + GeodeRing | +5 PhysResist, +5 EnergyResist, +5 Int |
| Coral Scale Set | CoralScale chest + legs + arms | +5 PhysResist, +5 PoisonResist, +5 Dex |
| Dwarven Plate Set | (DwarvenPlate armor pieces) | +5 PhysResist, +5 FireResist, +5 Str |
| Void Rift Set | VoidforgedBlade + NullMatterArmor + VoidWalkerBoots | +10 EnergyResist, +5 PhysResist, +10 Hits |

### Weapons & Equipment
- **MysticBow** — custom bow class that triggers arrow/bolt effects
- **MysticCrossbow** — custom crossbow class for bolt effects
- **DaemonSword** — faction-themed sword (LootTableIntegration)
- **DragonScale** — faction-themed crafting reagent (LootTableIntegration)
- **FactionLootItem** — generic faction-aligned loot container

### Enhanced Ammunition (14 types)
- **Arrows:** FireArrow, FrostArrow, PoisonArrow, LightningArrow, HolyArrow, VoidArrow, ExplosiveArrow
- **Bolts:** FireBolt, FrostBolt, PoisonBolt, LightningBolt, HolyBolt, ExplosiveBolt
- All have real OnHit effects: damage, status, AoE, sound, particles

### Craftable Consumables
- HeartyStew (Cooking), ManaBerryPie (Cooking), DragonBreathWhiskey (Cooking), GoblinAle (Cooking)
- SentientItemComponent — "Sentient Essence" crafted via Tinkering at 90.0/130.0 skill (ingredients: PhoenixFeather + VampireFang + DragonScale)

### Misc Items
- InstanceToken — dungeon entry item
- DungeonPortalItem — physical portal to dungeon instances
- BountyBoardItem, CommunityBoardItem — placeable board objects
- TownPlotDeed — housing plot deed
- LoveLetter — romance system letter
- MiscItems (additional utility items)

---

## New Dungeons

20 instanced dungeons across 4 tiers, each with 4 rooms + 1 boss, accessible via physical portals in Britain, Trinsic, and Minoc.

**Tier 1 — Normal (15-30):**
1. Britain Sewers (Britain) — Ratman/BleedAttack boss
2. Trinsic Sewers (Trinsic) — WaterElemental/ConcussionBlow boss
3. Abandoned Mine (Minoc) — GiantSpider/InfectiousStrike boss
4. Brigand Hideout (Trinsic) — OrcishLord/Disarm boss
5. Moonglow Crypts (Britain) — LichLord/PsychicAttack boss

**Tier 2 — Normal (25-50):**
6. The Shadow Vault (Britain)
7. The Emerald Catacombs (Britain)
8. The Goblin Warrens (Minoc)
9. The Sunken Temple (Trinsic)
10. The Crystal Caverns (Minoc)

**Tier 3 — Normal/Heroic (35-70):**
11. The Frost Citadel (Britain)
12. The Scarlet Monastery (Trinsic)
13. The Dwarven Forge (Minoc)
14. The Whispering Woods (Britain)
15. The Infernal Foundry (Minoc) — Heroic

**Tier 4 — Heroic/Epic (55-95):**
16. The Astral Observatory (Britain) — Heroic
17. The Necropolis of Souls (Trinsic) — Heroic
18. The Titan's Rest (Minoc) — Heroic
19. The Endless Library (Britain) — Epic
20. The Void Rift (Trinsic) — Epic

---

## Installation

### ServUO Setup

1. **Copy the files** into your ServUO installation: You need to download a copy of ServUO and throw my files to overwrite.
   ```
   Scripts/Custom/AIOrchestrator/  →  your ServUO/Scripts/Custom/AIOrchestrator/
   ```
   (Or compile directly — the project file at `Scripts/Scripts.csproj` auto-includes all `.cs` files under `Scripts/Custom/`.)

2. **Modify `SetItem.cs`** (ServUO base code): The tricky part (use the script)
   Add the 6 new SetItem enum values to `Scripts/Items/Artifacts/Equipment/Armor/Sets/SetItem.cs`:
   ```cs
   ShadowVault, FrostCitadel, CrystalCavern, DwarvenPlate, VoidRift, CoralScale
   ```

3. **Extend recipe enums** in:
   - `Scripts/Services/Craft/DefCooking.cs` — add `HeartyStew`, `ManaBerryPie`, `DragonBreathWhiskey`, `GoblinAle` to `CookRecipes`
   - `Scripts/Services/Craft/DefTinkering.cs` — add `SentientItemInfusion` to `TinkerRecipes`
   - `Scripts/Services/Craft/DefBlacksmithy.cs` — add `OrcishWarAxe`, `LichBoneStaff` to `SmithRecipes`
   - `Scripts/Services/Craft/DefBowFletching.cs` — add `FireArrow` through `ExplosiveBolt` (255-267) to `BowRecipes`

4. **Build**:
   ```bash
   cd /path/to/ServUO
   dotnet build
   ```

5. **Start the server** — AIOrchestrator initializes automatically on world start. The first run generates `Config/AIOrchestrator.cfg`.

### LLM Backend Setup

AIOrchestrator supports 7 LLM backends. You only need **one**. You can choose many though!

#### Option A: Ollama (Recommended — Easiest)
```bash
# Install Ollama from https://ollama.com
ollama pull pathfinder-speed   # or any compatible model
ollama serve
```
Default URL: `http://127.0.0.1:11434`

#### Option B: vLLM
```bash
pip install vllm
python -m vllm.entrypoints.openai.api_server --model pathfinder-speed --port 8000
```
Default URL: `http://127.0.0.1:8000`

#### Option C: LM Studio
1. Download from [lmstudio.ai](https://lmstudio.ai)
2. Load a compatible model (e.g., `pathfinder-speed` or any GGUF)
3. Start the local inference server (Settings → Local Inference Server → Start)
4. Default URL: `http://127.0.0.1:1234`

#### Option D: KoboldCPP
```bash
# Download from https://github.com/LostRuins/koboldcpp
./koboldcpp.py model.gguf --port 5001
```
Update URL in config to `http://127.0.0.1:5001`

#### Option E: Text Generation Inference (TGI)
```bash
# Using Docker
docker run -p 8080:80 ghcr.io/huggingface/text-generation-inference:latest --model-id pathfinder-speed
```
Update URL in config to `http://127.0.0.1:8080`

#### Option F: LlamaCpp
```bash
./server -m model.gguf --port 8080
```
Set `LLMBackend=llamacpp` and update URL in config.

#### Option G: OpenAI-Compatible API
Set `LLMBackend=openai`, configure `OpenAIBaseUrl` and optionally `OpenAIApiKey` for any OpenAI-compatible endpoint (OpenAI, Azure, Groq, Together, etc.).

---

## Running Without an LLM

AIOrchestrator works **without any LLM backend**. Set `Enabled=false` in `Config/AIOrchestrator.cfg` or toggle at runtime with `[AIToggle`.

When the LLM is disabled:
- All non-LLM gameplay systems still function: dungeons, loot, hirelings, pets, factions, territories, war, economy, quests, crafting, item sets, arrow effects, creature variants, instanced content
- NPCs use their default ServUO AI (no LLM dialogue, no LLM combat chatter, no LLM narration)
- The heartbeat system skips subagent inference calls
- Faction diplomacy runs on rule-based defaults instead of LLM-generated events
- The AI Game Master narrative system is paused

You get a fully functional gameplay expansion without any AI dependency.

---

## AI Orchestration Architecture

```
                         ┌─────────────────────────────────┐
                         │       AIOrchestratorInit         │
                         │   (static initialization)        │
                         └──────────────┬──────────────────┘
                                        │
                         ┌──────────────▼──────────────────┐
                         │         AIHeartbeat (500ms)      │
                         │  Iterates all AIComponents       │
                         └───────┬──────────┬──────────┬───┘
                                 │          │          │
              ┌──────────────────┘          │          └──────────────────┐
              ▼                              ▼                             ▼
     ┌────────────────┐           ┌──────────────────┐        ┌────────────────────┐
     │  AIComponent    │  ────►   │   AIComponent     │  ...   │   AIComponent      │
     │ (Creature A)    │          │  (Creature B)      │        │  (Creature N)      │
     └───────┬────────┘          └───────┬──────────┘        └───────┬────────────┘
             │                           │                           │
             ├─ DialogueSubagent         ├─ DialogueSubagent         ├─ DialogueSubagent
             ├─ CombatSubagent           ├─ CombatSubagent           ├─ CombatSubagent
             ├─ EnvironmentSubagent      ├─ EnvironmentSubagent      ├─ EnvironmentSubagent
             └─ HirelingSubagent         └─ HirelingSubagent         └─ HirelingSubagent
                                                        │
                                           ┌────────────▼────────────┐
                                           │     LLMClient           │
                                           │  (routes to backend)    │
                                           └────────────┬────────────┘
                                                        │
                              ┌─────────────────────────┼─────────────────────────┐
                              │                         │                         │
                              ▼                         ▼                         ▼
                        ┌──────────┐            ┌──────────────┐          ┌──────────────┐
                        │  Ollama   │            │  vLLM / TGI  │          │  OpenAI-compat│
                        │ :11434    │            │  :8000       │          │  (any)       │
                        └──────────┘            └──────────────┘          └──────────────┘
```

### Subagent Roles
| Subagent | Function | Model Config Key |
|----------|----------|-----------------|
| **CombatSubagent** | Morale lines, death cries, surrender, combat evaluation | `ModelCombat` |
| **DialogueSubagent** | Player-facing conversation, gossip, rumors | `ModelDialogue` |
| **EnvironmentSubagent** | Ambient behavior, zone reactions, routine actions | `ModelEnvironment` |
| **EconomySubagent** | Price manipulation, shortage events, trade route AI | `ModelEconomy` |
| **FactionDiplomatSubagent** | Alliance/war/peace LLM negotiation events | `ModelFaction` |
| **SpawnControllerSubagent** | Spawn density adjustment, variant selection | `ModelSpawner` |
| **DungeonMasterSubagent** | Room narration, boss encounter flavor text | `ModelDungeon` |
| **NarratorSubagent** | Zone-entry description, world event narration | `ModelNarrator` |

### Data Flow
1. **AIHeartbeat** fires every 500ms
2. For each player with nearby creatures, heartbeat determines mode (combat / hireling / environment)
3. The appropriate subagent constructs a prompt from NPC memory, mood, faction, and world context
4. **LLMClient** sends the prompt to the configured backend with timeout (default 30s)
5. Response is parsed and executed (speech, action, emote, combat decision)
6. NPC memory is updated (turn count, last interaction, mood delta)
7. Profiling records latency per subagent per tick

---

## Configuration

All settings live in `Config/AIOrchestrator.cfg` (auto-generated on first run, applied on server start, reloadable via `[AIReload`).

```ini
# Core
Enabled=true                       # Master toggle
HeartbeatMs=500                     # AI tick interval
MaxNpcsPerPlayer=25                 # Max AI creatures per player

# Backend
LLMBackend=ollama                   # ollama|vllm|openai|lmstudio|koboldcpp|tgi|llamacpp
OllamaBaseUrl=http://127.0.0.1:11434
OpenAIBaseUrl=http://127.0.0.1:8000
OpenAIApiKey=                       # Leave blank for local models

# Per-subagent models
ModelCombat=pathfinder-speed
ModelDialogue=pathfinder-speed
ModelEnvironment=pathfinder-speed
ModelEconomy=pathfinder-speed
ModelFaction=pathfinder-speed
ModelSpawner=pathfinder-speed
ModelDungeon=pathfinder-speed
ModelNarrator=pathfinder-speed

# Feature toggles
ChatterEnabled=true                 # Ambient NPC chatter
RoutineEnabled=true                 # NPC daily routines
GossipEnabled=true                  # NPC-to-NPC gossip
FavorEnabled=true                   # Favor/gift system
DenizenEnabled=true                 # Denizen encounters
AnomalyEnabled=true                 # Anomaly events

# Request limits
RequestTimeoutMs=30000              # LLM request timeout
MaxReplyChars=240                   # Max characters in NPC speech
MaxMemoryTurns=6                    # Conversation turns remembered
```

---

## License & Attribution

**AIOrchestrator** is released under the **MIT License**.

```
MIT License

Copyright (c) 2026 AIOrchestrator Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Third-Party Attributions

- **[ServUO](https://www.servuo.com)** — Game server emulator. ServUO is the community-maintained fork of the RunUO project. Licensed under GPL v2 / Custom ServUO license.
- **Ollama, vLLM, LM Studio, KoboldCPP, TGI, LlamaCpp** — Each is an independent open-source LLM inference engine with its own licensing; AIOrchestrator is backend-agnostic and does not modify or redistribute any of them.
- **Pathfinder-series models** — Recommended lightweight local models; any causal language model compatible with the above backends will work.
- **Ultima Online** — All game content references (creatures, items, locations, lore) are the property of Electronic Arts / Broadsword Online Games. AIOrchestrator is a fan project and is not affiliated with or endorsed by EA or Broadsword.

### AI Assistance

Portions of this codebase, including the orchestration prompt templates, subagent response parsers, and this README, were developed with assistance from large language models (LLMs).

---

Built for ServUO. Powered by LLMs. Inspired by the Virtues.
