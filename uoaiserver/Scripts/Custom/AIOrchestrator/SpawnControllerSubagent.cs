using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.AIOrchestrator.Subagents
{
    /// <summary>
    /// AI-driven Spawn Controller Subagent.
    /// Uses an LLM model to dynamically decide what creatures to spawn,
    /// where, and how many — based on player actions, faction balance,
    /// time of day, and threat levels.
    /// </summary>
    public static class SpawnControllerSubagent
    {
        private static Timer _timer;
        private static DateTime _lastSpawnEvent = DateTime.MinValue;
        private const int SpawnIntervalMinutes = 20;

        // Track spawn decisions made by the AI
        private static List<SpawnDirective> _activeDirectives = new List<SpawnDirective>();

        public static void Initialize()
        {
            _timer = Timer.DelayCall(TimeSpan.FromMinutes(8), TimeSpan.FromMinutes(SpawnIntervalMinutes), SpawnTick);
            Console.WriteLine("[AIOrchestrator] AI Spawn Controller initialized (every " + SpawnIntervalMinutes + " min).");
        }

        /// <summary>Get the multiplier for creature spawns based on AI directives.</summary>
        public static double GetSpawnMultiplier(string region)
        {
            lock (_activeDirectives)
            {
                foreach (var d in _activeDirectives)
                {
                    if (d.Region == "all" || d.Region == region.ToLowerInvariant())
                        return d.Multiplier;
                }
            }
            return 1.0;
        }

        /// <summary>Check if a specific creature type should be suppressed in a region.</summary>
        public static bool IsSuppressed(string creatureType, string region)
        {
            lock (_activeDirectives)
            {
                foreach (var d in _activeDirectives)
                {
                    if (d.Type == "SUPPRESS" &&
                        (d.Region == "all" || d.Region == region.ToLowerInvariant()) &&
                        (d.CreatureType == "all" || d.CreatureType == creatureType.ToLowerInvariant()))
                        return true;
                }
            }
            return false;
        }

        /// <summary>Check if a creature type should be empowered in a region.</summary>
        public static bool IsEmpowered(string creatureType, string region)
        {
            lock (_activeDirectives)
            {
                foreach (var d in _activeDirectives)
                {
                    if (d.Type == "EMPOWER" &&
                        (d.Region == "all" || d.Region == region.ToLowerInvariant()) &&
                        (d.CreatureType == "all" || d.CreatureType == creatureType.ToLowerInvariant()))
                        return true;
                }
            }
            return false;
        }

        private static void SpawnTick()
        {
            try
            {
                if (!AIConfig.Enabled) return;

                var worldState = BuildWorldState();
                if (string.IsNullOrEmpty(worldState)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await GenerateSpawnDirective(worldState);
                        if (!string.IsNullOrEmpty(result))
                        {
                            ApplySpawnDirective(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[AI Spawn] Generation error: " + ex.Message);
                    }
                });

                _lastSpawnEvent = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AI Spawn] Tick error: " + ex.Message);
            }
        }

        private static string BuildWorldState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Current spawn/world state:");

            // Player info
            int playerCount = 0;
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile?.Player == true)
                    playerCount++;
            }
            sb.AppendLine("- Players online: " + playerCount);

            // Time of day
            var now = DateTime.UtcNow;
            bool isNight = now.Hour < 6 || now.Hour >= 20;
            sb.AppendLine("- Time: " + (isNight ? "Night" : "Day"));

            // Regional threat levels
            sb.AppendLine("- Threat levels from known regions");
            sb.AppendLine("- Active directives: " + _activeDirectives.Count);

            return sb.ToString();
        }

        private static async Task<string> GenerateSpawnDirective(string worldState)
        {
            string prompt = "You are the Spawn Controller of Ultima Online. You decide what creatures appear in the world.\n\n" +
                            worldState +
                            "\n\nGenerate ONE spawn directive. Choose from:\n" +
                            "1. SURGE: Increase spawns of a creature type in a region (more monsters)\n" +
                            "2. SUPPRESS: Decrease spawns of a creature type in a region (fewer monsters)\n" +
                            "3. EMPOWER: Make a creature type stronger/buffed in a region\n" +
                            "4. INVASION: A specific faction launches an invasion in a region\n" +
                            "5. RETREAT: A creature type withdraws from a region entirely\n\n" +
                            "Format: TYPE|CREATURE_TYPE|REGION|MULTIPLIER|DESCRIPTION\n" +
                            "TYPE: SURGE, SUPPRESS, EMPOWER, INVASION, RETREAT\n" +
                            "CREATURE_TYPE: orc, undead, daemon, dragon, animal, lizardman, troll, all\n" +
                            "REGION: Britain, Trinsic, Moonglow, Minoc, Yew, Jhelom, Skara Brae, Vesper, Nujel'm, dungeon, forest, mountain, coast, all\n" +
                            "MULTIPLIER: 0.0 to 3.0 (0=no spawns, 1=normal, 2=double, 3=triple)\n" +
                            "DESCRIPTION: what's happening (1 sentence, max 160 chars)\n\n" +
                            "Output ONLY the pipe-delimited line. Nothing else.";

            try
            {
                return await LLMClient.ChatAsync("", prompt, AIConfig.ModelSpawner);
            }
            catch
            {
                return null;
            }
        }

        private static void ApplySpawnDirective(string eventText)
        {
            if (string.IsNullOrEmpty(eventText)) return;

            var parts = eventText.Split('|');
            if (parts.Length < 4) return;

            var type = parts[0].Trim().ToUpperInvariant();
            var creatureType = parts[1].Trim().ToLowerInvariant();
            var region = parts[2].Trim().ToLowerInvariant();
            double multiplier;
            double.TryParse(parts[3].Trim(), out multiplier);
            if (multiplier <= 0) multiplier = 1.0;
            multiplier = Math.Max(0.0, Math.Min(3.0, multiplier));

            string description = "";
            if (parts.Length >= 5)
                description = parts[4].Trim();

            // Store directive with 2-hour duration
            var directive = new SpawnDirective
            {
                Type = type,
                CreatureType = creatureType,
                Region = region,
                Multiplier = multiplier,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                DurationHours = 2 + Utility.Random(2)
            };

            lock (_activeDirectives)
            {
                // Remove old directives of same type/creature/region
                _activeDirectives.RemoveAll(d =>
                    d.Type == type && d.CreatureType == creatureType && d.Region == region);

                _activeDirectives.Add(directive);
                if (_activeDirectives.Count > 15)
                    _activeDirectives.RemoveAt(0);
            }

            // Broadcast for significant changes
            if (multiplier >= 2.0 || multiplier <= 0.5 || type == "INVASION")
            {
                var hue = multiplier >= 1.5 ? 0x22 : 0x44; // Red for danger, blue for safety
                BroadcastToAll("[Spawns] " + description, hue);
            }

            Console.WriteLine("[AI Spawn] Directive: " + type + " " + creatureType + "@" + region + " x" + multiplier);
        }

        private static void BroadcastToAll(string message, int hue)
        {
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null)
                    ns.Mobile.SendMessage(hue, message);
            }
        }

        /// <summary>Get a text summary of active spawn directives for other systems.</summary>
        public static string GetSpawnContext()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Active spawn directives:");
            lock (_activeDirectives)
            {
                if (_activeDirectives.Count == 0)
                {
                    sb.AppendLine("- None (normal spawns)");
                }
                else
                {
                    foreach (var d in _activeDirectives)
                    {
                        sb.AppendLine("- " + d.Type + " " + d.CreatureType + " in " + d.Region +
                                       " x" + d.Multiplier.ToString("F1") + " (" + d.RemainingHours + "h remaining)");
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>Clean up expired directives.</summary>
        public static void CleanupExpired()
        {
            lock (_activeDirectives)
            {
                _activeDirectives.RemoveAll(d => d.IsExpired);
            }
        }

        private class SpawnDirective
        {
            public string Type;
            public string CreatureType;
            public string Region;
            public double Multiplier;
            public string Description;
            public DateTime CreatedAt;
            public int DurationHours;

            public bool IsExpired
            {
                get { return (DateTime.UtcNow - CreatedAt).TotalHours > DurationHours; }
            }

            public int RemainingHours
            {
                get { return Math.Max(0, DurationHours - (int)(DateTime.UtcNow - CreatedAt).TotalHours); }
            }
        }
    }
}
