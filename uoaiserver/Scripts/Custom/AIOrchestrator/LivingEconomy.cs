using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Tracks global supply and demand based on player actions.
    /// High kills of creature X → X parts become plentiful (price drops).
    /// High harvest of item Y → Y becomes common (price drops).
    /// This data is fed into NPC dialogue to generate living economy rumors.
    /// </summary>
    public static class LivingEconomy
    {
        private static readonly Dictionary<string, int> KillCounts = new Dictionary<string, int>();       // creature -> count (last 24h)
        private static readonly Dictionary<string, int> HarvestCounts = new Dictionary<string, int>();    // item -> count (last 24h)
        private static DateTime _lastReset = DateTime.UtcNow;

        private const int MaxTrackedPerType = 30;
        private const int WindowHours = 24;

        public static void Initialize()
        {
            // Reset counters every 24 hours
            Timer.DelayCall(TimeSpan.FromHours(24), TimeSpan.FromHours(24), ResetCounters);
            Console.WriteLine("[AIOrchestrator] Living economy initialized.");
        }

        public static void RecordKill(string creatureName)
        {
            if (string.IsNullOrEmpty(creatureName)) return;
            PurgeIfStale();

            lock (KillCounts)
            {
                var key = creatureName.ToLowerInvariant();
                if (!KillCounts.ContainsKey(key))
                {
                    if (KillCounts.Count >= MaxTrackedPerType)
                    {
                        // Remove least-frequent entry
                        var min = KillCounts.OrderBy(k => k.Value).First();
                        KillCounts.Remove(min.Key);
                    }
                    KillCounts[key] = 0;
                }
                KillCounts[key]++;
            }
        }

        public static void RecordHarvest(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return;
            PurgeIfStale();

            lock (HarvestCounts)
            {
                var key = itemName.ToLowerInvariant();
                if (!HarvestCounts.ContainsKey(key))
                {
                    if (HarvestCounts.Count >= MaxTrackedPerType)
                    {
                        var min = HarvestCounts.OrderBy(k => k.Value).First();
                        HarvestCounts.Remove(min.Key);
                    }
                    HarvestCounts[key] = 0;
                }
                HarvestCounts[key]++;
            }
        }

        /// <summary>Get a formatted string describing the current economic state for NPC dialogue.</summary>
        public static string GetEconomyContext()
        {
            PurgeIfStale();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Current economic conditions:");

            lock (KillCounts)
            {
                var overpopulated = KillCounts.Where(k => k.Value > 50).OrderByDescending(k => k.Value).Take(3);
                if (overpopulated.Any())
                {
                    sb.AppendLine("- Overpopulated pests: " + string.Join(", ", overpopulated.Select(k => $"{k.Key} ({k.Value} killed recently)")));
                }

                var scarce = KillCounts.Where(k => k.Value < 5 && k.Value > 0).OrderBy(k => k.Value).Take(3);
                if (scarce.Any())
                {
                    sb.AppendLine("- Becoming scarce: " + string.Join(", ", scarce.Select(k => k.Key)));
                }
            }

            lock (HarvestCounts)
            {
                var plentiful = HarvestCounts.Where(k => k.Value > 30).OrderByDescending(k => k.Value).Take(3);
                if (plentiful.Any())
                {
                    sb.AppendLine("- Plentiful resources: " + string.Join(", ", plentiful.Select(k => $"{k.Key} ({k.Value} harvested)")));
                }
            }

            return sb.ToString();
        }

        /// <summary>Get a specific economic rumor for ambient NPC chat.</summary>
        public static string GetRandomRumor()
        {
            PurgeIfStale();
            var rumors = new List<string>();

            lock (KillCounts)
            {
                var top = KillCounts.OrderByDescending(k => k.Value).FirstOrDefault();
                if (top.Key != null && top.Value > 20)
                    rumors.Add($"I've heard {top.Key}s have been getting out of hand. Someone should thin the herd.");
            }

            lock (HarvestCounts)
            {
                var top = HarvestCounts.OrderByDescending(k => k.Value).FirstOrDefault();
                if (top.Key != null && top.Value > 20)
                    rumors.Add($"The market's flooded with {top.Key}. Prices are dropping.");
            }

            if (rumors.Count == 0)
                rumors.Add("Trade's been quiet lately. Nothing much happening.");

            return rumors[Utility.Random(rumors.Count)];
        }

        private static void PurgeIfStale()
        {
            if ((DateTime.UtcNow - _lastReset).TotalHours >= WindowHours)
            {
                ResetCounters();
            }
        }

        private static void ResetCounters()
        {
            lock (KillCounts) { KillCounts.Clear(); }
            lock (HarvestCounts) { HarvestCounts.Clear(); }
            _lastReset = DateTime.UtcNow;
            Console.WriteLine("[AIOrchestrator] Economy counters reset for new 24h window.");
        }

        // ── Price Influence ───────────────────────────────────────────

        /// <summary>Maps creature names → item keywords they affect.</summary>
        private static readonly Dictionary<string, string[]> CommodityMap = new Dictionary<string, string[]>
        {
            { "orc",         new[] { "orc", "skull", "hide" } },
            { "orc lord",    new[] { "orc", "skull", "hide" } },
            { "lizardman",   new[] { "lizard", "scale", "leather" } },
            { "troll",       new[] { "troll", "bone", "meat" } },
            { "skeleton",    new[] { "bone", "skull", "ribcage" } },
            { "zombie",      new[] { "bone", "rot", "corpse" } },
            { "dragon",      new[] { "dragon", "scale", "blood" } },
            { "daemon",      new[] { "daemon", "bone", "blood" } },
            { "spider",      new[] { "spider", "silk", "venom" } },
            { "snake",       new[] { "snake", "venom", "scale" } },
            { "mongbat",     new[] { "mongbat", "wing", "hide" } },
            { "wolf",        new[] { "wolf", "fur", "meat" } },
            { "bear",        new[] { "bear", "fur", "meat" } },
            { "golem",       new[] { "golem", "ingot", "ore" } },
            { "elemental",   new[] { "elemental", "ore", "ingot" } },
        };

        /// <summary>Active scarcity events: commodity → expiry time.</summary>
        private static readonly Dictionary<string, DateTime> _scarcityEvents = new Dictionary<string, DateTime>();

        /// <summary>Register a temporary scarcity event — prices of matching commodities spike.</summary>
        public static void ApplyScarcityEvent(string commodity, int durationMinutes)
        {
            if (string.IsNullOrEmpty(commodity)) return;
            lock (_scarcityEvents)
            {
                _scarcityEvents[commodity.ToLowerInvariant()] = DateTime.UtcNow.AddMinutes(durationMinutes);
            }
        }

        /// <summary>
        /// Returns a price multiplier (0.50–2.50) for a given item name.
        /// Vendors should call this when setting their prices.
        /// &lt; 1.0 = plentiful (surplus), &gt; 1.0 = scarce (high demand).
        /// </summary>
        public static double GetPriceModifier(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return 1.0;
            PurgeIfStale();

            string lower = itemName.ToLowerInvariant();

            // Check active scarcity events first (highest priority)
            lock (_scarcityEvents)
            {
                var now = DateTime.UtcNow;
                var expired = new List<string>();
                double scarcityMod = 1.0;

                foreach (var kvp in _scarcityEvents)
                {
                    if (now > kvp.Value)
                    {
                        expired.Add(kvp.Key);
                        continue;
                    }
                    // If the item name contains the scarcity commodity keyword
                    if (lower.Contains(kvp.Key))
                    {
                        scarcityMod = Math.Max(scarcityMod, 2.0);
                    }
                }

                foreach (var key in expired)
                    _scarcityEvents.Remove(key);

                if (scarcityMod > 1.0)
                    return scarcityMod;
            }

            // Check kill/harvest counts for supply-demand pricing
            string matchedCreature = null;
            foreach (var kvp in CommodityMap)
            {
                foreach (var keyword in kvp.Value)
                {
                    if (lower.Contains(keyword))
                    {
                        matchedCreature = kvp.Key;
                        break;
                    }
                }
                if (matchedCreature != null) break;
            }

            if (matchedCreature == null)
            {
                // Check harvest counts directly
                lock (HarvestCounts)
                {
                    foreach (var kvp in HarvestCounts)
                    {
                        if (lower.Contains(kvp.Key))
                        {
                            if (kvp.Value > 30) return 0.65;  // flooded
                            if (kvp.Value > 15) return 0.80;  // common
                            if (kvp.Value < 3) return 1.50;   // scarce
                            break;
                        }
                    }
                }
                return 1.0;
            }

            lock (KillCounts)
            {
                if (KillCounts.TryGetValue(matchedCreature, out int kills))
                {
                    if (kills > 50) return 0.50;   // overpopulated → cheap
                    if (kills > 25) return 0.70;   // plentiful → discounted
                    if (kills > 10) return 0.85;   // common → slight discount
                    if (kills > 0 && kills < 3) return 1.50;  // scarce → premium
                    if (kills > 0 && kills < 6) return 1.25;  // uncommon → slight premium
                }
            }

            return 1.0;
        }

        /// <summary>Get a short economy-shock context string for NPC dialogue during scarcity events.</summary>
        public static string GetEconomyShockContext()
        {
            lock (_scarcityEvents)
            {
                var now = DateTime.UtcNow;
                var active = _scarcityEvents.Where(kvp => kvp.Value > now).Select(kvp => kvp.Key).ToList();
                if (active.Count == 0) return "";

                return "Economic shock: " + string.Join(", ", active) + " prices are spiking!";
            }
        }
    }

    /// <summary>
    /// Hooks to feed data into the living economy.
    /// </summary>
    public static class EconomyDataHook
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
            // Harvest tracking could be expanded with ore/wood/lumber hooks
            Console.WriteLine("[AIOrchestrator] Economy data hooks registered.");
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Creature != null)
            {
                LivingEconomy.RecordKill(e.Creature.Name ?? e.Creature.GetType().Name);
            }
        }
    }
}
