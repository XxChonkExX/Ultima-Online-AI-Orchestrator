using System;
using Server;
using Server.ContextMenus;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Integrates the NPC Relationship System with creature taming.
    /// When any creature is tamed, a relationship entry is created.
    /// Tamed creatures gain relationship context menu options (Give Gift, Romance, etc.).
    /// </summary>
    public static class TameRelationshipIntegration
    {
        /// <summary>
        /// Subscribe to global events. Called once from AIOrchestratorInit.
        /// </summary>
        public static void Initialize()
        {
            EventSink.TameCreature += OnTameCreature;
            EventSink.ContextMenu += OnContextMenu;
            Console.WriteLine("[AIOrchestrator] Tame-Relationship integration initialized.");
        }

        /// <summary>
        /// When any creature is tamed, create/update its relationship with the tamer.
        /// Affinity scales with creature difficulty.
        /// </summary>
        private static void OnTameCreature(TameCreatureEventArgs e)
        {
            if (e.Mobile is PlayerMobile player && e.Creature is BaseCreature creature && !creature.Deleted)
            {
                // Get or create relationship entry
                var rel = NPCRelationshipSystem.GetOrCreate(player, creature);

                // Set initial state on first tame — Friend is the base relationship
                if (rel.State < NPCState.Friend)
                {
                    rel.State = NPCState.Friend;
                    rel.MetAt = DateTime.UtcNow;
                }

                // Affinity scales with how hard the creature was to tame
                int baseAffinity = Math.Max(25, (int)(creature.CurrentTameSkill * 2.5));
                rel.Affinity = Math.Min(rel.Affinity + baseAffinity, 1000);
                rel.LastInteraction = DateTime.UtcNow;

                Console.WriteLine($"[TAME-RELATIONSHIP] {player.Name} tamed {creature.GetType().Name} → affinity +{baseAffinity} (now {rel.Affinity})");
            }
        }

        /// <summary>
        /// Inject NPC relationship context entries into the context menu of tamed creatures.
        /// This fires for EVERY context menu in the game — we filter to relevant creatures.
        /// </summary>
        private static void OnContextMenu(ContextMenuEventArgs e)
        {
            if (e.Target is BaseCreature creature && e.Mobile is PlayerMobile player)
            {
                // Skip creatures that already self-manage relationship entries (e.g. HeroHireling)
                if (creature is HeroHireling)
                    return;

                // Show relationship entries if:
                //   a) The creature is controlled by this player, OR
                //   b) They already have an existing relationship with this creature
                bool isOwned = creature.Controlled && creature.ControlMaster == player;
                bool hasRelationship = false;

                if (!isOwned)
                {
                    try
                    {
                        var rel = NPCRelationshipSystem.GetOrCreate(player, creature);
                        hasRelationship = rel.Affinity != 0 || rel.State > NPCState.Stranger;
                    }
                    catch
                    {
                        // Ignore errors accessing relationship data
                    }
                }

                if (isOwned || hasRelationship)
                {
                    RelationshipContextMenu.AddEntries(creature, player, e.Entries);
                }
            }
        }
    }
}
