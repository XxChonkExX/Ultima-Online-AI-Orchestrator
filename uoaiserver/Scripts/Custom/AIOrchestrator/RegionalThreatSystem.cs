using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Tracks monster kill rates per region and escalates threats.
    /// Too many kills in a region → stronger monsters spawn, raiding parties form.
    /// Too few kills (player neglect) → monsters multiply, infest nearby areas.
    /// </summary>
    public static class RegionalThreatSystem
    {
        private class RegionThreat
        {
            public string RegionName;
            public int KillCount24h;
            public int ThreatLevel;     // 0-100
            public DateTime LastEscalation;
        }

        private static readonly Dictionary<string, RegionThreat> Regions = new Dictionary<string, RegionThreat>();
        private static Timer _threatTimer;
        private static readonly Random _rng = new Random();

        private const int ThreatThresholdLow = 30;    // Too many kills → escalation
        private const int ThreatThresholdHigh = 70;   // Critical → major event
        private const int DecayPerCycle = 5;          // Threat decays when no kills happen

        public static void Initialize()
        {
            _threatTimer = Timer.DelayCall(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), ThreatTick);
            Console.WriteLine("[AIOrchestrator] Regional threat system initialized.");
        }

        public static void RecordKill(string regionName)
        {
            if (string.IsNullOrEmpty(regionName)) return;

            lock (Regions)
            {
                if (!Regions.TryGetValue(regionName, out var threat))
                {
                    threat = new RegionThreat { RegionName = regionName };
                    Regions[regionName] = threat;
                }

                threat.KillCount24h++;
                // Escalate threat faster when many kills happen
                threat.ThreatLevel = Math.Min(100, threat.ThreatLevel + 2);
            }
        }

        private static void ThreatTick()
        {
            try
            {
                lock (Regions)
                {
                    foreach (var kvp in Regions)
                    {
                        var threat = kvp.Value;

                        // Decay threat if no kills happened recently
                        if (threat.KillCount24h == 0)
                        {
                            threat.ThreatLevel = Math.Max(0, threat.ThreatLevel - DecayPerCycle);
                            continue;
                        }

                        // Check thresholds
                        if (threat.ThreatLevel >= ThreatThresholdHigh && (DateTime.UtcNow - threat.LastEscalation).TotalMinutes > 15)
                        {
                            TriggerMajorEvent(threat);
                            threat.LastEscalation = DateTime.UtcNow;
                            threat.ThreatLevel = 30; // Reset after event
                        }
                        else if (threat.ThreatLevel >= ThreatThresholdLow && (DateTime.UtcNow - threat.LastEscalation).TotalMinutes > 10)
                        {
                            TriggerMinorEvent(threat);
                            threat.LastEscalation = DateTime.UtcNow;
                            threat.ThreatLevel = Math.Max(10, threat.ThreatLevel - 20);
                        }

                        // Reset 24h counter periodically
                        threat.KillCount24h = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AIOrchestrator] Threat tick error: " + ex.Message);
            }
        }

        private static void TriggerMinorEvent(RegionThreat threat)
        {
            var messages = new[]
            {
                $"[World] Strange creatures have been spotted near {threat.RegionName}...",
                $"[World] Travelers report increased monster activity around {threat.RegionName}.",
                $"[World] The {threat.RegionName} guards are on high alert. Something is stirring."
            };

            BroadcastToAll(messages[_rng.Next(messages.Length)], 0x44);
            Console.WriteLine($"[AI THREAT] Minor event in {threat.RegionName} (level={threat.ThreatLevel})");

            // Spawn 2-3 variant creatures in the threatened region
            SpawnThreatEncounter(threat.RegionName, minSpawns: 2, maxSpawns: 3, useElites: false);
        }

        private static void TriggerMajorEvent(RegionThreat threat)
        {
            var messages = new[]
            {
                $"[World] DARK OMEN: A horde of monsters marches toward {threat.RegionName}!",
                $"[World] RAID: {threat.RegionName} is under attack! Defenders needed!",
                $"[World] CATACLYSM: The veil weakens near {threat.RegionName}. Dark forces gather!"
            };

            BroadcastToAll(messages[_rng.Next(messages.Length)], 0x22);
            Console.WriteLine($"[AI THREAT] MAJOR event in {threat.RegionName} (level={threat.ThreatLevel})");

            // Spawn 1 elite boss + 3-5 variant creatures
            SpawnThreatEncounter(threat.RegionName, minSpawns: 3, maxSpawns: 5, useElites: true);
        }

        /// <summary>
        /// Spawn creature variants in the threatened region.
        /// Connected to CreatureVariants.cs for variant types.
        /// </summary>
        private static void SpawnThreatEncounter(string regionName, int minSpawns, int maxSpawns, bool useElites)
        {
            try
            {
                var region = FindRegionByName(regionName);
                if (region == null) return;

                // Pick a theme based on the region or random
                var theme = GetThreatTheme(regionName);
                int count = _rng.Next(minSpawns, maxSpawns + 1);

                for (int i = 0; i < count; i++)
                {
                    var creature = CreateThreatCreature(theme, useElites && i == 0);
                    if (creature == null) continue;

                    // Find spawn point within region
                    var loc = FindSpawnPoint(region);
                    if (loc == Point3D.Zero)
                    {
                        creature.Delete();
                        continue;
                    }

                    creature.MoveToWorld(loc, region.Map);
                    Console.WriteLine($"[AI THREAT] Spawned {creature.GetType().Name} in {regionName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI THREAT] Spawn error: {ex.Message}");
            }
        }

        /// <summary>Find a region by name across all maps.</summary>
        private static Region FindRegionByName(string name)
        {
            foreach (var map in Map.AllMaps)
            {
                if (map == null || map == Map.Internal) continue;
                // Search all regions
                foreach (var region in map.Regions.Values)
                {
                    if (region.Name != null &&
                        region.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return region;
                }
            }
            return null;
        }

        /// <summary>Determine the threat theme based on region name.</summary>
        private static string GetThreatTheme(string regionName)
        {
            var name = regionName.ToLowerInvariant();
            if (name.Contains("orc") || name.Contains("cave") || name.Contains("mountain"))
                return "orcs";
            if (name.Contains("undead") || name.Contains("crypt") || name.Contains("grave") ||
                name.Contains("tomb") || name.Contains("catacomb"))
                return "undead";
            if (name.Contains("lizard") || name.Contains("swamp") || name.Contains("jungle"))
                return "lizardman";
            if (name.Contains("troll") || name.Contains("hill"))
                return "troll";
            if (name.Contains("dragon") || name.Contains("volcano") || name.Contains("lava"))
                return "dragon";
            if (name.Contains("daemon") || name.Contains("hell") || name.Contains("abyss"))
                return "daemon";
            if (name.Contains("forest") || name.Contains("wood"))
                return "woodland";
            // Fallback: random
            var themes = new[] { "orcs", "undead", "lizardman", "troll", "daemon" };
            return themes[_rng.Next(themes.Length)];
        }

        /// <summary>Create a threat creature based on theme and whether it's an elite boss.</summary>
        private static BaseCreature CreateThreatCreature(string theme, bool isElite)
        {
            // Elite bosses
            if (isElite)
            {
                switch (theme)
                {
                    case "orcs": return new OrcWarlord();
                    case "undead": return new SkeletalLich();
                    case "lizardman": return new LizardmanHighPriest();
                    case "troll": return new TrollChieftain();
                    case "daemon": return new LizardmanHighPriest(); // daemon boss placeholder
                    case "dragon": return new GreaterTroll(); // dragon placeholder
                    default: return new OrcWarlord();
                }
            }

            // Regular variants
            switch (theme)
            {
                case "orcs":
                    switch (_rng.Next(6))
                    {
                        case 0: return new OrcShaman();
                        case 1: return new OrcArcher();
                        case 2: return new OrcKnight();
                        case 3: return new OrcBeastmaster();
                        case 4: return new GreaterOrc();
                        default: return new Orc();
                    }
                case "undead":
                    switch (_rng.Next(6))
                    {
                        case 0: return new SkeletalMage();
                        case 1: return new SkeletalArcher();
                        case 2: return new GreaterSkeleton();
                        case 3: return new SkeletalLich();
                        case 4: return new Wraith();
                        default: return new Skeleton();
                    }
                case "lizardman":
                    switch (_rng.Next(5))
                    {
                        case 0: return new LizardmanShaman();
                        case 1: return new LizardmanSniper();
                        case 2: return new Lizardman();
                        case 3: return new Lizardman();
                        default: return new LizardmanShaman();
                    }
                case "troll":
                    switch (_rng.Next(5))
                    {
                        case 0: return new TrollWitchdoctor();
                        case 1: return new GreaterTroll();
                        case 2: return new Troll();
                        case 3: return new Troll();
                        default: return new TrollWitchdoctor();
                    }
                default:
                    return new Orc();
            }
        }

        /// <summary>Find a suitable spawn point within a region.</summary>
        private static Point3D FindSpawnPoint(Region region)
        {
            // Use region's Area rectangles to find bounds
            if (region.Area == null || region.Area.Length == 0) return Point3D.Zero;

            // Pick a random area rectangle
            var rect = region.Area[_rng.Next(region.Area.Length)];
            var start = rect.Start;
            var end = rect.End;

            for (int i = 0; i < 20; i++)
            {
                int x = start.X + _rng.Next(Math.Max(1, end.X - start.X));
                int y = start.Y + _rng.Next(Math.Max(1, end.Y - start.Y));
                int z = region.Map.GetAverageZ(x, y);
                var loc = new Point3D(x, y, z);
                if (region.Map.CanSpawnMobile(loc))
                    return loc;
            }
            return Point3D.Zero;
        }

        /// <summary>
        /// Get the elite spawn chance modifier for CreatureVariantSpawner
        /// based on threat level in a region. Higher threat = more elites.
        /// </summary>
        public static double GetEliteSpawnChanceModifier(string regionName)
        {
            if (string.IsNullOrEmpty(regionName)) return 1.0;

            lock (Regions)
            {
                if (Regions.TryGetValue(regionName, out var threat))
                {
                    // At threat 50+: 2x elite chance, at 80+: 3x
                    if (threat.ThreatLevel >= 80) return 3.0;
                    if (threat.ThreatLevel >= 50) return 2.0;
                    if (threat.ThreatLevel >= 30) return 1.5;
                }
            }
            return 1.0;
        }

        private static void BroadcastToAll(string message, int hue)
        {
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null)
                {
                    ns.Mobile.SendMessage(hue, message);
                }
            }
        }

        /// <summary>Get threat context for environment subagent dialogue.</summary>
        public static string GetThreatContext()
        {
            lock (Regions)
            {
                var hot = Regions.Where(r => r.Value.ThreatLevel >= 20)
                    .OrderByDescending(r => r.Value.ThreatLevel)
                    .Take(3)
                    .ToList();

                if (hot.Count == 0) return "";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Regional threat report:");
                foreach (var h in hot)
                {
                    var severity = h.Value.ThreatLevel >= ThreatThresholdHigh ? "CRITICAL" :
                                   h.Value.ThreatLevel >= ThreatThresholdLow ? "ELEVATED" : "MONITORING";
                    sb.AppendLine($"- {h.Key}: {severity} (level {h.Value.ThreatLevel})");
                }
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Hooks creature deaths to feed the threat system.
    /// </summary>
    public static class ThreatDataHook
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Creature?.Region != null)
            {
                RegionalThreatSystem.RecordKill(e.Creature.Region.Name ?? "the wilds");
            }
        }
    }
}
