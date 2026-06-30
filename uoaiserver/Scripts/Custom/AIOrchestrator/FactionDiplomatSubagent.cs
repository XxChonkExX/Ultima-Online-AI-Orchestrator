using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator.Factions;

namespace Server.AIOrchestrator.Subagents
{
    /// <summary>
    /// AI-driven Faction Diplomat Subagent.
    /// Uses an LLM model to manage faction relationships dynamically:
    /// proposes alliances, vendettas, bounties, and faction-specific events.
    /// Runs on a timer and generates faction narrative events.
    /// </summary>
    public static class FactionDiplomatSubagent
    {
        private static Timer _timer;
        private static DateTime _lastEvent = DateTime.MinValue;
        private const int FactionIntervalMinutes = 25;

        // Track faction relationship state
        private static Dictionary<string, int> _factionTensions = new Dictionary<string, int>(); // 0=peace, 100=war
        private static List<FactionEvent> _activeFactionEvents = new List<FactionEvent>();

        public static void Initialize()
        {
            _timer = Timer.DelayCall(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(FactionIntervalMinutes), FactionTick);
            Console.WriteLine("[AIOrchestrator] AI Faction Diplomat initialized (every " + FactionIntervalMinutes + " min).");
        }

        /// <summary>Increase tension between two factions (called when a player kills a faction member).</summary>
        public static void IncreaseTension(string victimFactionId, string killerFactionId, int amount)
        {
            if (string.IsNullOrEmpty(victimFactionId) || string.IsNullOrEmpty(killerFactionId))
                return;

            var key = GetTensionKey(victimFactionId, killerFactionId);
            lock (_factionTensions)
            {
                int current;
                if (!_factionTensions.TryGetValue(key, out current))
                    current = 0;
                _factionTensions[key] = Math.Min(100, current + amount);
            }
        }

        /// <summary>Get faction relationship context for NPC dialogue.</summary>
        public static string GetFactionDiplomacyContext()
        {
            lock (_activeFactionEvents)
            {
                if (_activeFactionEvents.Count == 0)
                    return "";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Faction Diplomacy Report:");
                foreach (var evt in _activeFactionEvents)
                {
                    sb.AppendLine("- " + evt.Description);
                }
                return sb.ToString();
            }
        }

        private static string GetTensionKey(string fac1, string fac2)
        {
            // Sort to ensure consistent keys regardless of order
            string a, b;
            if (string.Compare(fac1, fac2, StringComparison.OrdinalIgnoreCase) < 0)
            {
                a = fac1; b = fac2;
            }
            else
            {
                a = fac2; b = fac1;
            }
            return a + "|" + b;
        }

        private static void FactionTick()
        {
            try
            {
                if (!AIConfig.Enabled) return;

                var worldState = BuildFactionState();
                if (string.IsNullOrEmpty(worldState)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await GenerateFactionEvent(worldState);
                        if (!string.IsNullOrEmpty(result))
                        {
                            ApplyFactionEvent(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[AI Faction] Generation error: " + ex.Message);
                    }
                });

                _lastEvent = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AI Faction] Tick error: " + ex.Message);
            }
        }

        private static string BuildFactionState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Current faction state of Britannia:");

            // List all known factions
            sb.AppendLine("Known factions:");
            foreach (var faction in NpcFaction.Presets.All)
            {
                sb.AppendLine("- " + faction.Id + ": " + faction.Name + " [" + faction.Virtue + "/" + faction.Vice + "]");
                if (faction.AlliedFactionIds.Length > 0)
                    sb.AppendLine("  Allies: " + string.Join(", ", faction.AlliedFactionIds));
                if (faction.EnemyFactionIds.Length > 0)
                    sb.AppendLine("  Enemies: " + string.Join(", ", faction.EnemyFactionIds));
            }

            // Tensions
            lock (_factionTensions)
            {
                var hot = _factionTensions.Where(t => t.Value >= 40).OrderByDescending(t => t.Value).Take(5).ToList();
                if (hot.Count > 0)
                {
                    sb.AppendLine("Rising tensions:");
                    foreach (var t in hot)
                        sb.AppendLine("- " + t.Key + ": tension " + t.Value + "/100");
                }
            }

            // Active events
            lock (_activeFactionEvents)
            {
                if (_activeFactionEvents.Count > 0)
                {
                    sb.AppendLine("Active faction events:");
                    foreach (var evt in _activeFactionEvents)
                        sb.AppendLine("- " + evt.Description);
                }
            }

            return sb.ToString();
        }

        private static async Task<string> GenerateFactionEvent(string worldState)
        {
            string prompt = "You are the Faction Diplomat of Ultima Online. You manage relationships between factions.\n\n" +
                            worldState +
                            "\n\nGenerate ONE faction event. Choose from:\n" +
                            "1. DECLARE_WAR: Two factions go to war (tension > 60)\n" +
                            "2. FORM_ALLIANCE: Two factions become allies (tension < 20)\n" +
                            "3. BOUNTY: A faction places a bounty on a player or creature type\n" +
                            "4. TRUCE: Two warring factions call a temporary truce\n" +
                            "5. SUMMON: A faction calls for heroes to aid them\n" +
                            "6. BETRAYAL: A faction member betrays their group\n\n" +
                            "Format: TYPE|TARGET_FACTION|OTHER_FACTION|DESCRIPTION\n" +
                            "TYPE: one of WAR, ALLIANCE, BOUNTY, TRUCE, SUMMON, BETRAYAL\n" +
                            "TARGET_FACTION: the primary faction ID\n" +
                            "OTHER_FACTION: the secondary faction ID (or 'none')\n" +
                            "DESCRIPTION: what happens (1 sentence, max 180 chars)\n\n" +
                            "Output ONLY the pipe-delimited line. Nothing else.";

            try
            {
                return await LLMClient.ChatAsync("", prompt, AIConfig.ModelFaction);
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyFactionEvent(string eventText)
        {
            if (string.IsNullOrEmpty(eventText)) return;

            var parts = eventText.Split('|');
            if (parts.Length < 4) return;

            var type = parts[0].Trim().ToUpperInvariant();
            var targetFaction = parts[1].Trim().ToLowerInvariant();
            var otherFaction = parts[2].Trim().ToLowerInvariant();
            var description = parts[3].Trim();

            // Validate factions exist
            NpcFaction targetFac = null;
            NpcFaction otherFac = null;
            NpcFaction.Presets.ById.TryGetValue(targetFaction, out targetFac);
            if (otherFaction != "none")
                NpcFaction.Presets.ById.TryGetValue(otherFaction, out otherFac);

            var evt = new FactionEvent
            {
                Type = type,
                TargetFaction = targetFaction,
                OtherFaction = otherFaction,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                DurationHours = 2 + Utility.Random(4)
            };

            lock (_activeFactionEvents)
            {
                _activeFactionEvents.Add(evt);
                if (_activeFactionEvents.Count > 10)
                    _activeFactionEvents.RemoveAt(0);
            }

            // Broadcast
            var hue = 0x44;
            if (type == "WAR" || type == "BETRAYAL")
                hue = 0x22; // Red for conflict
            else if (type == "ALLIANCE" || type == "TRUCE")
                hue = 0x48; // Green for peace

            BroadcastToAll("[Factions] " + description, hue);
            Console.WriteLine("[AI Faction] Event: " + type + " | " + description);
        }

        private static void BroadcastToAll(string message, int hue)
        {
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null)
                    ns.Mobile.SendMessage(hue, message);
            }
        }

        private class FactionEvent
        {
            public string Type;
            public string TargetFaction;
            public string OtherFaction;
            public string Description;
            public DateTime CreatedAt;
            public int DurationHours;

            public bool IsExpired
            {
                get { return (DateTime.UtcNow - CreatedAt).TotalHours > DurationHours; }
            }
        }
    }
}
