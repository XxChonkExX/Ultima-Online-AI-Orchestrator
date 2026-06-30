using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;

namespace Server.AIOrchestrator.Factions
{
    /// <summary>
    /// NPC faction alliance behaviors — dynamic shifts, combat cooperation,
    /// and reputation-driven reactions.
    /// </summary>
    public static class NpcAllianceBehavior
    {
        // ── Alliance Shift Tracking ────────────────────────────────
        // Tracks temporary alliance shifts (e.g., "orcs" and "undead" become allied)
        private static readonly Dictionary<string, HashSet<string>> DynamicAlliances = new Dictionary<string, HashSet<string>>();
        private static readonly Dictionary<string, HashSet<string>> DynamicEnmities = new Dictionary<string, HashSet<string>>();

        private static Timer _cleanupTimer;

        public static void Initialize()
        {
            _cleanupTimer = Timer.DelayCall(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30), CleanupExpiredShifts);
            Console.WriteLine("[AIOrchestrator] NPC alliance behavior initialized.");
        }

        // ── Dynamic Alliance Shifts ────────────────────────────────

        /// <summary>Temporarily ally two factions (e.g., orcs + undead).</summary>
        public static void AddAlliance(string factionA, string factionB, int durationMinutes = 120)
        {
            lock (DynamicAlliances)
            {
                if (!DynamicAlliances.ContainsKey(factionA))
                    DynamicAlliances[factionA] = new HashSet<string>();
                if (!DynamicAlliances.ContainsKey(factionB))
                    DynamicAlliances[factionB] = new HashSet<string>();

                DynamicAlliances[factionA].Add(factionB);
                DynamicAlliances[factionB].Add(factionA);
            }

            var expiry = DateTime.UtcNow.AddMinutes(durationMinutes);
            Timer.DelayCall(TimeSpan.FromMinutes(durationMinutes), () =>
            {
                lock (DynamicAlliances)
                {
                    DynamicAlliances[factionA]?.Remove(factionB);
                    DynamicAlliances[factionB]?.Remove(factionA);
                }
            });

            BroadcastFactionEvent($"[Factions] {GetFactionName(factionA)} and {GetFactionName(factionB)} have formed an alliance!", 0x44);
        }

        /// <summary>Temporarily make two factions enemies.</summary>
        public static void AddEnmity(string factionA, string factionB, int durationMinutes = 120)
        {
            lock (DynamicEnmities)
            {
                if (!DynamicEnmities.ContainsKey(factionA))
                    DynamicEnmities[factionA] = new HashSet<string>();
                if (!DynamicEnmities.ContainsKey(factionB))
                    DynamicEnmities[factionB] = new HashSet<string>();

                DynamicEnmities[factionA].Add(factionB);
                DynamicEnmities[factionB].Add(factionA);
            }

            Timer.DelayCall(TimeSpan.FromMinutes(durationMinutes), () =>
            {
                lock (DynamicEnmities)
                {
                    DynamicEnmities[factionA]?.Remove(factionB);
                    DynamicEnmities[factionB]?.Remove(factionA);
                }
            });

            BroadcastFactionEvent($"[Factions] {GetFactionName(factionA)} and {GetFactionName(factionB)} are now at war!", 0x22);
        }

        /// <summary>Check if two factions are currently allied (including dynamic).</summary>
        public static bool AreAllied(string factionA, string factionB)
        {
            if (factionA == factionB) return true;

            // Check permanent alliances from NpcFaction definitions
            if (NpcFaction.Presets.ById.TryGetValue(factionA, out var faction))
            {
                if (faction.IsAlliedWith(factionB)) return true;
            }

            // Check dynamic alliances
            lock (DynamicAlliances)
            {
                return DynamicAlliances.TryGetValue(factionA, out var allies) && allies.Contains(factionB);
            }
        }

        /// <summary>Check if two factions are enemies.</summary>
        public static bool AreEnemies(string factionA, string factionB)
        {
            if (factionA == factionB) return false;

            // Check permanent enmities
            if (NpcFaction.Presets.ById.TryGetValue(factionA, out var faction))
            {
                if (faction.IsEnemyOf(factionB)) return true;
            }

            // Check dynamic enmities
            lock (DynamicEnmities)
            {
                return DynamicEnmities.TryGetValue(factionA, out var enemies) && enemies.Contains(factionB);
            }
        }

        // ── Combat Cooperation ─────────────────────────────────────

        /// <summary>
        /// When a creature of faction A attacks a player, should creatures
        /// of faction B join the fight against the player?
        /// </summary>
        public static bool ShouldAssist(string attackerFaction, string helperFaction)
        {
            if (string.IsNullOrEmpty(attackerFaction) || string.IsNullOrEmpty(helperFaction))
                return false;

            // Allied factions assist each other
            if (AreAllied(attackerFaction, helperFaction))
                return true;

            return false;
        }

        /// <summary>
        /// When a player kills a creature of faction A, check nearby creatures
        /// from allied factions and make them aggressive.
        /// </summary>
        public static void OnFactionCreatureKilled(Mobile killer, BaseCreature victim, string victimFaction)
        {
            if (killer?.Player != true || string.IsNullOrEmpty(victimFaction)) return;

            // Find nearby creatures from allied factions and make them react
            foreach (Mobile m in victim.GetMobilesInRange(12))
            {
                if (m == victim || m == killer || !(m is BaseCreature ally)) continue;
                if (ally.Combatant != null) continue; // Already fighting

                var allyFaction = FactionReputationSystem.GetCreatureFaction(ally);
                if (string.IsNullOrEmpty(allyFaction)) continue;

                // Allied faction member saw the kill — become hostile
                if (AreAllied(victimFaction, allyFaction) && allyFaction != victimFaction)
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble() * 2.0), () =>
                    {
                        if (!ally.Deleted && ally.Alive)
                        {
                            ally.Combatant = killer;
                            ally.Say("*enraged by the killing!*");
                        }
                    });
                }
            }
        }

        // ── Reputation-Driven Reactions ────────────────────────────

        /// <summary>
        /// Get a reaction message based on the player's reputation with an NPC's faction.
        /// Returns null if neutral/no strong opinion.
        /// </summary>
        public static string GetReputationReaction(Mobile player, BaseCreature npc)
        {
            var faction = FactionReputationSystem.GetCreatureFaction(npc);
            if (string.IsNullOrEmpty(faction)) return null;

            var component = AIComponentRegistry.GetComponent(npc);
            if (component?.Memory == null) return null;

            var rep = component.Memory.GetFactionReputation(player.Serial.Value.ToString(), faction);

            if (rep >= 50)
            {
                string[] friendly = {
                    "I know you well, friend of our cause!",
                    "You've done much for us. How can I help?",
                    "Ah, a trusted ally! What brings you here?"
                };
                return friendly[Utility.Random(friendly.Length)];
            }
            else if (rep <= -50)
            {
                string[] hostile = {
                    "You're not welcome here, enemy!",
                    "I remember what you did. Leave before I call the guards!",
                    "Your reputation precedes you, villain. State your business quickly."
                };
                return hostile[Utility.Random(hostile.Length)];
            }

            return null; // Neutral
        }

        // ── Cleanup ────────────────────────────────────────────────

        private static void CleanupExpiredShifts()
        {
            // Dynamic alliances/enmities are cleaned up by their individual timers.
            // This just clears stale empty sets.
            lock (DynamicAlliances)
            {
                var empty = DynamicAlliances.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
                foreach (var k in empty) DynamicAlliances.Remove(k);
            }
            lock (DynamicEnmities)
            {
                var empty = DynamicEnmities.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
                foreach (var k in empty) DynamicEnmities.Remove(k);
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        private static string GetFactionName(string factionId)
        {
            return NpcFaction.Presets.ById.TryGetValue(factionId, out var faction)
                ? faction.Name
                : factionId;
        }

        private static void BroadcastFactionEvent(string message, int hue)
        {
            foreach (var ns in Server.Network.NetState.Instances)
            {
                if (ns.Mobile != null)
                    ns.Mobile.SendMessage(hue, message);
            }
        }
    }
}
