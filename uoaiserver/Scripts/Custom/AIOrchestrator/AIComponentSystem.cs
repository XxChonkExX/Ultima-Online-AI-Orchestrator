using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;
using Server.AIOrchestrator;
using Server.AIOrchestrator.Subagents;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Attachable AI component for any BaseCreature.
    /// Wraps DialogueSubagent, CombatSubagent, EnvironmentSubagent, HirelingSubagent
    /// so that ANY NPC can become AI-aware without subclassing.
    /// </summary>
    public class AIComponent
    {
        public BaseCreature Creature { get; private set; }
        public AIMemory Memory { get; private set; }
        public DialogueSubagent DialogueAI { get; private set; }
        public CombatSubagent CombatAI { get; private set; }
        public EnvironmentSubagent EnvironmentAI { get; private set; }
        public HirelingSubagent HirelingAI { get; private set; }

        public AIComponent(BaseCreature creature)
        {
            Creature = creature ?? throw new ArgumentNullException(nameof(creature));
            Memory = new AIMemory(creature.Serial.Value.ToString());
            Memory.Identity = NpcIdentityGenerator.Generate(creature);

            DialogueAI = new DialogueSubagent(creature, Memory);
            CombatAI = new CombatSubagent(creature, Memory);
            EnvironmentAI = new EnvironmentSubagent(creature, Memory);
            HirelingAI = new HirelingSubagent(creature, Memory);
        }

        public void OnHeartbeat(Mobile player)
        {
            if (Creature == null || Creature.Deleted || player == null)
                return;

            if (Creature.Combatant != null && Creature.Combatant.Alive && Creature.InRange(Creature.Combatant, Creature.RangePerception))
            {
                // Combat mode
                CombatAI?.OnHeartbeat(player);
            }
            else if (Creature.Controlled && Creature.ControlMaster == player)
            {
                // Hireling mode - follower/controlled creature
                HirelingAI?.OnHeartbeat(player);
            }
            else
            {
                // Environment mode - passive ambient behavior
                EnvironmentAI?.OnHeartbeat(player);
            }
        }

        public void OnSpeech(Mobile from, string speech)
        {
            DialogueAI?.OnSpeech(from, speech);
        }
    }

    /// <summary>
    /// Global registry of AIComponent instances.
    /// Persists across server restarts via World Save.
    /// </summary>
    public static class AIComponentRegistry
    {
        private static Dictionary<Serial, AIComponent> _components = new Dictionary<Serial, AIComponent>();
        private static bool _autoAttach = true;

        /// <summary>Whether newly-spawned creatures auto-receive AI.</summary>
        public static bool AutoAttach
        {
            get => _autoAttach;
            set
            {
                _autoAttach = value;
                Console.WriteLine($"[AIOrchestrator] AutoAttach set to {value}");
            }
        }

        public static int Count => _components.Count;

        private static Timer _autoRefreshTimer;

        public static void Configure()
        {
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;
            EventSink.MobileCreated += OnMobileCreated;

            // Auto-refresh AI on all NPCs every 30 seconds to catch dynamically-spawned creatures
            _autoRefreshTimer = Timer.DelayCall(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), AutoRefresh);

            Console.WriteLine("[AIOrchestrator] AIComponentRegistry initialized.");
            Console.WriteLine("[AIOrchestrator] Auto-refresh enabled: RegisterAll() every 3 minutes.");
        }

        private static void AutoRefresh()
        {
            try
            {
                int count = RegisterAll();
                if (count > 0)
                    Console.WriteLine($"[AIOrchestrator] Auto-refresh: Registered {count} new NPC(s). Total: {_components.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIOrchestrator] Auto-refresh error: {ex.Message}");
            }
        }

        /// <summary>Returns the AIComponent for a creature, or null if not AI-enabled.</summary>
        public static AIComponent GetComponent(BaseCreature creature)
        {
            if (creature == null || creature.Deleted)
                return null;

            _components.TryGetValue(creature.Serial, out var component);
            return component;
        }

        /// <summary>Returns true if the creature has an active AIComponent.</summary>
        public static bool HasAI(BaseCreature creature)
        {
            return creature != null && !creature.Deleted && _components.ContainsKey(creature.Serial);
        }

        /// <summary>Registers a creature for AI processing. Returns the existing or new component.</summary>
        public static AIComponent Register(BaseCreature creature)
        {
            if (creature == null || creature.Deleted)
                return null;

            // Already registered
            if (_components.TryGetValue(creature.Serial, out var existing))
                return existing;

            // Don't register other players or already-AI creatures
            if (creature is PlayerMobile)
                return null;

            var component = new AIComponent(creature);
            _components[creature.Serial] = component;
            return component;
        }

        /// <summary>Removes AI from a creature.</summary>
        public static void Unregister(Serial serial)
        {
            _components.Remove(serial);
        }

        /// <summary>Adds AI to every BaseCreature in the world.</summary>
        /// <returns>Number of creatures registered.</returns>
        public static int RegisterAll()
        {
            int count = 0;
            var toRegister = new List<BaseCreature>();

            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile is BaseCreature bc && !(bc is PlayerMobile) && !_components.ContainsKey(bc.Serial))
                {
                    toRegister.Add(bc);
                }
            }

            foreach (var bc in toRegister)
            {
                var component = new AIComponent(bc);
                _components[bc.Serial] = component;
                count++;
            }

            return count;
        }

        /// <summary>Removes AI from all creatures.</summary>
        public static void UnregisterAll()
        {
            _components.Clear();
        }

        /// <summary>Adds AI to creatures within range of a player.</summary>
        public static int RegisterNear(Mobile from, int range = 20)
        {
            int count = 0;
            var npcs = from.GetMobilesInRange(range);
            foreach (var npc in npcs)
            {
                if (npc is BaseCreature bc && !(bc is PlayerMobile) && !_components.ContainsKey(bc.Serial))
                {
                    var component = new AIComponent(bc);
                    _components[bc.Serial] = component;
                    count++;
                }
            }
            return count;
        }

        /// <summary>Returns all AI-enabled creatures for diagnostics.</summary>
        public static IEnumerable<KeyValuePair<Serial, AIComponent>> AllComponents => _components;

        // ─── Persistence ───────────────────────────────────────────────

        private static readonly string SavePath = Path.Combine(
            Core.BaseDirectory, "Saves", "AIOrchestrator", "AIComponentRegistry.bin"
        );

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            try
            {
                var dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var fs = new FileStream(SavePath, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(fs))
                {
                    writer.Write(_autoAttach);
                    writer.Write(_components.Count);

                    foreach (var kvp in _components)
                    {
                        var serial = kvp.Key;
                        var component = kvp.Value;

                        writer.Write(serial.Value);

                        var identity = component.Memory?.Identity;
                        writer.Write(identity?.Name ?? component.Creature?.Name ?? "Unknown");
                        writer.Write(identity?.Vocation ?? "");
                        writer.Write(identity?.Homeland ?? "");
                        writer.Write(identity?.Temperament ?? "");
                        writer.Write(identity?.Backstory ?? "");
                        writer.Write(identity?.SpeechStyle ?? "");
                        writer.Write(identity?.PrivateDrive ?? "");
                        writer.Write(identity?.Mood ?? 50);
                    }
                }

                Console.WriteLine($"[AIOrchestrator] Registry saved: {_components.Count} components.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIOrchestrator] Registry save error: {ex.Message}");
            }
        }

        private static void OnWorldLoad()
        {
            _components.Clear();

            try
            {
                if (!File.Exists(SavePath))
                {
                    Console.WriteLine("[AIOrchestrator] No registry save found. AI components will be attached on-demand.");
                    return;
                }

                using (var fs = new FileStream(SavePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs))
                {
                    _autoAttach = reader.ReadBoolean();
                    int count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        var serialValue = reader.ReadUInt32();
                        var serial = (Serial)(int)serialValue;

                        var name = reader.ReadString();
                        var vocation = reader.ReadString();
                        var homeland = reader.ReadString();
                        var temperament = reader.ReadString();
                        var backstory = reader.ReadString();
                        var speechStyle = reader.ReadString();
                        var privateDrive = reader.ReadString();
                        var mood = reader.ReadInt32();

                        var entity = World.FindMobile(serial);
                        if (entity is BaseCreature creature && !creature.Deleted)
                        {
                            var component = new AIComponent(creature);
                            component.Memory.Identity = new NpcIdentity
                            {
                                Name = name,
                                Vocation = vocation,
                                Homeland = homeland,
                                Temperament = temperament,
                                Backstory = backstory,
                                SpeechStyle = speechStyle,
                                PrivateDrive = privateDrive,
                                Mood = mood
                            };
                            _components[serial] = component;
                        }
                    }

                    Console.WriteLine($"[AIOrchestrator] Registry loaded: {_components.Count} components.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIOrchestrator] Registry load error: {ex.Message}");
                _components.Clear();
            }
        }

        // ─── Auto-Attach on MobileCreated ─────────────────────────────

        private static void OnMobileCreated(MobileCreatedEventArgs e)
        {
            if (!AIConfig.Enabled || !_autoAttach)
                return;

            if (e.Mobile is BaseCreature creature && !(creature is PlayerMobile))
            {
                // Already has AI via subclass or registry
                if (creature is AIBaseCreature || _components.ContainsKey(creature.Serial))
                    return;

                // Attach AI to every creature — monsters, animals, town NPCs, everything
                var component = new AIComponent(creature);
                _components[creature.Serial] = component;
            }
        }
    }
}
