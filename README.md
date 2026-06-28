[README.md](https://github.com/user-attachments/files/29433900/README.md)
# UO AI Shard - Setup Guide

An AI-powered Ultima Online server running on Ollama with LLM models.
Every NPC in the world has AI-generated dialogue, behaviors, and world events.
NOTE: As an Orchestrator, you can set several models to handle tasks. One for Dialogue, one for Combat, one for World Events. I use a q6 31b Dense Model for everything
but you can easily use smaller/more focused or specialist models as needed. I get 1 sec chat reponse times from NPCs on my Q6 18 gb 31b model. Edit: Made the script for AI Enable to fire off automatically. Let me know if there are bugs! Should be so many.

## What's in this package

```
F:\UOAIServer\
├── Server\Binaries\           # Compiled ServUO server + AI Scripts.dll
├── Server\Config\             # Server configuration files
├── Server\Data\               # Map tiles, decorations, bulk orders
├── Server\Spawns\             # NPC spawn definitions
├── Server\RevampedSpawns\     # Dungeon revamp spawns
├── Ollama\                    # Ollama startup scripts + Modelfile-speed
├── Source\AIOrchestrator\     # Full AI source code (open to modification)
├── start_ai_shard.bat         # Combined startup script
├── start_uo_client.bat        # Client launcher
└── README.md                  # This file
```

## Prerequisites

### 1. Install Ultima Online Classic Client
Download and install the UO Classic Client to:
`D:\uo\Electronic Arts\Ultima Online Classic`

Or update the path in `Server\Config\DataPath.cfg` to match your install location.

### 2. Install Ollama
Download from https://ollama.com/ and install.

Download a model of choice. This is an example of a decent model most consumer cards can run, lightweight and can handle calls.
```powershell 
ollama pull gemma2:9b-instruct-q4_K_M 
```

### 1. Start Ollama
run `ollama serve`.

### 2. Create the speed-optimized model wrapper
```powershell
ollama create YourModelHere -f F:\UOAIServer\Ollama\Modelfile-urnamehere
```

### 3. Start the server
```
F:\UOAIServer\start_ai_shard.bat
```

### 4. Edit AI config if needed
`F:\UOAIServer\Server\Config\AIOrchestrator.cfg`

Key settings:
```
Enabled=true
OllamaBaseUrl=http://127.0.0.1:11434/v1
ModelDialogue=# Change to your model name            NOTE: As an Orchestrator, you can set several models to handle these tasks. One for Dialogue, one for Combat, one for World.
ModelCombat=# Change to your model name             NOTE: As an Orchestrator, you can set several models to handle these tasks. One for Dialogue, one for Combat, one for World
ModelEnvironment=# Change to your model name          NOTE: As an Orchestrator, you can set several models to handle these tasks. One for Dialogue, one for Combat, one for World
MaxReplyChars=160
```

### 5. Launch the client
```
F:\UOAIServer\start_uo_client.bat
```

Connect to `127.0.0.1:2593`

First login: Account "admin" with any password (auto-created).

### 6. Enable AI on all NPCs
In-game, run:
```
[AIEnableAll
```

Then `[AIStatus` to confirm. Walk up to any NPC and say "hello"!

## In-Game Commands

| Command | Description |
|---------|-------------|
| `[AIEnableAll` | Attach AI to ALL NPCs in the world |
| `[AIDisableAll` | Remove AI from all NPCs |
| `[AIStatus` | Show how many NPCs have AI and current config |
| `[AIDebug` | Display current AI configuration |
| `[AIReload` | Reload AIOrchestrator.cfg without restart |
| `[AIToggle` | Enable/disable AI system on-the-fly |
| `[AISetModel dialogue YourModelHere` | Change the dialogue model |

## Architecture

```
Player speaks "hello"
  → EventSink.Speech fires
    → AIEventIntegration.OnSpeech()
      → DialogueSubagent.OnSpeech()
        → LLMClient.ChatAsync()  ← HTTP POST to Ollama /api/generate
          → Ollama (LLM)
            → Raw completion response (~1.5s)
        → Timer.DelayCall
          → PublicOverheadMessage()  ← Yellow chat bubble over NPC head
```

- Uses Ollama `/api/generate` with `raw=true` (bypasses chat template = no reasoning overhead)
- All requests serialized through a SemaphoreSlim (one at a time to avoid GPU contention)
- No external JSON libraries (manual build/parse for .NET Framework 4.8 compatibility)
- World events (weather, invasions) fire globally every 20-45 minutes

## Customization

- **Edit NPC personalities**: Modify `NpcIdentityGenerator.cs` in Source folder
- **Change response length**: Edit `MaxReplyChars` in `AIOrchestrator.cfg`
- **Change memory**: Edit `MaxMemoryTurns` in `AIOrchestrator.cfg`
- **Change Heartbeat frequency**: Edit `HeartbeatMs` in `AIOrchestrator.cfg`

## Troubleshooting

**"System.Text.Json" error**: Use the included Scripts.dll (already fixed). If rebuilding from source, ensure no `System.Text.Json` dependency.

**"A task was canceled"**: Check Ollama is running (`ollama list`). Wait for model to load on first request (~10-30s cold start).

**No NPC responses**: Run `[AIEnableAll` first. Check server console for `[AI SPEECH]` debug lines.

**Out of VRAM**: Use a smaller model. Change `ModelDialogue` in config and rebuild the speed wrapper.
