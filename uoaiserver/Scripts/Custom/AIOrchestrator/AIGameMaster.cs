using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator.Subagents;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// The AI Game Master orchestrates emergent multi-phase storylines.
    /// It monitors ALL subagent outputs (economy, faction, spawn, dungeon, environment)
    /// and weaves them into narrative phases that evolve over time.
    /// 
    /// Phase system:
    ///   CALM    - peace, rumors, discovery
    ///   BUILD   - tension rising, faction conflicts
    ///   CRISIS  - active invasion/disaster/manhunt
    ///   RESOLVE - aftermath, rewards, consequences
    /// 
    /// Expansions:
    ///   - Arc lifecycle: arcs auto-resolve in Resolve phase with epilogue
    ///   - World effects: phase biases other subsystems
    ///   - Weather bias: phase-specific weather weighting
    /// </summary>
    public static class AIGameMaster
    {
        private static Timer _masterTimer;
        private static DateTime _lastStoryEvent = DateTime.MinValue;
        private static NarrativePhase _currentPhase = NarrativePhase.Calm;
        private static int _phaseDurationTicks = 0;
        private static readonly List<NarrativeArc> _activeArcs = new List<NarrativeArc>();
        private static readonly List<string> _resolvedArcs = new List<string>(); // epilogue history

        private const int StoryIntervalMinutes = 15;
        private const int PhaseShiftMinTicks = 3;
        private const int PhaseShiftMaxTicks = 8;
        private const int ArcResolveTicks = 2; // resolve arcs after 2 ticks in Resolve phase

        /// <summary>Phase definition for narrative progression.</summary>
        public enum NarrativePhase
        {
            Calm,
            Build,
            Crisis,
            Resolve
        }

        private class NarrativeArc
        {
            public string Title;
            public string Description;
            public NarrativePhase Phase;
            public int TickCount;
            public bool Completed;
            public DateTime Created = DateTime.UtcNow;
        }

        public static void Initialize()
        {
            CommandSystem.Register("GMThink", AccessLevel.Administrator, OnGMThink);
            CommandSystem.Register("GMStory", AccessLevel.Administrator, OnGMStory);
            CommandSystem.Register("GMPhase", AccessLevel.Administrator, OnGMPhase);
            CommandSystem.Register("GMArcs", AccessLevel.Administrator, OnGMArcs);
            CommandSystem.Register("GMHistory", AccessLevel.Administrator, OnGMHistory);

            _masterTimer = Timer.DelayCall(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(StoryIntervalMinutes), MasterTick);
            _currentPhase = NarrativePhase.Calm;

            Console.WriteLine("[AIOrchestrator] AI Game Master initialized (multi-phase narrative + arc lifecycle).");
            Console.WriteLine("[AIOrchestrator] Phase: " + _currentPhase + " | Story interval: " + StoryIntervalMinutes + " min.");
        }

        /// <summary>Periodic tick — process subagent outputs and generate narrative events.</summary>
        private static void MasterTick()
        {
            try
            {
                if (!AIConfig.Enabled) return;

                // Phase progression
                _phaseDurationTicks++;
                MaybeShiftPhase();

                // ── Arc lifecycle: age & resolve ──────────────────────────
                lock (_activeArcs)
                {
                    foreach (var arc in _activeArcs)
                    {
                        arc.TickCount++;
                    }

                    // Resolve arcs that have been in Resolve phase long enough
                    if (_currentPhase == NarrativePhase.Resolve)
                    {
                        var toResolve = _activeArcs.FindAll(a => !a.Completed && a.TickCount >= ArcResolveTicks);
                        foreach (var arc in toResolve)
                        {
                            ResolveArc(arc);
                            arc.Completed = true;
                        }
                        _activeArcs.RemoveAll(a => a.Completed);
                    }
                }

                // ── World effects: apply phase bias ───────────────────────
                ApplyWorldEffects();

                // Gather full world state
                var worldState = GatherFullWorldState();

                // Generate story event via LLM
                Task.Run(async () =>
                {
                    try
                    {
                        var story = await GenerateStoryEvent(worldState, _currentPhase);
                        if (!string.IsNullOrEmpty(story))
                        {
                            ExecuteStoryEvent(story);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[AI GM] Story generation error: " + ex.Message);
                    }
                });

                _lastStoryEvent = DateTime.UtcNow;
                Console.WriteLine("[AI GM] Tick complete. Phase: " + _currentPhase + " | Arcs: " + _activeArcs.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AI GM] Master tick error: " + ex.Message);
            }
        }

        /// <summary>Apply phase-based modifiers to other subsystems.</summary>
        private static void ApplyWorldEffects()
        {
            switch (_currentPhase)
            {
                case NarrativePhase.Calm:
                    // Boost trade economy
                    EnvironmentSubagent.SetWeatherBias("calm");
                    break;

                case NarrativePhase.Build:
                    // Boost spawn controller
                    EnvironmentSubagent.SetWeatherBias("ominous");
                    break;

                case NarrativePhase.Crisis:
                    // Boost regional threats
                    EnvironmentSubagent.SetWeatherBias("violent");
                    break;

                case NarrativePhase.Resolve:
                    // Neutral weather
                    EnvironmentSubagent.SetWeatherBias("neutral");
                    break;
            }
        }

        /// <summary>Resolve a narrative arc with an epilogue broadcast.</summary>
        private static void ResolveArc(NarrativeArc arc)
        {
            string[] epilogues = new[]
            {
                "The tale of '{0}' has reached its end. The realm breathes easier.",
                "With '{0}' resolved, life slowly returns to normal.",
                "The scars of '{0}' remain, but Britannia endures.",
                "Bards will sing of '{0}' for generations to come.",
                "The chapter of '{0}' closes. What new story awaits?"
            };

            string epilogue = String.Format(epilogues[Utility.Random(epilogues.Length)], arc.Title);
            string historyEntry = "[" + arc.Phase + "] " + arc.Title + " — " + arc.Description;

            lock (_resolvedArcs)
            {
                _resolvedArcs.Add(historyEntry);
                if (_resolvedArcs.Count > 20)
                    _resolvedArcs.RemoveAt(0);
            }

            Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
            {
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile != null)
                    {
                        ns.Mobile.SendMessage(0x480, "[Arc Resolved] " + epilogue);
                    }
                }
                Console.WriteLine("[AI GM] Arc resolved: " + arc.Title + " — " + epilogue);
            });
        }

        /// <summary>Decide whether to shift the narrative phase.</summary>
        private static void MaybeShiftPhase()
        {
            if (_phaseDurationTicks < PhaseShiftMinTicks) return;

            if (_phaseDurationTicks >= PhaseShiftMaxTicks)
            {
                _phaseDurationTicks = 0;
                _currentPhase = GetNextPhase(_currentPhase);
                BroadcastPhaseShift();
                return;
            }

            if (Utility.RandomDouble() < 0.35)
            {
                _phaseDurationTicks = 0;
                _currentPhase = GetNextPhase(_currentPhase);
                BroadcastPhaseShift();
            }
        }

        private static NarrativePhase GetNextPhase(NarrativePhase current)
        {
            switch (current)
            {
                case NarrativePhase.Calm:    return Utility.RandomBool() ? NarrativePhase.Build : NarrativePhase.Calm;
                case NarrativePhase.Build:   return NarrativePhase.Crisis;
                case NarrativePhase.Crisis:  return NarrativePhase.Resolve;
                case NarrativePhase.Resolve: return NarrativePhase.Calm;
                default:                     return NarrativePhase.Calm;
            }
        }

        private static void BroadcastPhaseShift()
        {
            string message;
            int hue;

            switch (_currentPhase)
            {
                case NarrativePhase.Calm:
                    message = "The realm settles into an uneasy peace. Whispers fade, and the world breathes.";
                    hue = 0x44;
                    break;
                case NarrativePhase.Build:
                    message = "Tension ripples across Britannia. Dark clouds gather on the horizon...";
                    hue = 0x34;
                    break;
                case NarrativePhase.Crisis:
                    message = "CATASTROPHE! The realm trembles as ancient evils stir! Heroes are needed!";
                    hue = 0x20;
                    break;
                case NarrativePhase.Resolve:
                    message = "The tide turns. Slowly, order is restored. But scars remain...";
                    hue = 0x480;
                    break;
                default:
                    return;
            }

            Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
            {
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile != null)
                    {
                        ns.Mobile.SendMessage(hue, "[Narrative] " + message);
                        ns.Mobile.SendMessage(hue, "[Narrative] The world enters a phase of " + _currentPhase.ToString().ToLowerInvariant() + ".");
                    }
                }
                Console.WriteLine("[AI GM] Phase shift: " + _currentPhase);
            });
        }

        /// <summary>Gather world state from ALL subagents for rich narrative context.</summary>
        private static string GatherFullWorldState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== WORLD STATE ===");
            sb.AppendLine("Narrative Phase: " + _currentPhase);
            sb.AppendLine("Phase Ticks: " + _phaseDurationTicks);

            int playerCount = 0;
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile?.Player == true)
                    playerCount++;
            }
            sb.AppendLine("Active players: " + playerCount);

            try { var economy = EconomySubagent.GetEconomyContext(); if (!string.IsNullOrEmpty(economy)) sb.AppendLine(economy); } catch { }
            try { var legacyEconomy = LivingEconomy.GetEconomyContext(); if (!string.IsNullOrEmpty(legacyEconomy)) sb.AppendLine(legacyEconomy); } catch { }
            try { var factions = FactionDiplomatSubagent.GetFactionDiplomacyContext(); if (!string.IsNullOrEmpty(factions)) sb.AppendLine(factions); } catch { }
            try { var threats = RegionalThreatSystem.GetThreatContext(); if (!string.IsNullOrEmpty(threats)) sb.AppendLine(threats); } catch { }
            try { var deeds = PlayerDeedTracker.GetRecentDeedsContext(); if (!string.IsNullOrEmpty(deeds)) sb.AppendLine(deeds); } catch { }

            lock (_activeArcs)
            {
                if (_activeArcs.Count > 0)
                {
                    sb.AppendLine("Active Arcs:");
                    foreach (var arc in _activeArcs)
                    {
                        sb.AppendLine("- " + arc.Title + " (" + arc.Phase + ", tick " + arc.TickCount + ")");
                    }
                }
            }

            try { var spawnContext = SpawnControllerSubagent.GetSpawnContext(); if (!string.IsNullOrEmpty(spawnContext)) sb.AppendLine(spawnContext); } catch { }
            try { var flavor = DungeonMasterSubagent.GetDungeonFlavor("any"); if (!string.IsNullOrEmpty(flavor)) sb.AppendLine("Dungeon activity: " + flavor); } catch { }

            return sb.ToString();
        }

        /// <summary>Ask the LLM to generate a story event based on full world state and phase.</summary>
        private static async Task<string> GenerateStoryEvent(string worldState, NarrativePhase phase)
        {
            string phaseDescription;
            switch (phase)
            {
                case NarrativePhase.Calm:
                    phaseDescription = "PEACE: Discovery, trade, rumors, festivals, exploration. Small personal stories.";
                    break;
                case NarrativePhase.Build:
                    phaseDescription = "TENSION: Faction conflict, monster surges, mysterious omens, political intrigue.";
                    break;
                case NarrativePhase.Crisis:
                    phaseDescription = "DISASTER: Full-scale invasion, plague, dungeon awakening, siege, manhunt.";
                    break;
                case NarrativePhase.Resolve:
                    phaseDescription = "AFTERMATH: Reconstruction, reward, punishment of villains, memorials, new alliances.";
                    break;
                default:
                    phaseDescription = "Unknown";
                    break;
            }

            var prompt = "You are the hidden Game Master of Ultima Online. You weave world events into a living narrative.\n\n" +
                         worldState + "\n\n" +
                         "Current phase: " + phaseDescription + "\n\n" +
                         "Generate a story event (1 sentence, max 200 chars) that:\n" +
                         "- Matches the current narrative phase\n" +
                         "- References real game locations (Britain, Trinsic, Moonglow, Minoc, Jhelom, Skara Brae, Yew, Magincia, dungeons)\n" +
                         "- Creates urgency, mystery, or opportunity for roaming players\n" +
                         "- Feels organic — NOT meta. No mention of 'game master', 'AI', 'phase', 'subagent', etc.\n\n" +
                         "Format: a single line starting with [Rumor] or [Warning] or [Legend] or [Bard]";

            try
            {
                return await LLMClient.ChatAsync("", prompt, AIConfig.ModelNarrator);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Execute a story event — broadcast to players and optionally create narrative arcs.</summary>
        private static void ExecuteStoryEvent(string story)
        {
            if (string.IsNullOrEmpty(story)) return;

            bool spawnArc = false;
            if (_currentPhase == NarrativePhase.Build && _activeArcs.Count < 3)
                spawnArc = true;
            else if (_currentPhase == NarrativePhase.Crisis && _activeArcs.Count < 2)
                spawnArc = true;

            if (spawnArc)
            {
                lock (_activeArcs)
                {
                    _activeArcs.Add(new NarrativeArc
                    {
                        Title = "Event " + (_activeArcs.Count + 1),
                        Description = story.Length > 80 ? story.Substring(0, 80) + "..." : story,
                        Phase = _currentPhase
                    });
                }
            }

            if (_currentPhase == NarrativePhase.Crisis && Utility.RandomDouble() < 0.4)
                OfferCrisisQuests();
            else if (_currentPhase == NarrativePhase.Build && Utility.RandomDouble() < 0.3)
                OfferBuildQuests();

            Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
            {
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile != null)
                    {
                        ns.Mobile.SendMessage(0x47, "[Town Crier] " + story);
                    }
                }
                Console.WriteLine("[AI GM] Story: " + story);
            });
        }

        private static void OfferCrisisQuests()
        {
            string[] crisisMessages =
            {
                "The guards are recruiting anyone brave enough to face the threat! Speak to a guard captain.",
                "A bounty board has been posted in town. Seek out the town crier for details.",
                "Desperate times call for desperate measures. The town elders seek adventurers.",
                "A call to arms! Report to the nearest garrison for your assignment."
            };

            string msg = crisisMessages[Utility.Random(crisisMessages.Length)];
            Timer.DelayCall(TimeSpan.FromSeconds(2), () =>
            {
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile?.Player == true && ns.Mobile.Alive)
                    {
                        ns.Mobile.SendMessage(0x22, "[Crisis] " + msg);
                    }
                }
                Console.WriteLine("[AI GM] Crisis quest prompt broadcast.");
            });
        }

        private static void OfferBuildQuests()
        {
            string[] buildMessages =
            {
                "Strange sightings reported near the old ruins. Someone should investigate...",
                "Merchants are offering coin for escort duty on the roads.",
                "Scouts are needed to survey the perimeter. Inquire at the stables.",
                "The town guard is shorthanded. Ask about guard duty at the barracks."
            };

            string msg = buildMessages[Utility.Random(buildMessages.Length)];
            Timer.DelayCall(TimeSpan.FromSeconds(2), () =>
            {
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile?.Player == true && ns.Mobile.Alive && Utility.RandomDouble() < 0.5)
                    {
                        ns.Mobile.SendMessage(0x34, "[Rumor] " + msg);
                    }
                }
                Console.WriteLine("[AI GM] Build quest rumor broadcast.");
            });
        }

        /// <summary>Get current narrative phase for other systems to query.</summary>
        public static NarrativePhase GetCurrentPhase() { return _currentPhase; }

        /// <summary>Check if the world is in a crisis state.</summary>
        public static bool IsCrisisActive() { return _currentPhase == NarrativePhase.Crisis; }

        /// <summary>Get a weather bias string based on current phase. Called by EnvironmentSubagent.</summary>
        public static string GetWeatherBias()
        {
            switch (_currentPhase)
            {
                case NarrativePhase.Calm:    return "clear";
                case NarrativePhase.Build:   return "ominous";
                case NarrativePhase.Crisis:  return "violent";
                case NarrativePhase.Resolve: return "neutral";
                default:                     return "neutral";
            }
        }

        [Usage("GMThink")]
        [Description("Force the AI Game Master to think and generate a story event.")]
        private static void OnGMThink(CommandEventArgs e)
        {
            e.Mobile.SendMessage("[AI GM] Forcing story generation...");
            MasterTick();
            e.Mobile.SendMessage("[AI GM] Story generation triggered.");
        }

        [Usage("GMStory")]
        [Description("Show the Game Master's current world state.")]
        private static void OnGMStory(CommandEventArgs e)
        {
            e.Mobile.SendMessage(0x44, "=== AI Game Master World State ===");
            var state = GatherFullWorldState();
            foreach (var line in state.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    e.Mobile.SendMessage(0x44, line.Trim());
            }
        }

        [Usage("GMPhase")]
        [Description("Show or set the current narrative phase. Usage: GMPhase [Calm|Build|Crisis|Resolve]")]
        private static void OnGMPhase(CommandEventArgs e)
        {
            if (e.Length == 0)
            {
                e.Mobile.SendMessage(0x44, "Current narrative phase: " + _currentPhase + " (tick " + _phaseDurationTicks + ")");
                return;
            }

            var phaseStr = e.GetString(0);
            try
            {
                var newPhase = (NarrativePhase)Enum.Parse(typeof(NarrativePhase), phaseStr, true);
                _currentPhase = newPhase;
                _phaseDurationTicks = 0;
                BroadcastPhaseShift();
                e.Mobile.SendMessage(0x44, "Phase set to " + newPhase);
            }
            catch
            {
                e.Mobile.SendMessage(0x22, "Invalid phase. Use: Calm, Build, Crisis, or Resolve.");
            }
        }

        [Usage("GMArcs")]
        [Description("Show all active narrative arcs.")]
        private static void OnGMArcs(CommandEventArgs e)
        {
            lock (_activeArcs)
            {
                if (_activeArcs.Count == 0)
                {
                    e.Mobile.SendMessage(0x44, "No active narrative arcs.");
                    return;
                }

                e.Mobile.SendMessage(0x44, "=== Active Narrative Arcs ===");
                foreach (var arc in _activeArcs)
                {
                    e.Mobile.SendMessage(0x44, arc.Title + " [" + arc.Phase + "] - " + arc.Description);
                }
            }
        }

        [Usage("GMHistory")]
        [Description("Show recently resolved narrative arcs.")]
        private static void OnGMHistory(CommandEventArgs e)
        {
            lock (_resolvedArcs)
            {
                if (_resolvedArcs.Count == 0)
                {
                    e.Mobile.SendMessage(0x44, "No resolved narrative arcs.");
                    return;
                }

                e.Mobile.SendMessage(0x44, "=== Resolved Arcs ===");
                foreach (var entry in _resolvedArcs)
                {
                    e.Mobile.SendMessage(0x480, entry);
                }
            }
        }

        public static void Stop()
        {
            _masterTimer?.Stop();
            _masterTimer = null;
            _activeArcs.Clear();
        }
    }
}
