using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Wandering Hero Hirelings — high-quality NPCs that can be hired.
    /// Expansions:
    ///   - Leveling (1-20, XP from kills near master)
    ///   - Equipment upgrades at milestones
    ///   - Class-specific dialogue personality
    ///   - Bonding (retainer status at high level)
    /// </summary>
    public class HeroHireling : BaseCreature
    {
        public enum HeroClass
        {
            Warrior, Archer, Mage, Paladin, Ranger,
            Ninja, AnimalTamer, Necromancer, Bard, Alchemist
        }

        private HeroClass _heroClass;
        private string _heroTitle;
        private int _hireCost;

        // ── Leveling ──────────────────────────────────────────────────
        private int _level = 1;
        private int _experience = 0;

        private static readonly int[] LevelThresholds = {
            0, 100, 250, 500, 800, 1200, 1700, 2300, 3000, 3800,
            4700, 5700, 6800, 8000, 9300, 10700, 12200, 13800, 15500, 20000
        }; // level 20 cap

        // ── Bonding ───────────────────────────────────────────────────
        private bool _isBonded = false;

        private DateTime _lastWander;

        [CommandProperty(AccessLevel.GameMaster)]
        public HeroClass ClassType
        {
            get { return _heroClass; }
            set { _heroClass = value; ApplyClassAppearance(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HireCost
        {
            get { return _hireCost; }
            set { _hireCost = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get { return _level; }
            set { _level = Math.Max(1, Math.Min(20, value)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Experience
        {
            get { return _experience; }
            set { _experience = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBonded
        {
            get { return _isBonded; }
            set { _isBonded = value; }
        }

        [Constructable]
        public HeroHireling() : this((HeroClass)Utility.Random(10)) { }

        [Constructable]
        public HeroHireling(HeroClass heroClass) : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.4)
        {
            _heroClass = heroClass;
            _lastWander = DateTime.UtcNow;
            _level = 1;
            _experience = 0;
            _isBonded = false;

            Body = Utility.RandomBool() ? 0x190 : 0x191;
            Name = NameList.RandomName(Body == 0x191 ? "female" : "male");
            Hue = Utility.RandomSkinHue();
            SetStr(80, 120);
            SetDex(60, 100);
            SetInt(60, 100);
            Fame = 5000;
            Karma = 5000;
            Blessed = true;
            Direction = (Direction)Utility.Random(8);

            // Class setup
            SetupClass();

            PackGold(Utility.Random(200, 500));
            SetHits(Str);
            SetDamage(5, 15);
            SetResistance(ResistanceType.Physical, 30, 50);
            SetResistance(ResistanceType.Fire, 20, 40);
            SetResistance(ResistanceType.Cold, 20, 40);
            SetResistance(ResistanceType.Poison, 20, 40);
            SetResistance(ResistanceType.Energy, 20, 40);
            VirtualArmor = 30;

            if (_heroClass == HeroClass.Mage || _heroClass == HeroClass.Necromancer) RangeFight = 8;
            if (_heroClass == HeroClass.Archer || _heroClass == HeroClass.Ranger) RangeFight = 8;

            ControlOrder = OrderType.None;
            ControlTarget = null;
            RangeHome = 20;
        }

        private void SetupClass()
        {
            switch (_heroClass)
            {
                case HeroClass.Warrior:
                    _heroTitle = "the Warrior"; _hireCost = 2000 + Utility.Random(1000); Title = _heroTitle;
                    SetSkill(SkillName.Swords, 90, 120); SetSkill(SkillName.Tactics, 90, 120);
                    SetSkill(SkillName.Parry, 70, 100); SetSkill(SkillName.Anatomy, 80, 110);
                    Equip(new PlateChest(), new PlateArms(), new PlateLegs(), new PlateGloves(), new PlateGorget());
                    Equip(new VikingSword(), new MetalShield(), new Boots());
                    break;
                case HeroClass.Archer:
                    _heroTitle = "the Archer"; _hireCost = 1800 + Utility.Random(1200); Title = _heroTitle;
                    SetSkill(SkillName.Archery, 90, 120); SetSkill(SkillName.Tactics, 80, 110);
                    SetSkill(SkillName.Anatomy, 70, 100); SetSkill(SkillName.Parry, 40, 60);
                    Equip(new LeatherChest(), new LeatherArms(), new LeatherLegs(), new LeatherGloves());
                    Equip(new Bow(), new Bandana(Utility.RandomDyedHue()));
                    PackItem(new Arrow(50));
                    break;
                case HeroClass.Mage:
                    _heroTitle = "the Mage"; _hireCost = 2500 + Utility.Random(1500); Title = _heroTitle;
                    AI = AIType.AI_Mage;
                    SetSkill(SkillName.Magery, 90, 120); SetSkill(SkillName.EvalInt, 90, 120);
                    SetSkill(SkillName.Meditation, 80, 110); SetSkill(SkillName.MagicResist, 80, 110);
                    SetSkill(SkillName.Wrestling, 40, 60);
                    Equip(new Robe(Utility.RandomBlueHue()), new WizardsHat(), new Sandals(), new Spellbook());
                    PackReg(50);
                    break;
                case HeroClass.Paladin:
                    _heroTitle = "the Paladin"; _hireCost = 3000 + Utility.Random(2000); Title = _heroTitle;
                    AI = AIType.AI_Paladin;
                    SetSkill(SkillName.Swords, 80, 110); SetSkill(SkillName.Tactics, 80, 110);
                    SetSkill(SkillName.Chivalry, 80, 110); SetSkill(SkillName.MagicResist, 70, 100);
                    SetSkill(SkillName.Parry, 50, 80);
                    Equip(new PlateChest(), new PlateLegs(), new PlateArms(), new PlateHelm());
                    Equip(new Longsword(), new MetalShield(), new Cloak(0x480));
                    break;
                case HeroClass.Ranger:
                    _heroTitle = "the Ranger"; _hireCost = 2200 + Utility.Random(1000); Title = _heroTitle;
                    SetSkill(SkillName.Archery, 80, 110); SetSkill(SkillName.Tactics, 80, 110);
                    SetSkill(SkillName.Tracking, 90, 120); SetSkill(SkillName.Veterinary, 60, 90);
                    Equip(new StuddedChest(), new StuddedArms(), new StuddedLegs(), new StuddedGloves());
                    Equip(new Crossbow(), new FeatheredHat(Utility.RandomGreenHue()));
                    PackItem(new Bolt(50));
                    break;
                case HeroClass.Ninja:
                    _heroTitle = "the Ninja"; _hireCost = 2800 + Utility.Random(1200); Title = _heroTitle;
                    AI = AIType.AI_Ninja;
                    SetSkill(SkillName.Fencing, 90, 120); SetSkill(SkillName.Tactics, 80, 110);
                    SetSkill(SkillName.Hiding, 90, 120); SetSkill(SkillName.Stealth, 80, 110);
                    SetSkill(SkillName.Ninjitsu, 80, 110);
                    Equip(new LeatherNinjaJacket(), new LeatherNinjaPants(), new NinjaTabi(), new Tekagi());
                    Equip(new ClothNinjaHood(0x497));
                    break;
                case HeroClass.AnimalTamer:
                    _heroTitle = "the Beastmaster"; _hireCost = 2000 + Utility.Random(1000); Title = _heroTitle;
                    SetSkill(SkillName.AnimalTaming, 90, 120); SetSkill(SkillName.AnimalLore, 80, 110);
                    SetSkill(SkillName.Veterinary, 80, 110); SetSkill(SkillName.Peacemaking, 50, 80);
                    Equip(new StuddedChest(), new StuddedArms(), new StuddedLegs());
                    Equip(new FeatheredHat(Utility.RandomNondyedHue()), new ShepherdsCrook());
                    SpawnCompanion();
                    break;
                case HeroClass.Necromancer:
                    _heroTitle = "the Necromancer"; _hireCost = 2600 + Utility.Random(1400); Title = _heroTitle;
                    AI = AIType.AI_NecroMage;
                    SetSkill(SkillName.Necromancy, 90, 120); SetSkill(SkillName.Magery, 60, 90);
                    SetSkill(SkillName.SpiritSpeak, 90, 120); SetSkill(SkillName.Meditation, 70, 100);
                    SetSkill(SkillName.MagicResist, 70, 100);
                    Equip(new Robe(0x1), new WizardsHat(0x1), new Sandals(), new Spellbook());
                    break;
                case HeroClass.Bard:
                    _heroTitle = "the Bard"; _hireCost = 1500 + Utility.Random(800); Title = _heroTitle;
                    SetSkill(SkillName.Musicianship, 90, 120); SetSkill(SkillName.Discordance, 80, 110);
                    SetSkill(SkillName.Peacemaking, 80, 110); SetSkill(SkillName.Provocation, 80, 110);
                    SetSkill(SkillName.Magery, 50, 80);
                    Equip(new FancyShirt(Utility.RandomBrightHue()), new LongPants(Utility.RandomBrightHue()));
                    Equip(new Boots(), new TricorneHat(), new Tambourine());
                    break;
                case HeroClass.Alchemist:
                    _heroTitle = "the Alchemist"; _hireCost = 2000 + Utility.Random(1000); Title = _heroTitle;
                    SetSkill(SkillName.Alchemy, 90, 120); SetSkill(SkillName.TasteID, 70, 100);
                    SetSkill(SkillName.Magery, 60, 90); SetSkill(SkillName.Wrestling, 40, 60);
                    Equip(new Robe(Utility.RandomNondyedHue()), new Sandals());
                    PackItem(new LesserHealPotion()); PackItem(new LesserHealPotion());
                    PackItem(new LesserCurePotion()); PackItem(new LesserExplosionPotion());
                    break;
            }
        }

        /// <summary>Equip multiple items.</summary>
        private void Equip(params Item[] items)
        {
            foreach (var item in items)
                AddItem(item);
        }

        private void SpawnCompanion()
        {
            BaseCreature pet;
            double r = Utility.RandomDouble();
            if (r < 0.3) pet = new BrownBear();
            else if (r < 0.6) pet = new TimberWolf();
            else if (r < 0.8) pet = new Cat();
            else pet = new Horse();
            pet.MoveToWorld(new Point3D(X + 1, Y + 1, Z), Map);
            pet.Controlled = true;
            pet.ControlMaster = this;
            pet.ControlOrder = OrderType.Follow;
            pet.Loyalty = 100;
        }

        // ══════════════════════════════════════════════════════════════
        //  LEVELING SYSTEM
        // ══════════════════════════════════════════════════════════════

        /// <summary>Called when the hero or nearby master kills a creature.</summary>
        public void AwardKillXP(Mobile killed)
        {
            if (killed == null) return;
            int xp = Math.Max(1, killed.Fame / 10);
            GrantXP(xp);
        }

        /// <summary>Grant raw XP (from gifts, quests, relationship system, etc).</summary>
        public void GrantXP(int amount)
        {
            if (_level >= 20 || amount <= 0) return;
            _experience += amount;
            while (_level < 20 && _experience >= LevelThresholds[_level])
            {
                _experience -= LevelThresholds[_level];
                _level++;
                OnLevelUp();
            }
        }

        private void OnLevelUp()
        {
            // Stat gains
            int gain = Utility.RandomMinMax(2, 5);
            SetStr(Str + gain);
            SetDex(Dex + Utility.RandomMinMax(1, 3));
            SetInt(Int + Utility.RandomMinMax(1, 3));

            // Skill gains
            foreach (var si in Skills)
            {
                if (si.Base > 0 && si.Base < 120)
                    si.Base += Utility.RandomMinMax(0, 1);
            }

            SetHits(Str);
            SetDamage(Math.Min(5 + _level, 35), Math.Min(15 + _level, 50));

            // Resistance gains
            SetResistance(ResistanceType.Physical, Math.Min(30 + _level, 80));
            SetResistance(ResistanceType.Fire, Math.Min(20 + _level / 2, 70));
            SetResistance(ResistanceType.Cold, Math.Min(20 + _level / 2, 70));
            SetResistance(ResistanceType.Poison, Math.Min(20 + _level / 2, 70));
            SetResistance(ResistanceType.Energy, Math.Min(20 + _level / 2, 70));

            VirtualArmor = Math.Min(30 + _level * 2, 70);

            // Equipment upgrade at milestones
            if (_level % 5 == 0)
                UpgradeEquipment();

            // Broadcast level-up
            string msg = String.Format("{0} has reached level {1}!", Name, _level);
            IPooledEnumerable eable = GetClientsInRange(12);
            foreach (NetState ns in eable)
            {
                if (ns.Mobile != null)
                    ns.Mobile.SendMessage(0x44, msg);
            }
            eable.Free();

            // Bonding check
            if (!_isBonded && _level >= 10)
            {
                _isBonded = true;
                Blessed = false; // Bonded heroes can die but keep loyalty
                string bondMsg = String.Format("{0} has bonded with you. A true retainer!", Name);
                if (ControlMaster != null && ControlMaster.Player)
                    ControlMaster.SendMessage(0x44, bondMsg);
            }

            // Boost relationship affinity on level up
            if (ControlMaster is PlayerMobile masterPM)
            {
                int affinityGain = _isBonded ? 200 : 20;
                NPCRelationshipSystem.RecordPositiveInteraction(masterPM, this, affinityGain);
            }
        }

        /// <summary>Replace equipment with better versions at milestones.</summary>
        private void UpgradeEquipment()
        {
            // Find and replace key equipment items
            List<Item> toRemove = new List<Item>();
            List<Item> toAdd = new List<Item>();

            foreach (Item item in Items)
            {
                if (item is BaseWeapon && !(item is BaseRanged))
                {
                    toRemove.Add(item);
                    // Upgrade to better weapon type
                    if (item is VikingSword || item is Longsword)
                        toAdd.Add(new Broadsword { Quality = ItemQuality.Exceptional });
                    else if (item is Broadsword)
                        toAdd.Add(new Katana { Quality = ItemQuality.Exceptional });
                    else if (item is Tekagi)
                        toAdd.Add(new Daisho { Quality = ItemQuality.Exceptional });
                    else if (item is ShepherdsCrook)
                        toAdd.Add(new QuarterStaff { Quality = ItemQuality.Exceptional });
                    break;
                }
                else if (item is BaseRanged)
                {
                    toRemove.Add(item);
                    if (item is Bow) toAdd.Add(new HeavyCrossbow { Quality = ItemQuality.Exceptional });
                    else if (item is Crossbow) toAdd.Add(new RepeatingCrossbow { Quality = ItemQuality.Exceptional });
                    break;
                }
                else if (item is BaseShield)
                {
                    toRemove.Add(item);
                    toAdd.Add(new OrderShield { Quality = ItemQuality.Exceptional });
                    break;
                }
                else if (item is BaseArmor armor)
                {
                    toRemove.Add(item);
                    // Upgrade leather to studded, studded to chain, chain to ring, ring to plate
                    if (armor is LeatherChest) toAdd.Add(new StuddedChest());
                    else if (armor is StuddedChest) toAdd.Add(new ChainChest());
                    else if (armor is ChainChest) toAdd.Add(new RingmailChest());
                    else if (armor is RingmailChest) toAdd.Add(new PlateChest());
                    else if (armor is LeatherArms) toAdd.Add(new StuddedArms());
                    else if (armor is StuddedArms) toAdd.Add(new RingmailArms());
                    else if (armor is LeatherLegs) toAdd.Add(new StuddedLegs());
                    else if (armor is StuddedLegs) toAdd.Add(new ChainLegs());
                    else if (armor is ChainLegs) toAdd.Add(new RingmailLegs());
                    else if (armor is RingmailLegs) toAdd.Add(new PlateLegs());
                    else if (armor is LeatherGloves) toAdd.Add(new StuddedGloves());
                }
            }

            foreach (Item r in toRemove) r.Delete();
            foreach (Item a in toAdd) AddItem(a);
        }

        // ══════════════════════════════════════════════════════════════
        //  SPEECH / DIALOGUE
        // ══════════════════════════════════════════════════════════════

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);
            if (e.Mobile == null || e.Mobile == this) return;
            if (!e.Mobile.InRange(this, 4)) return;

            string speech = e.Speech.ToLowerInvariant();

            // Only respond if addressed or relevant
            bool addressed = speech.Contains(Name.ToLowerInvariant());
            bool masterSpeaking = (Controlled && ControlMaster == e.Mobile);

            if (!addressed && !masterSpeaking) return;

            // Generate class-appropriate response via LLM
            string personality;
            switch (_heroClass)
            {
                case HeroClass.Warrior: personality = "a blunt, battle-hardened warrior"; break;
                case HeroClass.Archer: personality = "a sharp-eyed, laconic ranger"; break;
                case HeroClass.Mage: personality = "a scholarly, slightly condescending mage"; break;
                case HeroClass.Paladin: personality = "a devout, righteous paladin of Virtue"; break;
                case HeroClass.Ranger: personality = "a nature-wise, gruff survivalist"; break;
                case HeroClass.Ninja: personality = "a cryptic, minimalist shadow-warrior"; break;
                case HeroClass.AnimalTamer: personality = "a warm, simple beast-speaker"; break;
                case HeroClass.Necromancer: personality = "a morbid, darkly philosophical necromancer"; break;
                case HeroClass.Bard: personality = "a flamboyant, rhyming storyteller"; break;
                case HeroClass.Alchemist: personality = "an eccentric, potion-obsessed chemist"; break;
                default: personality = "an adventurer"; break;
            }

            string bondStatus = _isBonded ? " You are deeply loyal to " + (ControlMaster?.Name ?? "your master") + "." : " You are a freelance mercenary.";
            string levelInfo = " Level " + _level + " " + _heroClass.ToString().ToLowerInvariant() + ".";

            // Include relationship context in dialogue
            string relContext = "";
            if (ControlMaster is PlayerMobile speakerPM)
            {
                var rel = NPCRelationshipSystem.GetOrCreate(speakerPM, this);
                if (rel.State == NPCState.RomanticPartner)
                    relContext = " You are in love with " + speakerPM.Name + " and speak with affection and warmth.";
                else if (rel.State == NPCState.Apprentice)
                    relContext = " You are " + speakerPM.Name + "'s apprentice and speak with deference and curiosity.";
                else if (rel.State >= NPCState.Hired)
                    relContext = " You respect " + speakerPM.Name + " as your employer.";
                else if (rel.Affinity >= 500)
                    relContext = " You consider " + speakerPM.Name + " a good friend.";
            }

            string sysPrompt = "You are " + Name + ", " + personality + "." + bondStatus + levelInfo + relContext +
                               " Speak in character, 1-2 sentences max (200 chars). Never break character.";

            Task.Run(async () =>
            {
                try
                {
                    string reply = await LLMClient.ChatAsync(sysPrompt,
                        e.Mobile.Name + " says: \"" + e.Speech + "\"",
                        AIConfig.ModelDialogue);
                    if (!string.IsNullOrEmpty(reply))
                    {
                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            if (!Deleted && Alive)
                                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, reply);
                        });
                    }
                }
                catch { }
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  THINK / COMBAT
        // ══════════════════════════════════════════════════════════════

        public override void OnThink()
        {
            base.OnThink();

            if (DateTime.UtcNow - _lastWander > TimeSpan.FromSeconds(30))
            {
                _lastWander = DateTime.UtcNow;
                if (!Controlled && AIObject != null)
                {
                    var dest = new Point3D(X + Utility.RandomMinMax(-10, 10), Y + Utility.RandomMinMax(-10, 10), Z);
                    if (Map != null && Map.CanFit(dest, 16, false, false))
                        AIObject.Action = ActionType.Wander;
                }
            }
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            base.OnDamage(amount, from, willKill);

            // Retaliatory bark at low HP
            if (willKill && Controlled && from != null && from != this)
            {
                string[] deathLines = {
                    "Tell my master... I tried...",
                    "So this is how it ends...",
                    "I regret nothing!",
                    "Master... avenge me..."
                };
                Say(deathLines[Utility.Random(deathLines.Length)]);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  HIRING / DISMISSAL
        // ══════════════════════════════════════════════════════════════

        public bool Hire(Mobile from)
        {
            if (from == null || from.Deleted) return false;
            if (Controlled) { from.SendMessage("This hero is already hired."); return false; }

            if (from.Backpack == null || from.Backpack.GetAmount(typeof(Gold)) < _hireCost)
            {
                from.SendMessage("You need " + _hireCost + " gold.");
                return false;
            }

            from.Backpack.ConsumeTotal(typeof(Gold), _hireCost);
            ControlMaster = from;
            Controlled = true;
            ControlOrder = OrderType.Follow;
            ControlTarget = from;
            Blessed = false;
            Loyalty = 100;

            // Record in relationship system
            if (from is PlayerMobile hiringPlayer)
            {
                var rel = NPCRelationshipSystem.GetOrCreate(hiringPlayer, this);
                if (rel.State < NPCState.Hired)
                {
                    rel.State = NPCState.Hired;
                    rel.HiredAt = DateTime.UtcNow;
                    rel.Role = NPCRole.Guard;
                }
                NPCRelationshipSystem.RecordPositiveInteraction(hiringPlayer, this, 50);
            }

            from.SendMessage("You hired " + Name + " " + _heroTitle + " for " + _hireCost + " gold!");
            Say("I am yours to command!");
            return true;
        }

        public void Dismiss()
        {
            if (!Controlled) return;

            // Reset relationship state
            if (ControlMaster is PlayerMobile pm)
            {
                var rel = NPCRelationshipSystem.GetOrCreate(pm, this);
                if (rel.State >= NPCState.Hired)
                    rel.State = NPCState.Friend;
                NPCRelationshipSystem.RecordNegativeInteraction(pm, this, 30);
            }

            ControlMaster = null;
            Controlled = false;
            ControlOrder = OrderType.None;
            ControlTarget = null;
            Blessed = true;
            Say("Farewell. May we meet again.");

            Timer.DelayCall(TimeSpan.FromSeconds(5), () =>
            {
                if (!Deleted && AIObject != null)
                    AIObject.Action = ActionType.Wander;
            });
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            if (from is PlayerMobile pm && from.InRange(this, 3))
            {
                if (!Controlled) list.Add(new HireEntry(from, this));
                else if (Controlled && ControlMaster == from) list.Add(new DismissEntry(from, this));

                // Add relationship system context entries (Give Gift, Romance, Set Role, Move to House)
                RelationshipContextMenu.AddEntries(this, pm, list);
            }
        }

        private class HireEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private HeroHireling m_Hero;
            public HireEntry(Mobile from, HeroHireling hero) : base(6129, 3) { m_From = from; m_Hero = hero; }
            public override void OnClick() { m_Hero.Hire(m_From); }
        }

        private class DismissEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private HeroHireling m_Hero;
            public DismissEntry(Mobile from, HeroHireling hero) : base(6128, 3) { m_From = from; m_Hero = hero; }
            public override void OnClick() { m_Hero.Dismiss(); }
        }

        // ══════════════════════════════════════════════════════════════
        //  SERIALIZATION
        // ══════════════════════════════════════════════════════════════

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version (1 = leveling, bonding)
            writer.Write((int)_heroClass);
            writer.Write(_heroTitle);
            writer.Write(_hireCost);
            writer.Write(_level);
            writer.Write(_experience);
            writer.Write(_isBonded);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int v = reader.ReadInt();
            _heroClass = (HeroClass)reader.ReadInt();
            _heroTitle = reader.ReadString();
            _hireCost = reader.ReadInt();
            _lastWander = DateTime.UtcNow;

            if (v >= 1)
            {
                _level = reader.ReadInt();
                _experience = reader.ReadInt();
                _isBonded = reader.ReadInt() != 0;
            }
            else
            {
                _level = 1;
                _experience = 0;
                _isBonded = false;
            }
        }

        public HeroHireling(Serial serial) : base(serial) { }

        private void ApplyClassAppearance() { /* re-setup if class changes via [props */ }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SPAWNER
    // ══════════════════════════════════════════════════════════════════

    public class HeroHirelingSpawner : Item
    {
        private Timer m_Timer;
        private int m_SpawnRange;
        private HeroHireling.HeroClass m_Class;
        private List<HeroHireling> m_Spawned = new List<HeroHireling>();

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnRange { get { return m_SpawnRange; } set { m_SpawnRange = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public HeroHireling.HeroClass ClassType { get { return m_Class; } set { m_Class = value; } }

        [Constructable]
        public HeroHirelingSpawner() : base(0x1F1C)
        {
            Name = "Hero Hireling Spawner"; Movable = false; Visible = false;
            m_SpawnRange = 15; m_Class = (HeroHireling.HeroClass)Utility.Random(10);
            StartTimer();
        }

        [Constructable]
        public HeroHirelingSpawner(HeroHireling.HeroClass heroClass) : this() { m_Class = heroClass; }

        public void StartTimer()
        {
            if (m_Timer == null)
                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(3), CheckSpawn);
        }

        public void StopTimer()
        {
            if (m_Timer != null) { m_Timer.Stop(); m_Timer = null; }
        }

        private void CheckSpawn()
        {
            if (Deleted || Map == null || Map == Map.Internal) return;
            m_Spawned.RemoveAll(h => h == null || h.Deleted);
            if (m_Spawned.Count == 0)
            {
                var hero = new HeroHireling(m_Class);
                hero.MoveToWorld(GetSpawnLocation(), Map);
                m_Spawned.Add(hero);
                if (Utility.RandomDouble() < 0.3)
                {
                    var hero2 = new HeroHireling((HeroHireling.HeroClass)Utility.Random(10));
                    hero2.MoveToWorld(GetSpawnLocation(), Map);
                    m_Spawned.Add(hero2);
                }
            }
        }

        private Point3D GetSpawnLocation()
        {
            for (int i = 0; i < 20; i++)
            {
                var x = X + Utility.RandomMinMax(-m_SpawnRange, m_SpawnRange);
                var y = Y + Utility.RandomMinMax(-m_SpawnRange, m_SpawnRange);
                var loc = new Point3D(x, y, Map.GetAverageZ(x, y));
                if (Map.CanSpawnMobile(loc)) return loc;
            }
            return Location;
        }

        public override void OnDelete()
        {
            StopTimer();
            foreach (var h in m_Spawned) { if (h != null && !h.Deleted) h.Delete(); }
            m_Spawned.Clear();
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_SpawnRange);
            writer.Write((int)m_Class);
            writer.Write(m_Spawned.Count);
            foreach (var h in m_Spawned) writer.Write(h);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_SpawnRange = reader.ReadInt();
            m_Class = (HeroHireling.HeroClass)reader.ReadInt();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var h = reader.ReadMobile() as HeroHireling;
                if (h != null) m_Spawned.Add(h);
            }
            StartTimer();
        }

        public HeroHirelingSpawner(Serial serial) : base(serial) { }
    }
}
