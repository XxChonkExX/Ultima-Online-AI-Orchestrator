using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator;
using Server.AIOrchestrator.Factions;

namespace Server.AIOrchestrator.Subagents
{
    public class DialogueSubagent : IAIOrchestrator
    {
        public SubagentType ActiveSubagent => SubagentType.Dialogue;
        private readonly BaseCreature _creature;
        private readonly AIMemory _memory;
        private NpcClass _npcClass = NpcClass.Generic;
        private bool _classified;

        // Quest-related keywords that trigger quest generation
        private static readonly string[] QuestKeywords = new[]
        {
            "work", "task", "quest", "job", "help", "need", "favor",
            "errand", "chore", "bounty", "mission", "deed"
        };

        // Bard-specific keywords that trigger news/rumor recitation
        private static readonly string[] BardKeywords = new[]
        {
            "news", "rumor", "gossip", "story", "tale", "song", "latest",
            "happening", "word", "tell me", "heard"
        };

        public DialogueSubagent(BaseCreature creature, AIMemory memory)
        {
            _creature = creature;
            _memory = memory;
        }

        public void OnHeartbeat(Mobile player)
        {
        }

        /// <summary>
        /// Classify this NPC once and cache the result.
        /// </summary>
        private NpcClass GetNpcClass()
        {
            if (!_classified)
            {
                _npcClass = PersonalityTemplates.Classify(_creature);
                _classified = true;
            }
            return _npcClass;
        }

        /// <summary>
        /// Build a comprehensive system prompt for this NPC including:
        /// - Core identity (name, vocation, temperament)
        /// - Personality template per NPC class
        /// - Time of day (night/day)
        /// - Weather/region context
        /// - Player relationship memory
        /// - Recent conversation history
        /// - Economy context
        /// - Active quest context
        /// </summary>
        private string BuildSystemPrompt(string playerSerial, string playerName, Mobile player)
        {
            var identity = _memory.Identity;
            if (identity == null)
                return "You are an NPC in Ultima Online. Respond in character.";

            // Time of day
            int hours = 12, mins = 0;
            Clock.GetTime(player.Map, player.X, player.Y, out hours, out mins);
            var isNight = hours < 6 || hours >= 20;

            // Region context
            var region = Region.Find(_creature.Location, _creature.Map);
            var weatherContext = "";
            if (region != null && !string.IsNullOrEmpty(region.Name))
                weatherContext = "Current region: " + region.Name;

            // Player relationship memory
            var relContext = _memory.GetRelationshipContext(playerSerial, playerName);

            // Economy context (town NPCs know about markets)
            var economyContext = "";
            if (GetNpcClass() == NpcClass.Merchant || GetNpcClass() == NpcClass.Tavernkeep || GetNpcClass() == NpcClass.Bard)
            {
                economyContext = LivingEconomy.GetEconomyContext();
            }

            // Quest context
            var questContext = AIQuestManager.GetQuestContext(player, _creature);

            // Bard special: recent deeds
            // if (BardDeedIntegration.IsBard(_creature))
            // {
            //     var bardPrompt = BardDeedIntegration.GetBardSystemPrompt(_creature);
            //     return bardPrompt + (questContext ?? "");
            // }

            // Build the full prompt using personality template
            var prompt = PersonalityTemplates.GetPrompt(GetNpcClass(), identity, isNight, weatherContext);

            if (!string.IsNullOrEmpty(relContext))
                prompt += "\n\n" + relContext;

            if (!string.IsNullOrEmpty(economyContext))
                prompt += "\n\n" + economyContext;

            if (!string.IsNullOrEmpty(questContext))
                prompt += "\n\n" + questContext;

            // Threat context for guards
            if (GetNpcClass() == NpcClass.Guard)
            {
                var threatContext = RegionalThreatSystem.GetThreatContext();
                if (!string.IsNullOrEmpty(threatContext))
                    prompt += "\n\n" + threatContext;
            }

            // Faction context for all NPCs
            var factionContext = _memory.GetFactionContext(playerSerial, playerName);
            if (!string.IsNullOrEmpty(factionContext))
                prompt += "\n\n" + factionContext;

            return prompt;
        }

        /// <summary>Check if the player's speech contains quest keywords.</summary>
        private bool IsQuestRequest(string speech)
        {
            var lower = speech.ToLowerInvariant();
            foreach (var kw in QuestKeywords)
            {
                if (lower.Contains(kw))
                    return true;
            }
            return false;
        }

        /// <summary>Check if the player is trying to complete/claim a quest reward.</summary>
        private bool IsQuestCompleteRequest(string speech)
        {
            var lower = speech.ToLowerInvariant();
            return lower.Contains("complete") || lower.Contains("reward") ||
                   lower.Contains("done") || lower.Contains("finished") ||
                   lower.Contains("claim") || lower.Contains("return");
        }

        /// <summary>Check if the player's speech contains bard/news keywords.</summary>
        private bool IsBardRequest(string speech)
        {
            // BardDeedIntegration is not yet implemented
            // if (!BardDeedIntegration.IsBard(_creature))
            //     return false;

            var lower = speech.ToLowerInvariant();
            foreach (var kw in BardKeywords)
            {
                if (lower.Contains(kw))
                    return true;
            }
            return false;
        }

        /// <summary>Try to offer a quest when the player asks for work.</summary>
        private void TryOfferQuest(Mobile from, string speech)
        {
            // Only non-animal NPCs give quests
            if (GetNpcClass() == NpcClass.Animal)
                return;

            // Check if NPC can give quests
            if (!AIQuestManager.CanGiveQuest(from, _creature))
            {
                // Already has a quest — prompt to complete it
                var questContext = AIQuestManager.GetQuestContext(from, _creature);
                if (!string.IsNullOrEmpty(questContext))
                {
                    Timer.DelayCall(TimeSpan.FromMilliseconds(100), () =>
                    {
                        if (!_creature.Deleted && _creature.Alive)
                            _creature.PublicOverheadMessage(MessageType.Regular, 0x3B2, false,
                                "You already have a task from me. See to it first.");
                    });
                }
                return;
            }

            // Generate a quest asynchronously
            Task.Run(async () =>
            {
                try
                {
                    var quest = await AIQuestManager.GenerateQuest(from, _creature);
                    if (quest != null)
                    {
                        Timer.DelayCall(TimeSpan.FromMilliseconds(200), () =>
                        {
                            if (!_creature.Deleted && _creature.Alive)
                            {
                                _creature.PublicOverheadMessage(MessageType.Regular, 0x44, false,
                                    $"[Quest] {quest.QuestDescription}");
                                from.SendMessage(0x44, $"[Quest] '{quest.QuestTitle}' accepted! Check your journal for details.");
                            }
                        });
                    }
                }
                catch { }
            });
        }

        public void OnSpeech(Mobile from, string speech)
        {
            if (!_creature.Alive || from == _creature || !from.Player || !from.InRange(_creature, 5))
                return;

            var playerSerial = from.Serial.Value.ToString();
            var playerName = from.Name;
            var modelName = AIConfig.ModelDialogue;

            // Record the greeting
            _memory.RecordGreeting(playerSerial, playerName);

            // Check if player has been hostile — NPC may refuse service
            var playerMem = _memory.GetOrCreatePlayerMemory(playerSerial, playerName);
            if (playerMem.Reputation <= -80 && Utility.RandomDouble() < 0.5)
            {
                var hostileReplies = new[]
                {
                    "I have nothing to say to you. Leave.",
                    "You are not welcome here.",
                    "After what you did? Get out of my sight.",
                    "I'd rather not speak with the likes of you."
                };
                var reply = hostileReplies[Utility.Random(hostileReplies.Length)];
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (!_creature.Deleted && _creature.Alive)
                        _creature.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, reply);
                });
                Console.WriteLine($"[AI SPEECH] {_creature.Name} refused to talk to {playerName} (rep={playerMem.Reputation})");
                return;
            }

            // Quest progress: talking to this NPC may progress NpcRelation quests
            AIQuestManager.ReportNpcRelation(from, _creature.Name);

            // Bard special: news/rumor request
            if (IsBardRequest(speech))
            {
                // BardDeedIntegration is not yet implemented; use inline prompt
                var bardPrompt = "You are a traveling bard NPC. Recite recent world events and rumors in a poetic, bardic style. Keep it brief and engaging.";
                Console.WriteLine("[AI SPEECH] Bard " + _creature.Name + " reciting news for " + playerName);

                Task.Run(async () =>
                {
                    try
                    {
                        var reply = await LLMClient.ChatAsync(bardPrompt,
                            "Player asked for news. Recite the most interesting recent event in your bardic style.",
                            modelName);

                        if (!string.IsNullOrEmpty(reply))
                        {
                            Timer.DelayCall(TimeSpan.Zero, () =>
                            {
                                if (!_creature.Deleted && _creature.Alive)
                                {
                                    _creature.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, reply);
                                    _memory.AddConversationTurn(playerSerial, playerName, speech, reply);
                                }
                            });
                        }
                    }
                    catch { }
                });
                return;
            }

            // Quest completion: check if player is trying to claim a reward
            if (IsQuestCompleteRequest(speech) && AIQuestManager.CompleteQuest(from, _creature))
            {
                var playerMem2 = _memory.GetOrCreatePlayerMemory(playerSerial, playerName);
                Timer.DelayCall(TimeSpan.FromMilliseconds(100), () =>
                {
                    if (!_creature.Deleted && _creature.Alive)
                        _creature.PublicOverheadMessage(MessageType.Regular, 0x44, false,
                            "Here's your reward, as promised!");
                });
                return;
            }

            // Quest request: check if player is asking for work
            if (IsQuestRequest(speech))
            {
                TryOfferQuest(from, speech);
                return; // Don't also do a normal LLM response for quest requests
            }

            // Standard dialogue
            var identityPrompt = BuildSystemPrompt(playerSerial, playerName, from);
            var context = _memory.GetConversationContext(playerSerial);

            Console.WriteLine($"[AI SPEECH] {_creature.Name} ({GetNpcClass()}) heard '{speech}' from {playerName}. Model: {modelName}");

            var prompt = "Recent conversation with " + playerName + ":\n" + context +
                         "\n\nPlayer: \"" + speech + "\"";

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var reply = await LLMClient.ChatAsync(identityPrompt, prompt, modelName);

                    if (!string.IsNullOrEmpty(reply))
                    {
                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            if (!_creature.Deleted && _creature.Alive && from.InRange(_creature, 5))
                            {
                                _creature.PublicOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, reply);
                                _memory.AddConversationTurn(playerSerial, playerName, speech, reply);
                                Console.WriteLine($"[AI SPEECH] {_creature.Name} replied: \"{reply}\"");
                            }
                        });
                    }
                    else
                    {
                        Console.WriteLine($"[AI SPEECH] {_creature.Name} got empty reply from LLM for: \"{speech}\"");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AI SPEECH ERROR] {_creature.Name}: {ex.Message}");
                }
            });
        }
    }
}