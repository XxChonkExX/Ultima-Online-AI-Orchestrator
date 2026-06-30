using System;
using Server.AIOrchestrator;

namespace Server.Items
{
    /// <summary>
    /// A badge that tracks bounty kills and displays rank. Stays in backpack.
    /// </summary>
    public class BountyHunterBadge : Item
    {
        private int _bountyKills;

        [CommandProperty(AccessLevel.GameMaster)]
        public int BountyKills
        {
            get { return _bountyKills; }
            set { _bountyKills = Math.Max(0, value); UpdateName(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Rank { get; private set; } = "Novice";

        [Constructable]
        public BountyHunterBadge() : base(0x1F14)
        {
            Name = "Bounty Hunter Badge";
            Hue = 0x482;
            Weight = 0.5;
            UpdateName();
        }

        private void UpdateName()
        {
            if (_bountyKills >= 50)
                Rank = "Legendary";
            else if (_bountyKills >= 25)
                Rank = "Veteran";
            else if (_bountyKills >= 10)
                Rank = "Seasoned";
            else if (_bountyKills >= 5)
                Rank = "Journeyman";
            else if (_bountyKills >= 2)
                Rank = "Apprentice";
            else
                Rank = "Novice";

            Name = string.Format("{0} Bounty Hunter Badge ({1} kills)", Rank, _bountyKills);
        }

        /// <summary>Record a bounty kill for a player carrying this badge.</summary>
        public static void RecordKill(Mobile killer)
        {
            if (killer == null) return;
            var badge = killer.Backpack?.FindItemByType<BountyHunterBadge>();
            if (badge != null)
            {
                badge.BountyKills++;
                killer.SendMessage(0x44, string.Format("Bounty kill recorded! Total: {0} (Rank: {1})", badge._bountyKills, badge.Rank));
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage(0x3B2, string.Format("Bounty Hunter Rank: {0}  |  Bounty Kills: {1}", Rank, _bountyKills));
        }

        public BountyHunterBadge(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(_bountyKills);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int v = reader.ReadInt();
            _bountyKills = reader.ReadInt();
            UpdateName();
        }
    }
}