using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;

namespace Server.Items
{
    /// <summary>
    /// A town housing exemption deed. When double-clicked in a guarded region,
    /// it grants permission to place one house there. Consumed on use.
    /// </summary>
    public class TownPlotDeed : Item
    {
        [Constructable]
        public TownPlotDeed() : base(0x14F0)
        {
            Name = "Town Plot Deed";
            Hue = 0x47E;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            // Check if we're in a guarded region
            var region = Region.Find(from.Location, from.Map);
            if (region.IsPartOf<GuardedRegion>())
            {
                // Grant a timed exemption: allow housing in guarded regions for 60 seconds
                from.SendMessage(0x44, "You feel a surge of property rights! Place your house now -- the deed grants you 60 seconds of town housing exemption.");
                from.SendMessage(0x3B2, "Use a house placement tool or [house command within 60 seconds.");

                // Use the TownHousingConfig global toggle with a timer to revert
                if (!AIOrchestrator.TownHousingConfig.AllowTownHousing)
                {
                    AIOrchestrator.TownHousingConfig.AllowTownHousing = true;
                    Timer.DelayCall(TimeSpan.FromSeconds(60), () =>
                    {
                        AIOrchestrator.TownHousingConfig.AllowTownHousing = false;

                        // Notify all online players
                        foreach (var kvp in Server.World.Mobiles)
                        {
                            var mobile = kvp.Value;
                            if (mobile is PlayerMobile pm)
                            {
                                pm.SendMessage(0x26, "Your town housing exemption has expired.");
                            }
                        }
                    });
                }

                Delete();
            }
            else
            {
                from.SendMessage(0x26, "This deed only works in guarded town regions.");
            }
        }

        public TownPlotDeed(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }
}