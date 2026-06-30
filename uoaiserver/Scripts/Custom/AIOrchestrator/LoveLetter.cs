using System;
using Server.AIOrchestrator;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// A love letter that can be written on and given to an NPC for a relationship affinity boost.
    /// </summary>
    public class LoveLetter : Item
    {
        private string _message = "";
        private string _author = "";

        [CommandProperty(AccessLevel.GameMaster)]
        public string Message
        {
            get => _message;
            set => _message = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Author
        {
            get => _author;
            set => _author = value;
        }

        [Constructable]
        public LoveLetter() : base(0x14EF)
        {
            Name = "Love Letter";
            Hue = 0x47E;
            Weight = 0.5;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && _author == "")
            {
                _author = pm.Name;
                _message = "My dearest..."; // placeholder, user can customize via props
                Name = $"Love Letter from {_author}";
                from.SendMessage(0x44, "You write a heartfelt love letter.");
            }
            else
            {
                from.SendMessage(0x3B2, $"A love letter written by {_author}.");
            }
        }

        /// <summary>Helper: when given to an NPC, boost affinity.</summary>
        public static bool TryGive(Mobile giver, BaseCreature npc)
        {
            if (giver is PlayerMobile player)
            {
                var letter = player.Backpack?.FindItemByType<LoveLetter>();
                if (letter != null)
                {
                    letter.Delete();
                    var rel = NPCRelationshipSystem.GetOrCreate(player, npc);
                    int bonus = 50;
                    if (rel.State == NPCState.RomanticPartner)
                        bonus = 100;
                    NPCRelationshipSystem.RecordPositiveInteraction(player, npc, bonus);
                    npc.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*reads the letter and smiles warmly*");
                    player.SendMessage(0x44, $"You give the love letter to {npc.Name}. Affinity +{bonus}!");
                    return true;
                }
                player.SendMessage("You need a love letter in your backpack.");
            }
            return false;
        }

        public LoveLetter(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(_message);
            writer.Write(_author);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int v = reader.ReadInt();
            _message = reader.ReadString();
            _author = reader.ReadString();
        }
    }
}
