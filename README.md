# ServUO AI Shard — Unified Multi-Backend Edition AKA: Orchestrator Symphony Edition

> A single-player/local LAN Ultima Online shard with **Ollama/vLLM/OpenAI-compatible AI NPCs**, AI NPC Faction Wars, AI controlled dynamic world systems and AI controlled emergent narrative. AI World events, Game Master control, AI Spawner Control, AI Quests, AI Faction/Diplomacy, AI Chat, Hireling system revamp, huge new variety of lesser and greater enemies, NPC/Enemy Memories of Players, NEMESIS ENEMY System (yeah they remember you), new faction items and weapons, Sentient weapons (yeah they talk), AI controlled Economy, NPC Loyalty/Hatred/Love/Romance/Apprenticeship, NPCs can live at your house, Player Actions have dynamic effects on world chatter and news, Pet Loyalty revamp. Hell. Everything got touched and has some AI control or wrapper attached.

This is my attempt at trying to create a living, breathing, most over the top version of Ultima Online possible. It is a bunch of my bad code combined with better AI code so expect there to be a trillion bugs. Please let me know what breaks so I can fix it. Or make Deepseek fix it.

---

## Quick Start

# Requirements
- Windows 10/11
- .NET Framework 4.8 SDK
- One of: Ollama 0.30+, vLLM, LM Studio, KoboldCpp, TGI, llama.cpp server

# Install
1. Copy this repo to `D:\uo` (or update paths in configs)
2. Run `dotnet build -c Debug` in `D:\uo\ServUO`
3. Start `D:\uo\ServUO\ServUO.exe`
4. Connect client to `127.0.0.1` port `2593`
```

---

## Core Philosophy

| Principle | Implementation |
|-----------|----------------|
| **No cloud required** | 100% local — any OpenAI-compatible backend or Ollama |
| **Single-player balanced** | 3× skill/stat caps, 75% cost reduction, fast travel via `[Go` |
| **GPU isolation** | Intel Arc reserved for Python fine-tuning (PID 24016); 7900 XTX only for LLM |
| **Ultima-authentic** | Factions = 8 Virtues / 8 Vices per wiki.ultimacodex.com |
| **Backend-agnostic** | Switch between Ollama/vLLM/OpenAI/LM Studio at runtime |

---

## Unified LLM Backend Support

### Single Binary, All Backends

| Backend | Config Value | Endpoint | Notes |
|---------|--------------|----------|-------|
| **Ollama** | `ollama` | `http://localhost:11434` | Uses `/api/generate` (Gemma4) or `/api/chat` |
| **vLLM** | `vllm` | `http://localhost:8000/v1` | Best throughput, PagedAttention |
| **LM Studio** | `lmstudio` | `http://localhost:1234/v1` | GUI + API |
| **KoboldCpp** | `koboldcpp` | `http://localhost:5001/v1` | GGUF support |
| **TGI** | `tgi` | `http://localhost:8080/v1` | HuggingFace Text-Gen-Inference |
| **llama.cpp server** | `llamacpp` | `http://localhost:8080/v1` | `llama-server -m model.gguf` |
| **OpenAI/Azure** | `openai` | `https://api.openai.com/v1` | Requires API key |

### Runtime Switching (No Rebuild)

```bash
# In-game commands
[AISetBackend vllm
[AISetModel narrator YourModelHere
[AISetModel economy mistral-7b

# Or edit D:\uo\ServUO\Config\AIOrchestrator.cfg:
LLMBackend=vllm
OpenAIBaseUrl=http://127.0.0.1:8000
OpenAIApiKey=
ModelNarrator=YourModelHere
ModelEconomy=mistral-7b-instruct
```

---

## Model Configuration (Per-Subagent)

```csharp
// All default to "YourModelHere" — change per subagent
ModelCombat     = "YourModelHere"   // NPC combat decisions
ModelDialogue   = "YourModelHere"   // NPC speech
ModelEnvironment= "YourModelHere"   // Weather/events
ModelEconomy    = "YourModelHere"   // Price fluctuations
ModelFaction    = "YourModelHere"   // Diplomacy/war
ModelSpawner    = "YourModelHere"   // Spawn directives
ModelDungeon    = "YourModelHere"   // Dungeon encounters
ModelNarrator   = "YourModelHere"   // Game Master stories
```

---

## AI Architecture

### 6 Model-Driven Subagents (Each Uses Its Own Model Config)

| Subagent | Interval | Output Format | Purpose |
|----------|----------|---------------|---------|
| **Economy** | 15 min | `ITEM\|REGION\|CHANGE\|DESC` | Dynamic prices from kill/harvest data |
| **Faction Diplomat** | 25 min | `TYPE\|TARGET\|OTHER\|DESC` | WAR/ALLIANCE/BOUNTY/TRUCE/SUMMON/BETRAYAL |
| **Spawn Controller** | 20 min | `TYPE\|CREATURE\|REGION\|MULT\|DESC` | SURGE/SUPPRESS/EMPOWER/INVASION/RETREAT |
| **Dungeon Master** | 15 min | `TYPE\|DUNGEON\|DESC\|EFFECT` | Encounters, traps, treasure, boss tactics |
| **Environment** | 10–15 min | `WEATHER\|DESC` / `TYPE\|DESC` | 12 weather types, 16 event types |
| **Game Master (Narrator)** | 15 min | Free text `[Rumor]`/`[Warning]` | Multi-phase narrative (Calm→Build→Crisis→Resolve) |

**All subagents:** These can easily be scaled up to suit performance and system/server needs.
- Call `LLMClient.ChatAsync(system, prompt, modelName)` — backend-agnostic
- Emit **pipe-delimited** lines (no JSON — avoids .NET 4.8 type init crash)
- Run async via `Task.Run` from static timers (non-blocking)
- Staggered intervals to spread GPU load
- `SemaphoreSlim(2)` concurrency gate, 30s timeout, `max_tokens=30`, `ctx=1024`

---

## Major Features

### 1. Virtue/Vice Faction System (18 Factions)
```
Virtue Factions                    Vice Factions
├── Britain Guard       (Honesty)  ├── Cult of Deceit        (Deceit)
├── Healer's Circle     (Compassion)├── Orcish Horde          (Cruelty)
├── Trinsic Paladins    (Valor)    ├── Bandit's Guild        (Cowardice)
├── Moonglow Mages      (Justice)  ├── Necromancer Cult      (Injustice)
├── Minoc Crafters      (Sacrifice)├── Undead Scourge        (Gluttony)
├── Jhelom Mercenaries  (Honor)    ├── Pirate Brotherhood    (Dishonor)
├── Woodland Protectors (Spirituality)├── Void Abyss            (Unbelief)
├── Humble Folk         (Humility) ├── Prideful Sorcerers    (Pride)
└── Merchant League (Neutral)      └── Dragon Brood (Neutral/Chaotic)
```
- Each NPC has `VirtueAlignment` + `ViceAlignment` enums
- Player actions shift reputation; NPCs remember & react

### 2. AI Quest System (13 Quest Types)
| Classic | New (AI-Expanded) |
|---------|-------------------|
| KillCount, Collect, Deliver, Escort, Explore | **CombatBoss, MerchantRun, CraftItem, NpcRelation, GuardDuty, Rescue, Scout, GatherResource, Bounty** |
- Progress hooks: `ReportKill`, `ReportDelivery`, `ReportCrafted`, `ReportGathered`, `ReportGuardDutyProgress`, `ReportScoutComplete`, `ReportNpcRelation`
- LLM generates flavor text per NPC personality

### 3. Hero Hirelings (10 Classes)
Warrior, Archer, Mage, Paladin, Ranger, Ninja, AnimalTamer, Necromancer, Bard, Alchemist
- Unique gear, hire cost, context-menu hire/dismiss
- Auto-repopulating spawner (3 min)

### 4. Creature Variants (16 New Types + AI-Integrated Spawner)
```
OrcShaman, OrcArcher, OrcKnight, OrcBeastmaster,
LizardmanShaman, LizardmanSniper,
TrollWitchdoctor,
SkeletalMage, SkeletalArcher,
LesserOrc, LesserDragon, LesserDaemon,
GreaterOrc, GreaterTroll, GreaterSkeleton
```
- **CreatureVariantSpawner** with 6 themes (OrcCamp, LizardmanNest, TrollCave, UndeadCrypt, LesserDungeon, GreaterThreat)
- **AI-integrated**: respects SpawnController directives (multiplier, suppress, empower)

### 5. Faction-Themed Loot
Dragon scales, daemon blades, tribal warclubs, shrouds, giant hammers, gems — 30% drop rate per creature type

### 6. Multi-Phase Narrative Engine
```
Calm  →  Build  →  Crisis  →  Resolve  →  Calm
  │       │         │          │
  ▼       ▼         ▼          ▼
Rumors  Tension  Invasion  Aftermath
```
- Phase shifts every 3–8 ticks (35% chance after min)
- Game Master queries **all 5 other subagents** + deeds/threats for context
- Commands: `[GMThink`, `[GMStory`, `[GMPhase`, `[GMArcs`

---

## Configuration (`D:\uo\ServUO\Config\AIOrchestrator.cfg`)

```ini
# AIOrchestrator.cfg - AI Orchestrator Configuration
# Edit and use [AIReload to apply changes

Enabled=true
HeartbeatMs=500
MaxNpcsPerPlayer=25
LLMBackend=ollama              # ollama | vllm | openai | lmstudio | koboldcpp | tgi | llamacpp
OllamaBaseUrl=http://127.0.0.1:11434
OpenAIBaseUrl=http://127.0.0.1:8000
OpenAIApiKey=
ModelCombat=YourModelHere
ModelDialogue=YourModelHere
ModelEnvironment=YourModelHere
ModelEconomy=YourModelHere
ModelFaction=YourModelHere
ModelSpawner=YourModelHere
ModelDungeon=YourModelHere
ModelNarrator=YourModelHere
RagEnabled=false
RagUrl=http://127.0.0.1:6333
RagCollection=uo_lore
ChatterEnabled=true
RoutineEnabled=true
GossipEnabled=true
FavorEnabled=true
DenizenEnabled=true
AnomalyEnabled=true
RequestTimeoutMs=30000
MaxReplyChars=240
MaxMemoryTurns=6
```

---

## Admin Commands

| Command | Access | Description |
|---------|--------|-------------|
| `[AIReload` | GM | Reload config from disk |
| `[AIToggle` | GM | Enable/disable all AI |
| `[AIDebug` / `[AIStatus` | GM | Show full config status |
| `[AISetModel <type> <model>` | GM | Set model for subagent (combat\|dialogue\|environment\|economy\|faction\|spawner\|dungeon\|narrator) |
| `[AISetBackend <backend>` | GM | Switch LLM backend (ollama\|vllm\|openai\|lmstudio\|koboldcpp\|tgi\|llamacpp) |
| `[GMThink` | Admin | Force Game Master story tick |
| `[GMStory` | Admin | Show full world state (all subagents) |
| `[GMPhase <Calm\|Build\|Crisis\|Resolve>` | Admin | Get/set narrative phase |
| `[GMArcs` | Admin | List active narrative arcs |

---

## Directory Structure
```
D:\uo\
├── ServUO.exe
├── Scripts.dll
├── Config\AIOrchestrator.cfg
└── Scripts\Custom\AIOrchestrator\      (33 .cs files)
    ├── Core: AIConfig, LLMClient, AIHeartbeat, AIOrchestratorInit
    ├── Subagents: Economy, FactionDiplomat, SpawnController, DungeonMaster, Environment, AIGameMaster
    ├── Quests: AIQuestSystem, AIQuestManager, QuestProgressHook
    ├── Factions: NpcFaction, FactionReputationSystem
    ├── World: LivingEconomy, RegionalThreatSystem, PlayerDeedTracker, NPCRelationshipSystem
    ├── Content: HeroHireling, CreatureVariants, LootTableIntegration
    └── Hooks: DungeonRegionHook, AIEventIntegration, EconomyDataHook2
```

---

## Performance Notes

| Metric | Ollama | vLLM (est.) |
|--------|--------|-------------|
| Model | pathfinder-speed 7B Q4_K_M | Same (HF format) |
| GPU layers | 40/61 | N/A (full GPU) |
| Generation speed | ~25s / 30 tok (1.4 tok/s) | ~2-5s / 30 tok |
| Max concurrent | 2 (semaphore) | 2 (configurable)adjustable) |
| VRAM | ~14 GB | ~16-18 GB |

---

## Migration: Ollama → vLLM (30 sec)

```bash
# 1. Convert model to HF format (if GGUF)
#    Use: llama.cpp convert.py or download HF version

# 2. Serve with vLLM
vllm serve YourModelHere --gpu-memory-utilization 0.9 --max-model-len 1024 --port 8000

# 3. In-game
[AISetBackend vllm
[AISetModel narrator YourModelHere

# 4. Verify
[AIStatus
```

## Credits
- **ServUO** — Core emulator (GPL-3.0)
- **Ollama / vLLM / LM Studio / KoboldCpp / TGI / llama.cpp** — Local LLM runtimes
- **pathfinder-speed** — Model (7B Q4_K_M)
- **Ultima Codex** — Virtue/Vice reference
- **DeepSeek 4 Flash and Nemotron 3

---

## License
ServUO is GPL-3.0. This AI orchestration layer follows the same license.
