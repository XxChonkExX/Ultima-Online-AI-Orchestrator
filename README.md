AIOrchestrator — ServUO AI Enhancement Project
Overview
AI-driven NPC systems for ServUO adding dynamic relationships, taming, bounties, hirelings, quests, economy, and emergent world events. Supports multiple LLM backends (Ollama, vLLM, OpenAI, LM Studio, KoboldCpp, TGI, LlamaCpp).

Installation
1. Copy Files to Your ServUO (download a copy of ServUO and jam these guys in there) Next part is a bit trickier.....


  UOAIServer/  (Copy your ServUO Into here or viceversa)

    Scripts/

          Custom/

              AIOrchestrator/*.cs (49 files)

                  Factions/*.cs                 (3 files)
                Scripts.csproj             <-- ADD ONE LINE

    Server/ (in the servUO)
      Server.csproj

    Ultima/
      Ultima.csproj

2. Register the Scripts Project
Edit Scripts/Scripts.csproj and add:

        <ItemGroup>
          <Compile Include="Custom\AIOrchestrator\**\*.cs" />
        </ItemGroup>

        or <Compile Include="Custom\AIOrchestrator\**\*.cs" />
(Or in Visual Studio: right-click Scripts project → Add → Existing Item → select all .cs files in AIOrchestrator)

3. Configure LLM Backend
Option A: Ollama (default, local)
# Install Ollama/vLLM/OpenAI whatevers (More instructions on that below)
curl -fsSL https://ollama.ai/install.sh | sh  # Linux/macOS
# Or download from https://ollama.ai for Windows

# Pull a model- Nemotron kept jamming Llama in here. Use any model you want. 
ollama pull llama3.1:8b


OK! The magic can happen here if you want it to. Assign multiple models through this system. Add specialists or generalists as you please. Grab a wordy or concise model. Supersmall coders/toolcallers. Since this is a unified backend compatible, I had Nemotron (god, he sucks. Deepseek is on cool down tho T.T) build instructions for you.

Config (Server/Config/AIOrchestrator.cfg):

Enabled=true
LLMBackend=ollama
OllamaBaseUrl=http://127.0.0.1:11434
ModelCombat=llama3.1:8b
ModelDialogue=llama3.1:8b
ModelEnvironment=llama3.1:8b
ModelEconomy=llama3.1:8b
ModelFaction=llama3.1:8b
ModelSpawner=llama3.1:8b
ModelDungeon=llama3.1:8b
ModelNarrator=llama3.1:8b

Option B: vLLM (high throughput, OpenAI-compatible)
# Install
pip install vllm

# Run server
python -m vllm.entrypoints.openai.api_server \
  --model meta-llama/Llama-3.1-8B-Instruct \
  --host 0.0.0.0 --port 8000 \
 
Config:

LLMBackend=vllm
OpenAIBaseUrl=http://127.0.0.1:8000
OpenAIApiKey=  # leave empty for local
ModelCombat=meta-llama/Llama-3.1-8B-Instruct
ModelDialogue=meta-llama/Llama-3.1-8B-Instruct
# ... same for all models

Option C: LM Studio (GUI + local server)
Install LM Studio from https://lmstudio.ai
Load a model (e.g., llama-3.1-8b-instruct)
Click "Start Server" (default port 1234)
Config:
LLMBackend=lmstudio
OpenAIBaseUrl=http://127.0.0.1:1234
ModelCombat=llama-3.1-8b-instruct
# ...

Option D: OpenAI / Cloud
LLMBackend=openai
OpenAIBaseUrl=https://api.openai.com
OpenAIApiKey=sk-your-key-here
ModelCombat=gpt-4o-mini
ModelDialogue=gpt-4o-mini
# ...

Option E: KoboldCpp / TGI / LlamaCpp
All use OpenAI-compatible API:

# KoboldCpp (--api flag)
LLMBackend=koboldcpp
OpenAIBaseUrl=http://127.0.0.1:5001

# TGI (HuggingFace Text Generation Inference)
LLMBackend=tgi
OpenAIBaseUrl=http://127.0.0.1:8080

# LlamaCpp (--server)
LLMBackend=llamacpp
OpenAIBaseUrl=http://127.0.0.1:8080
4. Network Configuration
Localhost Only (default)
All backends default to 127.0.0.1. Server and LLM must run on same machine.

LAN / Remote LLM
If LLM runs on another machine:

# Example: vLLM on 192.168.1.50:8000
OpenAIBaseUrl=http://192.168.1.50:8000
Ensure firewall allows the port (8000, 11434, 1234, 5001, 8080, etc.).

Docker / Container
If ServUO runs in Docker but LLM on host:

# Use host.docker.internal (Docker Desktop) or host IP
OllamaBaseUrl=http://host.docker.internal:11434
OpenAIBaseUrl=http://host.docker.internal:8000
Reverse Proxy (nginx/Traefik)
# nginx example for vLLM
server {
    listen 8000;
    location /v1/ {
        proxy_pass http://localhost:8001/v1/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
Then OpenAIBaseUrl=http://your-proxy:8000

5. Build & Run -Nemo built some whack batch file, Deepseek had a better one but Nemo trashed it so this is what you get since its late. Just run builder from ServUO if this sucks
# Windows
_windebug.bat

# Or via dotnet
dotnet build Scripts/Scripts.csproj -c Debug
dotnet build Server/Server.csproj -c Debug
Run:

_windebug.bat
# Server starts on port 2593 (default)
In-Game Commands (GM)
[AIReload           — Reload config without restart
[AIStatus           — Show current backend, models, settings
[AIToggle           — Enable/disable AI system
[AISetBackend <backend>  — Switch: ollama|vllm|openai|lmstudio|koboldcpp|tgi|llamacpp
[AISetModel <type> <name>  — combat|dialogue|environment|economy|faction|spawner|dungeon|narrator

Core Systems

NPC Relationship System
States: Stranger → Acquaintance → Friend → Trusted → RomanticPartner / Hired / Apprentice / Household
Affinity (0–1000) via gifts, dialogue, combat, proximity
Unlocks: hire, apprentice, romance, assign role (guard/vendor/crafter/farmer/cook/entertainer), move into house
Context menu on all tamable/hired/vendor NPCs
Universal Taming
All creatures tamable — auto MinTameSkill / ControlSlots from stats/abilities/fame
Paragon/boss scaling, taming mastery support
No manual creature edits
Tame → Relationship Integration
Tamed creatures auto-gain relationship entries
Affinity on tame, bond, level-up, feeding, combat
Hero Hirelings
Classes: Warrior/Mage/Ranger/Cleric/Bard/Rogue/Paladin/Necromancer
Level/XP, skill training, equipment, loyalty, inventory
Gold hire/dismiss, world spawners
Bounty System
Procedural bounties on elites (champions, paragons, bosses)
Region-aware, tiered rewards (gold, faction rep, rare items)
Bounty boards for towns/inns
Living Economy
Supply/demand per region, dynamic prices, trade routes
Faction-themed loot tables
Regional Threats
Threat levels escalate from player activity
Dynamic spawn scaling, elite patrols, world events
Nemesis System
Personal nemeses remember player, scale with them
Persistent grudges, unique loot
AI Game Master
Emergent multi-phase narrative arcs
World-state synthesis (economy, threats, deeds, factions)
LLM-driven event generation
Faction Reputation & Diplomacy
Player/NPC faction standing
Dynamic alliances, war, trade embargoes
AI diplomat subagent
Dynamic Quests
Procedural: kill, gather, escort, explore, craft, relation
Nested objectives, time limits, branching outcomes
Relationship integration
Quality-of-Life Features
Gumps
RelationshipGump — All NPC bonds, affinity, state, role, household
BountyBoardGump — Browse/accept bounties, track progress
HirelingMarketGump — Global hireling browser with filters
CommunityBoardGump — Player posts (trade, group, RP), 24hr expiry
Items
LoveLetter — Give to NPCs: +50 affinity (+100 if RomanticPartner)
BountyHunterBadge — Kill tracking, ranks (Novice→Legendary), auto-updates
TownPlotDeed — 60-sec town housing exemption in guarded regions
Food/Drink: HeartyStew (HP+Stam), DragonBreathWhiskey (Fire Resist), ManaBerryPie (Mana), GoblinAle (Str/Dex trade)
Weapons: LichBoneStaff (ManaLeech), OrcishWarAxe (StamLeech)
Armor: TrollSkinBoots (HP Regen)
Reagents: PhoenixFeather, VampireFang
NPCs & Quests
RomanceQuestGiver — 3-step: Nightshade → Defend rival → Craft Gold Ring → LoveLetter
Player Commands
[ViewRelations        — Target NPC → RelationshipGump
[AddCommunityBoard    — Place community board
[AddBountyBoard       — Place bounty board
[AddRomanceGiver      — Spawn romance quest giver
[HirelingMarket       — Open hireling market
Integration Hooks
Context menu: "View Relationship", "Give Love Letter" (when carrying)
Drag-drop: LoveLetter → NPC consumes for affinity
Event: BountyHunterBadge records kills on creature death
Registered in AIOrchestratorInit.Initialize()
Project Structure
Scripts/Custom/AIOrchestrator/
├── Core (49 files)
├── Factions/ (3 files)
└── QoL (12 files)
Requirements
ServUO (current)
.NET 8.0+
LLM backend of choice (see above)
