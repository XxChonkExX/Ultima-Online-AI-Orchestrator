using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.AIOrchestrator.Subagents;
using Server.Regions;

namespace Server.AIOrchestrator
{
    // ══════════════════════════════════════════════════════════════════
    //  CREATURE VARIANTS — Spellcasters, Bowman, Paladin, Tamer
    //  & Lesser/Greater versions of existing creatures
    // ══════════════════════════════════════════════════════════════════

    #region Orc Variants

    /// <summary>Orc Spellcaster — uses magery instead of melee</summary>
    public class OrcShaman : BaseCreature
    {
        [Constructable]
        public OrcShaman() : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "an orc shaman";
            Body = 17; // Orc body
            BaseSoundID = 0x45A;
            Hue = 0;

            SetStr(80, 110);
            SetDex(50, 70);
            SetInt(80, 120);

            SetHits(100, 150);
            SetDamage(5, 12);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Fire, 30);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 30, 50);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Magery, 60, 90);
            SetSkill(SkillName.EvalInt, 60, 90);
            SetSkill(SkillName.Meditation, 50, 80);
            SetSkill(SkillName.MagicResist, 50, 80);
            SetSkill(SkillName.Tactics, 40, 60);
            SetSkill(SkillName.Wrestling, 40, 60);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 28;

            PackItem(new Robe(0x22)); // Red robe
            PackItem(new WizardsHat(0x22));
            PackReg(20);

            if (Utility.RandomDouble() < 0.5)
                PackItem(new BoneHelm());
        }

        public override bool CanRummageCorpses { get { return true; } }

        public override void GenerateLoot() { VariantLoot.GenerateShamanLoot(this); }

        public OrcShaman(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    /// <summary>Orc Bowman — uses archery</summary>
    public class OrcArcher : BaseCreature
    {
        [Constructable]
        public OrcArcher() : base(AIType.AI_Archer, FightMode.Closest, 12, 1, 0.2, 0.4)
        {
            Name = "an orc archer";
            Body = 17;
            BaseSoundID = 0x45A;

            SetStr(70, 100);
            SetDex(70, 100);
            SetInt(30, 50);

            SetHits(80, 130);
            SetDamage(6, 14);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.Archery, 70, 100);
            SetSkill(SkillName.Tactics, 60, 90);
            SetSkill(SkillName.MagicResist, 30, 50);
            SetSkill(SkillName.Anatomy, 40, 60);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 20;

            AddItem(new Bow());
            PackItem(new Arrow(50));
            AddItem(new LeatherChest());
            AddItem(new LeatherArms());
            AddItem(new LeatherLegs());
        }

        public override void GenerateLoot() { VariantLoot.GenerateArcherLoot(this); }

        public OrcArcher(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Orc Paladin — heavy armor and chivalry</summary>
    public class OrcKnight : BaseCreature
    {
        [Constructable]
        public OrcKnight() : base(AIType.AI_Paladin, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "an orc knight";
            Body = 17;
            BaseSoundID = 0x45A;

            SetStr(120, 160);
            SetDex(50, 70);
            SetInt(40, 60);

            SetHits(150, 220);
            SetDamage(12, 20);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Fire, 20);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Swords, 80, 110);
            SetSkill(SkillName.Tactics, 80, 110);
            SetSkill(SkillName.Chivalry, 40, 70);
            SetSkill(SkillName.MagicResist, 50, 70);
            SetSkill(SkillName.Parry, 60, 80);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 40;

            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateLegs());
            AddItem(new PlateGloves());
            AddItem(new PlateHelm());
            AddItem(new VikingSword());
            AddItem(new MetalShield());
            AddItem(new Cloak(0x22));
        }

        public override void GenerateLoot() { VariantLoot.GenerateKnightLoot(this); }

        public OrcKnight(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Orc Beastmaster — controls animals in battle</summary>
    public class OrcBeastmaster : BaseCreature
    {
        private Timer m_SummonTimer;

        [Constructable]
        public OrcBeastmaster() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "an orc beastmaster";
            Body = 17;
            BaseSoundID = 0x45A;

            SetStr(90, 120);
            SetDex(60, 80);
            SetInt(50, 70);

            SetHits(100, 150);
            SetDamage(7, 14);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Tactics, 60, 90);
            SetSkill(SkillName.Wrestling, 60, 90);
            SetSkill(SkillName.AnimalTaming, 70, 100);
            SetSkill(SkillName.AnimalLore, 60, 90);
            SetSkill(SkillName.Veterinary, 50, 80);

            Fame = 3500;
            Karma = -3000;

            VirtualArmor = 25;

            AddItem(new StuddedChest());
            AddItem(new StuddedArms());
            AddItem(new StuddedLegs());
            AddItem(new ShepherdsCrook());

            // Start summoning timer
            m_SummonTimer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), SummonAnimal);
        }

        private void SummonAnimal()
        {
            if (Deleted || !Alive || Combatant == null)
                return;

            BaseCreature pet;
            switch (Utility.Random(5))
            {
                case 0: pet = new BrownBear(); break;
                case 1: pet = new TimberWolf(); break;
                case 2: pet = new Snake(); break;
                default: pet = new Mongbat(); break;
            }

            var loc = new Point3D(X + Utility.RandomMinMax(-2, 2), Y + Utility.RandomMinMax(-2, 2), Z);
            pet.MoveToWorld(loc, Map);
            pet.Controlled = true;
            pet.ControlMaster = this;
            pet.ControlOrder = OrderType.Attack;
            pet.Combatant = Combatant;
        }

        public OrcBeastmaster(Serial serial) : base(serial) { }
        public override void GenerateLoot() { VariantLoot.GenerateBeastmasterLoot(this); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); m_SummonTimer = Timer.DelayCall(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), SummonAnimal); }
    }

    #endregion

    #region Lizardman Variants

    /// <summary>Lizardman Shaman — tribal spellcaster</summary>
    public class LizardmanShaman : BaseCreature
    {
        [Constructable]
        public LizardmanShaman() : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a lizardman shaman";
            Body = Utility.RandomList(35, 36);
            BaseSoundID = 0x287;

            SetStr(70, 100);
            SetDex(50, 70);
            SetInt(80, 120);

            SetHits(80, 130);
            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Poison, 50);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 40, 60);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Magery, 60, 90);
            SetSkill(SkillName.EvalInt, 60, 90);
            SetSkill(SkillName.Meditation, 50, 70);
            SetSkill(SkillName.MagicResist, 50, 80);
            SetSkill(SkillName.Poisoning, 60, 90);
            SetSkill(SkillName.Wrestling, 40, 60);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 25;

            PackItem(new BoneHelm());
            PackItem(new Robe(Utility.RandomGreenHue()));
            PackReg(15);
        }

        public LizardmanShaman(Serial serial) : base(serial) { }
        public override void GenerateLoot() { VariantLoot.GenerateShamanLoot(this); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Lizardman Sniper — poisoned archer</summary>
    public class LizardmanSniper : BaseCreature
    {
        [Constructable]
        public LizardmanSniper() : base(AIType.AI_Archer, FightMode.Closest, 15, 1, 0.2, 0.4)
        {
            Name = "a lizardman sniper";
            Body = Utility.RandomList(35, 36);
            BaseSoundID = 0x287;

            SetStr(60, 90);
            SetDex(80, 110);
            SetInt(30, 50);

            SetHits(70, 120);
            SetDamage(8, 16);

            SetResistance(ResistanceType.Physical, 20, 30);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 50, 70);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.Archery, 80, 110);
            SetSkill(SkillName.Tactics, 70, 100);
            SetSkill(SkillName.Poisoning, 60, 90);
            SetSkill(SkillName.Hiding, 50, 80);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 18;

            AddItem(new Bow());
            PackItem(new Arrow(50));
            AddItem(new LeatherChest());
            AddItem(new LeatherLegs());
        }

        public LizardmanSniper(Serial serial) : base(serial) { }
        public override void GenerateLoot() { VariantLoot.GenerateArcherLoot(this); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    #endregion

    #region Troll Variants

    /// <summary>Troll Witchdoctor — giant tribal mage</summary>
    public class TrollWitchdoctor : BaseCreature
    {
        [Constructable]
        public TrollWitchdoctor() : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a troll witchdoctor";
            Body = 54; // Troll body
            BaseSoundID = 0x4E2;

            SetStr(120, 160);
            SetDex(40, 60);
            SetInt(60, 100);

            SetHits(150, 220);
            SetDamage(8, 16);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Fire, 40);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 30, 50);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Magery, 50, 80);
            SetSkill(SkillName.EvalInt, 50, 80);
            SetSkill(SkillName.Meditation, 40, 60);
            SetSkill(SkillName.MagicResist, 50, 70);
            SetSkill(SkillName.Tactics, 50, 70);
            SetSkill(SkillName.Wrestling, 60, 80);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 30;

            PackItem(new GnarledStaff());
            PackReg(15);
        }

        public TrollWitchdoctor(Serial serial) : base(serial) { }
        public override void GenerateLoot() { VariantLoot.GenerateWitchdoctorLoot(this); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    #endregion

    #region Undead Variants

    /// <summary>Skeletal Mage — spellcasting skeleton</summary>
    public class SkeletalMage : BaseCreature
    {
        [Constructable]
        public SkeletalMage() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a skeletal mage";
            Body = 50; // Skeleton body
            BaseSoundID = 0x48D;

            SetStr(60, 90);
            SetDex(50, 70);
            SetInt(80, 120);

            SetHits(80, 130);
            SetDamage(6, 12);

            SetDamageType(ResistanceType.Physical, 30);
            SetDamageType(ResistanceType.Cold, 70);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 50, 70);
            SetResistance(ResistanceType.Poison, 40, 60);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Necromancy, 60, 90);
            SetSkill(SkillName.SpiritSpeak, 60, 90);
            SetSkill(SkillName.Magery, 50, 80);
            SetSkill(SkillName.EvalInt, 50, 80);
            SetSkill(SkillName.Meditation, 50, 70);
            SetSkill(SkillName.MagicResist, 50, 80);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 25;

            AddItem(new Robe(0x1)); // Black
            AddItem(new WizardsHat());
            AddItem(new Spellbook());
            PackReg(15);
        }

        public SkeletalMage(Serial serial) : base(serial) { }
        public override void GenerateLoot() { VariantLoot.GenerateSkeletalMageLoot(this); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Skeletal Archer — bone bowman</summary>
    public class SkeletalArcher : BaseCreature
    {
        [Constructable]
        public SkeletalArcher() : base(AIType.AI_Archer, FightMode.Closest, 12, 1, 0.2, 0.4)
        {
            Name = "a skeletal archer";
            Body = 50;
            BaseSoundID = 0x48D;

            SetStr(60, 90);
            SetDex(70, 100);
            SetInt(30, 50);

            SetHits(70, 120);
            SetDamage(8, 15);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Cold, 20);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 40, 60);
            SetResistance(ResistanceType.Poison, 30, 50);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.Archery, 70, 100);
            SetSkill(SkillName.Tactics, 60, 90);
            SetSkill(SkillName.MagicResist, 30, 50);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 20;

            AddItem(new Bow());
            PackItem(new Arrow(50));
        }

        public SkeletalArcher(Serial serial) : base(serial) { }
        public override void GenerateLoot() { VariantLoot.GenerateSkeletalArcherLoot(this); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    #endregion

    #region Lesser Creature Versions

    /// <summary>Lesser Dragon — weaker, lower-level dragon</summary>
    public class LesserDragon : BaseCreature
    {
        [Constructable]
        public LesserDragon() : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a lesser dragon";
            Body = Utility.RandomList(12, 59);
            BaseSoundID = 0x16A;

            SetStr(200, 280);
            SetDex(80, 110);
            SetInt(60, 90);

            SetHits(180, 250);
            SetDamage(10, 18);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Fire, 40);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Magery, 50, 70);
            SetSkill(SkillName.EvalInt, 50, 70);
            SetSkill(SkillName.Meditation, 40, 60);
            SetSkill(SkillName.MagicResist, 50, 70);
            SetSkill(SkillName.Tactics, 60, 80);
            SetSkill(SkillName.Wrestling, 60, 80);

            Fame = 6000;
            Karma = -6000;

            VirtualArmor = 30;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 85.0;
        }

        public override int TreasureMapLevel { get { return 2; } }

        public override void GenerateLoot() { VariantLoot.GenerateLesserDragonLoot(this); }

        public LesserDragon(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Lesser Daemon — weaker daemon variant</summary>
    public class LesserDaemon : BaseCreature
    {
        [Constructable]
        public LesserDaemon() : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a lesser daemon";
            Body = 9;
            BaseSoundID = 0x174;

            SetStr(80, 120);
            SetDex(60, 90);
            SetInt(70, 110);

            SetHits(100, 150);
            SetDamage(8, 16);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Fire, 60);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Magery, 50, 80);
            SetSkill(SkillName.EvalInt, 50, 80);
            SetSkill(SkillName.Meditation, 40, 60);
            SetSkill(SkillName.MagicResist, 40, 70);
            SetSkill(SkillName.Tactics, 40, 60);
            SetSkill(SkillName.Wrestling, 50, 70);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 24;

            Tamable = false;
        }

        public override void GenerateLoot() { VariantLoot.GenerateLesserDaemonLoot(this); }

        public LesserDaemon(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Lesser Orc — weakest orc variant</summary>
    public class LesserOrc : BaseCreature
    {
        [Constructable]
        public LesserOrc() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a lesser orc";
            Body = 17;
            BaseSoundID = 0x45A;

            SetStr(40, 60);
            SetDex(30, 50);
            SetInt(15, 25);

            SetHits(40, 70);
            SetDamage(3, 8);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Cold, 10, 15);
            SetResistance(ResistanceType.Poison, 10, 15);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.Tactics, 25, 45);
            SetSkill(SkillName.Wrestling, 25, 45);

            Fame = 500;
            Karma = -500;

            VirtualArmor = 12;

            AddItem(new ShortSpear());
            AddItem(new Boots());
        }

        public LesserOrc(Serial serial) : base(serial) { }
        public override void GenerateLoot() { PackGold(20, 60); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    #endregion

    #region Greater Creature Versions

    /// <summary>Greater Orc — very strong orc</summary>
    public class GreaterOrc : BaseCreature
    {
        [Constructable]
        public GreaterOrc() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a greater orc";
            Body = 17;
            BaseSoundID = 0x45A;

            SetStr(150, 200);
            SetDex(60, 80);
            SetInt(40, 60);

            SetHits(180, 280);
            SetDamage(14, 24);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Fire, 20);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 35, 45);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.Swords, 90, 120);
            SetSkill(SkillName.Tactics, 90, 120);
            SetSkill(SkillName.MagicResist, 50, 70);
            SetSkill(SkillName.Parry, 60, 80);

            Fame = 8000;
            Karma = -8000;

            VirtualArmor = 40;

            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateLegs());
            AddItem(new PlateHelm());
            AddItem(new VikingSword());
            AddItem(new MetalShield());
        }

        public GreaterOrc(Serial serial) : base(serial) { }
        public override void GenerateLoot() { AddLoot(LootPack.Rich); VariantLoot.GenerateKnightLoot(this); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Greater Troll — huge troll variant</summary>
    public class GreaterTroll : BaseCreature
    {
        [Constructable]
        public GreaterTroll() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a greater troll";
            Body = 54;
            BaseSoundID = 0x4E2;

            SetStr(200, 280);
            SetDex(50, 70);
            SetInt(30, 50);

            SetHits(250, 400);
            SetDamage(18, 30);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Tactics, 90, 120);
            SetSkill(SkillName.Wrestling, 90, 120);
            SetSkill(SkillName.MagicResist, 40, 60);

            Fame = 10000;
            Karma = -10000;

            VirtualArmor = 45;

            AddItem(new Club());
        }

        public override int TreasureMapLevel { get { return 3; } }

        public override void GenerateLoot() { AddLoot(LootPack.Rich); VariantLoot.GenerateEliteLoot(this); }

        public GreaterTroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Greater Skeleton — elite undead warrior</summary>
    public class GreaterSkeleton : BaseCreature
    {
        [Constructable]
        public GreaterSkeleton() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a greater skeleton";
            Body = 50;
            BaseSoundID = 0x48D;

            SetStr(120, 160);
            SetDex(60, 90);
            SetInt(40, 60);

            SetHits(150, 250);
            SetDamage(12, 22);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Cold, 40);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 60, 80);
            SetResistance(ResistanceType.Poison, 50, 70);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.Swords, 90, 120);
            SetSkill(SkillName.Tactics, 80, 110);
            SetSkill(SkillName.MagicResist, 50, 70);
            SetSkill(SkillName.Parry, 50, 70);

            Fame = 6000;
            Karma = -6000;

            VirtualArmor = 35;

            AddItem(new BoneHelm());
            AddItem(new Longsword());
            AddItem(new MetalShield());
        }

        public GreaterSkeleton(Serial serial) : base(serial) { }
        public override void GenerateLoot() { AddLoot(LootPack.Rich); AddLoot(LootPack.MedScrolls); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    #endregion

    #region Spawner for Creature Variants

    /// <summary>
    /// Spawner for creature variants that repopulates an area with 
    /// themed creatures (spellcasters, bowmen, etc.)
    /// Places spawners in the world via [add command.
    /// </summary>
    public class CreatureVariantSpawner : Item
    {
        public enum VariantTheme
        {
            OrcCamp,         // Orcs + OrcShaman + OrcArcher + OrcKnight + OrcBeastmaster
            LizardmanNest,   // Lizardmen + LizardmanShaman + LizardmanSniper
            TrollCave,       // Trolls + TrollWitchdoctor
            UndeadCrypt,     // Skeletons + SkeletalMage + SkeletalArcher + Zombies
            LesserDungeon,   // LesserOrc + LesserDragon + LesserDaemon
            GreaterThreat    // GreaterOrc + GreaterTroll + GreaterSkeleton + Dragons
        }

        private VariantTheme m_Theme;
        private int m_SpawnRange;
        private int m_MaxCount;
        private Timer m_Timer;
        private List<Mobile> m_Spawned = new List<Mobile>();

        [CommandProperty(AccessLevel.GameMaster)]
        public VariantTheme Theme
        {
            get { return m_Theme; }
            set { m_Theme = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnRange
        {
            get { return m_SpawnRange; }
            set { m_SpawnRange = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxCount
        {
            get { return m_MaxCount; }
            set { m_MaxCount = value; }
        }

        [Constructable]
        public CreatureVariantSpawner() : this((VariantTheme)Utility.Random(6))
        {
        }

        [Constructable]
        public CreatureVariantSpawner(VariantTheme theme) : base(0x1F1C)
        {
            Name = "Creature Variant Spawner";
            Movable = false;
            Visible = false;
            m_Theme = theme;
            m_SpawnRange = 20;
            m_MaxCount = 5;
            StartTimer();
        }

        private Mobile CreateCreature()
        {
            switch (m_Theme)
            {
                case VariantTheme.OrcCamp:
                    if (Utility.RandomDouble() < 0.05) return new OrcWarlord(); // 5% elite boss
                    switch (Utility.Random(8))
                    {
                        case 0: return new Orc();
                        case 1: return new OrcishLord();
                        case 2: return new OrcShaman();
                        case 3: return new OrcArcher();
                        case 4: return new OrcKnight();
                        case 5: return new OrcBeastmaster();
                        case 6: return new LesserOrc();
                        default: return new Orc();
                    }

                case VariantTheme.LizardmanNest:
                    if (Utility.RandomDouble() < 0.05) return new LizardmanHighPriest();
                    switch (Utility.Random(5))
                    {
                        case 0: return new Lizardman();
                        case 1: return new Lizardman();
                        case 2: return new LizardmanShaman();
                        case 3: return new LizardmanSniper();
                        default: return new Lizardman();
                    }

                case VariantTheme.TrollCave:
                    if (Utility.RandomDouble() < 0.05) return new TrollChieftain();
                    switch (Utility.Random(5))
                    {
                        case 0: return new Troll();
                        case 1: return new Troll();
                        case 2: return new TrollWitchdoctor();
                        case 3: return new GreaterTroll();
                        default: return new Troll();
                    }

                case VariantTheme.UndeadCrypt:
                    if (Utility.RandomDouble() < 0.05) return new SkeletalLich();
                    switch (Utility.Random(8))
                    {
                        case 0: return new Skeleton();
                        case 1: return new Skeleton();
                        case 2: return new Zombie();
                        case 3: return new SkeletalMage();
                        case 4: return new SkeletalArcher();
                        case 5: return new GreaterSkeleton();
                        case 6: return new Wraith();
                        default: return new Skeleton();
                    }

                case VariantTheme.LesserDungeon:
                    switch (Utility.Random(5))
                    {
                        case 0: return new LesserOrc();
                        case 1: return new LesserDragon();
                        case 2: return new LesserDaemon();
                        case 3: return new LesserOrc();
                        default: return new LesserDragon();
                    }

                case VariantTheme.GreaterThreat:
                    if (Utility.RandomDouble() < 0.08) // 8% elite boss for Greater threat
                    {
                        switch (Utility.Random(4))
                        {
                            case 0: return new OrcWarlord();
                            case 1: return new TrollChieftain();
                            case 2: return new SkeletalLich();
                            default: return new LizardmanHighPriest();
                        }
                    }
                    switch (Utility.Random(6))
                    {
                        case 0: return new GreaterOrc();
                        case 1: return new GreaterTroll();
                        case 2: return new GreaterSkeleton();
                        case 3: return new Dragon();
                        case 4: return new Daemon();
                        default: return new GreaterOrc();
                    }

                default:
                    return new Orc();
            }
        }

        private void StartTimer()
        {
            if (m_Timer == null)
                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), CheckSpawn);
        }

        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private void CheckSpawn()
        {
            if (Deleted || Map == null || Map == Map.Internal)
                return;

            m_Spawned.RemoveAll(m => m == null || m.Deleted);

            // Integrate with AI Spawn Controller
            string regionName = Region.Find(Location, Map)?.Name ?? "unknown";

            // Check if this theme is suppressed in the region
            string themeStr = m_Theme.ToString().ToLowerInvariant();
            if (SpawnControllerSubagent.IsSuppressed(themeStr, regionName))
            {
                // Theme is suppressed — don't spawn more, gradually despawn existing
                if (m_Spawned.Count > 0 && Utility.RandomDouble() < 0.2)
                {
                    var toRemove = m_Spawned[0];
                    m_Spawned.RemoveAt(0);
                    if (toRemove != null && !toRemove.Deleted)
                        toRemove.Delete();
                }
                return;
            }

            // Get spawn multiplier from AI controller
            double multiplier = SpawnControllerSubagent.GetSpawnMultiplier(regionName);
            int adjustedMax = Math.Max(1, (int)(m_MaxCount * multiplier));

            int toSpawn = adjustedMax - m_Spawned.Count;
            if (toSpawn <= 0) return;

            toSpawn = Math.Min(toSpawn, Math.Max(1, (int)(toSpawn * multiplier)));

            for (int i = 0; i < toSpawn; i++)
            {
                var creature = CreateCreature();
                var loc = GetSpawnLocation();
                if (loc != Point3D.Zero)
                {
                    creature.MoveToWorld(loc, Map);

                    // Empower creatures if the AI directive says so
                    if (SpawnControllerSubagent.IsEmpowered(themeStr, regionName) && creature is BaseCreature empowered)
                    {
                        // Give them bonus stats
                        empowered.RawStr = (int)(empowered.RawStr * 1.3);
                        empowered.RawDex = (int)(empowered.RawDex * 1.2);
                        empowered.RawInt = (int)(empowered.RawInt * 1.2);
                        empowered.Hits = empowered.HitsMax;
                        empowered.Mana = empowered.ManaMax;
                        empowered.Stam = empowered.StamMax;
                        empowered.DamageMin = (int)(empowered.DamageMin * 1.3);
                        empowered.DamageMax = (int)(empowered.DamageMax * 1.3);
                    }

                    m_Spawned.Add(creature);
                }
                else
                    creature.Delete();
            }
        }

        private Point3D GetSpawnLocation()
        {
            for (int i = 0; i < 20; i++)
            {
                var x = X + Utility.RandomMinMax(-m_SpawnRange, m_SpawnRange);
                var y = Y + Utility.RandomMinMax(-m_SpawnRange, m_SpawnRange);
                var z = Map.GetAverageZ(x, y);
                var loc = new Point3D(x, y, z);
                if (Map.CanSpawnMobile(loc))
                    return loc;
            }
            return Point3D.Zero;
        }

        public override void OnDelete()
        {
            StopTimer();
            foreach (var m in m_Spawned)
                if (m != null && !m.Deleted)
                    m.Delete();
            m_Spawned.Clear();
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write((int)m_Theme);
            writer.Write(m_SpawnRange);
            writer.Write(m_MaxCount);
            writer.Write(m_Spawned.Count);
            foreach (var m in m_Spawned)
                writer.Write(m);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Theme = (VariantTheme)reader.ReadInt();
            m_SpawnRange = reader.ReadInt();
            m_MaxCount = reader.ReadInt();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var m = reader.ReadMobile();
                if (m != null)
                    m_Spawned.Add(m);
            }
            StartTimer();
        }

        public CreatureVariantSpawner(Serial serial) : base(serial) { }
    }

    #endregion

    #region Tactical AI Overrides — Spell/Target prioritisation, distance, ability cycling

    /// <summary>Tactical spell priorities for AI_Mage variants — heal when hurt, curse before nuke</summary>
    public abstract class TacticalMageBase : BaseCreature
    {
        protected TacticalMageBase(AIType ai, FightMode mode, int scanRange, int maxRange, double activeSpeed, double passiveSpeed)
            : base(ai, mode, scanRange, maxRange, activeSpeed, passiveSpeed) { }

        public override void OnThink()
        {
            base.OnThink();
            if (Alive && Combatant != null && Utility.RandomDouble() < 0.15)
                PerformTacticalAction();
        }

        protected virtual void PerformTacticalAction()
        {
            // Heal self if below 40% HP
            if (Hits < HitsMax * 0.4 && Skills.Magery.Value >= 50)
            {
                // Use Greater Heal via spell casting — the AI_Mage handles this,
                // but we force a higher priority here.
                if (Utility.RandomBool())
                {
                    // Cast healing spell manually
                    CastHeal();
                    return;
                }
            }

            // Curse debuff before damage
            if (Combatant != null && Combatant is Mobile enemy)
            {
                if (enemy.Hits > enemy.HitsMax * 0.5 && Utility.RandomDouble() < 0.3)
                {
                    CastCurse(enemy);
                }
            }
        }

        private void CastHeal()
        {
            if (Skills.Magery.Value >= 70)
            {
                // Use GreaterHeal spell
                var spell = new Server.Spells.Fourth.GreaterHealSpell(this, null);
                spell.Cast();
            }
            else
            {
                var spell = new Server.Spells.Second.CureSpell(this, null);
                spell.Cast();
            }
        }

        private void CastCurse(Mobile target)
        {
            if (target == null) return;
            // Weaken, Clumsy, Feeblemind — debuff
            var spells = new Server.Spells.Spell[]
            {
                new Server.Spells.First.WeakenSpell(this, null),
                new Server.Spells.First.ClumsySpell(this, null),
                new Server.Spells.First.FeeblemindSpell(this, null)
            };
            var selected = spells[Utility.Random(spells.Length)];
            selected.Cast();
        }

        protected TacticalMageBase(Serial serial) : base(serial) { }
    }

    /// <summary>Tactical archer base — maintain distance, retreat when overwhelmed</summary>
    public abstract class TacticalArcherBase : BaseCreature
    {
        protected TacticalArcherBase(AIType ai, FightMode mode, int scanRange, int maxRange, double activeSpeed, double passiveSpeed)
            : base(ai, mode, scanRange, maxRange, activeSpeed, passiveSpeed) { }

        public override void OnThink()
        {
            base.OnThink();
            if (!Alive || Combatant == null) return;

            // Maintain distance (kite)
            if (Combatant is Mobile enemy && enemy.GetDistanceToSqrt(this) < 3)
            {
                // Move away
                var awayDir = (Direction)(((int)GetDirectionTo(enemy) + 4) % 8);
                Direction = awayDir;
                if (Utility.RandomDouble() < 0.5)
                    CurrentSpeed = ActiveSpeed; // Move faster (run)
            }
        }

        protected TacticalArcherBase(Serial serial) : base(serial) { }
    }

    #endregion

    #region Custom Loot Tables for Variants

    /// <summary>Provides themed loot generation for creature variants.</summary>
    public static class VariantLoot
    {
        public static void GenerateShamanLoot(BaseCreature creature)
        {
            creature.PackGold(50, 150);
            creature.PackReg(Utility.RandomMinMax(5, 20));
            if (Utility.RandomDouble() < 0.3) creature.AddItem(new Robe(Utility.RandomNondyedHue()));
            if (Utility.RandomDouble() < 0.2) creature.PackItem(new MagicArrowScroll());
            if (Utility.RandomDouble() < 0.1) creature.PackItem(new GreaterHealScroll());
        }

        public static void GenerateArcherLoot(BaseCreature creature)
        {
            creature.PackGold(40, 120);
            var arrows = new Arrow(Utility.RandomMinMax(10, 40));
            creature.PackItem(arrows);
            if (Utility.RandomDouble() < 0.15) creature.AddItem(new Bow());
            if (Utility.RandomDouble() < 0.1) creature.PackItem(new LesserExplosionPotion());
        }

        public static void GenerateKnightLoot(BaseCreature creature)
        {
            creature.PackGold(80, 200);
            if (Utility.RandomDouble() < 0.25) creature.AddItem(new PlateHelm());
            if (Utility.RandomDouble() < 0.15) creature.AddItem(new VikingSword());
            if (Utility.RandomDouble() < 0.1) creature.PackItem(new GoldRing());
        }

        public static void GenerateBeastmasterLoot(BaseCreature creature)
        {
            creature.PackGold(60, 150);
            creature.PackItem(new Bandage(Utility.RandomMinMax(5, 15)));
            if (Utility.RandomDouble() < 0.2) creature.AddItem(new ShepherdsCrook());
        }

        public static void GenerateWitchdoctorLoot(BaseCreature creature)
        {
            creature.PackGold(70, 180);
            creature.PackReg(Utility.RandomMinMax(10, 25));
            if (Utility.RandomDouble() < 0.25) creature.AddItem(new GnarledStaff());
            if (Utility.RandomDouble() < 0.1) creature.PackItem(new NightSightPotion());
        }

        public static void GenerateSkeletalMageLoot(BaseCreature creature)
        {
            creature.PackGold(50, 140);
            creature.PackReg(Utility.RandomMinMax(5, 15));
            if (Utility.RandomDouble() < 0.2) creature.AddItem(new Robe(0x1));
            if (Utility.RandomDouble() < 0.1) creature.PackItem(new AnimateDeadScroll());
        }

        public static void GenerateSkeletalArcherLoot(BaseCreature creature)
        {
            creature.PackGold(40, 110);
            creature.PackItem(new Arrow(Utility.RandomMinMax(10, 30)));
            if (Utility.RandomDouble() < 0.15) creature.AddItem(new Bow());
        }

        public static void GenerateLesserDragonLoot(BaseCreature creature)
        {
            creature.PackGold(100, 300);
            creature.PackItem(new SulfurousAsh(Utility.RandomMinMax(5, 15)));
            if (Utility.RandomDouble() < 0.3) creature.AddItem(new DragonBlood(5));
        }

        public static void GenerateLesserDaemonLoot(BaseCreature creature)
        {
            creature.PackGold(80, 200);
            creature.PackItem(new SulfurousAsh(Utility.RandomMinMax(5, 10)));
            if (Utility.RandomDouble() < 0.1) creature.PackItem(new DaemonBone());
        }

        public static void GenerateEliteLoot(BaseCreature creature)
        {
            creature.PackGold(200, 500);
            if (Utility.RandomDouble() < 0.3) creature.PackItem(new SpellScroll(Utility.RandomMinMax(4, 7), 0x1F2C));
            if (Utility.RandomDouble() < 0.15) creature.PackItem(new MagicWand());
            if (Utility.RandomDouble() < 0.1) creature.PackItem(new GoldRing());
            if (Utility.RandomDouble() < 0.05) creature.PackItem(new Gold(Utility.RandomMinMax(50, 150)));
        }
    }

    #endregion

    #region Elite Boss Variants — named, unique abilities, custom loot

    /// <summary>Orc Warlord — aura of ferocity, battle shout</summary>
    public class OrcWarlord : BaseCreature
    {
        private DateTime _nextShout = DateTime.UtcNow;

        [Constructable]
        public OrcWarlord() : base(AIType.AI_Paladin, FightMode.Closest, 12, 1, 0.2, 0.4)
        {
            Name = "the orc warlord";
            Body = 17;
            Hue = 0x8A4; // Pale green hue
            BaseSoundID = 0x45A;

            SetStr(250, 350);
            SetDex(80, 110);
            SetInt(60, 90);

            SetHits(350, 500);
            SetDamage(22, 35);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Fire, 30);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 45, 55);
            SetResistance(ResistanceType.Cold, 35, 45);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.Swords, 110, 130);
            SetSkill(SkillName.Tactics, 110, 130);
            SetSkill(SkillName.Chivalry, 60, 90);
            SetSkill(SkillName.MagicResist, 70, 90);
            SetSkill(SkillName.Parry, 80, 100);
            SetSkill(SkillName.Anatomy, 90, 110);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 55;

            AddItem(new PlateChest { Hue = 0x497 });
            AddItem(new PlateArms { Hue = 0x497 });
            AddItem(new PlateLegs { Hue = 0x497 });
            AddItem(new PlateGloves { Hue = 0x497 });
            AddItem(new PlateHelm { Hue = 0x497 });
            AddItem(new VikingSword { Hue = 0x497 });
            AddItem(new MetalShield { Hue = 0x497 });
            AddItem(new Cloak(0x22));

            PackGold(300, 600);
        }

        public override void OnThink()
        {
            base.OnThink();
            if (!Alive || Combatant == null || DateTime.UtcNow < _nextShout) return;

            if (Hits < HitsMax * 0.3 && Utility.RandomDouble() < 0.4)
            {
                // Battle shout — buff nearby allies
                _nextShout = DateTime.UtcNow + TimeSpan.FromSeconds(15);
                Say("*roars a battle cry!*");
                Effects.PlaySound(Location, Map, 0x1FB);
                foreach (Mobile m in GetMobilesInRange(8))
                {
                    if (m != this && m is BaseCreature bc && bc.Combatant != null && bc.ControlMaster == null)
                    {
                        bc.RawStr = (int)(bc.RawStr * 1.15);
                        bc.Hits = bc.HitsMax;
                        bc.FixedParticles(0x373A, 10, 15, 5018, 0x22, 0, EffectLayer.Waist);
                    }
                }
            }
            else if (Hits < HitsMax * 0.5 && Utility.RandomDouble() < 0.3)
            {
                // Personal rage — increase damage
                _nextShout = DateTime.UtcNow + TimeSpan.FromSeconds(20);
                Say("*Warlord's rage intensifies!*");
                FixedParticles(0x37B9, 10, 20, 5018, 0x22, 3, EffectLayer.Head);
                DamageMin = (int)(DamageMin * 1.3);
                DamageMax = (int)(DamageMax * 1.3);
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich);
            AddLoot(LootPack.UltraRich);
            VariantLoot.GenerateEliteLoot(this);
            if (Utility.RandomDouble() < 0.5)
                PackItem(new Gold(Utility.RandomMinMax(100, 300)));
        }

        public override int TreasureMapLevel { get { return 4; } }

        public OrcWarlord(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Lizardman High Priest — devastating magic, mana drain</summary>
    public class LizardmanHighPriest : BaseCreature
    {
        private DateTime _nextSpecial = DateTime.UtcNow;

        [Constructable]
        public LizardmanHighPriest() : base(AIType.AI_Mage, FightMode.Closest, 15, 1, 0.2, 0.4)
        {
            Name = "the lizardman high priest";
            Body = 35;
            Hue = 0x8A0; // Bright green
            BaseSoundID = 0x287;

            SetStr(100, 140);
            SetDex(60, 90);
            SetInt(150, 200);

            SetHits(180, 280);
            SetDamage(12, 22);

            SetDamageType(ResistanceType.Physical, 30);
            SetDamageType(ResistanceType.Poison, 70);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 70, 90);
            SetResistance(ResistanceType.Energy, 30, 45);

            SetSkill(SkillName.Magery, 100, 130);
            SetSkill(SkillName.EvalInt, 100, 130);
            SetSkill(SkillName.Meditation, 90, 110);
            SetSkill(SkillName.MagicResist, 90, 110);
            SetSkill(SkillName.Poisoning, 100, 120);
            SetSkill(SkillName.Wrestling, 60, 80);

            Fame = 14000;
            Karma = -14000;

            VirtualArmor = 35;

            AddItem(new BoneHelm { Hue = 0x4A7 });
            AddItem(new Robe(0x4A7));
            AddItem(new GnarledStaff { Hue = 0x4A7 });
            PackReg(30);
            PackGold(200, 400);
        }

        public override void OnThink()
        {
            base.OnThink();
            if (!Alive || Combatant == null || DateTime.UtcNow < _nextSpecial) return;

            if (Utility.RandomDouble() < 0.25)
            {
                _nextSpecial = DateTime.UtcNow + TimeSpan.FromSeconds(10);
                // Mana drain aura
                Say("*chants a draining incantation!*");
                FixedParticles(0x3789, 10, 30, 5032, 0x59, 0, EffectLayer.Head);
                foreach (Mobile m in GetMobilesInRange(5))
                {
                    if (m != this && m.Player)
                    {
                        int drain = Utility.RandomMinMax(10, 30);
                        m.Mana = Math.Max(0, m.Mana - drain);
                        Mana += drain;
                        m.SendMessage("You feel your mana being drained!");
                    }
                }
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich);
            AddLoot(LootPack.MedScrolls);
            VariantLoot.GenerateEliteLoot(this);
            if (Utility.RandomDouble() < 0.3)
                PackItem(new DeadlyPoisonPotion());
        }

        public override int TreasureMapLevel { get { return 4; } }

        public LizardmanHighPriest(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Troll Chieftain — massive regen, club stun</summary>
    public class TrollChieftain : BaseCreature
    {
        private DateTime _nextStomp = DateTime.UtcNow;

        [Constructable]
        public TrollChieftain() : base(AIType.AI_Melee, FightMode.Closest, 12, 1, 0.2, 0.4)
        {
            Name = "the troll chieftain";
            Body = 54;
            Hue = 0x8A4;
            BaseSoundID = 0x4E2;

            SetStr(350, 450);
            SetDex(60, 90);
            SetInt(50, 80);

            SetHits(400, 650);
            SetDamage(25, 40);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 35, 45);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Tactics, 110, 130);
            SetSkill(SkillName.Wrestling, 110, 130);
            SetSkill(SkillName.MagicResist, 60, 80);
            SetSkill(SkillName.Anatomy, 80, 110);

            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 50;

            AddItem(new Club { Hue = 0x497 });

            PackGold(400, 700);
        }

        public override void OnThink()
        {
            base.OnThink();
            if (!Alive || Combatant == null || DateTime.UtcNow < _nextStomp) return;

            if (Combatant is Mobile stompTarget && stompTarget.GetDistanceToSqrt(this) <= 2 && Utility.RandomDouble() < 0.2)
            {
                _nextStomp = DateTime.UtcNow + TimeSpan.FromSeconds(12);
                // Ground stomp — stun nearby enemies briefly
                Say("*stomps the ground violently!*");
                Effects.PlaySound(Location, Map, 0x508);
                Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3789, 10, 30, 0x59, 0, 5020, 0);

                foreach (Mobile m in GetMobilesInRange(3))
                {
                    if (m != this && (m.Player || (m is BaseCreature bc && bc.ControlMaster != null)))
                    {
                        m.Freeze(TimeSpan.FromSeconds(1.5));
                        m.SendMessage("The ground shakes violently, stunning you!");
                    }
                }
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich);
            AddLoot(LootPack.SuperBoss);
            VariantLoot.GenerateEliteLoot(this);
            PackItem(new SpellScroll(Utility.RandomMinMax(5, 8), 0x1F2C));
            if (Utility.RandomDouble() < 0.2)
                PackItem(new Gold(Utility.RandomMinMax(200, 400)));
        }

        public override int TreasureMapLevel { get { return 5; } }

        public TrollChieftain(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Skeletal Lich — summon spam, life drain aura</summary>
    public class SkeletalLich : BaseCreature
    {
        private DateTime _nextSummon = DateTime.UtcNow;
        private DateTime _nextDrain = DateTime.UtcNow;

        [Constructable]
        public SkeletalLich() : base(AIType.AI_NecroMage, FightMode.Closest, 15, 1, 0.2, 0.4)
        {
            Name = "the skeletal lich";
            Body = 0x1; // Wraith-like body for lich
            Hue = 0x47E; // Pale blue
            BaseSoundID = 0x48D;

            SetStr(120, 160);
            SetDex(60, 90);
            SetInt(200, 300);

            SetHits(250, 400);
            SetDamage(15, 28);

            SetDamageType(ResistanceType.Physical, 30);
            SetDamageType(ResistanceType.Cold, 70);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 70, 85);
            SetResistance(ResistanceType.Poison, 60, 80);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.Necromancy, 110, 130);
            SetSkill(SkillName.SpiritSpeak, 110, 130);
            SetSkill(SkillName.Magery, 90, 120);
            SetSkill(SkillName.EvalInt, 90, 120);
            SetSkill(SkillName.Meditation, 90, 110);
            SetSkill(SkillName.MagicResist, 90, 110);

            Fame = 20000;
            Karma = -20000;

            VirtualArmor = 45;

            AddItem(new Robe(0x1));
            AddItem(new WizardsHat { Hue = 0x1 });
            AddItem(new Spellbook { Hue = 0x1 });

            PackGold(300, 600);
            PackReg(30);
        }

        public override void OnThink()
        {
            base.OnThink();
            if (!Alive) return;

            // Life drain aura (periodic)
            if (Combatant != null && DateTime.UtcNow > _nextDrain)
            {
                _nextDrain = DateTime.UtcNow + TimeSpan.FromSeconds(8);
                foreach (Mobile m in GetMobilesInRange(4))
                {
                    if (m != this && (m.Player || (m is BaseCreature bc && bc.ControlMaster != null)))
                    {
                        int drain = Utility.RandomMinMax(5, 15);
                        m.Damage(drain, this);
                        Hits = Math.Min(HitsMax, Hits + drain);
                        m.FixedParticles(0x374A, 10, 15, 5018, 0x59, 0, EffectLayer.Waist);
                    }
                }
            }

            // Summon skeletons
            if (Combatant != null && DateTime.UtcNow > _nextSummon && Utility.RandomDouble() < 0.2)
            {
                _nextSummon = DateTime.UtcNow + TimeSpan.FromSeconds(15);
                Say("*raises skeletal minions!*");
                Effects.PlaySound(Location, Map, 0x1FB);
                for (int i = 0; i < 3; i++)
                {
                    var skele = new Skeleton();
                    var loc = new Point3D(X + Utility.RandomMinMax(-2, 2), Y + Utility.RandomMinMax(-2, 2), Z);
                    skele.MoveToWorld(loc, Map);
                    skele.Combatant = Combatant;
                    Timer.DelayCall(TimeSpan.FromSeconds(30), () =>
                    {
                        if (skele != null && !skele.Deleted)
                            skele.Delete();
                    });
                }
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.SuperBoss);
            AddLoot(LootPack.HighScrolls);
            VariantLoot.GenerateEliteLoot(this);
            if (Utility.RandomDouble() < 0.3)
                PackItem(new LichFormScroll());
            if (Utility.RandomDouble() < 0.1)
                PackItem(new WitherScroll());
        }

        public override int TreasureMapLevel { get { return 5; } }

        public SkeletalLich(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    #endregion

    #region Elite Spawner Integration — adds elite variants to themed spawners

    // Extends the CreatureVariantSpawner to include elite spawn chances
    // and updates existing spawner themes to include elite bosses rarely.

    #endregion
}
