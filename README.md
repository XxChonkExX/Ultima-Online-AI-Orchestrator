# AI Orchestrator for ServUO

An AI-powered framework for ServUO that replaces static NPCs with intelligent, goal-driven agents. Hirelings converse, fight, harvest, craft, crew boats, run shops, guard your home, and react emotionally — all powered by a pluggable LLM backend (or fully offline).

> **~12,500 lines of C# | 66 files | 7 AI backends supported**

---

## Quick Start

```bash
# 1. Clone ServUO
git clone https://github.com/ServUO/ServUO.git
cd ServUO

# 2. Copy AI Orchestrator files into Scripts/Custom/AIOrchestrator/

# 3. (Optional) Install Ollama for AI conversations
ollama pull llama3.2
ollama serve

# 4. Build & run
dotnet build ServUO.sln -c Debug
cd Server/bin/Debug/net8.0
ServUO.exe
```

---

## What You Get

### 18 Hero Hireling Classes
Warrior, Archer, Mage, Paladin, Ranger, Ninja, Animal Tamer, Necromancer, Bard, Alchemist, **Assassin, Berserker, Warlock, Spellblade, Crusader, Shadowmage, Beast Rider, and The Avatar** — each with unique stats, skills, equipment, and appearance.

### The Avatar — Legendary Hero
One male and one female Avatar stand in **Trinsic center** (Felucca + Trammel). Maxed everything: 200+ stats, all skills 120, 80-95 resistances, Vanquishing Sword of Justice, Shield of the Virtues, Ring of the Eight Virtues with full regen, all spells, level 20, bonded, blessed.

### Multi-Agent AI Brain
Every hireling has an AI-driven "brain" that dispatches specialized sub-agents:

- **Commander** — combat, following, guarding
- **Vendor** — shopkeeping, pricing, customer buying
- **Harvester** — mining (Iron→Valorite) and lumberjacking (Log→Frostwood)
- **Sailor** — navigation to 16 ports, auto-fishing
- **Guard Post Controller** — area patrol, auto-engage hostiles

### NPC Mood & Personality
10 mood states (Ecstatic→Betrayed) × 10 personalities (Brave→Playful) = 100 combinations affecting combat, work speed, loyalty, and dialogue tone. NPCs bark when their mood changes.

### Hireling Commands
- **Combat**: "All kill", "Follow me", "Stay", "Guard me"
- **Vendor**: "Set up shop", price items, customers browse & buy
- **Harvest**: "Mine here", "Chop here", "Stop harvesting"
- **Guard**: "Guard post here", "Dismiss guard post"
- **Mood**: Praise ("Good job!"), scold ("Bad!"), gift gold, `/Report`

### Faction System (8 Factions)
Britannian Crown, Minax, Shadowlords, Council of Mages, True Britannians, Orcish Horde, Undead Legion, Outlaw. Full reputation tracking (±5000), faction quartermasters (9 vendors in Britain), faction-colored items, reputation-gated access, faction loot drops.

### Bounty System
Auto-generated bounties on notorious players. Placeable bounty boards for houses/guilds. Wearable Bounty Hunter Badge (Bronze→Mythic tiers with stat bonuses). Outlaw tracking with guard hostility.

### Sea Battles & Naval Encounters
Random NPC fleet encounters at 11 coastal cities. 3-7 crew + captain per battle. Full lifecycle (Announced→Active→Completed). Faction-aligned enemies. Loot chests with gold, items, treasure maps. Global broadcasts.

### Sailor Hirelings & Boat Automation
Hire sailors to navigate to 16 ports, auto-fish, crew your boat. Boat registry with upkeep cleanup. Commands: "Take me to Britain", "Go fish", "Crew my boat", "Disembark".

### Dynamic World Systems

| System | What it does |
|---|---|
| **Living Economy** | Supply/demand price fluctuations across all vendors |
| **Regional Threats** | Orc invasions, undead rising, monster migrations that escalate over time |
| **Nemesis System** | Arch-enemies that gain power each time they escape, with 5 lieutenant tiers |
| **AI Game Master** | Multi-phase narrative engine that generates story events based on world state |
| **Dungeon Master** | Per-dungeon AI overseer with dynamic encounters, loot balancing, trap placement |
| **Spawn Controller** | Dynamic respawn rates, difficulty scaling, boss spawn conditions |

### 22 New Creature Variants
Orc Shaman/Archer/Knight/Beastmaster/Warlord, Lizardman Shaman/Sniper/High Priest, Troll Witchdoctor/Chieftain, Skeletal Mage/Archer/Lich, Lesser Dragon/Daemon/Orc, Greater Orc/Troll/Skeleton. Each with unique abilities and themed loot.

### Relationship & Social Systems
NPC relationships with 14 roles (Stranger→Spouse→Nemesis), romance system (flirt, date, propose, marry), love letters, gift tracking, pet bonding. Community bulletin boards for player posts.

### Quest & Progression
3 faction quest types (kill targets, collect items, earn titles), universal taming system (tame ANY creature), All Skills Book (any skill, 0→120), player deed tracking with `[mytitles` command.

### 20+ New Items
Sentient weapons/armor that level up and speak, Hearty Stew, Dragon Breath Whiskey, Mana Berry Pie, Lich Bone Staff, Orcish War Axe, Troll Skin Boots, Phoenix Feather, Vampire Fang, and more.

### All 7 AI Backends Supported
Ollama, vLLM, LM Studio, OpenAI, KoboldCpp, TGI, LlamaCpp — all interchangeable via `AIConfig.cs`. Each sub-agent can use a different model. Output sanitization strips inference artifacts. Max 4 concurrent LLM slots.

### 14 Admin Commands
`[aiconfig`, `[aiinfo`, `[aiglobal`, `[ainear`, `[aistatus`, `[aidebug`, `[globalai`, `[spawnhero`, `[factionrep`, `[economy`, `[outlawstatus`, `[mytitles`, `[ai-model set`

---

## Offline Automation (New)

Your hirelings keep working even when you're logged off. These systems run on **global timers** independent of player proximity:

| System | Interval | What happens while you're away |
|---|---|---|
| **Household Crafting** | 2 minutes | NPCs at workstations (anvil, loom, oven, etc.) craft items from house storage, gain skill |
| **Vendor Shop** | 30 seconds | NPC stays in place, customers can buy. Auto-restocks from nearby items every 30 min |
| **Mining / Lumber** | 5 seconds | Keeps harvesting — ore/wood fills backpack continuously |
| **Sea Battles** | 10 minutes | Naval encounters spawn and resolve autonomously |
| **Economy** | 5 minutes | Prices fluctuate, supply/demand adjusts globally |
| **Regional Threats** | 5 minutes | Threats escalate or de-escalate |
| **Game Master** | 5 minutes | Story arc progresses, events generate |
| **Mood Drift** | 1 minute | NPC moods drift naturally |
| **Bounties** | 5–15 minutes | New bounties generate on kills |
| **Boat Upkeep** | 1 minute | Orphaned boats cleaned up |

### Crafting → Vendor Pipeline

A hireling in **vendor mode** doing **household tasks** will automatically:
1. Craft items at workstations using house storage materials
2. Deposit finished goods into their backpack (not the floor)
3. Auto-price items based on type (plate armor = 300gp, potions = 25gp, etc.)
4. Sell to customers who walk up and say "Buy"
5. Keep restocking as long as materials exist

**What this means**: Set up a hireling with a forge, feed them ingots, enable vendor mode. They'll smith plate armor 24/7 and sell it to passing players — fully automated.

---

## No AI Backend? No Problem

Without an LLM backend, all hireling features still work: combat, vendor, harvest, guard, sailing, crafting, moods, relationships, faction reputation, sea battles, bounties, threats, and quests. The AI backend adds conversational intelligence and proactive behavior on top.

---

## Configuration

Edit `Scripts/Custom/AIOrchestrator/AIConfig.cs`:

```csharp
public static string BaseUrl = "http://localhost:11434";  // Your LLM backend
public static string Model = "llama3.2";                   // Model name
public static int HeartbeatMs = 3000;                      // AI tick rate
public static int MaxNpcsPerPlayer = 5;                    // Max AI NPCs near you
```

Or use `[aiconfig` in-game to view/edit settings at runtime.

---

## FAQs

**Q: Will this break my existing world?**  
A: No. All systems use unique namespaces. Existing data is untouched. Additive only.

**Q: Can I use this on a production shard?**  
A: Yes. Designed for live servers with balanced mood effects, rep caps, and spawn rates.

**Q: Can I add my own hero class?**  
A: Yes — add to the `HeroClass` enum, add a `case` in `SetupClass()` with stats/gear.

**Q: Can I turn existing NPCs into AI NPCs?**  
A: Yes — use `[aiinfo` on any `BaseCreature` to toggle AI integration.

**Q: Which LLM model should I use?**  
A: Local: Llama 3.2 3B (fast) or Llama 3.1 8B (smarter). Cloud: GPT-4o-mini or Claude 3 Haiku.

---

## License

Provided as a modification for ServUO. ServUO is licensed under GPL v2.

---

*Built for ServUO. Powered by LLMs. Inspired by the Virtues.*
