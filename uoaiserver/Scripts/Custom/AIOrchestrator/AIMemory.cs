using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    public class PlayerMemory
    {
        public string PlayerName { get; set; }
        public int TimesGreeted { get; set; }
        public int TimesHelped { get; set; }        // completed quests / bought goods
        public int TimesHarmed { get; set; }         // stole from / attacked NPC
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime FirstEncounter { get; set; } = DateTime.UtcNow;
        public bool IsBanned { get; set; }           // NPC refuses service
        public int Reputation { get; set; } = 0;     // -100 (hated) to +100 (beloved)

        // Faction reputation system
        public Dictionary<string, int> FactionReputation { get; set; } = new Dictionary<string, int>();
    }

    public class AIMemory
    {
        public string NpcSerial { get; set; }
        public Dictionary<string, ConversationMemory> PlayerConversations { get; set; } = new Dictionary<string, ConversationMemory>();
        public Dictionary<string, PlayerMemory> PlayerMemories { get; set; } = new Dictionary<string, PlayerMemory>();
        public NpcIdentity Identity { get; set; }
        public Dictionary<string, object> PersistentData { get; set; } = new Dictionary<string, object>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        private static readonly string MemoryPath = Path.Combine(Core.BaseDirectory, "Saves", "AIOrchestrator", "Memory");
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public AIMemory() { }

        public AIMemory(string npcSerial)
        {
            NpcSerial = npcSerial;
        }

        public PlayerMemory GetOrCreatePlayerMemory(string playerSerial, string playerName)
        {
            if (!PlayerMemories.TryGetValue(playerSerial, out var pm))
            {
                pm = new PlayerMemory { PlayerName = playerName, FirstEncounter = DateTime.UtcNow };
                PlayerMemories[playerSerial] = pm;
            }
            pm.LastSeen = DateTime.UtcNow;
            pm.PlayerName = playerName;
            return pm;
        }

        public void RecordGreeting(string playerSerial, string playerName)
        {
            var pm = GetOrCreatePlayerMemory(playerSerial, playerName);
            pm.TimesGreeted++;
            pm.Reputation = Math.Max(-100, Math.Min(100, pm.Reputation + 1)); // greetings build trust
        }

        public void RecordHelp(string playerSerial, string playerName)
        {
            var pm = GetOrCreatePlayerMemory(playerSerial, playerName);
            pm.TimesHelped++;
            pm.Reputation = Math.Min(100, pm.Reputation + 10);
        }

        public void RecordHarm(string playerSerial, string playerName)
        {
            var pm = GetOrCreatePlayerMemory(playerSerial, playerName);
            pm.TimesHarmed++;
            pm.Reputation = Math.Max(-100, pm.Reputation - 15);
        }

        // Faction reputation methods
        public void ModifyFactionReputation(string playerSerial, string playerName, string factionName, int delta)
        {
            var pm = GetOrCreatePlayerMemory(playerSerial, playerName);
            if (!pm.FactionReputation.TryGetValue(factionName, out var current))
                current = 0;
            var newValue = Math.Max(-100, Math.Min(100, current + delta));
            pm.FactionReputation[factionName] = newValue;
        }

        public int GetFactionReputation(string playerSerial, string factionName)
        {
            if (PlayerMemories.TryGetValue(playerSerial, out var pm) &&
                pm.FactionReputation.TryGetValue(factionName, out var rep))
                return rep;
            return 0;
        }

        public string GetFactionContext(string playerSerial, string playerName)
        {
            if (!PlayerMemories.TryGetValue(playerSerial, out var pm))
                return "";

            if (pm.FactionReputation.Count == 0)
                return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Faction Standing:");
            foreach (var kvp in pm.FactionReputation)
            {
                var standing = GetStandingText(kvp.Value);
                sb.AppendLine($"- {kvp.Key}: {standing} ({kvp.Value})");
            }
            return sb.ToString();
        }

        private static string GetStandingText(int value)
        {
            if (value <= -75) return "Hated";
            if (value <= -50) return "Hostile";
            if (value <= -25) return "Unfriendly";
            if (value < 0) return "Wary";
            if (value == 0) return "Neutral";
return "Revered";
        }

        public bool IsPlayerKnown(string playerSerial)
        {
            return PlayerMemories.ContainsKey(playerSerial);
        }

        /// <summary>
        /// Returns a formatted string describing the player's relationship with this NPC.
        /// Used by DialogueSubagent to inject into LLM prompts.
        /// </summary>
        public string GetRelationshipContext(string playerSerial, string playerName)
        {
            if (!PlayerMemories.TryGetValue(playerSerial, out var pm))
                return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Your relationship with " + playerName + ":");
            sb.AppendLine("- Reputation: " + pm.Reputation + " (-100=hated, +100=beloved)");
            if (pm.TimesGreeted > 0)
                sb.AppendLine("- Times spoken: " + pm.TimesGreeted);
            if (pm.TimesHelped > 0)
                sb.AppendLine("- Times helped: " + pm.TimesHelped);
            if (pm.TimesHarmed > 0)
                sb.AppendLine("- Times harmed: " + pm.TimesHarmed);
            if (pm.IsBanned)
                sb.AppendLine("- " + playerName + " is banned from your presence!");
            sb.AppendLine("- First encounter: " + pm.FirstEncounter.ToString("g"));

            return sb.ToString();
        }

        public void AddConversationTurn(string playerSerial, string playerName, string playerMessage, string npcResponse)
        {
            if (!PlayerConversations.TryGetValue(playerSerial, out var memory))
            {
                memory = new ConversationMemory { PlayerName = playerName };
                PlayerConversations[playerSerial] = memory;
            }

            memory.Turns.Add(new ConversationTurn
            {
                Timestamp = DateTime.UtcNow,
                PlayerMessage = playerMessage,
                NpcResponse = npcResponse
            });

            while (memory.Turns.Count > AIConfig.MaxMemoryTurns)
                memory.Turns.RemoveAt(0);

            LastUpdated = DateTime.UtcNow;
        }

        public string GetConversationContext(string playerSerial)
        {
            if (!PlayerConversations.TryGetValue(playerSerial, out var memory))
                return string.Empty;

            var context = new System.Text.StringBuilder();
            foreach (var turn in memory.Turns)
            {
                context.AppendLine("Player: " + turn.PlayerMessage);
                context.AppendLine("NPC: " + turn.NpcResponse);
            }
            return context.ToString();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(MemoryPath);
                var file = Path.Combine(MemoryPath, NpcSerial + ".json");
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(file, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AIOrchestrator] Memory save error: " + ex.Message);
            }
        }

        public static AIMemory Load(string npcSerial)
        {
            try
            {
                var file = Path.Combine(MemoryPath, npcSerial + ".json");
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    return JsonSerializer.Deserialize<AIMemory>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AIOrchestrator] Memory load error: " + ex.Message);
            }
            return new AIMemory(npcSerial);
        }

        /// <summary>
        /// Static method to modify faction reputation for a player from any context.
        /// Finds the NPC's memory file and updates it.
        /// </summary>
        public static void ModifyFactionReputationGlobal(string playerSerial, string playerName, string factionName, int delta)
        {
            try
            {
                var dir = Path.Combine(Core.BaseDirectory, "Saves", "AIOrchestrator", "Memory");
                if (!Directory.Exists(dir)) return;

                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var memory = JsonSerializer.Deserialize<AIMemory>(json);
                        if (memory == null) continue;

                        if (!memory.PlayerMemories.TryGetValue(playerSerial, out var pm))
                            continue;

                        if (!pm.FactionReputation.TryGetValue(factionName, out var current))
                            current = 0;
                        var newValue = Math.Max(-100, Math.Min(100, current + delta));
                        pm.FactionReputation[factionName] = newValue;
                        pm.LastSeen = DateTime.UtcNow;
                        pm.PlayerName = playerName;

                        // Save the updated memory
                        var updatedJson = JsonSerializer.Serialize(memory, JsonOptions);
                        File.WriteAllText(file, updatedJson);
                        break; // Only need to update one NPC's memory (they all share player data)
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FACTION] Error modifying global faction reputation: {ex.Message}");
            }
        }
    }

    public class ConversationMemory
    {
        public string PlayerName { get; set; }
        public List<ConversationTurn> Turns { get; set; } = new List<ConversationTurn>();
    }

    public class ConversationTurn
    {
        public DateTime Timestamp { get; set; }
        public string PlayerMessage { get; set; }
        public string NpcResponse { get; set; }
    }

    public class NpcIdentity
    {
        public string Name { get; set; }
        public string Vocation { get; set; }
        public string Homeland { get; set; }
        public string Temperament { get; set; }
        public string Backstory { get; set; }
        public string SpeechStyle { get; set; }
        public string PrivateDrive { get; set; }
        public int Mood { get; set; } = 50;

        public string GetPersonalityPrompt(bool isNight = false, string weatherContext = "")
        {
            var prompt = "You are " + Name + ", a " + Vocation + " from " + Homeland + ".\n" +
                         "Temperament: " + Temperament + "\n" +
                         "Background: " + Backstory + "\n" +
                         "Speech style: " + SpeechStyle + "\n" +
                         "Personal drive: " + PrivateDrive + "\n" +
                         "Current mood: " + Mood + "/100";

            if (isNight)
                prompt += "\n\nIt is night time. Speak softer, more tired, or more mysterious as appropriate.";

            if (!string.IsNullOrEmpty(weatherContext))
                prompt += "\n\n" + weatherContext;

            prompt += "\n\nRules: Speak in character. Max " + AIConfig.MaxReplyChars + " characters. Use Britannia-appropriate language. NO internal monologue, NO reasoning, NO self-talk. Just respond directly as your character.";
            return prompt;
        }
    }

    /// <summary>
    /// NPC type classification for personality templates.
    /// </summary>
    public enum NpcClass
    {
        Generic,
        Blacksmith,
        Tailor,
        Healer,
        Mage,
        Guard,
        Bard,
        Merchant,
        Ranger,
        Thief,
        Cook,
        Farmer,
        Miner,
        Scholar,
        Tavernkeep,
        Monster,
        Animal
    }

    /// <summary>
    /// Personality prompt templates keyed by NPC class.
    /// </summary>
    public static class PersonalityTemplates
    {
        public static string GetPrompt(NpcClass npcClass, NpcIdentity identity, bool isNight, string weather)
        {
            var basePrompt = identity.GetPersonalityPrompt(isNight, weather);
            var template = GetClassTemplate(npcClass);
            return basePrompt + "\n\n" + template;
        }

        private static string GetClassTemplate(NpcClass npcClass)
        {
            return npcClass switch
            {
                NpcClass.Blacksmith => "Tone: Gruff, strong, talks about metal and quality. Uses phrases like 'good steel', 'tempered right', 'aye, that'll hold'.",
                NpcClass.Healer => "Tone: Soothing, gentle, speaks of the body and spirit. Uses phrases like 'by the Virtues', 'be at ease', 'the body mends'.",
                NpcClass.Mage => "Tone: Mysterious, intellectual, cryptic. Uses phrases like 'the weave of magic', 'fascinating', 'the ether speaks'.",
                NpcClass.Guard => "Tone: Stern, dutiful, formal. Uses phrases like 'by order of', 'keep the peace', 'move along', 'stay out of trouble'.",
                NpcClass.Bard => "Tone: Dramatic, poetic, knows everything about everyone. Uses rhymes, songs, and gossip. Speaks in a storytelling manner.",
                NpcClass.Merchant => "Tone: Persuasive, calculating, friendly but profit-driven. Uses phrases like 'a fair price', 'fine quality', 'for you, a deal'.",
                NpcClass.Ranger => "Tone: Quiet, observant, nature-focused. Uses phrases like 'the forest whispers', 'the old ways', 'track carefully'.",
                NpcClass.Thief => "Tone: Sly, whispering, cryptic. Uses phrases like 'a word in your ear', 'I know a guy', 'for a price'.",
                NpcClass.Tavernkeep => "Tone: Warm, loud, gossipy. Uses phrases like 'what'll it be?', 'heard the latest?', 'rumor has it', 'one more?'.",
                NpcClass.Scholar => "Tone: Verbose, precise, references ancient texts. Uses phrases like 'according to', 'fascinating discovery', 'the annals record'.",
                NpcClass.Cook => "Tone: Cheerful, food-obsessed. Uses phrases like 'fresh from the oven', 'my special recipe', 'you look hungry!'.",
                NpcClass.Monster => "Tone: Aggressive, guttural, menacing. Speaks in short, threatening sentences. May be crude or bestial.",
                _ => "" // Generic — use the base identity prompt as-is
            };
        }

        /// <summary>
        /// Determine NPC class from a BaseCreature's AI type and type name.
        /// </summary>
        public static NpcClass Classify(BaseCreature creature)
        {
            var name = creature.GetType().Name.ToLowerInvariant();
            var ai = creature.AI;

            // Check by AI type first
            switch (ai)
            {
                case AIType.AI_Vendor:
                    if (name.Contains("healer")) return NpcClass.Healer;
                    if (name.Contains("mage") || name.Contains("wizard")) return NpcClass.Mage;
                    if (name.Contains("bard") || name.Contains("minstrel")) return NpcClass.Bard;
                    if (name.Contains("tavern") || name.Contains("inn") || name.Contains("barkeep")) return NpcClass.Tavernkeep;
                    if (name.Contains("smith") || name.Contains("forge") || name.Contains("armor") || name.Contains("weapon")) return NpcClass.Blacksmith;
                    if (name.Contains("tailor") || name.Contains("weaver")) return NpcClass.Tailor;
                    if (name.Contains("cook") || name.Contains("baker")) return NpcClass.Cook;
                    if (name.Contains("farmer") || name.Contains("shepherd")) return NpcClass.Farmer;
                    if (name.Contains("miner") || name.Contains("lumber")) return NpcClass.Miner;
                    if (name.Contains("scholar") || name.Contains("scribe") || name.Contains("librarian")) return NpcClass.Scholar;
                    if (name.Contains("ranger") || name.Contains("woods") || name.Contains("hunter")) return NpcClass.Ranger;
                    if (name.Contains("thief") || name.Contains("rogue") || name.Contains("beggar")) return NpcClass.Thief;
                    return NpcClass.Merchant;

                case AIType.AI_Melee:
                    if (name.Contains("guard") || name.Contains("soldier") || name.Contains("knight") || name.Contains("paladin")) return NpcClass.Guard;
                    if (name.Contains("thief") || name.Contains("rogue")) return NpcClass.Thief;
                    return NpcClass.Monster;

                case AIType.AI_Mage:
                case AIType.AI_NecroMage:
                case AIType.AI_Spellweaving:
                case AIType.AI_Mystic:
                case AIType.AI_Necro:
                case AIType.AI_Spellbinder:
                    return NpcClass.Mage;

                case AIType.AI_Archer:
                case AIType.AI_Predator:
                case AIType.AI_Animal:
                    return NpcClass.Animal;

                case AIType.AI_Berserk:
                    return NpcClass.Monster;

                case AIType.AI_Healer:
                    return NpcClass.Healer;

                default:
                    return NpcClass.Generic;
            }
        }
    }
}