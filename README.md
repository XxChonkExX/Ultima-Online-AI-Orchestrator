# AI Orchestrator for ServUO

An **AI-powered framework** for ServUO (Ultima Online emulator) that replaces static NPCs with intelligent, goal-driven agents. Hirelings can hold conversations, follow complex commands, fight, harvest resources, crew boats, and react emotionally — all powered by a pluggable LLM backend.

---

## Table of Contents

1. [Features Overview](#features-overview)
2. [New Hireling Classes (17 Hero Types)](#new-hireling-classes-17-hero-types)
3. [Multi-Agent AI Orchestrator](#multi-agent-ai-orchestrator)
4. [NPC Mood & Personality System](#npc-mood--personality-system)
5.. [Hireling Command System](#hireling-command-system)
6. [Harvest Subagent (Mining / Lumberjacking)](#harvest-subagent-mining--lumberjacking)
7. [Faction System Overhaul](#faction-system-overhaul)
8. [Sea Battles & Naval Encounters](#sea-battles--naval-encounters)
9. [Sailor Hirelings & Boat Automation](#sailor-hirelings--boat-automation)
10. [AI Quest & Reputation Integration](#ai-quest--reputation-integration)
11. [All AI Inference Backends Supported](#all-ai-inference-backends-supported)
12. [Installation Guide](#installation-guide)
13. [Configuration](#configuration)
14. [Commands Reference](#commands-reference)
15. [Frequently Asked Questions](#frequently-asked-questions)

---

## Features Overview

| Feature | Description |
|---|---|
| **17 Hero Hireling Classes** | From Warrior to Beast Rider, each with unique stats, gear, skills, and appearance |
| **The Avatar** | Legendary male/female hero in Trinsic with maxed everything |
| **Multi-Agent Orchestrator** | NPC brains run as role-specialized sub-agents (Commander, GuardPostController, Harvester, Sailor, Vendor) |
| **NPC Mood & Personality** | 10 mood states × 10 personalities = 100 unique behavioral combinations |
| **Guard Post System** | Assign hirelings to protect an area; heartbeat patrol + auto-engage |
| **Vendor Mode** | Turn any hireling into a shopkeeper with priced inventory |
| **Harvest Subagent** | Autonomous mining (all ore types) and lumberjacking (all wood types) |
| **Faction Quartermasters** | 9 faction-specific vendors with reputation-based access |
| **Sea Battles** | Random NPC fleet encounters at 11 coastal cities with faction-aligned crews |
| **Sailor Hirelings** | Navigate to 16 ports, auto-fish, crew your boat |
| **Boat Automation** | Port registry, boat tracking, upkeep cleanup |
| **Faction Quests** | Kill targets, collect items, earn titles; reputation hooks on all kills |
| **AI Backend** | Ollama, vLLM, LM Studio, OpenAI, KoboldCpp, TGI, LlamaCpp — all supported |
| **All-Skills Book** | NPCs learn skills from a skill book, up to 120 |

---

## New Hireling Classes (17 Hero Types)

All hirelings use the shared `HeroHireling` base (extends `BaseHire`). Each class has unique stats, skills, equipment, and a cosmetic override via `ApplyClassAppearance()`.

| # | Class | Style | Hire Cost | Key Skills |
|---|---|---|---|---|
| 0 | Warrior | Melee tank | ~800 | Swords, Tactics, Parry |
| 1 | Archer | Ranged DPS | ~800 | Archery, Tactics |
| 2 | Mage | Spellcaster | ~800 | Magery, EvalInt, Meditation |
| 3 | Paladin | Holy warrior | ~1000 | Chivalry, Swords, Healing |
| 4 | Ranger | Hybrid ranged | ~800 | Archery, Tracking, Healing |
| 5 | Ninja | Stealth burst | ~1200 | Ninjitsu, Fencing, Stealth |
| 6 | Animal Tamer | Pet master | ~1500 | AnimalTaming, AnimalLore, Veterinary |
| 7 | Necromancer | Dark caster | ~1200 | Necromancy, SpiritSpeak, Magery |
| 8 | Bard | Support/control | ~1000 | Musicianship, Discordance, Peacemaking |
| 9 | Alchemist | Utility caster | ~1000 | Alchemy, Magery, Poisoning |
| 10 | Assassin | Stealth DPS | ~1500 | Stealing, Stealth, Fencing, Poisoning |
| 11 | Berserker | Glass cannon | ~1200 | Axe, Tactics, Healing |
| 12 | Warlock | Hybrid dark | ~2000 | Necromancy, Magery, Swords |
| 13 | Spellblade | Magic melee | ~1500 | Magery, Swords, Parry |
| 14 | Crusader | Tank/healer | ~1500 | Chivalry, Mace, Parry |
| 15 | Shadowmage | Illusion caster | ~1800 | Magery, Ninjitsu, Hiding |
| 16 | Beast Rider | Mounted archer | ~3500 | AnimalTaming, Archery, Veterinary |
| **17** | **Avatar** | **Legendary** | **10000** | **All skills 120** |


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
