using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;

namespace Server.AIOrchestrator.Factions
{
    /// <summary>
    /// Manages faction reputation changes from world events.
    /// Integrates with AIMemory's faction reputation system.
    /// </summary>
    public static class FactionReputationSystem
    {
        private static readonly Dictionary<string, FactionKillReward> KillRewards = new Dictionary<string, FactionKillReward>
        {
            // Killing orcs angers orcs, pleases guards/merchants
            ["orcs"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["orcs"] = -25,
                    ["undead"] = -10,
                    ["britain_guards"] = +10,
                    ["trinsic_guards"] = +10,
                    ["merchants"] = +5,
                    ["woodland"] = +5
                }
            },

            // Killing undead angers undead/necromancers, pleases guards/healers
            ["undead"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["undead"] = -20,
                    ["necromancers"] = -15,
                    ["britain_guards"] = +15,
                    ["trinsic_guards"] = +15,
                    ["healers"] = +10
                }
            },

            // Killing guards angers guards, pleases bandits/orcs
            ["britain_guards"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["britain_guards"] = -50,
                    ["trinsic_guards"] = -25,
                    ["orcs"] = +10,
                    ["bandits"] = +10,
                    ["pirates"] = +5
                }
            },

            ["trinsic_guards"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["trinsic_guards"] = -50,
                    ["britain_guards"] = -25,
                    ["orcs"] = +10,
                    ["undead"] = +5,
                    ["necromancers"] = +10
                }
            },

            // Killing merchants angers merchants, pleases bandits
            ["merchants"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["merchants"] = -40,
                    ["britain_guards"] = -20,
                    ["bandits"] = +15,
                    ["pirates"] = +10
                }
            },

            // Killing woodland creatures angers woodland, pleases orcs/lumberjacks
            ["woodland"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["woodland"] = -30,
                    ["orcs"] = +10,
                    ["necromancers"] = +5
                }
            },

            // Killing necromancers pleases guards/healers
            ["necromancers"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["necromancers"] = -30,
                    ["undead"] = -10,
                    ["britain_guards"] = +20,
                    ["trinsic_guards"] = +20,
                    ["healers"] = +15
                }
            },

            // Killing pirates pleases merchants/guards
            ["pirates"] = new FactionKillReward
            {
                FactionChanges = new Dictionary<string, int>
                {
                    ["pirates"] = -25,
                    ["merchants"] = +15,
                    ["britain_guards"] = +10,
                    ["trinsic_guards"] = +10
                }
            }
        };

        /// <summary>
        /// Initialize faction reputation system event hooks.
        /// </summary>
        public static void Initialize()
        {
            Console.WriteLine("[AIOrchestrator] Faction reputation system initialized.");
        }

        /// <summary>
        /// Apply faction reputation changes when a creature is killed.
        /// </summary>
        public static void OnCreatureKilled(Mobile killer, BaseCreature victim)
        {
            if (killer?.Player != true) return;

            var victimFaction = GetCreatureFaction(victim);
            if (string.IsNullOrEmpty(victimFaction)) return;

            if (!KillRewards.TryGetValue(victimFaction, out var reward)) return;

            var killerSer = killer.Serial.Value.ToString();
            var killerName = killer.Name;

            foreach (var kvp in reward.FactionChanges)
            {
                var factionId = kvp.Key;
                var delta = kvp.Value;

                // Apply to killer's faction reputation
                AIMemory.ModifyFactionReputationGlobal(killerSer, killer.Name, factionId, delta);
            }

            // Log significant changes
            foreach (var kvp in reward.FactionChanges)
            {
                if (Math.Abs(kvp.Value) >= 20)
                {
                    Console.WriteLine($"[FACTION] {killer.Name} killed {victim.Name} ({victimFaction}) → {kvp.Key} {kvp.Value:+#;-#;0}");
                }
            }
        }

        /// <summary>
        /// Apply faction reputation when a quest is completed for a specific NPC.
        /// </summary>
        public static void OnQuestCompleted(Mobile player, BaseCreature questGiver)
        {
            if (player?.Player != true) return;

            var giverFaction = GetCreatureFaction(questGiver);
            if (string.IsNullOrEmpty(giverFaction)) return;

            var playerSer = player.Serial.Value.ToString();

            // Positive reputation with quest giver's faction
            AIMemory.ModifyFactionReputationGlobal(playerSer, player.Name, giverFaction, +15);

            // Small boost to allied factions
            NpcFaction faction = null;
            if (NpcFaction.Presets.ById.TryGetValue(giverFaction, out faction))
            {
                foreach (var alliedId in faction.AlliedFactionIds)
                {
                    AIMemory.ModifyFactionReputationGlobal(playerSer, player.Name, alliedId, +5);
                }
            }
        }

        /// <summary>
        /// Determine the primary faction of a creature.
        /// </summary>
        public static string GetCreatureFaction(BaseCreature creature)
        {
            if (creature == null) return "";

            // Check preset factions
            foreach (var faction in NpcFaction.Presets.All)
            {
                if (faction.IsMember(creature))
                    return faction.Id;
            }

            // Fallback: derive from creature type/name
            var name = creature.GetType().Name.ToLowerInvariant();
            var cname = creature.Name?.ToLowerInvariant() ?? "";

            if (name.Contains("orc") || cname.Contains("orc")) return "orcs";
            if (name.Contains("guard") || cname.Contains("guard")) return "britain_guards";
            if (name.Contains("skeleton") || name.Contains("zombie") || name.Contains("ghost") || 
                name.Contains("wraith") || name.Contains("lich") || name.Contains("vampire")) return "undead";
            if (name.Contains("necromancer") || name.Contains("lich") || name.Contains("dark")) return "necromancers";
            if (name.Contains("merchant") || name.Contains("vendor") || name.Contains("shopkeeper")) return "merchants";
            if (name.Contains("pirate") || name.Contains("buccaneer") || name.Contains("corsair")) return "pirates";
            if (name.Contains("orc") || name.Contains("goblin") || name.Contains("troll") || name.Contains("ogre")) return "orcs";
            if (name.Contains("bear") || name.Contains("wolf") || name.Contains("deer") || 
                name.Contains("rabbit") || name.Contains("bird") || name.Contains("fox")) return "woodland";

            return "";
        }

        public class FactionKillReward
        {
            public Dictionary<string, int> FactionChanges { get; set; } = new Dictionary<string, int>();
        }
    }
}