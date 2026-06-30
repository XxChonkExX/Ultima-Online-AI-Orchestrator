using System;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator.Subagents
{
    public class EnvironmentSubagent : IAIOrchestrator
    {
        public SubagentType ActiveSubagent => SubagentType.Environment;
        private readonly BaseCreature _creature;
        private readonly AIMemory _memory;

        // Global one-shot timers so 7800 NPCs don't all fire events independently
        private static DateTime _nextWeatherBroadcast = DateTime.MinValue;
        private static DateTime _nextEventBroadcast = DateTime.MinValue;
        private static readonly TimeSpan WeatherInterval = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan EventInterval = TimeSpan.FromMinutes(10);
        private static readonly object _lock = new object();
        private static bool _firstRun = true;

        // ── AIGameMaster weather bias ─────────────────────────────────
        private static string _weatherBias = "neutral";

        /// <summary>Called by AIGameMaster to influence weather generation per narrative phase.</summary>
        public static void SetWeatherBias(string bias)
        {
            _weatherBias = bias ?? "neutral";
        }

        public EnvironmentSubagent(BaseCreature creature, AIMemory memory)
        {
            _creature = creature;
            _memory = memory;
        }

        public void OnHeartbeat(Mobile player)
        {
            if (!_creature.Alive || player == null) return;

            lock (_lock)
            {
                var now = DateTime.UtcNow;

                if (_firstRun)
                {
                    _firstRun = false;
                    _nextWeatherBroadcast = now;
                    _nextEventBroadcast = now;
                    BroadcastWeather(player);
                    BroadcastRegionalEvent(player);
                    return;
                }

                if (now - _nextWeatherBroadcast >= WeatherInterval)
                {
                    _nextWeatherBroadcast = now;
                    BroadcastWeather(player);
                }

                if (now - _nextEventBroadcast >= EventInterval)
                {
                    _nextEventBroadcast = now;
                    BroadcastRegionalEvent(player);
                }
            }
        }

        /// <summary>
        /// AI-driven weather broadcast. The LLM decides weather type AND description.
        /// </summary>
        private void BroadcastWeather(Mobile player)
        {
            if (player == null || player.Deleted) return;

            var region = Region.Find(player.Location, player.Map);
            var regionName = region?.Name ?? "the realm";

            // Gather context from other subagents for richer weather generation
            var economyContext = LivingEconomy.GetEconomyContext();
            var factionContext = FactionDiplomatSubagent.GetFactionDiplomacyContext();

            var prompt = "You are the Weather Spirit of Britannia. Generate a weather event.\n\n" +
                         "Region: " + regionName + "\n" +
                         "Time: " + (DateTime.UtcNow.Hour < 6 || DateTime.UtcNow.Hour >= 20 ? "Night" : "Day") + "\n" +
                         "Economy: " + economyContext + "\n" +
                         "Factions: " + factionContext + "\n" +
                         "Narrative bias: " + _weatherBias + "\n\n" +
                         "Choose ONE weather condition and describe it atmospherically (1 sentence, max 160 chars).\n" +
                         "Options: clear skies, light rain, heavy storm, thick fog, howling wind, snow, heat wave, magical storm, rainbow, meteor shower, eclipse, blood moon, starfall\n\n" +
                         "If narrative bias is 'clear', favor clear weather. If 'ominous', favor storms/fog. If 'violent', favor storms/blood moon. If 'neutral', any weather.\n\n" +
                         "Format: WEATHER|DESCRIPTION\n" +
                         "Output ONLY the pipe-delimited line. Nothing else.";

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var reply = await LLMClient.ChatAsync(
                        "You describe weather changes briefly.",
                        prompt,
                        AIConfig.ModelEnvironment
                    );

                    if (!string.IsNullOrEmpty(reply))
                    {
                        var parts = reply.Split('|');
                        var description = parts.Length >= 2 ? parts[1].Trim() : reply.Trim();

                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            foreach (var ns in NetState.Instances)
                            {
                                if (ns.Mobile != null && ns.Mobile.Map == player.Map && ns.Mobile.InRange(player, 60))
                                {
                                    ns.Mobile.SendMessage(0x482, "[Weather] " + description);
                                }
                            }
                        });
                    }
                }
                catch { }
            });
        }

        /// <summary>
        /// AI-driven regional event broadcast. The LLM decides event type AND description.
        /// </summary>
        private void BroadcastRegionalEvent(Mobile player)
        {
            if (player == null || player.Deleted) return;

            var region = Region.Find(player.Location, player.Map);
            var regionName = region?.Name ?? "the realm";

            // Gather context
            var economyContext = EconomySubagent.GetEconomyContext();
            var factionContext = FactionDiplomatSubagent.GetFactionDiplomacyContext();

            var prompt = "You are the World Herald of Ultima Online. Generate a regional event.\n\n" +
                         "Region: " + regionName + "\n" +
                         "Economy: " + economyContext + "\n" +
                         "Factions: " + factionContext + "\n\n" +
                         "Choose ONE event type and describe it (1 sentence, max 180 chars).\n" +
                         "Options: monster_invasion, town_festival, merchant_caravan, dungeon_awakening, " +
                         "spell_plague, noble_visit, guild_rivalry, wandering_hermit, ancient_discovery, " +
                         "refugee_crisis, tournament, execution, prophecy, none\n\n" +
                         "If 'none', just output 'none'.\n" +
                         "Otherwise format: TYPE|DESCRIPTION\n" +
                         "Output ONLY the line. Nothing else.";

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var reply = await LLMClient.ChatAsync(
                        "You are a town crier announcing events briefly.",
                        prompt,
                        AIConfig.ModelEnvironment
                    );

                    if (!string.IsNullOrEmpty(reply) && reply.Trim().ToLowerInvariant() != "none")
                    {
                        var parts = reply.Split('|');
                        var description = parts.Length >= 2 ? parts[1].Trim() : reply.Trim();

                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            foreach (var ns in NetState.Instances)
                            {
                                if (ns.Mobile != null)
                                {
                                    ns.Mobile.SendMessage(0x47E, "[World Event] " + description);
                                }
                            }
                        });
                    }
                }
                catch { }
            });
        }
    }
}
