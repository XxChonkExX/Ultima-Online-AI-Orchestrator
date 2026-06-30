using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator.Subagents
{
    public class CombatSubagent : IAIOrchestrator
    {
        public SubagentType ActiveSubagent => SubagentType.Combat;
        private readonly BaseCreature _creature;
        private readonly AIMemory _memory;

        // ── Threshold & Morale State ──────────────────────────────────
        private int _lastBarkThreshold;  // 0=none, 1=firstHit, 2=halfHp, 3=killingBlow
        private bool _hasRetreated;      // already did a retreat this fight

        private const double RetreatHpPct = 0.20;
        private const double HalfHpPct = 0.50;
        private const int AllyRetreatRange = 18;

        private static readonly Dictionary<Serial, CombatSubagent> _allInstances
            = new Dictionary<Serial, CombatSubagent>();

        public CombatSubagent(BaseCreature creature, AIMemory memory)
        {
            _creature = creature;
            _memory = memory;
            _lastBarkThreshold = 0;
            _hasRetreated = false;

            lock (_allInstances) { _allInstances[creature.Serial] = this; }
        }

        /// <summary>Static init — hooks creature death for death lines.</summary>
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeathGlobal;
            Console.WriteLine("[CombatSubagent] Death-line hook registered.");
        }

        /// <summary>Global death handler — triggers death lines + ally reaction barks.</summary>
        private static void OnCreatureDeathGlobal(CreatureDeathEventArgs e)
        {
            if (e.Creature == null) return;

            // ── Death line for the dying creature ──
            CombatSubagent subagent;
            lock (_allInstances)
            {
                if (_allInstances.TryGetValue(e.Creature.Serial, out subagent))
                {
                    _allInstances.Remove(e.Creature.Serial);
                    subagent.OnDeath(e);
                }
            }

            // ── Ally reaction barks ──
            // When a creature dies, nearby allies of the same team may react
            if (e.Creature is BaseCreature deadBC)
            {
                var nearby = e.Creature.GetMobilesInRange(AllyRetreatRange);
                foreach (Mobile mob in nearby)
                {
                    if (mob is BaseCreature allyBC && allyBC != e.Creature && allyBC.Alive &&
                        allyBC.Team > 0 && allyBC.Team == deadBC.Team)
                    {
                        CombatSubagent allySub;
                        lock (_allInstances)
                        {
                            if (_allInstances.TryGetValue(allyBC.Serial, out allySub))
                            {
                                Task.Run(() => allySub.OnAllyDeath(deadBC));
                            }
                        }
                    }
                }
                nearby.Free();
            }
        }

        /// <summary>Trigger death line when this creature dies.</summary>
        private void OnDeath(CreatureDeathEventArgs e)
        {
            if (!ShouldCombatBark()) return;

            var killer = _creature.FindMostRecentDamager(true);
            string killerName = (killer != null ? killer.Name : "something");
            string prompt = "You are dying. " + _creature.Name + " has fallen to " + killerName + ".\n" +
                            "Generate a SHORT death line (max 160 chars) — final words, curse, or scream. No actions, just speech.";

            Task.Run(async () =>
            {
                try
                {
                    var reply = await LLMClient.ChatAsync(
                        "You are an NPC breathing your last words.",
                        prompt,
                        AIConfig.ModelCombat
                    );

                    if (!string.IsNullOrEmpty(reply))
                    {
                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            if (!_creature.Deleted)
                                _creature.PublicOverheadMessage(MessageType.Regular, 0x26, false, reply);
                        });
                    }
                }
                catch { }
            });
        }

        /// <summary>Called when the creature is attacked or attacks.</summary>
        public void OnCombatStart(Mobile attacker)
        {
            if (_creature.Deleted || !_creature.Alive || attacker == null || attacker.Deleted)
                return;

            if (!ShouldCombatBark())
                return;

            // Reset threshold tracking on new combat encounter
            _lastBarkThreshold = 0;
            _hasRetreated = false;

            // First-hit bark
            _lastBarkThreshold = 1;
            GenerateThresholdBark(attacker, "firstHit");

            var allies = GetNearbyAllies();
            if (allies.Count > 0)
            {
                CoordinateWithAllies(allies, new List<Mobile> { attacker });
            }
        }

        public void OnHeartbeat(Mobile player)
        {
            if (_creature.Deleted || !_creature.Alive)
                return;

            var combatant = _creature.Combatant as Mobile;
            if (combatant == null || !combatant.Alive)
                return;

            if (!ShouldCombatBark())
                return;

            double hpPct = (double)_creature.Hits / Math.Max(1, _creature.HitsMax);
            var threats = new List<Mobile> { combatant };
            var allies = GetNearbyAllies();

            // ── Threshold barks ───────────────────────────────────────
            if (hpPct <= 0.20 && _lastBarkThreshold < 3)
            {
                _lastBarkThreshold = 3;
                GenerateThresholdBark(combatant, "killingBlow");
            }
            else if (hpPct <= 0.50 && _lastBarkThreshold < 2)
            {
                _lastBarkThreshold = 2;
                GenerateThresholdBark(combatant, "halfHp");
            }

            // ── Retreat at very low HP ─────────────────────────────────
            if (hpPct <= RetreatHpPct && !_hasRetreated)
            {
                _hasRetreated = true;
                DoRetreat(combatant);
                return; // skip normal combat bark, retreating
            }

            // ── Ally coordination ─────────────────────────────────────
            if (allies.Count > 0)
                CoordinateWithAllies(allies, threats);

            // Normal periodic bark (throttled by LLM call, ~every 5s heartbeat)
            if (Utility.RandomDouble() < 0.40)
                GenerateCombatBark(threats);
        }

        /// <summary>Called when a nearby ally dies — triggers reaction bark.</summary>
        public void OnAllyDeath(BaseCreature ally)
        {
            if (!ShouldCombatBark() || _creature.Deleted || !_creature.Alive)
                return;

            string allyName = ally.Name ?? ally.GetType().Name;
            string prompt = "Your ally " + allyName + " just died beside you.\n" +
                            "Generate a SHORT bark (max 160 chars) — rage, shock, or grief. No actions.";

            Task.Run(async () =>
            {
                try
                {
                    var reply = await LLMClient.ChatAsync(
                        "You are an NPC who just saw an ally fall in battle.",
                        prompt,
                        AIConfig.ModelCombat
                    );
                    if (!string.IsNullOrEmpty(reply))
                    {
                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            if (!_creature.Deleted && _creature.Alive)
                                _creature.PublicOverheadMessage(MessageType.Regular, 0x26, false, reply);
                        });
                    }
                }
                catch { }
            });
        }

        /// <summary>Threshold-specific bark (first hit, half HP, killing blow).</summary>
        private void GenerateThresholdBark(Mobile target, string threshold)
        {
            string targetName = target?.Name ?? "foe";
            string prompt = "Combat: " + _creature.Name + " vs " + targetName + ". ";
            prompt += "HP: " + _creature.Hits + "/" + _creature.HitsMax + ". ";

            switch (threshold)
            {
                case "firstHit":
                    prompt += "Combat JUST STARTED. Generate a taunt or battle cry (max 140 chars).";
                    break;
                case "halfHp":
                    prompt += _creature.Name + " is at HALF HEALTH and wounded. Generate a desperate roar, prayer, or curse (max 140 chars).";
                    break;
                case "killingBlow":
                    prompt += _creature.Name + " is ABOUT TO DIE! Generate a final curse, surrender, or defiant scream (max 140 chars).";
                    break;
            }

            Task.Run(async () =>
            {
                try
                {
                    var reply = await LLMClient.ChatAsync(
                        "You are an NPC in combat. Generate brief combat dialogue.",
                        prompt,
                        AIConfig.ModelCombat
                    );
                    if (!string.IsNullOrEmpty(reply))
                    {
                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            if (!_creature.Deleted && _creature.Alive)
                                _creature.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, reply);
                        });
                    }
                }
                catch { }
            });
        }

        /// <summary>Retreat — flee bark + actually flee from combat.</summary>
        private void DoRetreat(Mobile from)
        {
            string targetName = from?.Name ?? "foe";
            string prompt = _creature.Name + " is badly wounded and FLEEING from " + targetName + ".\n" +
                            "Generate a SHORT retreat bark (max 120 chars) — surrender, panic, or 'fall back!'. No actions.";

            Task.Run(async () =>
            {
                try
                {
                    var reply = await LLMClient.ChatAsync(
                        "You are a wounded NPC retreating from combat.",
                        prompt,
                        AIConfig.ModelCombat
                    );
                    if (!string.IsNullOrEmpty(reply))
                    {
                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            if (!_creature.Deleted && _creature.Alive)
                            {
                                _creature.PublicOverheadMessage(MessageType.Regular, 0x22, false, reply);
                            }
                        });
                    }
                }
                catch { }
            });

            // Actual flee logic
            Timer.DelayCall(TimeSpan.FromMilliseconds(200), () =>
            {
                if (_creature.Deleted || !_creature.Alive) return;
                _creature.Warmode = false;       // stop fighting
                _creature.Combatant = null;      // disengage
                _creature.FocusMob = null;
                _creature.Debug = false;
                _creature.Frozen = false;

                // Run away from the attacker
                if (from != null && !from.Deleted)
                {
                    Direction away = (Direction)((int)from.GetDirectionTo(_creature) & 0x7);
                    _creature.Direction = away;

                    // Move 6-10 tiles away
                    for (int i = 0; i < Utility.RandomMinMax(6, 10); i++)
                    {
                        int dx = 0, dy = 0;
                        Server.Movement.Movement.Offset(away, ref dx, ref dy);
                        int tx = _creature.X + dx;
                        int ty = _creature.Y + dy;
                        if (_creature.Map != null && _creature.Map.CanFit(tx, ty, _creature.Z, 16, false, false))
                        {
                            _creature.MoveToWorld(new Point3D(tx, ty, _creature.Z), _creature.Map);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            });
        }

        /// <summary>Returns true only for creatures that should produce combat barks.</summary>
        private bool ShouldCombatBark()
        {
            var body = _creature.Body;

            if (body.IsGhost)
                return false;

            // ── Animal detection ──────────────────────────────────────
            // Body.IsAnimal only matches BodyType.Animal, but many animals
            // in ServUO's bodyTable.cfg are classified as BodyType.Monster.
            // Use name-based + AI-based detection to be comprehensive.
            {
                var name = (_creature.Name ?? "").ToLowerInvariant();
                var typeName = _creature.GetType().Name.ToLowerInvariant();

                // Animal substrings — any creature whose name/type contains
                // any of these is an animal that should never combat-bark.
                string[] animalNames = {
                    "goat", "sheep", "pig", "cow", "bull", "hind", "hart",
                    "deer", "llama", "horse", "pony", "chicken", "rooster",
                    "hen", "bird", "finch", "crow", "raven", "eagle",
                    "falcon", "hawk", "duck", "goose", "swan",
                    "cat", "dog", "wolf", "fox", "bear", "rabbit",
                    "bunny", "squirrel", "rat", "mouse",
                    "frog", "toad", "snake", "turtle",
                    "fish", "trout", "bass", "salmon",
                    "butterfly", "moth", "bee", "wasp",
                    "boar", "grizzly", "panther", "cougar",
                    "timber wolf", "dire wolf", "brown bear",
                    "black bear", "polar bear", "gore fox",
                    "jackal", "hyena", "panther", "hind",
                    "great hart", "mountain goat",
                    "llama", "alpaca", "ostard", "frenzied ostard",
                    "desert ostard", "forest ostard", "ridgeback",
                    "savage ridgeback", "hiryu", "lesser hiryu",
                    "cu sidhe", "reptalon", "saurian",
                    "pixie", "centaur", "ethereal"
                };

                foreach (var kw in animalNames)
                    if (name.Contains(kw) || typeName.Contains(kw))
                        return false;
            }

            // AI-based animal check (catch any we missed by name)
            var ai = _creature.AI;
            switch (ai)
            {
                case AIType.AI_Animal:
                case AIType.AI_Predator:
                case AIType.AI_Vendor:
                case AIType.AI_Use_Default:
                    return false;
            }

            // Body-type-based animal check
            if (body.IsAnimal)
                return false;

            if (body.IsHuman)
                return true;

            if (body.Type == BodyType.Sea)
                return false;

            if (body.IsMonster)
            {
                var name = (_creature.Name ?? "").ToLowerInvariant();
                var typeName = _creature.GetType().Name.ToLowerInvariant();

                string[] nonSpeaking = {
                    "slime", "ooze", "pudding", "elemental", "golem",
                    "mongbat", "scorpion", "beetle", "insect", "ant",
                    "spider", "snake", "serpent", "corpse", "zombie",
                    "skeleton", "ghoul", "spectre", "shade", "phantom",
                    "wraith", "gazer", "lich",
                    // additional non-speaking monsters
                    "mummy", "wight", "bogle", "revenant", "shadow"
                };

                string[] intelligent = {
                    "orc", "lizardman", "daemon", "balron", "succubus",
                    "dragon", "wyrm", "drake",
                    "gargoyle", "ogre", "ettin", "titan", "cyclops",
                    "kraken", "lich lord", "neira", "barracoon",
                    "mephitis", "rikktor", "semonath", "exodus", "shadowlord"
                };

                foreach (var kw in nonSpeaking)
                    if (name.Contains(kw) || typeName.Contains(kw))
                        return false;

                foreach (var kw in intelligent)
                    if (name.Contains(kw) || typeName.Contains(kw))
                        return true;

                return false;
            }

            return false;
        }

        private List<Mobile> AssessThreats()
        {
            var threats = new List<Mobile>();
            foreach (var mob in _creature.GetMobilesInRange(_creature.RangePerception))
            {
                if (mob != _creature && mob.Alive && !mob.IsDeadBondedPet &&
                    _creature.CanBeHarmful(mob) && _creature.InLOS(mob))
                {
                    threats.Add(mob);
                }
            }
            return threats;
        }

        private List<BaseCreature> GetNearbyAllies()
        {
            var allies = new List<BaseCreature>();
            foreach (var mob in _creature.GetMobilesInRange(12))
            {
                if (mob is BaseCreature bc && bc != _creature && bc.Team == _creature.Team && bc.Team > 0)
                {
                    allies.Add(bc);
                }
            }
            return allies;
        }

        private void CoordinateWithAllies(List<BaseCreature> allies, List<Mobile> threats)
        {
            if (threats.Count == 0) return;

            var priorityTarget = threats[0];
            foreach (var ally in allies)
            {
                ally.Combatant = priorityTarget;
            }

            if (allies.Count >= 2)
            {
                var directions = new[] { Direction.North, Direction.East, Direction.South, Direction.West };
                for (int i = 0; i < Math.Min(allies.Count, directions.Length); i++)
                {
                    var offset = GetDirectionOffset(directions[i]);
                    var targetLoc = new Point3D(priorityTarget.X + offset.X, priorityTarget.Y + offset.Y, priorityTarget.Z);
                    allies[i].MoveToWorld(targetLoc, priorityTarget.Map);
                }
            }

            int lowHpCount = 0;
            foreach (var ally in allies)
            {
                if (ally.Hits < ally.HitsMax * 0.5)
                    lowHpCount++;
            }
            if (_creature.Hits < _creature.HitsMax * 0.5)
                lowHpCount++;

            if (lowHpCount > (allies.Count + 1) / 2)
            {
                foreach (var ally in allies)
                    ally.Warmode = true;
                _creature.Warmode = true;
            }
        }

        private void GenerateCombatBark(List<Mobile> threats)
        {
            if (threats.Count == 0) return;

            var target = threats[0];
            var prompt = "Combat situation: " + _creature.Name + " (" + _creature.GetType().Name + ") vs " + target.Name + " (" + target.GetType().Name + ").\n" +
                         "HP: " + _creature.Hits + "/" + _creature.HitsMax + ". Allies nearby: " + GetNearbyAllies().Count + ".\n" +
                         "Generate a SHORT combat bark (max 240 chars) - taunt, battle cry, or tactical call. No actions, just speech.";

            Task.Run(async () =>
            {
                var reply = await LLMClient.ChatAsync(
                    "You are an NPC in combat. Generate brief, character-appropriate combat dialogue.",
                    prompt,
                    AIConfig.ModelCombat
                );

                if (!string.IsNullOrEmpty(reply))
                {
                    Timer.DelayCall(TimeSpan.Zero, () =>
                    {
                        if (!_creature.Deleted && _creature.Alive)
                            _creature.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, reply);
                    });
                }
            });
        }

        private Point3D GetDirectionOffset(Direction dir)
        {
            int x = 0, y = 0;
            Server.Movement.Movement.Offset(dir, ref x, ref y);
            return new Point3D(x, y, 0);
        }
    }
}
