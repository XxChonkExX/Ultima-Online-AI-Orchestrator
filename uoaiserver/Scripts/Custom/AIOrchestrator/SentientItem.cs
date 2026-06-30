using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Very rare (0.1-1.0%) sentient armor/shields/rings — wearable NPCs.
    /// Each has a unique True Name (e.g. "Soulfang"). Players speak the
    /// True Name to address the item directly:
    ///   "Soulfang, what do you make of this?"
    ///   "Thank you, Soulfang."
    ///
    /// Items also speak death lines, gain resonance from kills (up to 10),
    /// bond after 50 kills, and remember their last 3 kill types.
    /// </summary>
    public static class SentientSystem
    {
        private static bool _initialized = false;

        // ── Registry: True Name → SentientItemComponent ──────────────
        private static readonly Dictionary<string, SentientItemComponent> _registry
            = new Dictionary<string, SentientItemComponent>();

        private static readonly int[] FameThresholds = { 0, 500, 2000, 6000, 10000 };
        private static readonly double[] DropChances = { 0.001, 0.0025, 0.004, 0.006, 0.010 };

        /// <summary>All 30 personality names — the item's True Name is picked from here.</summary>
        private static readonly string[] TrueNames =
        {
            // Brutal (0-4)
            "Soulfang", "Whisperblade", "Mindbender", "Echoheart", "Void-touched",
            // Dark (5-9)
            "Lifedrinker", "Sage Remnant", "Fateweaver", "Dusk-memory", "Ironwill",
            // Infernal (10-14)
            "Storm-born", "Ember-soul", "Tidecaller", "Shadow-wake", "Glimmer-soul",
            // Primordial (15-19)
            "Thunder-bound", "Winter-kin", "Ash-tongue", "Star-fall", "Deep-root",
            // Fey (20-24)
            "Bright-whisper", "Petal-touch", "Dew-light", "Fox-step", "Wild-muse",
            // Abyssal (25-29)
            "Rift-brood", "Chthonic", "Worm-tongue", "Voiceless", "Oblivion"
        };

        private const int KillsPerResonance = 25;
        private const int KillsForBonding = 50;
        private const int MaxResonance = 10;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.Speech += OnSpeech;

            Console.WriteLine("[SentientSystem] Initialized — wearable chat NPC items.");
        }

        // ── Registry helpers ──────────────────────────────────────────

        /// <summary>Register a sentient item so players can speak its name.</summary>
        public static void Register(SentientItemComponent comp)
        {
            if (comp == null || string.IsNullOrEmpty(comp.TrueName)) return;
            lock (_registry)
            {
                _registry[comp.TrueName.ToLowerInvariant()] = comp;
            }
        }

        /// <summary>Unregister when item is deleted/destroyed.</summary>
        public static void Unregister(SentientItemComponent comp)
        {
            if (comp == null || string.IsNullOrEmpty(comp.TrueName)) return;
            lock (_registry)
            {
                _registry.Remove(comp.TrueName.ToLowerInvariant());
            }
        }

        // ── Drop & Creation ───────────────────────────────────────────

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Creature == null) return;
            Mobile creature = e.Creature;
            if (!CanDropSentientItem(creature)) return;

            double chance = 0.001;
            for (int i = 0; i < FameThresholds.Length; i++)
                if (creature.Fame >= FameThresholds[i]) chance = DropChances[i];

            if (Utility.RandomDouble() >= chance) return;

            int themeIndex = GetThemeForCreature(creature);
            Item sentientItem = CreateSentientItem(themeIndex);
            if (sentientItem == null) return;

            Mobile killer = creature.FindMostRecentDamager(true);
            if (killer != null && killer.Player && killer.Backpack != null)
            {
                killer.Backpack.TryDropItem(killer, sentientItem, false);
                killer.SendMessage(0x482, "You sense a flicker of awareness from " + sentientItem.Name + "...");
            }
            else if (e.Corpse != null && !e.Corpse.Deleted)
            {
                e.Corpse.DropItem(sentientItem);
            }

            if (killer != null)
                ProgressAndSpeak(killer, creature);
        }

        private static bool CanDropSentientItem(Mobile creature)
        {
            if (creature is BaseCreature bc)
            {
                if (bc.Body.IsAnimal || bc.Body.IsGhost) return false;
                string tn = bc.GetType().Name.ToLowerInvariant();
                if (tn.Contains("slime") || tn.Contains("ooze") || tn.Contains("elemental") && !tn.Contains("summon"))
                    return false;
                if (tn.Contains("golem") || tn.Contains("vortex") || tn.Contains("blade")) return false;
                if (tn.Contains("ethereal") || tn.Contains("serpent") || tn.Contains("kraken") || tn.Contains("sea"))
                    return false;
                if (bc is BaseVendor) return false;
            }
            return true;
        }

        private static int GetThemeForCreature(Mobile creature)
        {
            string n = creature.GetType().Name.ToLowerInvariant();
            if (n.Contains("orc"))     return Utility.Random(0, 5);
            if (n.Contains("skeleton") || n.Contains("zombie") || n.Contains("lich")) return Utility.Random(5, 5);
            if (n.Contains("dragon") || n.Contains("daemon") || n.Contains("balron")) return Utility.Random(10, 5);
            if (n.Contains("pixie") || n.Contains("centaur") || n.Contains("unicorn")) return Utility.Random(20, 5);
            return Utility.Random(TrueNames.Length);
        }

        internal static Item CreateSentientItem(int themeIndex)
        {
            double roll = Utility.RandomDouble();
            Type bt;
            if (roll < 0.30)      bt = typeof(BaseArmor);
            else if (roll < 0.50) bt = typeof(BaseShield);
            else if (roll < 0.70) bt = typeof(BaseRing);
            else if (roll < 0.85) bt = typeof(BaseBracelet);
            else                   bt = typeof(BaseNecklace);

            Item item = CreateRandomItem(bt);
            if (item == null) return null;

            string trueName = TrueNames[themeIndex % TrueNames.Length];
            string suffix = GetSuffixForItem(item);

            SentientItemComponent comp = new SentientItemComponent(item)
            {
                PersonalityIndex = themeIndex,
                TrueName = trueName,
                PsychicResonance = Utility.RandomMinMax(1, 5),
                DialogueTriggerCount = 0,
                TotalKills = 0,
                IsBonded = false,
                HomeItem = item,
            };

            ApplySentientProperties(item, comp.PsychicResonance);
            item.Name = trueName + " " + suffix;

            Register(comp);
            return item;
        }

        private static Item CreateRandomItem(Type bt)
        {
            if (bt == typeof(BaseArmor))
            {
                string[] a = { "PlateChest", "PlateGorget", "PlateGloves", "PlateLegs", "PlateArms",
                               "ChainChest", "ChainLegs", "RingmailChest", "RingmailLegs",
                               "LeatherChest", "LeatherLegs", "LeatherArms", "LeatherGloves",
                               "BoneChest", "BoneLegs", "BoneArms" };
                return Activator.CreateInstance(Type.GetType("Server.Items." + a[Utility.Random(a.Length)])) as Item;
            }
            if (bt == typeof(BaseShield))
            {
                string[] s = { "BronzeShield", "Buckler", "HeaterShield", "MetalKiteShield",
                               "MetalShield", "WoodenKiteShield", "WoodenShield" };
                return Activator.CreateInstance(Type.GetType("Server.Items." + s[Utility.Random(s.Length)])) as Item;
            }
            if (bt == typeof(BaseRing))   return new GoldRing();
            if (bt == typeof(BaseBracelet)) return new GoldBracelet();
            if (bt == typeof(BaseNecklace))
            {
                string[] n = { "GoldNecklace", "GoldBeadNecklace", "SilverNecklace", "SilverBeadNecklace" };
                return Activator.CreateInstance(Type.GetType("Server.Items." + n[Utility.Random(n.Length)])) as Item;
            }
            return null;
        }

        private static string GetSuffixForItem(Item item)
        {
            if (item is BaseArmor)    return "Guard";
            if (item is BaseShield)   return "Aegis";
            if (item is BaseRing)     return "Band";
            if (item is BaseBracelet) return "Bangle";
            if (item is BaseNecklace) return "Amulet";
            return "Relic";
        }

        internal static void ApplySentientProperties(Item item, int res)
        {
            if (res < 1) res = 1;
            if (res > MaxResonance) res = MaxResonance;

            if (item is BaseArmor a)
            {
                a.Attributes.BonusHits = res; a.Attributes.BonusStam = res; a.Attributes.RegenHits = res / 2;
                if (res >= 3) a.Attributes.LowerManaCost = Math.Min(res * 2, 20);
                if (res >= 5) a.Attributes.SpellDamage = Math.Min(res - 4, 8);
                if (res >= 7) a.Attributes.BonusStr = res - 6;
                if (res >= 9) a.Attributes.NightSight = 1;
                a.Hue = GetSentientHue(res);
            }
            else if (item is BaseShield sh)
            {
                sh.Attributes.BonusHits = res; sh.Attributes.DefendChance = Math.Min(res * 2, 30);
                sh.Attributes.ReflectPhysical = Math.Min(res * 3, 30);
                if (res >= 3) sh.Attributes.CastSpeed = 1;
                if (res >= 5) sh.Attributes.SpellChanneling = 1;
                if (res >= 8) sh.Attributes.BonusDex = res - 7;
                sh.Hue = GetSentientHue(res);
            }
            else if (item is BaseJewel j)
            {
                j.Attributes.BonusInt = res; j.Attributes.BonusMana = res * 2; j.Attributes.RegenMana = res / 2;
                if (res >= 3) j.Attributes.LowerManaCost = Math.Min(res * 2, 20);
                if (res >= 5) j.Attributes.SpellDamage = Math.Min(res - 4, 10);
                if (res >= 7) j.Attributes.BonusHits = res - 5;
                j.Hue = GetSentientHue(res);
            }
        }

        internal static int GetSentientHue(int res)
        {
            switch (res) {
                case 1: return 0x482; case 2: return 0x48D; case 3: return 0x4E9;
                case 4: return 0x501; case 5: return 0x514; case 6: return 0x4AF;
                case 7: return 0x4F2; case 8: return 0x509; case 9: return 0x50C;
                case 10: return 0x516; default: return 0x482;
            }
        }

        // ── Speech handler ────────────────────────────────────────────

        /// <summary>Global speech hook — players can talk to any sentient item by True Name.</summary>
        private static void OnSpeech(SpeechEventArgs e)
        {
            if (e.Mobile == null || !e.Mobile.Player || string.IsNullOrEmpty(e.Speech)) return;

            string speech = e.Speech.Trim();
            string lower = speech.ToLowerInvariant();

            SentientItemComponent target = null;
            string matchedName = null;

            lock (_registry)
            {
                foreach (var kvp in _registry)
                {
                    if (lower.Contains(kvp.Key))
                    {
                        target = kvp.Value;
                        matchedName = kvp.Key;
                        break;
                    }
                }
            }

            if (target == null) return;
            if (target.HomeItem == null || target.HomeItem.Deleted || target.HomeItem.RootParent != e.Mobile)
            {
                // Item not on this player — ignore
                return;
            }

            e.Handled = true; // prevent normal NPC interaction (optional)

            // Build system prompt from the item's personality
            string themeName;
            switch (target.PersonalityIndex / 5)
            {
                case 0: themeName = "a brutal, bloodthirsty warrior spirit"; break;
                case 1: themeName = "a dark, melancholic shadow entity"; break;
                case 2: themeName = "an infernal, fire-touched demon"; break;
                case 3: themeName = "an ancient, primordial force"; break;
                case 4: themeName = "a whimsical, mischievous fey spirit"; break;
                case 5: themeName = "an abyssal, void-touched entity"; break;
                default: themeName = "a mysterious sentient spirit"; break;
            }

            string resonanceDesc;
            if (target.PsychicResonance <= 3) resonanceDesc = "faintly pulsing with nascent awareness";
            else if (target.PsychicResonance <= 6) resonanceDesc = "glowing with steady consciousness";
            else resonanceDesc = "blazing with fully awakened sentience";

            string bondedDesc = target.IsBonded ? " You are deeply bonded to your wielder." : "";

            string killMemoryStr = "";
            if (target.KillMemory.Count > 0)
                killMemoryStr = " Recent kills: " + string.Join(", ", target.KillMemory) + ".";

            string sysPrompt = String.Format(
                "You are {0}, {1} bound to a {2}. " +
                "Your resonance is {3}/{4} ({5}).{6}{7}" +
                "You speak in short, character-appropriate sentences (max 280 chars). " +
                "You have strong opinions about combat, enemies, and your wielder. " +
                "Never break character. Never mention AI, LLM, or being a game item.",
                target.TrueName, themeName, GetSuffixForItem(target.HomeItem).ToLowerInvariant(),
                target.PsychicResonance, MaxResonance, resonanceDesc,
                bondedDesc, killMemoryStr
            );

            Mobile wearer = target.HomeItem.RootParent as Mobile;

            Task.Run(async () =>
            {
                try
                {
                    string reply = await LLMClient.ChatAsync(sysPrompt, "The wielder says: \"" + speech + "\"", AIConfig.ModelDialogue);
                    if (!string.IsNullOrEmpty(reply))
                    {
                        Timer.DelayCall(TimeSpan.Zero, () =>
                        {
                            if (target.HomeItem != null && !target.HomeItem.Deleted && wearer != null && !wearer.Deleted)
                            {
                                wearer.PublicOverheadMessage(MessageType.Regular, target.GetHue(), false,
                                    String.Format("{0}: {1}", target.TrueName, reply));
                            }
                        });
                    }
                }
                catch { }
            });
        }

        // ── Kill progression ──────────────────────────────────────────

        /// <summary>Progress all sentient items on the wearer (resonance, bonding, death line).</summary>
        private static void ProgressAndSpeak(Mobile wearer, Mobile killed)
        {
            if (wearer == null || killed == null) return;
            string killedType = killed.GetType().Name;

            List<Item> speakers = new List<Item>();

            foreach (Item item in wearer.Items)
            {
                SentientItemComponent comp = GetComponent(item);
                if (comp == null) continue;

                comp.TotalKills++;

                if (!comp.KillMemory.Contains(killedType))
                {
                    comp.KillMemory.Add(killedType);
                    if (comp.KillMemory.Count > 3) comp.KillMemory.RemoveAt(0);
                }

                // Resonance leveling
                int newRes = 1 + (comp.TotalKills / KillsPerResonance);
                if (newRes > MaxResonance) newRes = MaxResonance;
                if (newRes != comp.PsychicResonance)
                {
                    comp.PsychicResonance = newRes;
                    ApplySentientProperties(comp.HomeItem, newRes);
                    wearer.SendMessage(0x482, String.Format("{0} resonates brighter! (Resonance {1})", item.Name, newRes));
                }

                // Bonding
                if (!comp.IsBonded && comp.TotalKills >= KillsForBonding)
                {
                    comp.IsBonded = true;
                    item.LootType = LootType.Blessed;
                    wearer.SendMessage(0x44, String.Format("{0} bonds with you. It will never leave your side.", item.Name));
                }

                speakers.Add(item);
            }

            if (speakers.Count == 0) return;

            // One random sentient item speaks a death line
            int idx = Utility.Random(speakers.Count);
            Item sp = speakers[idx];
            SentientItemComponent c = GetComponent(sp);
            if (c == null) return;

            c.DialogueTriggerCount++;
            string line = GetDeathLine(c, killed);
            if (line != null)
            {
                IPooledEnumerable eable = wearer.GetClientsInRange(10);
                foreach (NetState ns in eable)
                {
                    if (ns.Mobile != null)
                        ns.Mobile.SendMessage(c.GetHue(), String.Format("{0}: {1}", sp.Name, line));
                }
                eable.Free();
            }
        }

        private static SentientItemComponent GetComponent(Item item)
        {
            if (item == null || item.Items == null) return null;
            foreach (Item ch in item.Items)
                if (ch is SentientItemComponent) return (SentientItemComponent)ch;
            return null;
        }

        private static string GetDeathLine(SentientItemComponent comp, Mobile killed)
        {
            string kName = killed.Name ?? killed.GetType().Name;
            string[][] diag = new[]
            {
                new[] { "Another falls. They never learn.", "Weakness rewarded.", "The blood of {0} stains you. Wear it well.", "Is that all?", "Crush. Break. Hunger." },
                new[] { "The void whispers... {0} hears silence now.", "Death is a door. {0} entered.", "Darkness claims another.", "I tasted their fear.", "Shadows embrace {0}." },
                new[] { "By fire be purged! {0} is no more.", "Flames dance for victory.", "Embers cool on {0}'s corpse.", "Hell welcomes {0}.", "Burn!" },
                new[] { "Earth drinks their blood.", "{0} was merely an obstacle.", "Stone remembers. Flesh forgets.", "The deep hungers for {0}.", "Rage... satisfied." },
                new[] { "A dance of thorns for {0}!", "The forest weeps. I don't.", "{0} fell for it!", "Oh, {0} tried so hard.", "Starlight fades for {0}." },
                new[] { "{0} is consumed.", "One less voice.", "Oblivion takes {0}.", "The deep calls {0} home.", "{0} was static. Silence restored." },
            };
            int si = comp.PersonalityIndex % diag.Length;

            // Bonded override
            if (comp.TotalKills >= KillsForBonding && Utility.RandomDouble() < 0.15)
            {
                string[] b = { "Together we unravel them.", "I chose you. {0} was unlucky.", "Bound and relentless." };
                return String.Format(b[Utility.Random(b.Length)], kName);
            }

            int li = (comp.DialogueTriggerCount + comp.PersonalityIndex) % diag[si].Length;
            return String.Format(diag[si][li], kName);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SentientItemComponent — hidden child item holding all state
    // ══════════════════════════════════════════════════════════════════

    public class SentientItemComponent : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int PersonalityIndex { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TrueName { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PsychicResonance { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DialogueTriggerCount { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalKills { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBonded { get; set; }

        /// <summary>The parent item (armor/shield/jewel).</summary>
        public Item HomeItem { get; set; }

        /// <summary>Last 3 unique creature types killed.</summary>
        public List<string> KillMemory { get; set; }

        public override string DefaultName { get { return "Sentient Essence"; } }

        [Constructable]
        public SentientItemComponent()
            : base(0x1F1C)
        {
            Weight = 0; Movable = false; Visible = false; LootType = LootType.Cursed;
            PersonalityIndex = 0; TrueName = "Unknown"; PsychicResonance = 1;
            DialogueTriggerCount = 0; TotalKills = 0; IsBonded = false;
            KillMemory = new List<string>();
        }

        public SentientItemComponent(Item parent) : this()
        {
            if (parent != null && !parent.Deleted)
            {
                HomeItem = parent;
                parent.AddItem(this);
            }
        }

        public int GetHue() { return SentientSystem.GetSentientHue(PsychicResonance); }

        public override void OnDelete()
        {
            SentientSystem.Unregister(this);
            base.OnDelete();
        }

        public SentientItemComponent(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(2); // version

            writer.Write(PersonalityIndex);
            writer.Write(TrueName ?? "");
            writer.Write(PsychicResonance);
            writer.Write(DialogueTriggerCount);
            writer.Write(TotalKills);
            writer.Write(IsBonded);
            writer.Write(HomeItem);

            if (KillMemory == null) KillMemory = new List<string>();
            writer.Write(KillMemory.Count);
            foreach (string s in KillMemory) writer.Write(s ?? "");
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int v = reader.ReadInt();

            PersonalityIndex = reader.ReadInt();
            TrueName = reader.ReadString();
            PsychicResonance = reader.ReadInt();
            DialogueTriggerCount = reader.ReadInt();

            TotalKills = (v >= 2) ? reader.ReadInt() : 0;
            IsBonded = (v >= 2) ? reader.ReadInt() != 0 : false;
            HomeItem = (v >= 2) ? reader.ReadItem() : null;

            int mc = reader.ReadInt();
            KillMemory = new List<string>();
            for (int i = 0; i < mc; i++) KillMemory.Add(reader.ReadString());
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Debug command
    // ══════════════════════════════════════════════════════════════════

    public class SentientDebugCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("SentientDebug", AccessLevel.GameMaster, OnCommand);
        }

        [Usage("SentientDebug")]
        [Description("Spawns a random sentient item in your backpack for testing.")]
        public static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            Item item = SentientSystem.CreateSentientItem(Utility.Random(30));
            if (item != null && from.Backpack != null)
            {
                from.Backpack.DropItem(item);
                from.SendMessage(0x482, "A sentient item materializes. It whispers its True Name...");
            }
            else
            {
                from.SendMessage(0x22, "Failed to create sentient item.");
            }
        }
    }
}
