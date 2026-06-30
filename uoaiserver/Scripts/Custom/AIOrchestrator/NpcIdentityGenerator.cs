using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator
{
    public static class NpcIdentityGenerator
    {
        private static readonly string[] Vocations = 
        {
            "blacksmith", "tailor", "carpenter", "alchemist", "healer", "mage",
            "warrior", "ranger", "bard", "tinker", "cook", "farmer",
            "fisherman", "miner", "lumberjack", "shepherd", "merchant", "guard",
            "scholar", "scribe", "bowyer", "fletcher", "armorer", "weaponsmith"
        };

        private static readonly string[] Homelands = 
        {
            "Britain", "Minoc", "Vesper", "Yew", "Trinsic", "Skara Brae",
            "Jhelom", "Moonglow", "Nujel'm", "Magincia", "Cove", "Buccaneer's Den",
            "Ocllo", "Serpent's Hold", "Delucia", "Papua", "Zento", "Wind"
        };

        private static readonly string[] Temperaments = 
        {
            "stoic", "cheerful", "gruff", "wise", "cynical", "optimistic",
            "melancholic", "fiery", "calm", "eccentric", "pragmatic", "dreamer"
        };

        private static readonly string[] SpeechStyles = 
        {
            "formal and precise", "colloquial and warm", "terse and direct",
            "flowery and poetic", "rustic and simple", "scholarly and verbose",
            "gruff and blunt", "whimsical and mysterious", "military and crisp",
            "merchant-like and persuasive"
        };

        private static readonly Dictionary<string, string[]> VocationBackstories = new Dictionary<string, string[]>
        {
            ["blacksmith"] = new[] { "forged the king's crown", "lost an arm to a dragon", "seeks the perfect steel" },
            ["healer"] = new[] { "saved a village from plague", "studied under the Empath Abbey", "carries a guilty secret" },
            ["mage"] = new[] { "was an apprentice to a lich", "burned down their tower", "seeks forbidden knowledge" },
            ["warrior"] = new[] { "survived the Battle of Trinsic", "owes a life debt to a paladin", "hunts a nemesis" },
            ["ranger"] = new[] { "tracked a wyrm for years", "lives by the Ranger's Code", "protects a sacred grove" },
            ["bard"] = new[] { "sang for Lord British", "voice broke a curse", "collects forgotten songs" },
            ["merchant"] = new[] { "lost a fleet to pirates", "knows every trade route", "smuggles rare goods" },
            ["guard"] = new[] { "failed to protect a noble", "patrolled the Yew woods", "dreams of knighthood" }
        };

        private static readonly string[] Drives = 
        {
            "redemption for a past failure", "mastery of their craft", "knowledge of the ancients",
            "protecting the innocent", "finding a lost loved one", "proving their worth",
            "uncovering a conspiracy", "building a legacy", "atonement for a sin",
            "discovering the truth of the Virtues"
        };

        public static NpcIdentity Generate(BaseCreature creature)
        {
            var vocation = Vocations[Utility.Random(Vocations.Length)];
            var homeland = Homelands[Utility.Random(Homelands.Length)];
            var temperament = Temperaments[Utility.Random(Temperaments.Length)];
            var speechStyle = SpeechStyles[Utility.Random(SpeechStyles.Length)];
            var drive = Drives[Utility.Random(Drives.Length)];

            string backstory;
            if (VocationBackstories.TryGetValue(vocation, out var stories))
            {
                backstory = stories[Utility.Random(stories.Length)];
            }
            else
            {
                backstory = "walked a long road to get here";
            }

            if (creature is BaseVendor)
            {
                vocation = "merchant";
            }
            else if (creature is BaseHire)
            {
                vocation = "mercenary";
            }

            return new NpcIdentity
            {
                Name = creature.Name ?? "Unknown",
                Vocation = vocation,
                Homeland = homeland,
                Temperament = temperament,
                Backstory = backstory,
                SpeechStyle = speechStyle,
                PrivateDrive = drive,
                Mood = Utility.Random(30, 70)
            };
        }
    }
}