using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Nemesis System — Monsters remember who killed them and return stronger.
    /// When a player kills a named/unique monster, it can come back as a Vengeful Nemesis
    /// with scaled stats, a name prefix, and relentless hunting behavior.
    /// </summary>
    public static class NemesisSystem
    {
        private static readonly Dictionary<string, NemesisEntry> PlayerNemesis =
            new Dictionary<string, NemesisEntry>(StringComparer.OrdinalIgnoreCase);

        private static readonly string SavePath = System.IO.Path.Combine(
            Core.BaseDirectory, "Saves", "AIOrchestrator", "NemesisData.bin");

        public static void Initialize()
        {
            Load();
            Console.WriteLine("[AIOrchestrator] Nemesis system initialized.");
        }

        /// <summary>Record a kill — may spawn a nemesis later.</summary>
        public static void RecordKill(Mobile killer, BaseCreature victim)
        {
            if (killer?.Player != true) return;

            var killerSer = killer.Serial.Value.ToString();
            var victimType = victim.GetType().Name;
            var victimName = victim.Name ?? victimType;

            // Only track notable monsters (named, bosses, high fame)
            if (!IsNemesisEligible(victim)) return;

            lock (PlayerNemesis)
            {
                if (!PlayerNemesis.TryGetValue(killerSer, out var entry))
                {
                    entry = new NemesisEntry { PlayerSerial = killerSer, PlayerName = killer.Name };
                    PlayerNemesis[killerSer] = entry;
                }

                var key = victimType + "|" + victimName;
                if (!entry.NemesisMonsters.TryGetValue(key, out var data))
                {
                    data = new NemesisData
                    {
                        MonsterType = victimType,
                        MonsterName = victimName,
                        FirstKill = DateTime.UtcNow
                    };
                    entry.NemesisMonsters[key] = data;
                }

                data.KillCount++;
                data.LastKill = DateTime.UtcNow;
                data.PeakLevel = Math.Max(data.PeakLevel, CalculateNemesisLevel(data));
                data.PeakFame = Math.Max(data.PeakFame, victim.Fame);

                Save();
            }
        }

        private static bool IsNemesisEligible(BaseCreature creature)
        {
            // Named NPCs, bosses, high fame, or specific types
            return !string.IsNullOrEmpty(creature.Name) &&
                   (creature.Fame >= 500 || creature is BaseChampion || creature.Name.Contains("'") || creature.Name.Contains("the"));
        }

        private static int CalculateNemesisLevel(NemesisData data)
        {
            // Level 1 at 3 kills, level 2 at 6, level 3 at 10, level 4 at 15, level 5 at 21
            if (data.KillCount >= 21) return 5;
            if (data.KillCount >= 15) return 4;
            if (data.KillCount >= 10) return 3;
            if (data.KillCount >= 6) return 2;
            if (data.KillCount >= 3) return 1;
            return 0;
        }

        private static void Save()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(SavePath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                using (var fs = new System.IO.FileStream(SavePath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                using (var writer = new System.IO.BinaryWriter(fs))
                {
                    writer.Write(PlayerNemesis.Count);
                    foreach (var kvp in PlayerNemesis)
                    {
                        var entry = kvp.Value;
                        writer.Write(entry.PlayerSerial);
                        writer.Write(entry.PlayerName);
                        writer.Write(entry.NemesisMonsters.Count);
                        foreach (var monsterKvp in entry.NemesisMonsters)
                        {
                            var data = monsterKvp.Value;
                            writer.Write(data.MonsterType);
                            writer.Write(data.MonsterName);
                            writer.Write(data.KillCount);
                            writer.Write(data.PeakLevel);
                            writer.Write(data.PeakFame);
                            writer.Write(data.FirstKill.ToBinary());
                            writer.Write(data.LastKill.ToBinary());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIOrchestrator] Nemesis save error: {ex.Message}");
            }
        }

        private static void Load()
        {
            try
            {
                if (!System.IO.File.Exists(SavePath))
                    return;

                using (var fs = new System.IO.FileStream(SavePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                using (var reader = new System.IO.BinaryReader(fs))
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var playerSer = reader.ReadString();
                        var playerName = reader.ReadString();
                        int monsterCount = reader.ReadInt32();

                        var entry = new NemesisEntry { PlayerSerial = playerSer, PlayerName = playerName };
                        for (int j = 0; j < monsterCount; j++)
                        {
                            var data = new NemesisData
                            {
                                MonsterType = reader.ReadString(),
                                MonsterName = reader.ReadString(),
                                KillCount = reader.ReadInt32(),
                                PeakLevel = reader.ReadInt32(),
                                PeakFame = reader.ReadInt32(),
                                FirstKill = DateTime.FromBinary(reader.ReadInt64()),
                                LastKill = DateTime.FromBinary(reader.ReadInt64())
                            };
                            entry.NemesisMonsters[data.MonsterType + "|" + data.MonsterName] = data;
                        }
                        PlayerNemesis[playerSer] = entry;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIOrchestrator] Nemesis load error: {ex.Message}");
            }
        }

        /// <summary>Chance to spawn a nemesis when a matching creature spawns naturally.</summary>
        public static void OnCreatureSpawn(BaseCreature creature)
        {
            // Check all players in range for nemesis matches
            var players = creature.GetMobilesInRange(24).OfType<PlayerMobile>().ToList();
            foreach (var player in players)
            {
                TrySpawnNemesis(creature, player);
            }
        }

        private static void TrySpawnNemesis(BaseCreature spawned, PlayerMobile player)
        {
            var playerSer = player.Serial.Value.ToString();
            lock (PlayerNemesis)
            {
                if (!PlayerNemesis.TryGetValue(player.Serial.Value.ToString(), out var entry))
                    return;

                var spawnedType = spawned.GetType().Name;
                var spawnedName = spawned.Name ?? "";

                foreach (var kvp in entry.NemesisMonsters)
                {
                    var data = kvp.Value;
                    if (data.PeakLevel <= 0) continue;

                    var matchType = data.MonsterType.Equals(spawnedType, StringComparison.OrdinalIgnoreCase);
                    var matchName = !string.IsNullOrEmpty(data.MonsterName) &&
                                    spawnedName.IndexOf(data.MonsterName, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!(matchType || matchName)) continue;

                    // 10% base chance per nemesis level
                    if (Utility.RandomDouble() > 0.10 * data.PeakLevel) continue;

                    // Spawn nemesis!
                    ApplyNemesisMods(spawned, data, player);
                    return;
                }
            }
        }

        /// <summary>Apply nemesis stat scaling and behavior to a spawned monster.</summary>
        public static void ApplyNemesisMods(BaseCreature creature, NemesisData nemesis, Mobile targetPlayer)
        {
            var level = nemesis.PeakLevel;

            // Scale stats
            double scale = 1.0 + (level * 0.25); // 1.25x, 1.5x, 1.75x, 2.0x, 2.25x

            creature.RawStr = (int)(creature.RawStr * scale);
            creature.RawDex = (int)(creature.RawDex * scale);
            creature.RawInt = (int)(creature.RawInt * scale);

            // Use the proper way to set max stats
            creature.SetHits((int)(creature.HitsMax * scale));
            creature.Hits = creature.HitsMax;
            // Mana/Stam scaling handled by SetStam/SetMana if needed

            // Scale damage
            creature.DamageMin = (int)(creature.DamageMin * scale);
            creature.DamageMax = (int)(creature.DamageMax * scale);

            // Scale fame/karma rewards
            creature.Fame = (int)(creature.Fame * scale);
            creature.Karma = (int)(creature.Karma * scale);

            // Nemesis naming
            var levelNames = new[] { "Vengeful", "Relentless", "Unstoppable", "Apocalyptic", "World-Ending" };
            var prefix = levelNames[Math.Min(nemesis.PeakLevel - 1, levelNames.Length - 1)];
            creature.Name = $"{prefix} {creature.Name ?? nemesis.MonsterName}";

            // Nemesis hue (reddish)
            creature.Hue = 0x485; // Blood red

            // Make them track the player relentlessly
            creature.RangePerception = 36; // Can smell player from far away
            creature.RangeFight = 12;

            // Store nemesis target using extension
            creature.SetNemesisTarget(targetPlayer);

            Console.WriteLine($"[NEMESIS] {creature.Name} spawned for {targetPlayer.Name} (Level {nemesis.PeakLevel}, {scale:P0} stats)");
        }
    }

    /// <summary>
    /// A lieutenant of a nemesis monster — stronger, named, hunts specific player.
    /// </summary>
    public class NemesisLieutenant : BaseCreature
    {
        public string OriginalMonsterType { get; set; }
        public string OriginalMonsterName { get; set; }
        public int NemesisLevel { get; set; }
        public Mobile HuntTarget { get; set; }

        public NemesisLieutenant(string monsterType, string monsterName, int nemesisLevel)
            : base(AIType.AI_Melee, FightMode.Closest, 12, 1, 0.2, 0.4)
        {
            OriginalMonsterType = monsterType;
            OriginalMonsterName = monsterName;
            NemesisLevel = nemesisLevel;

            Name = $"Lieutenant of {monsterName}";
            Hue = 0x485;
            Body = 0x190; // Generic humanoid, would be overridden per monster type
            BaseSoundID = 0x1DB;

            // Scale stats by nemesis level
            double scale = 1.0 + (nemesisLevel * 0.3);
            SetStr((int)(200 * scale), (int)(300 * scale));
            SetDex((int)(80 * scale), (int)(120 * scale));
            SetInt((int)(100 * scale), (int)(150 * scale));

            SetHits((int)(300 * scale), (int)(500 * scale));
            SetDamage((int)(15 * scale), (int)(25 * scale));

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 50, 70);
            SetResistance(ResistanceType.Fire, 40, 60);
            SetResistance(ResistanceType.Cold, 40, 60);
            SetResistance(ResistanceType.Poison, 40, 60);
            SetResistance(ResistanceType.Energy, 40, 60);

            SetSkill(SkillName.MagicResist, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 90.0, 110.0);
            SetSkill(SkillName.Wrestling, 90.0, 110.0);

            Fame = 10000 * NemesisLevel;
            Karma = -10000 * NemesisLevel;

            VirtualArmor = 50;
        }

        public override void OnThink()
        {
            base.OnThink();

            // If we have a hunt target, prioritize them
            if (HuntTarget != null && !HuntTarget.Deleted && HuntTarget.Alive)
            {
                if (InRange(HuntTarget, 36) && CanBeHarmful(HuntTarget) && InLOS(HuntTarget))
                {
                    Combatant = HuntTarget;
                    FocusMob = HuntTarget;
                }
            }
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (HuntTarget?.Player == true)
            {
                // Broadcast lieutenant death
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile?.Player == true)
                        ns.Mobile.SendMessage(0x44, $"[News] {HuntTarget.Name} has slain the Lieutenant of {OriginalMonsterName}!");
                }
            }
        }

        public NemesisLieutenant(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
            writer.Write(OriginalMonsterType);
            writer.Write(OriginalMonsterName);
            writer.Write(NemesisLevel);
            writer.Write(HuntTarget);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            OriginalMonsterType = reader.ReadString();
            OriginalMonsterName = reader.ReadString();
            NemesisLevel = reader.ReadInt();
            HuntTarget = reader.ReadMobile();
        }
    }

    #region Data Structures

    public class NemesisEntry
    {
        public string PlayerSerial { get; set; }
        public string PlayerName { get; set; }
        public Dictionary<string, NemesisData> NemesisMonsters { get; set; } = new Dictionary<string, NemesisData>();
    }

    public class NemesisData
    {
        public string MonsterType { get; set; }
        public string MonsterName { get; set; }
        public int KillCount { get; set; }
        public int PeakLevel { get; set; }
        public int PeakFame { get; set; }
        public DateTime FirstKill { get; set; }
        public DateTime LastKill { get; set; }
    }

    #endregion

    /// <summary>
    /// Hook into CreatureDeath to record nemesis kills.
    /// </summary>
    public static class NemesisHook
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
            Console.WriteLine("[AIOrchestrator] Nemesis system hooks registered.");
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer?.Player != true) return;
            if (!(e.Creature is BaseCreature bc)) return;

            NemesisSystem.RecordKill(e.Killer, bc);
        }
    }

    /// <summary>
    /// Extension for BaseCreature to hold nemesis target.
    /// </summary>
    public static class NemesisExtensions
    {
        private static readonly ConditionalWeakTable<BaseCreature, Mobile> NemesisTargets =
            new ConditionalWeakTable<BaseCreature, Mobile>();

        public static Mobile GetNemesisTarget(this BaseCreature creature)
        {
            NemesisTargets.TryGetValue(creature, out var target);
            return target;
        }

        public static void SetNemesisTarget(this BaseCreature creature, Mobile target)
        {
            NemesisTargets.Remove(creature);
            if (target != null)
                NemesisTargets.Add(creature, target);
        }
    }
}