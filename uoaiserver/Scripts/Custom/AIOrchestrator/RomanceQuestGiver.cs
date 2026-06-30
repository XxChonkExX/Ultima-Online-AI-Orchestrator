using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// A quest-giving NPC that offers romance quests to players.
    /// 3-step chain:
    ///   1) Deliver a rare flower
    ///   2) Defend a spawned NPC
    ///   3) Craft a ring and propose
    /// </summary>
    public class RomanceQuestGiver : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override bool IsActiveVendor => false;

        public override void InitSBInfo()
        {
            m_SBInfos.Clear();
        }

        [Constructable]
        public RomanceQuestGiver() : base("the Matchmaker")
        {
            Body = 0x191; // Female
            Name = NameList.RandomName("female");
            Hue = Utility.RandomSkinHue();
            Blessed = true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && from.InRange(this, 4))
            {
                // Check quest state
                var questState = GetQuestState(pm);

                switch (questState)
                {
                    case 0: // No quest started
                        OfferQuest(pm);
                        break;
                    case 1: // Step 1 - need flower
                        pm.SendMessage(0x44, "Bring me a Nightshade flower as proof of your romantic intent.");
                        break;
                    case 2: // Step 2 - need to defend
                        pm.SendMessage(0x44, "Your love interest is in danger! Defend them!");
                        break;
                    case 3: // Step 3 - need ring
                        pm.SendMessage(0x44, "Craft a Gold Ring and give it to your beloved as a proposal.");
                        break;
                    default: // Completed
                        SayTo(pm, true, "You have already proven your devotion. Go enjoy your romance!");
                        break;
                }
            }
        }

        private int GetQuestState(PlayerMobile pm)
        {
            // Simple quest state tracking via PlayerDeedTracker or a custom tag
            var deed = PlayerDeedTracker.GetDeed(pm, "RomanceQuest");
            if (deed == null) return 0; // Not started

            return deed switch
            {
                "Step1" => 1,
                "Step2" => 2,
                "Step3" => 3,
                "Complete" => 4,
                _ => 0
            };
        }

        private void OfferQuest(PlayerMobile pm)
        {
            SayTo(pm, true, "Ah, a hopeful romantic! I can help you win the heart of your chosen one.");
            SayTo(pm, true, "First, bring me a Nightshade — the dark flower of everlasting love.");
            PlayerDeedTracker.RecordDeed(pm, "RomanceQuest", "Step1");
            pm.SendMessage(0x44, "Quest: Romance — Step 1: Bring a Nightshade to the Matchmaker.");
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (from is PlayerMobile pm && dropped is Nightshade ns)
            {
                var state = GetQuestState(pm);
                if (state == 1)
                {
                    ns.Delete();
                    PlayerDeedTracker.RecordDeed(pm, "RomanceQuest", "Step2");
                    SayTo(pm, true, "Excellent! Now, true love must be protected. Defend your beloved!");
                    // Spawn a weak attacker near the player
                    SpawnAttacker(pm);
                    pm.SendMessage(0x44, "Quest: Romance — Step 2: Defend yourself from the jealous rival!");
                    return true;
                }
            }

            if (from is PlayerMobile pm2 && dropped is BaseRing ring && ring.Name != null && ring.Name.ToLower().Contains("gold"))
            {
                var state = GetQuestState(pm2);
                if (state == 3)
                {
                    ring.Delete();
                    PlayerDeedTracker.RecordDeed(pm2, "RomanceQuest", "Complete");
                    SayTo(pm2, true, "A ring! Now go propose to your beloved. True love conquers all!");
                    // Grant a reward
                    GiveRomanceReward(pm2);
                    pm2.SendMessage(0x44, "Quest: Romance — Complete! Give a Love Letter to your beloved to start the romance!");
                    return true;
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        private void SpawnAttacker(PlayerMobile pm)
        {
            if (pm.Map == null || pm.Map == Map.Internal) return;

            BaseCreature attacker = new Mongbat();
            attacker.Name = "Jealous Rival";
            attacker.MoveToWorld(pm.Location, pm.Map);
            attacker.Combatant = pm;
            attacker.SendMessage(0x26, "You'll never find true love!");
            pm.SendMessage(0x26, "A jealous rival attacks!");
        }

        private void GiveRomanceReward(PlayerMobile pm)
        {
            // Give a love letter to start the romance
            var letter = new LoveLetter();
            pm.Backpack?.DropItem(letter);
            pm.SendMessage(0x44, "You received a Love Letter! Use it to express your feelings.");

            // Also boost their relationship with the matchmaker
            NPCRelationshipSystem.RecordPositiveInteraction(pm, this, 100);
        }

        public RomanceQuestGiver(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }
}
