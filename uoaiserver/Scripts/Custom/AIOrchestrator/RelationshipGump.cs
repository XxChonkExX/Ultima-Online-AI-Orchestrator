using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.AIOrchestrator
{
    public class RelationshipGump : Gump
    {
        private readonly PlayerMobile _player;
        private readonly List<NPCRelationship> _rels;

        public RelationshipGump(PlayerMobile from) : base(50, 50)
        {
            _player = from;
            _rels = NPCRelationshipSystem.GetRelationshipsForPlayer(from);

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            int width = 520;
            int height = 420;

            AddPage(0);
            AddBackground(0, 0, width, height, 9270);
            AddLabel(20, 20, 0x47E, "NPC Relationships");
            AddLabel(20, 42, 0x3B2, $"Player: {from.Name}  |  Total bonds: {_rels.Count}");

            if (_rels.Count == 0)
            {
                AddHtml(40, 80, width - 80, 60,
                    "<basefont color=#AAAAAA>No relationships yet. Tame wild creatures, give them gifts, or fight alongside them to build affinity. High affinity unlocks romance, apprenticeship, and household membership.</basefont>",
                    false, false);
            }
            else
            {
                AddHtml(20, 66, width - 40, 22,
                    "<basefont color=#888888>Name  |  State  |  Affinity  |  Role</basefont>",
                    false, false);

                int y = 86;
                foreach (var rel in _rels)
                {
                    if (y > height - 50) break;

                    string stateStr = rel.State.ToString();
                    string roleStr = rel.Role == NPCRole.None ? "-" : rel.Role.ToString();
                    string affinityStr = rel.Affinity.ToString("+#;-#;0");
                    int hue = rel.Affinity >= 700 ? 0x44 :
                              rel.Affinity >= 300 ? 0x3B2 :
                              rel.Affinity > 0 ? 0x482 : 0x26;

                    string icon = rel.State == NPCState.RomanticPartner ? "[R] " :
                                  rel.State >= NPCState.Hired ? "[H] " :
                                  rel.State >= NPCState.Friend ? "[F] " : "";

                    AddLabel(30, y, hue, $"{icon}{Truncate(rel.NPCName, 18)}  {stateStr,-16}  {affinityStr,5}  {roleStr}");

                    y += 24;
                }
            }

            // Help text
            AddHtml(20, height - 80, width - 40, 60,
                "<basefont color=#666666>[R] = Romantic Partner  [H] = Hired/Household  [F] = Friend  |  Use .gift <name> to give items, .love <name> to propose.</basefont>",
                false, false);

            AddButton(width - 120, height - 40, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
            AddLabel(width - 100, height - 38, 0x3B2, "Close");
        }

        private static string Truncate(string s, int max)
        {
            return s != null && s.Length > max ? s.Substring(0, max - 1) + "..." : s ?? "?";
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
        }
    }
}
