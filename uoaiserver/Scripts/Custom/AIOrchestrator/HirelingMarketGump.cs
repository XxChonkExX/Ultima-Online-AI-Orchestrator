using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.AIOrchestrator
{
    public class HirelingMarketGump : Gump
    {
        private readonly PlayerMobile _player;
        private readonly List<HeroHireling> _hirelings;

        public HirelingMarketGump(PlayerMobile from) : base(50, 50)
        {
            _player = from;
            Closable = true; Disposable = true; Dragable = true; Resizable = false;

            // Find available hirelings in the world
            _hirelings = World.Mobiles.Values
                .OfType<HeroHireling>()
                .Where(h => !h.Deleted && h.Alive && !h.Controlled && _player.InRange(h, 50))
                .Take(12)
                .ToList();

            int width = 580;
            int height = 440;

            AddPage(0);
            AddBackground(0, 0, width, height, 9270);
            AddLabel(20, 20, 0x47E, "Hireling Market");
            AddLabel(20, 42, 0x3B2, $"Nearby hireables: {_hirelings.Count}");

            if (_hirelings.Count == 0)
            {
                AddHtml(40, 80, width - 80, 60,
                    "<basefont color=#AAAAAA>No hirelings available nearby. Seek wandering heroes in towns and taverns.</basefont>",
                    false, false);
            }
            else
            {
                // Header
                AddLabel(30, 68, 0x47E, "Name");
                AddLabel(140, 68, 0x47E, "Class");
                AddLabel(230, 68, 0x47E, "Lv");
                AddLabel(270, 68, 0x47E, "Cost");
                AddLabel(340, 68, 0x47E, "Location");

                int y = 88;
                int idx = 0;
                foreach (var h in _hirelings)
                {
                    if (y > height - 50) break;

                    string loc = h.Region?.Name ?? h.Map?.ToString() ?? "?";
                    AddLabel(30, y, 0x3B2, h.Name ?? "?");
                    AddLabel(140, y, 0x482, h.ClassType.ToString());
                    AddLabel(230, y, 0x44, h.Level.ToString());
                    AddLabel(270, y, 0x3B2, $"{h.HireCost}gp");
                    AddLabel(340, y, 0x888, loc);

                    // Hire button — only if player can afford
                    if (from.Backpack != null && from.Backpack.GetAmount(typeof(Server.Items.Gold)) >= h.HireCost)
                    {
                        AddButton(width - 90, y, 0xFA5, 0xFA7, 200 + idx, GumpButtonType.Reply, 0);
                        AddLabel(width - 75, y, 0x44, "Hire");
                    }
                    else
                    {
                        AddLabel(width - 75, y, 0x26, "Too expensive");
                    }

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
            if (id >= 200)
            {
                int idx = id - 200;
                if (idx < _hirelings.Count)
                {
                    var hero = _hirelings[idx];
                    if (!hero.Deleted && hero.Alive && !hero.Controlled)
                    {
                        hero.Hire(_player);
                    }
                    else
                    {
                        _player.SendMessage("That hireling is no longer available.");
                    }
                }
            }
        }
    }
}
