using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server;
using Server.Commands;

namespace Server.AIOrchestrator
{
    public enum LLMBackend
    {
        Ollama,
        VLLM,
        OpenAI,
        LMStudio,
        KoboldCpp,
        TGI,
        LlamaCpp
    }

    public static class AIConfig
    {
        public static bool Enabled { get; private set; } = true;
        public static int HeartbeatMs { get; private set; } = 1000;
        public static int MaxNpcsPerPlayer { get; private set; } = 10;
        
        // Backend selection
        public static LLMBackend LLMBackend { get; private set; } = LLMBackend.Ollama;
        
        // Backend URLs
        public static string OllamaBaseUrl { get; private set; } = "http://127.0.0.1:11434";
        public static string OpenAIBaseUrl { get; private set; } = "http://127.0.0.1:8000";
        public static string OpenAIApiKey { get; private set; } = "";

        // Model names per subagent
        public static string ModelCombat { get; private set; } = "pathfinder-speed";
        public static string ModelDialogue { get; private set; } = "pathfinder-speed";
        public static string ModelEnvironment { get; private set; } = "pathfinder-speed";
        public static string ModelEconomy { get; private set; } = "pathfinder-speed";
        public static string ModelFaction { get; private set; } = "pathfinder-speed";
        public static string ModelSpawner { get; private set; } = "pathfinder-speed";
        public static string ModelDungeon { get; private set; } = "pathfinder-speed";
        public static string ModelNarrator { get; private set; } = "pathfinder-speed";

        // Legacy RAG (unused)
        public static bool RagEnabled { get; private set; } = false;
        public static string RagUrl { get; private set; } = "http://127.0.0.1:6333";
        public static string RagCollection { get; private set; } = "uo_lore";

        // Feature toggles
        public static bool ChatterEnabled { get; private set; } = true;
        public static bool RoutineEnabled { get; private set; } = true;
        public static bool GossipEnabled { get; private set; } = true;
        public static bool FavorEnabled { get; private set; } = true;
        public static bool DenizenEnabled { get; private set; } = true;
        public static bool AnomalyEnabled { get; private set; } = true;

        // Request settings
        public static int RequestTimeoutMs { get; private set; } = 30000;
        public static int MaxReplyChars { get; private set; } = 160;
        public static int MaxMemoryTurns { get; private set; } = 3;

        private static readonly string ConfigPath = Path.Combine(Core.BaseDirectory, "Config", "AIOrchestrator.cfg");

        public static void Initialize()
        {
            Load();
            CommandSystem.Register("AIReload", AccessLevel.GameMaster, new CommandEventHandler(OnReload));
            CommandSystem.Register("AIToggle", AccessLevel.GameMaster, new CommandEventHandler(OnToggle));
            CommandSystem.Register("AIDebug", AccessLevel.GameMaster, new CommandEventHandler(OnDebug));
            CommandSystem.Register("AIStatus", AccessLevel.GameMaster, new CommandEventHandler(OnStatus));
            CommandSystem.Register("AISetModel", AccessLevel.GameMaster, new CommandEventHandler(OnSetModel));
            CommandSystem.Register("AISetBackend", AccessLevel.GameMaster, new CommandEventHandler(OnSetBackend));
        }

        public static void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                WriteDefaultConfig();
            }

            var lines = File.ReadAllLines(ConfigPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var parts = trimmed.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key.ToLowerInvariant())
                {
                    case "enabled": Enabled = bool.Parse(value); break;
                    case "heartbeatms": HeartbeatMs = int.Parse(value); break;
                    case "maxnpcsperplayer": MaxNpcsPerPlayer = int.Parse(value); break;
                    case "llmbackend": LLMBackend = ParseBackend(value); break;
                    case "ollamabaseurl": OllamaBaseUrl = value; break;
                    case "openaibaseurl": OpenAIBaseUrl = value; break;
                    case "openaikey": OpenAIApiKey = value; break;
                    case "modelcombat": ModelCombat = value; break;
                    case "modeldialogue": ModelDialogue = value; break;
                    case "modelenvironment": ModelEnvironment = value; break;
                    case "modeleconomy": ModelEconomy = value; break;
                    case "modelfaction": ModelFaction = value; break;
                    case "modelspawner": ModelSpawner = value; break;
                    case "modeldungeon": ModelDungeon = value; break;
                    case "modelnarrator": ModelNarrator = value; break;
                    case "ragenabled": RagEnabled = bool.Parse(value); break;
                    case "ragurl": RagUrl = value; break;
                    case "ragcollection": RagCollection = value; break;
                    case "chatterenabled": ChatterEnabled = bool.Parse(value); break;
                    case "routineenabled": RoutineEnabled = bool.Parse(value); break;
                    case "gossipenabled": GossipEnabled = bool.Parse(value); break;
                    case "favorenabled": FavorEnabled = bool.Parse(value); break;
                    case "denizenenabled": DenizenEnabled = bool.Parse(value); break;
                    case "anomalyenabled": AnomalyEnabled = bool.Parse(value); break;
                    case "requesttimeoutms": RequestTimeoutMs = int.Parse(value); break;
                    case "maxreplychars": MaxReplyChars = int.Parse(value); break;
                    case "maxmemoryturns": MaxMemoryTurns = int.Parse(value); break;
                }
            }
        }

        private static LLMBackend ParseBackend(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "ollama": return LLMBackend.Ollama;
                case "vllm": return LLMBackend.VLLM;
                case "openai": return LLMBackend.OpenAI;
                case "lmstudio": return LLMBackend.LMStudio;
                case "koboldcpp": return LLMBackend.KoboldCpp;
                case "tgi": return LLMBackend.TGI;
                case "llamacpp": return LLMBackend.LlamaCpp;
                default: return LLMBackend.Ollama;
            }
        }

        private static void WriteDefaultConfig()
        {
            var config = @"# AIOrchestrator.cfg - AI Orchestrator Configuration
# Generated automatically - edit and use [AIReload to apply changes

Enabled=true
HeartbeatMs=500
MaxNpcsPerPlayer=25
LLMBackend=ollama
OllamaBaseUrl=http://127.0.0.1:11434
OpenAIBaseUrl=http://127.0.0.1:8000
OpenAIApiKey=
ModelCombat=pathfinder-speed
ModelDialogue=pathfinder-speed
ModelEnvironment=pathfinder-speed
ModelEconomy=pathfinder-speed
ModelFaction=pathfinder-speed
ModelSpawner=pathfinder-speed
ModelDungeon=pathfinder-speed
ModelNarrator=pathfinder-speed
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
";
            File.WriteAllText(ConfigPath, config);
        }

        public static void Reload()
        {
            Load();
            Console.WriteLine("[AIOrchestrator] Configuration reloaded.");
        }

        private static void OnReload(CommandEventArgs e)
        {
            Reload();
            e.Mobile.SendMessage("[AIOrchestrator] Configuration reloaded.");
        }

        private static void OnToggle(CommandEventArgs e)
        {
            Enabled = !Enabled;
            e.Mobile.SendMessage($"[AIOrchestrator] AI {(Enabled ? "enabled" : "disabled")}.");
        }

        private static void OnDebug(CommandEventArgs e)
        {
            e.Mobile.SendMessage($"[AIOrchestrator] Enabled: {Enabled}");
            e.Mobile.SendMessage($"[AIOrchestrator] Backend: {LLMBackend}");
            e.Mobile.SendMessage($"[AIOrchestrator] Heartbeat: {HeartbeatMs}ms");
            e.Mobile.SendMessage($"[AIOrchestrator] Max NPCs/Player: {MaxNpcsPerPlayer}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Combat: {ModelCombat}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Dialogue: {ModelDialogue}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Environment: {ModelEnvironment}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Economy: {ModelEconomy}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Faction: {ModelFaction}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Spawner: {ModelSpawner}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Dungeon: {ModelDungeon}");
            e.Mobile.SendMessage($"[AIOrchestrator] Model Narrator: {ModelNarrator}");
        }

        private static void OnStatus(CommandEventArgs e)
        {
            OnDebug(e);
        }

        private static void OnSetModel(CommandEventArgs e)
        {
            if (e.Arguments.Length >= 2)
            {
                var type = e.Arguments[0].ToLowerInvariant();
                var model = e.Arguments[1];

                switch (type)
                {
                    case "combat": ModelCombat = model; break;
                    case "dialogue": ModelDialogue = model; break;
                    case "environment": ModelEnvironment = model; break;
                    case "economy": ModelEconomy = model; break;
                    case "faction": ModelFaction = model; break;
                    case "spawner": ModelSpawner = model; break;
                    case "dungeon": ModelDungeon = model; break;
                    case "narrator": ModelNarrator = model; break;
                    default:
                        e.Mobile.SendMessage("Usage: [AISetModel <combat|dialogue|environment|economy|faction|spawner|dungeon|narrator> <model>");
                        return;
                }
                e.Mobile.SendMessage($"[AIOrchestrator] {type} model set to {model}");
            }
            else
            {
                e.Mobile.SendMessage("Usage: [AISetModel <combat|dialogue|environment|economy|faction|spawner|dungeon|narrator> <model>");
            }
        }

        private static void OnSetBackend(CommandEventArgs e)
        {
            if (e.Arguments.Length >= 1)
            {
                var backendStr = e.Arguments[0].ToLowerInvariant();
                try
                {
                    LLMBackend = ParseBackend(backendStr);
                    e.Mobile.SendMessage($"[AIOrchestrator] Backend set to {LLMBackend}");
                }
                catch
                {
                    e.Mobile.SendMessage("Usage: [AISetBackend <ollama|vllm|openai|lmstudio|koboldcpp|tgi|llamacpp>");
                }
            }
            else
            {
                e.Mobile.SendMessage("Usage: [AISetBackend <ollama|vllm|openai|lmstudio|koboldcpp|tgi|llamacpp>");
            }
        }
    }
}