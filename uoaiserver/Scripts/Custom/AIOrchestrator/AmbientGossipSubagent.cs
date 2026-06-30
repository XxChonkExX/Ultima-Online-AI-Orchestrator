using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator.Subagents
{
    /// <summary>
    /// Ambient NPC-to-NPC conversation system.
    /// Pairs up nearby AI-enabled NPCs and generates short gossip exchanges
    /// to make towns feel alive.
    /// </summary>
    public static class AmbientGossipSubagent
    {
        private static Timer _gossipTimer;
        private static readonly TimeSpan GossipInterval = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan GossipDelay = TimeSpan.FromSeconds(60);
        private static readonly Random _rng = new Random();

        /// <summary>Topics NPCs can gossip about.</summary>
        private static readonly string[] GossipTopics =
        {
            "the weather",
            "a recent monster attack",
            "a traveling merchant",
            "local politics",
            "strange noises at night",
            "a treasure rumor",
            "the price of goods",
            "a newcomer in town",
            "an old legend",
            "the Virtues",
            "a wedding",
            "a fight at the tavern",
            "the king's decree",
            "bandits on the road",
            "a mysterious stranger"
        };

        public static void Initialize()
        {
            _gossipTimer = Timer.DelayCall(GossipDelay, GossipInterval, GossipTick);
            Console.WriteLine("[AIOrchestrator] Ambient gossip started: every " + GossipInterval.TotalSeconds + "s");
        }

        private static void GossipTick()
        {
            try
            {
                if (!AIConfig.Enabled)
                    return;

                int totalGossips = 0;

                // Find all player-populated regions and spawn gossip there
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile?.Player != true || ns.Mobile.Deleted)
                        continue;

                    var player = ns.Mobile;
                    var candidates = new List<BaseCreature>();

                    // Find AI-enabled NPCs near this player
                    foreach (var mob in player.GetMobilesInRange(15))
                    {
                        if (mob is BaseCreature bc && bc != player && bc.Alive && !bc.Player &&
                            AIComponentRegistry.HasAI(bc) && CanGossip(bc))
                        {
                            // Must have an identity (named NPCs more interesting)
                            var component = AIComponentRegistry.GetComponent(bc);
                            if (component?.Memory?.Identity != null)
                                candidates.Add(bc);
                        }
                    }

                    // Need at least 2 NPCs for a gossip pair
                    if (candidates.Count < 2)
                        continue;

                    // Pick a random pair
                    var npcA = candidates[_rng.Next(candidates.Count)];
                    candidates.Remove(npcA);
                    var npcB = candidates[_rng.Next(candidates.Count)];

                    // Must be near each other
                    if (!npcA.InRange(npcB, 8))
                        continue;

                    // Generate gossip
                    var topic = GossipTopics[_rng.Next(GossipTopics.Length)];
                    var componentA = AIComponentRegistry.GetComponent(npcA);
                    var componentB = AIComponentRegistry.GetComponent(npcB);
                    var idA = componentA?.Memory?.Identity;
                    var idB = componentB?.Memory?.Identity;
                    if (idA == null || idB == null)
                        continue;

                    var promptA = idA.GetPersonalityPrompt(false, "") +
                                  "\n\nYou are chatting with " + idB.Name + " (" + idB.Vocation + ") in " +
                                  Region.Find(npcA.Location, npcA.Map)?.Name ?? "town" +
                                  ". Topic: " + topic +
                                  ". Say ONE short line (max 120 chars) to start the conversation.";

                    var promptB = idB.GetPersonalityPrompt(false, "") +
                                  "\n\nYou are chatting with " + idA.Name + " (" + idA.Vocation + ")." +
                                  " They just said something about " + topic +
                                  ". Respond briefly (max 120 chars).";

                    // Fire off both LLM calls
                    Task.Run(async () =>
                    {
                        try
                        {
                            var replyA = await LLMClient.ChatAsync("", promptA, AIConfig.ModelDialogue);
                            if (!string.IsNullOrEmpty(replyA))
                            {
                                Timer.DelayCall(TimeSpan.FromSeconds(0.5), () =>
                                {
                                    if (!npcA.Deleted && npcA.Alive)
                                    {
                                        npcA.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, replyA);
                                        Console.WriteLine($"[AI GOSSIP] {npcA.Name} -> {npcB.Name}: \"{replyA}\"");
                                    }
                                });
                            }

                            var replyB = await LLMClient.ChatAsync("", promptB, AIConfig.ModelDialogue);
                            if (!string.IsNullOrEmpty(replyB))
                            {
                                Timer.DelayCall(TimeSpan.FromSeconds(2.5), () =>
                                {
                                    if (!npcB.Deleted && npcB.Alive)
                                    {
                                        npcB.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, replyB);
                                        Console.WriteLine($"[AI GOSSIP] {npcB.Name} -> {npcA.Name}: \"{replyB}\"");
                                    }
                                });
                            }
                        }
                        catch { }
                    });

                    totalGossips++;
                }

                if (totalGossips > 0)
                    Console.WriteLine($"[AIOrchestrator] Gossip: {totalGossips} conversation(s) triggered.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AIOrchestrator] Gossip error: " + ex.Message);
            }
        }

        /// <summary>Only humanoid/intelligent NPCs should gossip — no animals or simple monsters.</summary>
        private static bool CanGossip(BaseCreature bc)
        {
            var ai = bc.AI;
            switch (ai)
            {
                case AIType.AI_Animal:
                case AIType.AI_Predator:
                    return false;
                default:
                    return true;
            }
        }

        public static void Stop()
        {
            _gossipTimer?.Stop();
            _gossipTimer = null;
        }
    }
}
