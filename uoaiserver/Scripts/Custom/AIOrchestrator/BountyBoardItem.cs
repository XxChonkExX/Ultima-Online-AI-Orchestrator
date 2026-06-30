using System;
using System.Linq;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// A placeable bounty board that shows active bounties when double-clicked.
    /// </summary>
    public class BountyBoardItem : Item
    {
        [Constructable]
        public BountyBoardItem() : base(0x1E5E)
        {
            Name = "Bounty Board";
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && from.InRange(this, 4))
            {
                from.SendGump(new BountyBoardGump(pm));
            }
            else if (!from.InRange(this, 4))
            {
                from.SendMessage("You are too far away.");
            }
        }

        public BountyBoardItem(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    public class BountyBoardGump : Gump
    {
        private readonly PlayerMobile _player;

        public BountyBoardGump(PlayerMobile from) : base(50, 50)
        {
            _player = from;
            Closable = true; Disposable = true; Dragable = true; Resizable = false;

            int width = 520;
            int height = 420;

            AddPage(0);
            AddBackground(0, 0, width, height, 9270);
            AddLabel(20, 20, 0x47E, "Bounty Board");
            AddLabel(20, 42, 0x3B2, "Active bounties — slay the target and return to claim reward.");

            var bounties = BountySystem.GetActiveBounties();

            if (bounties.Count == 0)
            {
                AddHtml(40, 80, width - 80, 40,
                    "<basefont color=#AAAAAA>No bounties posted at this time. Check back later.</basefont>",
                    false, false);
            }
            else
            {
                int y = 70;
                int idx = 0;
                foreach (var b in bounties)
                {
                    if (y > height - 50) break;

                    int hue = b.RewardGold >= 2000 ? 0x44 : b.RewardGold >= 1500 ? 0x3B2 : 0x482;
                    AddLabel(30, y, hue, string.Format("#{0}  {1,-25}  {2,-20}  {3} gp", b.BountyId.Length > 6 ? b.BountyId.Substring(0, 6) : b.BountyId, b.DisplayName, b.RegionName, b.RewardGold));

                    // Accept button
                    AddButton(width - 80, y, 0xFA5, 0xFA7, 100 + idx, GumpButtonType.Reply, 0);
                    AddLabel(width - 65, y, 0x3B2, "Accept");

                    y += 26;
                    idx++;
                }
            }

            AddButton(width - 120, height - 40, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
            AddLabel(width - 100, height - 38, 0x3B2, "Close");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int id = info.ButtonID;
            if (id >= 100)
            {
                int bountyIdx = id - 100;
                var bounties = BountySystem.GetActiveBounties();
                if (bountyIdx < bounties.Count)
                {
                    _player.SendMessage(0x44, $"Bounty accepted: {bounties[bountyIdx].DisplayName} in {bounties[bountyIdx].RegionName}.");
                    // Track acceptance on the player
                    _player.SendMessage("Slay the target and report back to claim your reward.");
                }
            }
        }
    }
}
