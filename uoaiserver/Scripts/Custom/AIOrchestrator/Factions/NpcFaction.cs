using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;

namespace Server.AIOrchestrator.Factions
{
    /// <summary>
    /// Represents a faction in the world that NPCs can belong to and players can have reputation with.
    /// Now aligned with Ultima's Eight Virtues and their Vice counterparts.
    /// </summary>
    public class NpcFaction
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Color { get; } // Hue for UI/overhead display
        public VirtueAlignment Virtue { get; }
        public ViceAlignment Vice { get; }
        public string[] EnemyFactionIds { get; }
        public string[] AlliedFactionIds { get; }
        public Func<BaseCreature, bool> MemberPredicate { get; }
        public bool IsVirtuous { get; }

        public NpcFaction(string id, string name, string description, int color, 
            VirtueAlignment virtue, ViceAlignment vice,
            string[] enemyFactionIds, string[] alliedFactionIds, Func<BaseCreature, bool> memberPredicate,
            bool isVirtuous = true)
        {
            Id = id;
            Name = name;
            Description = description;
            Color = color;
            Virtue = virtue;
            Vice = vice;
            EnemyFactionIds = enemyFactionIds ?? Array.Empty<string>();
            AlliedFactionIds = alliedFactionIds ?? Array.Empty<string>();
            MemberPredicate = memberPredicate ?? (_ => false);
            IsVirtuous = isVirtuous;
        }

        public bool IsMember(BaseCreature creature) => MemberPredicate(creature);

        public bool IsEnemyOf(string otherFactionId) => EnemyFactionIds.Contains(otherFactionId);
        public bool IsAlliedWith(string otherFactionId) => AlliedFactionIds.Contains(otherFactionId);

        /// <summary>Pre-defined Ultima Virtue/Vice aligned factions for Britannia.</summary>
        public static class Presets
        {
            // ── Virtue Factions (represent the 8 Virtues) ────────────────

            /// <summary>Honesty — Britain Guard, town watch, lawkeepers</summary>
            public static readonly NpcFaction BritainGuards = new NpcFaction(
                "britain_guards",
                "Britain Guard",
                "The city watch of Lord British, upholders of Honesty in the capital.",
                0x44, // Blue
                VirtueAlignment.Honesty,
                ViceAlignment.Deceit,
                new[] { "orcs", "undead", "bandits", "pirates", "cult_of_deceit" },
                new[] { "trinsic_paladins", "moonglow_mages", "minoc_dwarves" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Guard") || 
                     bc is ArcherGuard || bc is WarriorGuard || bc is BaseGuard)
            );

            /// <summary>Compassion — Healers, shrines, priests of the Virtues</summary>
            public static readonly NpcFaction HealersCircle = new NpcFaction(
                "healers",
                "Healer's Circle",
                "Devoted to Compassion — healers, priests, and caretakers of Britannia.",
                0x44, // Blue
                VirtueAlignment.Compassion,
                ViceAlignment.Cruelty,
                new[] { "necromancers", "orcs", "undead", "bandits" },
                new[] { "britain_guards", "trinsic_paladins", "woodland_protectors" },
                c => c is BaseHealer || c.AI == AIType.AI_Healer ||
                     (c is BaseCreature bc && bc.GetType().Name.Contains("Healer"))
            );

            /// <summary>Valor — Paladins, knights, warriors of virtue</summary>
            public static readonly NpcFaction TrinsicPaladins = new NpcFaction(
                "trinsic_paladins",
                "Trinsic Paladins",
                "The holy warriors of Trinsic, exemplars of Valor.",
                0x482, // Gold
                VirtueAlignment.Valor,
                ViceAlignment.Cowardice,
                new[] { "orcs", "undead", "bandits", "necromancers", "cult_of_deceit" },
                new[] { "britain_guards", "jhelom_mercenaries" },
                c => c.AI == AIType.AI_Paladin ||
                     (c is BaseCreature bc && (bc.GetType().Name.Contains("Paladin") || bc.GetType().Name.Contains("Knight")))
            );

            /// <summary>Justice — Moonglow mages, scholars, arbiters</summary>
            public static readonly NpcFaction MoonglowMages = new NpcFaction(
                "moonglow_mages",
                "Moonglow Mages",
                "The mages of Moonglow who seek truth through Justice and knowledge.",
                0x48, // Green
                VirtueAlignment.Justice,
                ViceAlignment.Injustice,
                new[] { "necromancers", "undead", "cult_of_deceit" },
                new[] { "britain_guards", "trinsic_paladins" },
                c => c.AI == AIType.AI_Mage && 
                     !(c is BaseCreature bc && (bc.GetType().Name.Contains("Necro") || bc.GetType().Name.Contains("Lich")))
            );

            /// <summary>Sacrifice — Minoc miners, smiths, crafters</summary>
            public static readonly NpcFaction MinocDwarves = new NpcFaction(
                "minoc_dwarves",
                "Minoc Crafters",
                "The hard-working smiths and miners of Minoc, knowing the value of Sacrifice.",
                0x3F, // Forest green
                VirtueAlignment.Sacrifice,
                ViceAlignment.Gluttony,
                new[] { "orcs", "bandits", "pirates" },
                new[] { "britain_guards", "moonglow_mages" },
                c => c is BaseVendor && (c.GetType().Name.Contains("Smith") || 
                     c.GetType().Name.Contains("Miner") || c.GetType().Name.Contains("Tinker") ||
                     c.GetType().Name.Contains("Carpenter"))
            );

            /// <summary>Honor — Jhelom mercenaries, warriors</summary>
            public static readonly NpcFaction JhelomMercenaries = new NpcFaction(
                "jhelom_mercenaries",
                "Jhelom Mercenaries",
                "Sellswords of Jhelom bound by a code of Honor above all.",
                0x22, // Red
                VirtueAlignment.Honor,
                ViceAlignment.Dishonor,
                new[] { "orcs", "bandits", "pirates", "cult_of_deceit" },
                new[] { "trinsic_paladins", "britain_guards" },
                c => c.AI == AIType.AI_Melee && 
                     c is BaseCreature bc && (bc.GetType().Name.Contains("Mercenary") || 
                     bc.GetType().Name.Contains("Guard") || bc.GetType().Name.Contains("Soldier"))
            );

            /// <summary>Spirituality — Woodland protectors, druids, rangers</summary>
            public static readonly NpcFaction WoodlandProtectors = new NpcFaction(
                "woodland_protectors",
                "Woodland Protectors",
                "Guardians of the forests and wilds, connected to the Spirituality of nature.",
                0x3F, // Forest green
                VirtueAlignment.Spirituality,
                ViceAlignment.Unbelief,
                new[] { "orcs", "undead", "necromancers" },
                new[] { "healers", "trinsic_paladins" },
                c => c.AI == AIType.AI_Animal || c.AI == AIType.AI_Predator ||
                     (c is BaseCreature bc && (bc.GetType().Name.Contains("Ranger") || 
                      bc.GetType().Name.Contains("Druid") || bc.GetType().Name.Contains("Forest")))
            );

            /// <summary>Humility — Peasants, farmers, beggars, simple folk</summary>
            public static readonly NpcFaction HumbleFolk = new NpcFaction(
                "humble_folk",
                "Humble Folk",
                "Common people of Britannia who embody Humility in their daily lives.",
                0x44, // Blue
                VirtueAlignment.Humility,
                ViceAlignment.Pride,
                new[] { "bandits", "orcs", "undead" },
                new[] { "britain_guards", "healers" },
                c => c is BaseCreature bc && (bc is Farmer || bc is Peasant || 
                     bc is Rancher || bc.GetType().Name.Contains("Beggar") ||
                     bc.GetType().Name.Contains("Shepherd"))
            );

            // ── Vice Factions (corrupt/evil counterparts) ───────────────

            /// <summary>Deceit — Shadowlords, dark cultists, manipulators</summary>
            public static readonly NpcFaction CultOfDeceit = new NpcFaction(
                "cult_of_deceit",
                "Cult of Deceit",
                "Worshippers of the Shadowlords who spread lies and corruption.",
                0x1, // Dark grey
                VirtueAlignment.Honesty,
                ViceAlignment.Deceit,
                new[] { "britain_guards", "trinsic_paladins", "jhelom_mercenaries", "healers" },
                new[] { "necromancers", "orcs" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Shadow") || 
                     bc.GetType().Name.Contains("Cult") || bc.GetType().Name.Contains("Dark") ||
                     bc.GetType().Name.Contains("Deceit")),
                false
            );

            /// <summary>Cruelty — Orcs, trolls, ogres, brutal humanoids</summary>
            public static readonly NpcFaction OrcishHorde = new NpcFaction(
                "orcs",
                "Orcish Horde",
                "Brutal raiders who delight in Cruelty and destruction.",
                0x22, // Red
                VirtueAlignment.Compassion,
                ViceAlignment.Cruelty,
                new[] { "britain_guards", "trinsic_paladins", "healers", "humble_folk" },
                new[] { "undead_scourge", "cult_of_deceit" },
                c => c is BaseCreature bc && bc.GetType().Name.Contains("Orc"),
                false
            );

            /// <summary>Cowardice — Bandits, thieves, brigands</summary>
            public static readonly NpcFaction BanditsGuild = new NpcFaction(
                "bandits",
                "Bandit's Guild",
                "Cowardly cutthroats who prey on the weak and helpless.",
                0x45, // Orange
                VirtueAlignment.Valor,
                ViceAlignment.Cowardice,
                new[] { "britain_guards", "trinsic_paladins", "humble_folk", "jhelom_mercenaries" },
                new[] { "orcs", "pirates" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Bandit") || 
                     bc.GetType().Name.Contains("Thief") || bc.GetType().Name.Contains("Brigand") ||
                     bc.GetType().Name.Contains("Rogue")),
                false
            );

            /// <summary>Injustice — Necromancers, liches, dark mages</summary>
            public static readonly NpcFaction NecromancerCult = new NpcFaction(
                "necromancers",
                "Necromancer Cult",
                "Practitioners of dark magic who pervert Justice for their own ends.",
                0x497, // Dark purple
                VirtueAlignment.Justice,
                ViceAlignment.Injustice,
                new[] { "healers", "trinsic_paladins", "moonglow_mages", "britain_guards" },
                new[] { "undead_scourge", "cult_of_deceit" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Necro") || 
                     bc.GetType().Name.Contains("Lich") || bc.GetType().Name.Contains("Dark")),
                false
            );

            /// <summary>Gluttony — Undead (endless hunger for the living)</summary>
            public static readonly NpcFaction UndeadScourge = new NpcFaction(
                "undead_scourge",
                "Undead Scourge",
                "Mindless corpses driven by an endless Gluttony for the living.",
                0x1, // Dark grey
                VirtueAlignment.Sacrifice,
                ViceAlignment.Gluttony,
                new[] { "britain_guards", "trinsic_paladins", "healers", "humble_folk" },
                new[] { "necromancers", "cult_of_deceit" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Skeleton") || 
                    bc.GetType().Name.Contains("Zombie") || bc.GetType().Name.Contains("Ghost") ||
                    bc.GetType().Name.Contains("Wraith") || bc.GetType().Name.Contains("Spectre") ||
                    bc.GetType().Name.Contains("Mummy") || bc.GetType().Name.Contains("Ghoul")),
                false
            );

            /// <summary>Dishonor — Pirates, buccaneers, corsairs</summary>
            public static readonly NpcFaction PirateBrotherhood = new NpcFaction(
                "pirates",
                "Pirate Brotherhood",
                "Seafaring raiders with no Honor, who live by plunder and treachery.",
                0x45, // Orange
                VirtueAlignment.Honor,
                ViceAlignment.Dishonor,
                new[] { "merchants", "britain_guards", "jhelom_mercenaries" },
                new[] { "bandits", "smugglers" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Pirate") || 
                    bc.GetType().Name.Contains("Buccaneer") || bc.GetType().Name.Contains("Corsair")),
                false
            );

            /// <summary>Unbelief — Void creatures, daemons, abyssals</summary>
            public static readonly NpcFaction VoidAbyss = new NpcFaction(
                "void_abyss",
                "Void Abyss",
                "Otherworldly horrors from beyond, denying all Spirituality and belief.",
                0x1, // Dark grey
                VirtueAlignment.Spirituality,
                ViceAlignment.Unbelief,
                new[] { "britain_guards", "trinsic_paladins", "healers", "woodland_protectors" },
                new[] { "necromancers", "cult_of_deceit" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Daemon") || 
                     bc.GetType().Name.Contains("Void") || bc.GetType().Name.Contains("Abyss") ||
                     bc.GetType().Name.Contains("Balron") || bc.GetType().Name.Contains("Devourer")),
                false
            );

            /// <summary>Pride — Evil mages, sorcerers, those who see themselves as above all</summary>
            public static readonly NpcFaction PridefulSorcerers = new NpcFaction(
                "prideful_sorcerers",
                "Prideful Sorcerers",
                "Arrogant mages who believe their power places them above all Virtues.",
                0x497, // Dark purple
                VirtueAlignment.Humility,
                ViceAlignment.Pride,
                new[] { "healers", "britain_guards", "humble_folk" },
                new[] { "necromancers", "cult_of_deceit" },
                c => c.AI == AIType.AI_Mage && c is BaseCreature bc &&
                     (bc.GetType().Name.Contains("Evil") || bc.GetType().Name.Contains("Sorcerer") || 
                      bc.GetType().Name.Contains("Wizard")),
                false
            );

            // ── Neutral / Mercantile ───────────────────────────────────

            /// <summary>Neutral merchants — not aligned to any Virtue/Vice</summary>
            public static readonly NpcFaction MerchantLeague = new NpcFaction(
                "merchants",
                "Merchant League",
                "Traders and craftsfolk who value profit above Virtue or Vice.",
                0x48, // Green
                VirtueAlignment.Honesty,
                ViceAlignment.Deceit,
                new[] { "bandits", "pirates", "orcs" },
                new[] { "britain_guards", "minoc_dwarves" },
                c => c is BaseVendor && !c.GetType().Name.Contains("Smith") && 
                     !c.GetType().Name.Contains("Miner") && !c.GetType().Name.Contains("Tinker") &&
                     !c.GetType().Name.Contains("Carpenter") && !c.GetType().Name.Contains("Farmer") &&
                     !c.GetType().Name.Contains("Healer"),
                true
            );

            // ── Dragon factions ────────────────────────────────────────
            public static readonly NpcFaction DragonBrood = new NpcFaction(
                "dragons",
                "Dragon Brood",
                "Ancient wyrms and drakes — forces of nature aligned to no virtue.",
                0x44, // Blue
                VirtueAlignment.Valor,
                ViceAlignment.Cowardice,
                new[] { "orcs", "undead_scourge", "bandits" },
                new[] { "woodland_protectors" },
                c => c is BaseCreature bc && (bc.GetType().Name.Contains("Dragon") || 
                     bc.GetType().Name.Contains("Drake") || bc.GetType().Name.Contains("Wyrm") ||
                     bc.GetType().Name.Contains("Wyvern")),
                true
            );

            /// <summary>All registered factions</summary>
            public static readonly NpcFaction[] All = new[]
            {
                BritainGuards, HealersCircle, TrinsicPaladins, MoonglowMages,
                MinocDwarves, JhelomMercenaries, WoodlandProtectors, HumbleFolk,
                CultOfDeceit, OrcishHorde, BanditsGuild, NecromancerCult,
                UndeadScourge, PirateBrotherhood, VoidAbyss, PridefulSorcerers,
                MerchantLeague, DragonBrood
            };

            public static readonly Dictionary<string, NpcFaction> ById = new Dictionary<string, NpcFaction>();
            static Presets()
            {
                foreach (var f in All)
                    ById[f.Id] = f;
            }
        }
    }

    /// <summary>Ultima's Eight Virtues</summary>
    public enum VirtueAlignment
    {
        Honesty,
        Compassion,
        Valor,
        Justice,
        Sacrifice,
        Honor,
        Spirituality,
        Humility
    }

    /// <summary>The Eight Vices — corrupt counterparts of the Virtues</summary>
    public enum ViceAlignment
    {
        Deceit,
        Cruelty,
        Cowardice,
        Injustice,
        Gluttony,
        Dishonor,
        Unbelief,
        Pride
    }
}