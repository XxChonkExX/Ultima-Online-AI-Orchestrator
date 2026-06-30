using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Integrates faction-themed items and quest rewards into creature loot tables.
    /// Hooks into creature death events to add Virtue/Vice-aligned loot.
    /// </summary>
    public static class LootTableIntegration
    {
        private static bool _registered;

        public static void Initialize()
        {
            if (_registered) return;
            _registered = true;

            EventSink.CreatureDeath += OnCreatureDeath;
            Console.WriteLine("[AIOrchestrator] Loot table integration initialized.");
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Creature == null || e.Creature.Deleted)
                return;

            // Add faction-specific loot based on creature type
            var creature = e.Creature;
            var name = creature.GetType().Name.ToLowerInvariant();
            var cname = creature.Name?.ToLowerInvariant() ?? "";
            var corpse = e.Corpse;

            // Determine what kind of loot to add
            if (name.Contains("dragon") || name.Contains("wyrm") || name.Contains("drake"))
            {
                AddDragonLoot(corpse);
            }
            else if (name.Contains("lich") || name.Contains("necromancer") || name.Contains("skeletal"))
            {
                AddUndeadMageLoot(corpse);
            }
            else if (name.Contains("orc") || name.Contains("goblin"))
            {
                AddHumanoidLoot(corpse, 0x22); // Red hue for orcs
            }
            else if (name.Contains("daemon") || name.Contains("balron") || name.Contains("devil"))
            {
                AddDaemonLoot(corpse);
            }
            else if (name.Contains("gazer") || name.Contains("mindflay") || name.Contains("elder"))
            {
                AddElderLoot(corpse);
            }
            else if (name.Contains("troll") || name.Contains("ogre") || name.Contains("ettin"))
            {
                AddGiantLoot(corpse);
            }
            else if (name.Contains("elemental") || name.Contains("golem"))
            {
                AddElementalLoot(corpse);
            }
            else if (name.Contains("rat") || name.Contains("spider") || name.Contains("snake") || name.Contains("mongbat"))
            {
                AddLesserCreatureLoot(corpse);
            }
        }

        private static bool TryDropItem(Container corpse, Item item)
        {
            if (corpse == null || corpse.Deleted || item == null)
                return false;

            if (Utility.RandomDouble() < 0.3) // 30% chance for faction loot
            {
                corpse.DropItem(item);
                return true;
            }

            item.Delete();
            return false;
        }

        #region Loot Tables

        private static void AddDragonLoot(Container corpse)
        {
            if (corpse == null) return;

            // Dragon scales, gems, gold
            if (Utility.RandomDouble() < 0.6)
                corpse.DropItem(new DragonScale(Utility.RandomMinMax(3, 8)));

            if (Utility.RandomDouble() < 0.4)
                corpse.DropItem(new StarSapphire(Utility.RandomMinMax(1, 3)));

            if (Utility.RandomDouble() < 0.3)
            {
                // Faction-themed weapon
                var weapon = new Longsword();
                weapon.Name = "Dragonfang Blade";
                weapon.Hue = 0x4DE; // Fire hue
                corpse.DropItem(weapon);
            }
        }

        private static void AddUndeadMageLoot(Container corpse)
        {
            if (corpse == null) return;

            if (Utility.RandomDouble() < 0.5)
                corpse.DropItem(new BatWing(Utility.RandomMinMax(2, 6)));

            if (Utility.RandomDouble() < 0.4)
                corpse.DropItem(new GraveDust(Utility.RandomMinMax(2, 5)));

            if (Utility.RandomDouble() < 0.25)
            {
                var robe = new Robe(0x1); // Black
                robe.Name = "Shroud of the Accursed";
                corpse.DropItem(robe);
            }

            if (Utility.RandomDouble() < 0.2)
                corpse.DropItem(new NoxCrystal(Utility.RandomMinMax(1, 3)));
        }

        private static void AddHumanoidLoot(Container corpse, int hue)
        {
            if (corpse == null) return;

            if (Utility.RandomDouble() < 0.5)
                corpse.DropItem(new Gold(Utility.RandomMinMax(30, 120)));

            if (Utility.RandomDouble() < 0.3)
            {
                var weapon = new Club();
                weapon.Hue = hue;
                weapon.Name = "Tribal Warclub";
                corpse.DropItem(weapon);
            }

            if (Utility.RandomDouble() < 0.2)
                corpse.DropItem(new Bone(Utility.RandomMinMax(1, 5)));
        }

        private static void AddDaemonLoot(Container corpse)
        {
            if (corpse == null) return;

            if (Utility.RandomDouble() < 0.6)
                corpse.DropItem(new Gold(Utility.RandomMinMax(100, 400)));

            if (Utility.RandomDouble() < 0.4)
                corpse.DropItem(new BlackPearl(Utility.RandomMinMax(2, 8)));

            if (Utility.RandomDouble() < 0.3)
            {
                var weapon = new DaemonSword();
                weapon.Hue = 0x497; // Dark purple
                corpse.DropItem(weapon);
            }

            if (Utility.RandomDouble() < 0.2)
                corpse.DropItem(new Bloodmoss(Utility.RandomMinMax(2, 6)));
        }

        private static void AddElderLoot(Container corpse)
        {
            if (corpse == null) return;

            if (Utility.RandomDouble() < 0.5)
                corpse.DropItem(new Gold(Utility.RandomMinMax(50, 200)));

            if (Utility.RandomDouble() < 0.35)
                corpse.DropItem(new Nightshade(Utility.RandomMinMax(2, 6)));

            if (Utility.RandomDouble() < 0.25)
                corpse.DropItem(new MandrakeRoot(Utility.RandomMinMax(2, 5)));

            if (Utility.RandomDouble() < 0.15)
            {
                var boots = new Sandals();
                boots.Hue = 0x482;
                boots.Name = "Sandals of the Seer";
                corpse.DropItem(boots);
            }
        }

        private static void AddGiantLoot(Container corpse)
        {
            if (corpse == null) return;

            if (Utility.RandomDouble() < 0.5)
                corpse.DropItem(new Gold(Utility.RandomMinMax(40, 160)));

            if (Utility.RandomDouble() < 0.35)
                corpse.DropItem(new IronIngot(Utility.RandomMinMax(3, 10)));

            if (Utility.RandomDouble() < 0.2)
            {
                var weapon = new HammerPick();
                weapon.Name = "Giant's Hammer";
                weapon.Hue = 0x44E;
                corpse.DropItem(weapon);
            }
        }

        private static void AddElementalLoot(Container corpse)
        {
            if (corpse == null) return;

            if (Utility.RandomDouble() < 0.6)
                corpse.DropItem(new Gold(Utility.RandomMinMax(60, 240)));

            if (Utility.RandomDouble() < 0.5)
                corpse.DropItem(new IronIngot(Utility.RandomMinMax(5, 20)));

            if (Utility.RandomDouble() < 0.3)
            {
                Item gem;
                switch (Utility.Random(6))
                {
                    case 0: gem = new Ruby(); break;
                    case 1: gem = new Sapphire(); break;
                    case 2: gem = new Emerald(); break;
                    case 3: gem = new Diamond(); break;
                    case 4: gem = new Amethyst(); break;
                    default: gem = new Citrine(); break;
                }
                gem.Amount = Utility.RandomMinMax(1, 4);
                corpse.DropItem(gem);
            }
        }

        private static void AddLesserCreatureLoot(Container corpse)
        {
            if (corpse == null) return;

            if (Utility.RandomDouble() < 0.3)
                corpse.DropItem(new Gold(Utility.RandomMinMax(5, 25)));

            if (Utility.RandomDouble() < 0.15)
                corpse.DropItem(new Bandage(Utility.RandomMinMax(1, 3)));
        }

        #endregion
    }

    /// <summary>Faction-themed faction items drop from specific creature types.</summary>
    public class FactionLootItem : Item
    {
        public string FactionId { get; set; }
        public string Description { get; set; }

        [Constructable]
        public FactionLootItem() : base(0x1BF2)
        {
            Name = "Faction Relic";
            Hue = Utility.RandomNondyedHue();
        }

        public FactionLootItem(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(FactionId ?? "");
            writer.Write(Description ?? "");
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            FactionId = reader.ReadString();
            Description = reader.ReadString();
        }
    }

    /// <summary>Dragon scales — used for crafting faction gear.</summary>
    public class DragonScale : Item
    {
        [Constructable]
        public DragonScale() : this(1) { }

        [Constructable]
        public DragonScale(int amount) : base(0x26B4)
        {
            Name = "Dragon Scale";
            Hue = 0x4DE;
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public DragonScale(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Daemon-forged sword — faction weapon.</summary>
    public class DaemonSword : BaseSword
    {
        [Constructable]
        public DaemonSword() : base(0x26CE)
        {
            Name = "Daemon Blade";
            Hue = 0x497;
            Weight = 6.0;
            Slayer = SlayerName.DaemonDismissal;
            WeaponAttributes.HitFireball = 20;
            SkillBonuses.SetValues(0, SkillName.Swords, 5.0);
        }

        public DaemonSword(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }
}
