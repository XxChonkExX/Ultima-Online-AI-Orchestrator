using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Items;
using Server.Regions;

namespace Server.AIOrchestrator.Subagents
{
    /// <summary>
    /// AI-driven Dungeon Master Subagent.
    /// Monsters player dungeon activity and generates dynamic encounters,
    /// trap placements, treasure adjustments, and boss tactics via LLM.
    /// Runs on a timer and activates when players enter dungeon regions.
    /// </summary>
    public static class DungeonMasterSubagent
    {
        private static Timer _timer;
        private static DateTime _lastEvent = DateTime.MinValue;
        private const int DungeonIntervalMinutes = 15;

        // Track which dungeons players are in and what's happening
        private static Dictionary<string, DungeonState> _dungeonStates = new Dictionary<string, DungeonState>();

        public static void Initialize()
        {
            _timer = Timer.DelayCall(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(DungeonIntervalMinutes), DungeonTick);
            Console.WriteLine("[AIOrchestrator] AI Dungeon Master initialized (every " + DungeonIntervalMinutes + " min).");
        }

        /// <summary>Called when a player enters a dungeon region.</summary>
        public static void OnPlayerEnterDungeon(Mobile player, Region region)
        {
            if (player == null || region == null) return;

            var dungeonId = region.Name ?? "unknown_dungeon";
            lock (_dungeonStates)
            {
                DungeonState state;
                if (!_dungeonStates.TryGetValue(dungeonId, out state))
                {
                    state = new DungeonState { DungeonName = region.Name };
                    _dungeonStates[dungeonId] = state;
                }
                state.LastPlayerActivity = DateTime.UtcNow;
                if (!state.ActivePlayers.Contains(player.Serial.Value.ToString()))
                    state.ActivePlayers.Add(player.Serial.Value.ToString());
            }
        }

        /// <summary>Called when a player leaves a dungeon region.</summary>
        public static void OnPlayerLeaveDungeon(Mobile player)
        {
            if (player == null) return;
            var playerSer = player.Serial.Value.ToString();

            lock (_dungeonStates)
            {
                foreach (var state in _dungeonStates.Values)
                {
                    state.ActivePlayers.Remove(playerSer);
                }
            }
        }

        /// <summary>Get dungeon flavor text for the current dungeon.</summary>
        public static string GetDungeonFlavor(string dungeonName)
        {
            lock (_dungeonStates)
            {
                DungeonState state;
                if (_dungeonStates.TryGetValue(dungeonName, out state) && !string.IsNullOrEmpty(state.Flavor))
                    return state.Flavor;
            }
            return "";
        }

        /// <summary>Get a difficulty multiplier for loot in a dungeon.</summary>
        public static double GetLootMultiplier(string dungeonName)
        {
            lock (_dungeonStates)
            {
                DungeonState state;
                if (_dungeonStates.TryGetValue(dungeonName, out state))
                    return state.LootMultiplier;
            }
            return 1.0;
        }

        private static void DungeonTick()
        {
            try
            {
                if (!AIConfig.Enabled) return;

                // Find active dungeons
                string activeDungeons = BuildActiveDungeonState();
                if (string.IsNullOrEmpty(activeDungeons)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        var result = await GenerateDungeonEvent(activeDungeons);
                        if (!string.IsNullOrEmpty(result))
                        {
                            ApplyDungeonEvent(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[AI Dungeon] Generation error: " + ex.Message);
                    }
                });

                _lastEvent = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AI Dungeon] Tick error: " + ex.Message);
            }
        }

        private static string BuildActiveDungeonState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Active dungeon status:");

            lock (_dungeonStates)
            {
                var active = _dungeonStates.Values
                    .Where(d => d.ActivePlayers.Count > 0 ||
                          (DateTime.UtcNow - d.LastPlayerActivity).TotalMinutes < 30)
                    .ToList();

                if (active.Count == 0) return "";

                foreach (var d in active)
                {
                    sb.AppendLine("- " + d.DungeonName + ": " + d.ActivePlayers.Count + " player(s)");
                    sb.AppendLine("  Difficulty: " + d.DifficultyLevel + "/10");
                    sb.AppendLine("  Loot multiplier: " + d.LootMultiplier.ToString("F1"));
                }
            }

            return sb.ToString();
        }

        private static async Task<string> GenerateDungeonEvent(string dungeonState)
        {
            string prompt = "You are the Dungeon Master of Ultima Online. You control the dangers and treasures of dungeons.\n\n" +
                            dungeonState +
                            "\n\nGenerate ONE dungeon event. Choose from:\n" +
                            "1. ENCOUNTER: A unique monster encounter with special abilities\n" +
                            "2. TRAP: A trap is triggered in the dungeon\n" +
                            "3. TREASURE: Hidden treasure is discovered in a room\n" +
                            "4. BOSS_TACTIC: A boss changes its behavior\n" +
                            "5. AMBIENCE: The dungeon's atmosphere changes\n" +
                            "6. HAZARD: An environmental hazard appears (lava, poison gas, etc.)\n\n" +
                            "Format: TYPE|DUNGEON|DESCRIPTION|EFFECT\n" +
                            "TYPE: ENCOUNTER, TRAP, TREASURE, BOSS_TACTIC, AMBIENCE, HAZARD\n" +
                            "DUNGEON: the dungeon name\n" +
                            "DESCRIPTION: what the player experiences (1 sentence, max 180 chars)\n" +
                            "EFFECT: difficulty change (+1, -1, or 0 for loot multiplier)\n\n" +
                            "Output ONLY the pipe-delimited line. Nothing else.";

            try
            {
                return await LLMClient.ChatAsync("", prompt, AIConfig.ModelDungeon);
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyDungeonEvent(string eventText)
        {
            if (string.IsNullOrEmpty(eventText)) return;

            var parts = eventText.Split('|');
            if (parts.Length < 3) return;

            var type = parts[0].Trim().ToUpperInvariant();
            var dungeonName = parts[1].Trim();
            var description = parts[2].Trim();

            int effect = 0;
            if (parts.Length >= 4)
                int.TryParse(parts[3].Trim(), out effect);

            // Update dungeon state
            lock (_dungeonStates)
            {
                DungeonState state;
                if (!_dungeonStates.TryGetValue(dungeonName, out state))
                {
                    state = new DungeonState { DungeonName = dungeonName };
                    _dungeonStates[dungeonName] = state;
                }

                state.Flavor = description;
                state.LastEventType = type;

                // Apply difficulty effect
                if (effect > 0)
                {
                    state.DifficultyLevel = Math.Min(10, state.DifficultyLevel + effect);
                    state.LootMultiplier = Math.Min(3.0, state.LootMultiplier + (effect * 0.2));
                }
                else if (effect < 0)
                {
                    state.DifficultyLevel = Math.Max(1, state.DifficultyLevel + effect);
                    state.LootMultiplier = Math.Max(0.5, state.LootMultiplier + (effect * 0.2));
                }
            }

            // Spawn actual encounter based on event type
            SpawnDungeonEncounter(dungeonName, type, description);

            // Broadcast to players in that dungeon
            BroadcastToDungeon(dungeonName, "[Dungeon] " + description, 0x44);
            Console.WriteLine("[AI Dungeon] " + type + " in " + dungeonName + ": " + description);
        }

        /// <summary>Spawn real creatures in the dungeon based on event type.</summary>
        private static void SpawnDungeonEncounter(string dungeonName, string eventType, string description)
        {
            try
            {
                var region = FindDungeonRegion(dungeonName);
                if (region == null) return;

                switch (eventType)
                {
                    case "ENCOUNTER":
                        // Spawn 2-4 variant creatures
                        SpawnVariantGroup(region, 2, 4);
                        break;

                    case "BOSS_TACTIC":
                        // Spawn 1 elite boss + 2 guards
                        SpawnEliteBoss(region, dungeonName);
                        SpawnVariantGroup(region, 1, 2);
                        break;

                    case "HAZARD":
                        // Spawn 3-5 aggressive creatures
                        SpawnVariantGroup(region, 3, 5);
                        break;

                    case "TRAP":
                        // Spawn 1-2 stealthy/ambush creatures
                        SpawnVariantGroup(region, 1, 2);
                        break;

                    case "TREASURE":
                        // Spawn guards (2-3 creatures)
                        SpawnVariantGroup(region, 2, 3);
                        // Could also spawn a treasure chest in future
                        break;

                    case "AMBIENCE":
                        // Spawn 1 creature for atmosphere
                        SpawnVariantGroup(region, 1, 1);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI Dungeon] SpawnEncounter error: {ex.Message}");
            }
        }

        /// <summary>Find a dungeon region by name across all maps.</summary>
        private static DungeonRegion FindDungeonRegion(string name)
        {
            foreach (var map in Map.AllMaps)
            {
                if (map == null || map == Map.Internal) continue;
                foreach (var region in map.Regions.Values)
                {
                    if (region is DungeonRegion dr &&
                        region.Name != null &&
                        region.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return dr;
                }
            }
            return null;
        }

        /// <summary>Spawn a group of variant creatures in a region.</summary>
        private static void SpawnVariantGroup(Region region, int min, int max)
        {
            int count = Utility.RandomMinMax(min, max);
            for (int i = 0; i < count; i++)
            {
                var creature = CreateDungeonCreature(region);
                if (creature == null) continue;

                var loc = FindDungeonSpawnPoint(region);
                if (loc == Point3D.Zero) { creature.Delete(); continue; }

                creature.MoveToWorld(loc, region.Map);
            }
        }

        /// <summary>Spawn an elite boss in the dungeon.</summary>
        private static void SpawnEliteBoss(Region region, string dungeonName)
        {
            BaseCreature boss;
            // Pick boss based on dungeon name or random
            var name = dungeonName.ToLowerInvariant();
            if (name.Contains("orc") || name.Contains("cave"))
                boss = new OrcWarlord();
            else if (name.Contains("undead") || name.Contains("crypt") || name.Contains("tomb") || name.Contains("lich"))
                boss = new SkeletalLich();
            else if (name.Contains("lizard") || name.Contains("swamp"))
                boss = new LizardmanHighPriest();
            else if (name.Contains("troll") || name.Contains("mountain"))
                boss = new TrollChieftain();
            else
            {
                // Random elite
                var elites = new BaseCreature[] { new OrcWarlord(), new SkeletalLich(), new LizardmanHighPriest(), new TrollChieftain() };
                boss = elites[Utility.Random(elites.Length)];
            }

            var loc = FindDungeonSpawnPoint(region);
            if (loc != Point3D.Zero)
            {
                boss.MoveToWorld(loc, region.Map);
                // Broadcast boss emergence
                BroadcastToDungeon(region.Name ?? "the dungeon",
                    $"[Dungeon] {boss.Name} emerges from the shadows!", 0x22);
            }
            else
                boss.Delete();
        }

        /// <summary>Create a dungeon-appropriate creature variant.</summary>
        private static BaseCreature CreateDungeonCreature(Region region)
        {
            // Use the region name to select a theme
            var name = region.Name?.ToLowerInvariant() ?? "";
            double roll = Utility.RandomDouble();

            // 5% chance of elite boss
            if (roll < 0.05)
            {
                var elites = new BaseCreature[] { new OrcWarlord(), new SkeletalLich(), new LizardmanHighPriest(), new TrollChieftain() };
                return elites[Utility.Random(elites.Length)];
            }

            // Rest: regular variants based on theme
            if (name.Contains("orc") || name.Contains("cave"))
            {
                var variants = new BaseCreature[] { new OrcShaman(), new OrcArcher(), new OrcKnight(), new OrcBeastmaster(), new Orc() };
                return variants[Utility.Random(variants.Length)];
            }
            if (name.Contains("undead") || name.Contains("crypt") || name.Contains("tomb") || name.Contains("grave"))
            {
                var variants = new BaseCreature[] { new SkeletalMage(), new SkeletalArcher(), new GreaterSkeleton(), new Skeleton() };
                return variants[Utility.Random(variants.Length)];
            }
            if (name.Contains("lizard") || name.Contains("swamp"))
            {
                var variants = new BaseCreature[] { new LizardmanShaman(), new LizardmanSniper(), new Lizardman() };
                return variants[Utility.Random(variants.Length)];
            }
            if (name.Contains("troll") || name.Contains("mountain"))
            {
                var variants = new BaseCreature[] { new TrollWitchdoctor(), new GreaterTroll(), new Troll() };
                return variants[Utility.Random(variants.Length)];
            }

            // Generic dungeon creature
            var generics = new BaseCreature[] { new OrcShaman(), new SkeletalMage(), new LizardmanShaman() };
            return generics[Utility.Random(generics.Length)];
        }

        /// <summary>Find a valid spawn point inside a region.</summary>
        private static Point3D FindDungeonSpawnPoint(Region region)
        {
            if (region.Area == null || region.Area.Length == 0) return Point3D.Zero;

            var rect = region.Area[Utility.Random(region.Area.Length)];
            var start = rect.Start;
            var end = rect.End;

            for (int i = 0; i < 20; i++)
            {
                int x = start.X + Utility.Random(Math.Max(1, end.X - start.X));
                int y = start.Y + Utility.Random(Math.Max(1, end.Y - start.Y));
                int z = region.Map.GetAverageZ(x, y);
                var loc = new Point3D(x, y, z);
                if (region.Map.CanSpawnMobile(loc) && region.Contains(loc))
                    return loc;
            }
            return Point3D.Zero;
        }

        private static void BroadcastToDungeon(string dungeonName, string message, int hue)
        {
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null)
                {
                    var region = Region.Find(ns.Mobile.Location, ns.Mobile.Map);
                    if (region != null && region.Name == dungeonName)
                        ns.Mobile.SendMessage(hue, message);
                }
            }
        }

        /// <summary>Cleanup stale dungeon states.</summary>
        public static void CleanupStale()
        {
            lock (_dungeonStates)
            {
                var stale = _dungeonStates.Values
                    .Where(d => d.ActivePlayers.Count == 0 &&
                          (DateTime.UtcNow - d.LastPlayerActivity).TotalHours > 1)
                    .Select(d => d.DungeonName)
                    .ToList();

                foreach (var name in stale)
                    _dungeonStates.Remove(name);
            }
        }

        private class DungeonState
        {
            public string DungeonName;
            public List<string> ActivePlayers = new List<string>();
            public DateTime LastPlayerActivity = DateTime.UtcNow;
            public int DifficultyLevel = 5;
            public double LootMultiplier = 1.0;
            public string Flavor = "";
            public string LastEventType = "";
        }
    }
}
