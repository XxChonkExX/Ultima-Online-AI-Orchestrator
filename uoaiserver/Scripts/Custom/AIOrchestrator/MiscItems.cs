using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    // ══════════════════════════════════════════════════════════════
    //  FOOD & DRINK
    // ══════════════════════════════════════════════════════════════

    /// <summary>Hearty Stew — restores 30 HP and 30 stamina.</summary>
    public class HeartyStew : Food
    {
        [Constructable]
        public HeartyStew() : base(0x1F7B)
        {
            Name = "Hearty Stew";
            Hue = 0x456;
            Weight = 2.0;
            FillFactor = 6;
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from)) return false;
            from.Hits = Math.Min(from.HitsMax, from.Hits + 30);
            from.Stam = Math.Min(from.StamMax, from.Stam + 30);
            from.SendMessage(0x44, "You feel revitalized by the hearty stew!");
            return true;
        }

        public HeartyStew(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Dragon Breath Whiskey — grants fire resistance buff for 5 minutes.</summary>
    public class DragonBreathWhiskey : BeverageBottle
    {
        private static readonly TimeSpan BuffDuration = TimeSpan.FromMinutes(5);

        [Constructable]
        public DragonBreathWhiskey() : base(BeverageType.Liquor)
        {
            Name = "Dragon Breath Whiskey";
            Hue = 0x4DE;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (IsEmpty)
            {
                from.SendMessage("The bottle is empty.");
                return;
            }

            // Consume one quantity
            Quantity--;

            from.SendMessage(0x44, "Fire burns in your veins! Fire resistance increased for 5 minutes.");

            // Apply resistance mod
            var mod = new ResistanceMod(ResistanceType.Fire, 10);
            from.AddResistanceMod(mod);

            Timer.DelayCall(BuffDuration, () =>
            {
                if (!from.Deleted)
                {
                    from.RemoveResistanceMod(mod);
                    from.SendMessage(0x26, "The dragon breath warmth fades.");
                }
            });
        }

        public DragonBreathWhiskey(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Mana Berry Pie — restores 20 mana.</summary>
    public class ManaBerryPie : Food
    {
        [Constructable]
        public ManaBerryPie() : base(0x1042)
        {
            Name = "Mana Berry Pie";
            Hue = 0x1B;
            Weight = 1.0;
            FillFactor = 3;
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from)) return false;
            from.Mana = Math.Min(from.ManaMax, from.Mana + 20);
            from.SendMessage(0x44, "Sweet berries restore your mana!");
            return true;
        }

        public ManaBerryPie(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Goblin Ale — temporary strength buff but also confusion (dex penalty).</summary>
    public class GoblinAle : BeverageBottle
    {
        private static readonly TimeSpan BuffDuration = TimeSpan.FromMinutes(3);

        [Constructable]
        public GoblinAle() : base(BeverageType.Ale)
        {
            Name = "Goblin Ale";
            Hue = 0x44;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (IsEmpty)
            {
                from.SendMessage("The bottle is empty.");
                return;
            }

            Quantity--;

            from.SendMessage(0x44, "The foul brew gives you strength but makes you clumsy!");

            from.RawStr += 5;
            from.RawDex -= 3;

            Timer.DelayCall(BuffDuration, () =>
            {
                if (!from.Deleted)
                {
                    from.RawStr -= 5;
                    from.RawDex += 3;
                    from.SendMessage(0x26, "The goblin ale wears off.");
                }
            });
        }

        public GoblinAle(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    // ══════════════════════════════════════════════════════════════
    //  WEAPONS
    // ══════════════════════════════════════════════════════════════

    /// <summary>Lich Bone Staff — a necromantic quarterstaff with mana leech.</summary>
    public class LichBoneStaff : QuarterStaff
    {
        [Constructable]
        public LichBoneStaff()
        {
            Name = "Lich Bone Staff";
            Hue = 0x481;
            WeaponAttributes.HitLeechMana = 30;
            WeaponAttributes.HitDispel = 25;
            SkillBonuses.SetValues(0, SkillName.Necromancy, 10.0);
            Slayer = SlayerName.Silver; // Silver slayer is undead
        }

        public LichBoneStaff(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Orcish War Axe — a brutal weapon with stamina drain.</summary>
    public class OrcishWarAxe : BattleAxe
    {
        [Constructable]
        public OrcishWarAxe()
        {
            Name = "Orcish War Axe";
            Hue = 0x22;
            WeaponAttributes.HitLeechStam = 40;
            WeaponAttributes.HitFatigue = 30;
            SkillBonuses.SetValues(0, SkillName.Fencing, 5.0); // Axes use Fencing
        }

        public OrcishWarAxe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    // ══════════════════════════════════════════════════════════════
    //  ARMOR
    // ══════════════════════════════════════════════════════════════

    /// <summary>Troll Skin Boots — boots with HP regen.</summary>
    public class TrollSkinBoots : Boots
    {
        [Constructable]
        public TrollSkinBoots()
        {
            Name = "Troll Skin Boots";
            Hue = 0x44E;
            Attributes.RegenHits = 2;
            // SelfRepair is a weapon attribute only
        }

        public TrollSkinBoots(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    // ══════════════════════════════════════════════════════════════
    //  REAGENTS / MATERIALS
    // ══════════════════════════════════════════════════════════════

    /// <summary>Phoenix Feather — rare reagent used in resurrection scrolls.</summary>
    public class PhoenixFeather : Item
    {
        [Constructable]
        public PhoenixFeather() : this(1) { }

        [Constructable]
        public PhoenixFeather(int amount) : base(0x1021)
        {
            Name = "Phoenix Feather";
            Hue = 0x4DE;
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public PhoenixFeather(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    /// <summary>Vampire Fang — life steal ingredient used in dark crafting.</summary>
    public class VampireFang : Item
    {
        [Constructable]
        public VampireFang() : this(1) { }

        [Constructable]
        public VampireFang(int amount) : base(0x315C)
        {
            Name = "Vampire Fang";
            Hue = 0x26;
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public VampireFang(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }
}