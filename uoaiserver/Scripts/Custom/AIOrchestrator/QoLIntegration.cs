using System;
using System.Collections.Generic;
using Server.Commands;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Integrates all new QoL features (gumps, items, commands, context menus)
    /// into the existing ServUO systems. Called from AIOrchestratorInit.
    /// </summary>
    public static class QoLIntegration
    {
        public static void Initialize()
        {
            // Context menu: View Relationship on AI-enabled NPCs
            EventSink.ContextMenu += OnContextMenu;

            // Creature death: BountyHunterBadge tracking
            EventSink.CreatureDeath += OnCreatureDeath;

            // Commands
            CommandSystem.Register("ViewRelations", AccessLevel.Player, ViewRelations_OnCommand);
            CommandSystem.Register("AddCommunityBoard", AccessLevel.GameMaster, AddCommunityBoard_OnCommand);
            CommandSystem.Register("AddBountyBoard", AccessLevel.GameMaster, AddBountyBoard_OnCommand);
            CommandSystem.Register("AddRomanceGiver", AccessLevel.GameMaster, AddRomanceGiver_OnCommand);
            CommandSystem.Register("HirelingMarket", AccessLevel.Player, HirelingMarket_OnCommand);

            Console.WriteLine("[QoLIntegration] Gumps, items, commands, and context menus wired.");
        }

        // Context menu: "View Relationship" on any AI-enabled or BaseCreature NPC

        private static void OnContextMenu(ContextMenuEventArgs e)
        {
            if (!(e.Target is BaseCreature npc)) return;
            if (!(e.Mobile is PlayerMobile pm)) return;
            if (!npc.Controlled && !npc.Tamable && !(npc is BaseVendor)) return;

            // Add "View Relationship" entry
            e.Entries.Add(new ViewRelationshipEntry(npc, pm));
        }

        private class ViewRelationshipEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _pm;

            public ViewRelationshipEntry(BaseCreature npc, PlayerMobile pm)
                : base(6169, 3) // "View" entry
            {
                _npc = npc;
                _pm = pm;
            }

            public override void OnClick()
            {
                if (_pm.InRange(_npc, 4))
                {
                    _pm.SendGump(new RelationshipGump(_pm));
                }
                else
                {
                    _pm.SendMessage("That creature is too far away.");
                }
            }
        }

        // Creature death -> BountyHunterBadge tracking

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer is PlayerMobile killer && e.Creature is BaseCreature victim)
            {
                // Only track for creatures that would reasonably be "bounties"
                // Skip: tamable, bonded, dungeon creatures (trivial), summoned, etc.
                if (victim.Tamable || victim.IsBonded || victim.Summoned)
                    return;

                // Check if killer has a bounty hunter badge
                var badge = killer.Backpack?.FindItemByType<BountyHunterBadge>();
                if (badge != null)
                {
                    badge.BountyKills++;
                    killer.SendMessage(0x44, string.Format("Bounty kill recorded! Total: {0} (Rank: {1})", badge.BountyKills, badge.Rank));
                }
            }
        }

        // Commands

        [Usage("ViewRelations")]
        [Description("Opens the relationship gump for your currently targeted creature or NPC.")]
        private static void ViewRelations_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            from.SendMessage("Target the NPC or creature to view your relationship.");
            from.Target = new ViewRelationsTarget();
        }

        private class ViewRelationsTarget : Target
        {
            public ViewRelationsTarget() : base(12, false, TargetFlags.None) { }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from is PlayerMobile pm && targeted is BaseCreature npc)
                {
                    pm.SendGump(new RelationshipGump(pm));
                }
                else
                {
                    from.SendMessage("You must target a creature or NPC.");
                }
            }
        }

        [Usage("AddCommunityBoard")]
        [Description("Place a community board at your current location.")]
        private static void AddCommunityBoard_OnCommand(CommandEventArgs e)
        {
            var board = new CommunityBoardItem();
            board.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
            e.Mobile.SendMessage(0x44, "Community board placed.");
        }

        [Usage("AddBountyBoard")]
        [Description("Place a bounty board at your current location.")]
        private static void AddBountyBoard_OnCommand(CommandEventArgs e)
        {
            var board = new BountyBoardItem();
            board.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
            e.Mobile.SendMessage(0x44, "Bounty board placed.");
        }

        [Usage("AddRomanceGiver")]
        [Description("Spawn a romance quest giver NPC at your location.")]
        private static void AddRomanceGiver_OnCommand(CommandEventArgs e)
        {
            var giver = new RomanceQuestGiver();
            giver.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
            e.Mobile.SendMessage(0x44, "Romance quest giver spawned.");
        }

        [Usage("HirelingMarket")]
        [Description("Open the hireling market gump to view available hirelings worldwide.")]
        private static void HirelingMarket_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                pm.SendGump(new HirelingMarketGump(pm));
            }
        }
    }
}