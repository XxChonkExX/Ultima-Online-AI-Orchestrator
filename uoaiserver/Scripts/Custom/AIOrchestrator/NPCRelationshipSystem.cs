using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Multis;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// NPC Relationship System — Romance, Apprenticeship, and Household Management.
    /// NPCs form relationships with players through gifts, dialogue, and actions.
    /// High-affinity NPCs can become: Romantic Partners, Hired Staff, or Apprentices.
    /// They can live in player houses, work as vendors/crafters/guards, and gain XP.
    /// </summary>
    public static class NPCRelationshipSystem
    {
        private static readonly Dictionary<string, NPCRelationship> Relationships = new Dictionary<string, NPCRelationship>();
        private static readonly object Lock = new object();

        private const int MaxAffinity = 1000;
        private const int RomanceThreshold = 700;
        private const int HireThreshold = 500;
        private const int ApprenticeThreshold = 600;

        public static void Initialize()
        {
            // Daily decay timer
            Timer.DelayCall(TimeSpan.FromHours(1), TimeSpan.FromHours(1), DecayAffinities);
            Console.WriteLine("[AIOrchestrator] NPC Relationship system initialized.");
        }

        #region Core API

        /// <summary>Get or create relationship between player and NPC.</summary>
        public static NPCRelationship GetOrCreate(PlayerMobile player, BaseCreature npc)
        {
            var key = $"{player.Serial.Value}:{npc.Serial.Value}";
            lock (Lock)
            {
                if (!Relationships.TryGetValue(key, out var rel))
                {
                    rel = new NPCRelationship
                    {
                        PlayerSerial = (uint)player.Serial.Value,
                        NPCSerial = (uint)npc.Serial.Value,
                        PlayerName = player.Name,
                        NPCName = npc.Name ?? npc.GetType().Name,
                        MetAt = DateTime.UtcNow
                    };
                    Relationships[key] = rel;
                }
                return rel;
            }
        }

        /// <summary>Get all relationships for a given player, ordered by affinity descending.</summary>
        public static List<NPCRelationship> GetRelationshipsForPlayer(PlayerMobile player)
        {
            lock (Lock)
            {
                return Relationships.Values
                    .Where(r => r.PlayerSerial == (uint)player.Serial.Value)
                    .OrderByDescending(r => r.Affinity)
                    .ToList();
            }
        }

        /// <summary>Record a gift given to NPC — major affinity boost.</summary>
        public static void RecordGift(PlayerMobile player, BaseCreature npc, Item gift)
        {
            if (player == null || npc == null || gift == null) return;

            var rel = GetOrCreate(player, npc);
            var baseValue = CalculateGiftValue(gift);
            var affinityGain = Math.Max(1, baseValue / 10); // 100gp = +10 affinity

            rel.Affinity = Math.Min(MaxAffinity, rel.Affinity + affinityGain);
            rel.LastInteraction = DateTime.UtcNow;
            rel.TotalGiftValue += baseValue;
            rel.GiftHistory.Add(new GiftRecord
            {
                ItemName = gift.Name ?? gift.GetType().Name,
                Value = baseValue,
                Timestamp = DateTime.UtcNow
            });

            // If HeroHireling, award XP from gift value
            if (npc is HeroHireling hero && hero.Controlled)
            {
                int xp = Math.Max(1, baseValue / 10);
                hero.GrantXP(xp);
                Console.WriteLine($"[RELATIONSHIP] {hero.Name} gained {xp} XP from {player.Name}'s gift!");
            }

            // Immediate reaction
            ReactToGift(npc, player, affinityGain, baseValue);

            Console.WriteLine($"[RELATIONSHIP] {player.Name} gifted {gift.Name} ({baseValue}gp) to {npc.Name} → +{affinityGain} affinity (now {rel.Affinity})");
        }

        /// <summary>Record positive interaction (quest help, defense, kind words).</summary>
        public static void RecordPositiveInteraction(PlayerMobile player, BaseCreature npc, int amount)
        {
            var rel = GetOrCreate(player, npc);
            rel.Affinity = Math.Min(MaxAffinity, rel.Affinity + amount);
            rel.LastInteraction = DateTime.UtcNow;
        }

        /// <summary>Record negative interaction (attack, steal, insult).</summary>
        public static void RecordNegativeInteraction(PlayerMobile player, BaseCreature npc, int amount)
        {
            var rel = GetOrCreate(player, npc);
            rel.Affinity = Math.Max(-MaxAffinity, rel.Affinity - amount);
            rel.LastInteraction = DateTime.UtcNow;
        }

        /// <summary>Attempt to hire NPC (requires affinity ≥ HireThreshold).</summary>
        public static bool TryHire(PlayerMobile player, BaseCreature npc)
        {
            // HeroHirelings handle hiring through their own gold-based system
            if (npc is HeroHireling)
                return false;

            var rel = GetOrCreate(player, npc);
            if (rel.Affinity < HireThreshold)
                return false;

            if (rel.State >= NPCState.Hired)
                return false; // Already hired or higher

            rel.State = NPCState.Hired;
            rel.HiredAt = DateTime.UtcNow;
            rel.Role = NPCRole.Guard; // Default role

            // Configure NPC as hireling
            npc.ControlMaster = player;
            npc.ControlOrder = OrderType.Guard;
            npc.Loyalty = 100; // Max loyalty
            npc.IsBonded = true;

            Console.WriteLine($"[RELATIONSHIP] {player.Name} hired {npc.Name} as {rel.Role}");
            return true;
        }

        /// <summary>Attempt to take NPC as apprentice (requires affinity ≥ ApprenticeThreshold).</summary>
        public static bool TryApprentice(PlayerMobile player, BaseCreature npc)
        {
            var rel = GetOrCreate(player, npc);
            if (rel.Affinity < ApprenticeThreshold)
                return false;

            if (rel.State >= NPCState.Apprentice)
                return false;

            rel.State = NPCState.Apprentice;
            rel.ApprenticedAt = DateTime.UtcNow;
            rel.Role = NPCRole.Apprentice;

            npc.ControlMaster = player;
            npc.ControlOrder = OrderType.Follow;
            npc.Loyalty = 100;

            // Apprentices gain skill XP over time
            Timer.DelayCall(TimeSpan.FromHours(1), TimeSpan.FromHours(1), () => ApprenticeTick(npc, player));

            Console.WriteLine($"[RELATIONSHIP] {player.Name} took {npc.Name} as apprentice");
            return true;
        }

        /// <summary>Attempt romance (requires affinity ≥ RomanceThreshold, not already married).</summary>
        public static bool TryRomance(PlayerMobile player, BaseCreature npc)
        {
            var rel = GetOrCreate(player, npc);
            if (rel.Affinity < RomanceThreshold)
                return false;

            if (rel.State == NPCState.RomanticPartner)
                return false;

            rel.State = NPCState.RomanticPartner;
            rel.RomanceStartedAt = DateTime.UtcNow;
            rel.Role = NPCRole.Partner;

            npc.ControlMaster = player;
            npc.ControlOrder = OrderType.Stay;
            npc.Loyalty = 100;

            // If HeroHireling, also trigger bonding
            if (npc is HeroHireling hero)
            {
                hero.IsBonded = true;
                hero.Blessed = false;
                Console.WriteLine($"[RELATIONSHIP] {hero.Name} bonded through romance with {player.Name}!");
            }

            // Romantic partner gets special dialogue
            Console.WriteLine($"[RELATIONSHIP] {player.Name} and {npc.Name} became romantic partners!");
            return true;
        }

        /// <summary>Move NPC into player's house (requires Hired/Apprentice/Partner).</summary>
        public static bool MoveIntoHouse(PlayerMobile player, BaseCreature npc, BaseHouse house)
        {
            var rel = GetOrCreate(player, npc);
            if (rel.State < NPCState.Hired)
                return false;

            if (house == null || !house.IsOwner(player))
                return false;

            // Find a free tile in the house
            var loc = FindFreeTileInHouse(house);
            if (loc == Point3D.Zero)
                return false;

            npc.MoveToWorld(loc, house.Map);
            rel.AssignedHouse = (uint)house.Serial.Value;
            rel.AssignedRoom = loc;
            rel.State = NPCState.HouseholdMember;

            // Set home location
            npc.Home = loc;

            Console.WriteLine($"[RELATIONSHIP] {npc.Name} moved into {player.Name}'s house at {loc}");
            return true;
        }

        /// <summary>Set NPC's work role (vendor, crafter, guard, farmer, etc.).</summary>
        public static void SetRole(PlayerMobile player, BaseCreature npc, NPCRole role)
        {
            var rel = GetOrCreate(player, npc);
            if (rel.State < NPCState.Hired)
                return;

            rel.Role = role;
            ConfigureNPCForRole(npc, role, player);

            Console.WriteLine($"[RELATIONSHIP] {player.Name} set {npc.Name} as {role}");
        }

        #endregion

        #region Internal Logic

        private static int CalculateGiftValue(Item item)
        {
            // Base value + enchantment bonuses
            int value = item.GetType().Name switch
            {
                var n when n.Contains("Gold") => item.Amount,
                var n when n.Contains("Gem") || n.Contains("Ruby") || n.Contains("Sapphire") => 500,
                var n when n.Contains("Flower") || n.Contains("Rose") => 50,
                var n when n.Contains("Wine") || n.Contains("Ale") => 100,
                var n when n.Contains("Book") || n.Contains("Scroll") => 200,
                var n when n.Contains("Weapon") || n.Contains("Armor") => 1000,
                _ => Math.Max(1, item.GetType().GetProperty("Value")?.GetValue(item) as int? ?? item.GetType().GetProperty("Price")?.GetValue(item) as int? ?? 10)
            };

            // Quality bonus - check for IQuality (BaseTool, etc.)
            if (item is IQuality q && q.Quality == ItemQuality.Exceptional)
                value *= 2;
            if (item.LootType == LootType.Blessed)
                value = (int)(value * 1.5);

            return value;
        }

        private static void ReactToGift(BaseCreature npc, PlayerMobile player, int affinityGain, int value)
        {
            string reaction;
            if (affinityGain >= 50)
                reaction = "*eyes widen* For me? This is... extraordinary. Thank you.";
            else if (affinityGain >= 20)
                reaction = "*smiles warmly* A fine gift. You have good taste.";
            else if (affinityGain >= 10)
                reaction = "Thank you kindly.";
            else
                reaction = "How thoughtful.";

            Timer.DelayCall(TimeSpan.FromSeconds(0.5), () =>
            {
                if (!npc.Deleted && npc.Alive)
                    npc.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, reaction);
            });
        }

        private static Point3D FindFreeTileInHouse(BaseHouse house)
        {
            // Use the house's region area (Rectangle3D array)
            var areas = house.Region?.Area;
            if (areas == null || areas.Length == 0)
                return Point3D.Zero;

            for (int i = 0; i < 20; i++)
            {
                var area = areas[Utility.Random(areas.Length)];
                int x = area.Start.X + Utility.RandomMinMax(0, Math.Max(0, area.Width - 1));
                int y = area.Start.Y + Utility.RandomMinMax(0, Math.Max(0, area.Height - 1));
                int z = house.Z;
                if (house.Map.CanFit(x, y, z, 16, false, false))
                    return new Point3D(x, y, z);
            }
            return Point3D.Zero;
        }

        private static void ConfigureNPCForRole(BaseCreature npc, NPCRole role, Mobile master)
        {
            switch (role)
            {
                case NPCRole.Vendor:
                    npc.AI = AIType.AI_Vendor;
                    // Would need to set up vendor inventory
                    break;
                case NPCRole.Crafter:
                    npc.AI = AIType.AI_Melee; // Stay nearby
                    // Could add crafting timer
                    break;
                case NPCRole.Guard:
                    npc.AI = AIType.AI_Melee;
                    npc.RangeFight = 10;
                    npc.RangePerception = 24;
                    npc.ControlOrder = OrderType.Guard;
                    break;
                case NPCRole.Farmer:
                    npc.AI = AIType.AI_Animal;
                    // Could add planting/harvesting timer
                    break;
                case NPCRole.Cook:
                    npc.AI = AIType.AI_Vendor;
                    break;
                case NPCRole.Entertainer:
                    npc.AI = AIType.AI_Vendor;
                    // Could add bard songs
                    break;
            }
        }

        private static void ApprenticeTick(BaseCreature apprentice, PlayerMobile master)
        {
            if (apprentice.Deleted || master.Deleted || !master.Alive) return;

            var relKey = $"{master.Serial.Value}:{apprentice.Serial.Value}";
            lock (Lock)
            {
                if (!Relationships.TryGetValue(relKey, out var rel) || rel.State != NPCState.Apprentice)
                    return;
            }

            // Grant skill XP
            var skills = new[] { SkillName.Blacksmith, SkillName.Tailoring, SkillName.Carpentry, SkillName.Alchemy, SkillName.Cooking };
            var skill = skills[Utility.Random(skills.Length)];
            apprentice.Skills[skill].BaseFixedPoint += 100; // +1.0 skill
            apprentice.Skills[skill].BaseFixedPoint = Math.Min(apprentice.Skills[skill].BaseFixedPoint, 10000); // Cap at 100.0

            // Notify
            if (master.NetState != null)
                master.SendMessage(0x44, $"[Apprentice] {apprentice.Name} improved {skill}!");
        }

        private static void DecayAffinities()
        {
            lock (Lock)
            {
                var toRemove = new List<string>();
                foreach (var kvp in Relationships)
                {
                    var rel = kvp.Value;
                    var daysSinceInteraction = (DateTime.UtcNow - rel.LastInteraction).TotalDays;
                    if (daysSinceInteraction > 30)
                    {
                        // Decay 5% per month of no interaction
                        rel.Affinity = (int)(rel.Affinity * 0.95);
                    }
                    if (daysSinceInteraction > 180)
                    {
                        // Remove very stale relationships
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in toRemove)
                    Relationships.Remove(key);
            }
        }

        #endregion

        #region Persistence

        private static readonly string SavePath = System.IO.Path.Combine(
            Core.BaseDirectory, "Saves", "AIOrchestrator", "NPCRelationships.bin");

        public static void Save()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(SavePath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                using (var fs = new System.IO.FileStream(SavePath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                using (var writer = new System.IO.BinaryWriter(fs))
                {
                    writer.Write(Relationships.Count);
                    foreach (var kvp in Relationships)
                    {
                        var rel = kvp.Value;
                        writer.Write(rel.PlayerSerial);
                        writer.Write(rel.NPCSerial);
                        writer.Write(rel.PlayerName);
                        writer.Write(rel.NPCName);
                        writer.Write(rel.Affinity);
                        writer.Write((int)rel.State);
                        writer.Write((int)rel.Role);
                        writer.Write(rel.MetAt.ToBinary());
                        writer.Write(rel.LastInteraction.ToBinary());
                        writer.Write(rel.HiredAt.ToBinary());
                        writer.Write(rel.ApprenticedAt.ToBinary());
                        writer.Write(rel.RomanceStartedAt.ToBinary());
                        writer.Write(rel.AssignedHouse);
                        writer.Write(rel.AssignedRoom.X);
                        writer.Write(rel.AssignedRoom.Y);
                        writer.Write(rel.AssignedRoom.Z);
                        writer.Write(rel.TotalGiftValue);
                        writer.Write(rel.GiftHistory.Count);
                        foreach (var g in rel.GiftHistory)
                        {
                            writer.Write(g.ItemName);
                            writer.Write(g.Value);
                            writer.Write(g.Timestamp.ToBinary());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RELATIONSHIP] Save error: {ex.Message}");
            }
        }

        public static void Load()
        {
            try
            {
                if (!System.IO.File.Exists(SavePath))
                    return;

                using (var fs = new System.IO.FileStream(SavePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                using (var reader = new System.IO.BinaryReader(fs))
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var rel = new NPCRelationship
                        {
                            PlayerSerial = reader.ReadUInt32(),
                            NPCSerial = reader.ReadUInt32(),
                            PlayerName = reader.ReadString(),
                            NPCName = reader.ReadString(),
                            Affinity = reader.ReadInt32(),
                            State = (NPCState)reader.ReadInt32(),
                            Role = (NPCRole)reader.ReadInt32(),
                            MetAt = DateTime.FromBinary(reader.ReadInt64()),
                            LastInteraction = DateTime.FromBinary(reader.ReadInt64()),
                            HiredAt = DateTime.FromBinary(reader.ReadInt64()),
                            ApprenticedAt = DateTime.FromBinary(reader.ReadInt64()),
                            RomanceStartedAt = DateTime.FromBinary(reader.ReadInt64()),
                            AssignedHouse = reader.ReadUInt32(),
                            AssignedRoom = new Point3D(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                            TotalGiftValue = reader.ReadInt32(),
                        };

                        int giftCount = reader.ReadInt32();
                        for (int j = 0; j < giftCount; j++)
                        {
                            rel.GiftHistory.Add(new GiftRecord
                            {
                                ItemName = reader.ReadString(),
                                Value = reader.ReadInt32(),
                                Timestamp = DateTime.FromBinary(reader.ReadInt64())
                            });
                        }

                        var key = $"{rel.PlayerSerial}:{rel.NPCSerial}";
                        Relationships[key] = rel;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RELATIONSHIP] Load error: {ex.Message}");
            }
        }

        #endregion
    }

    #region Data Types

    public enum NPCState
    {
        Stranger = 0,        // Just met
        Acquaintance = 1,    // Positive interactions
        Friend = 2,          // High affinity
        Hired = 3,           // Employee
        Apprentice = 4,      // Learning skills
        HouseholdMember = 5, // Lives in house
        RomanticPartner = 6  // Romance
    }

    public enum NPCRole
    {
        None = 0,
        Guard = 1,
        Vendor = 2,
        Crafter = 3,
        Farmer = 4,
        Cook = 5,
        Entertainer = 6,
        Apprentice = 7,
        Partner = 8,
        Bodyguard = 9
    }

    public class NPCRelationship
    {
        public uint PlayerSerial { get; set; }
        public uint NPCSerial { get; set; }
        public string PlayerName { get; set; }
        public string NPCName { get; set; }
        public int Affinity { get; set; } // -1000 to +1000
        public NPCState State { get; set; }
        public NPCRole Role { get; set; }
        public DateTime MetAt { get; set; }
        public DateTime LastInteraction { get; set; }
        public DateTime HiredAt { get; set; }
        public DateTime ApprenticedAt { get; set; }
        public DateTime RomanceStartedAt { get; set; }
        public uint AssignedHouse { get; set; }
        public Point3D AssignedRoom { get; set; }
        public int TotalGiftValue { get; set; }
        public List<GiftRecord> GiftHistory { get; set; } = new List<GiftRecord>();
    }

    public class GiftRecord
    {
        public string ItemName { get; set; }
        public int Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Integration Hooks

    /// <summary>
    /// Hook into the Give event (when player drags item onto NPC).
    /// </summary>
    public static class RelationshipGiveHook
    {
        public static void Initialize()
        {
            // ServUO doesn't have EventSink.Give, so we hook via OnDragDrop in a custom NPC base
            // Alternative: Add a context menu entry "Give Gift" to AI-enabled NPCs
            Console.WriteLine("[RELATIONSHIP] Gift hook registered (context menu).");
        }
    }

    /// <summary>
    /// Context menu integration — add "Give Gift", "Hire", "Apprentice", "Romance" entries.
    /// </summary>
    public static class RelationshipContextMenu
    {
        public static void AddEntries(BaseCreature npc, PlayerMobile from, List<ContextMenuEntry> list)
        {
            var rel = NPCRelationshipSystem.GetOrCreate(from, npc);

            // Give Gift (only if holding an item)
            list.Add(new GiveGiftEntry(npc, from));

            // Give Love Letter (if player has one in backpack)
            if (from.Backpack?.FindItemByType<LoveLetter>() != null)
                list.Add(new GiveLoveLetterEntry(npc, from));

            // Hire — skip for HeroHirelings (they have their own gold-based hire/dismiss)
            if (!(npc is HeroHireling) && rel.Affinity >= 500 && rel.State < NPCState.Hired)
                list.Add(new HireNPCEntry(npc, from));

            // Apprentice
            if (rel.Affinity >= 600 && rel.State < NPCState.Apprentice)
                list.Add(new ApprenticeNPCEntry(npc, from));

            // Romance
            if (rel.Affinity >= 700 && rel.State < NPCState.RomanticPartner)
                list.Add(new RomanceNPCEntry(npc, from));

            // Set Role
            if (rel.State >= NPCState.Hired)
                list.Add(new SetRoleEntry(npc, from));

            // Move to House
            if (rel.State >= NPCState.Hired && BaseHouse.FindHouseAt(from) != null)
                list.Add(new MoveToHouseEntry(npc, from));
        }

        private class GiveGiftEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public GiveGiftEntry(BaseCreature npc, PlayerMobile from) : base(6120, 2) // "Give Gift"
            {
                _npc = npc; _from = from;
            }

            public override void OnClick()
            {
                _from.Target = new GiftTarget(_npc, _from);
                _from.SendMessage("Select an item to give as a gift.");
            }
        }

        private class GiftTarget : Target
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public GiftTarget(BaseCreature npc, PlayerMobile from) : base(2, false, TargetFlags.None)
            {
                _npc = npc; _from = from;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item item && item.RootParent == from)
                {
                    NPCRelationshipSystem.RecordGift(_from, _npc, item);
                    item.Delete(); // Gift is consumed
                }
                else
                {
                    _from.SendMessage("You must select an item in your backpack.");
                }
            }
        }

        private class GiveLoveLetterEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public GiveLoveLetterEntry(BaseCreature npc, PlayerMobile from) : base(6126, 2) // "Give Love Letter"
            {
                _npc = npc; _from = from;
            }

            public override void OnClick()
            {
                var letter = _from.Backpack?.FindItemByType<LoveLetter>();
                if (letter != null)
                {
                    LoveLetter.TryGive(_from, _npc);
                }
                else
                {
                    _from.SendMessage("You don't have a love letter in your backpack.");
                }
            }
        }

        private class HireNPCEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public HireNPCEntry(BaseCreature npc, PlayerMobile from) : base(6121, 2) // "Hire"
            {
                _npc = npc; _from = from;
            }

            public override void OnClick()
            {
                if (NPCRelationshipSystem.TryHire(_from, _npc))
                    _from.SendMessage($"You have hired {_npc.Name}!");
                else
                    _from.SendMessage("They are not willing to work for you yet.");
            }
        }

        private class ApprenticeNPCEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public ApprenticeNPCEntry(BaseCreature npc, PlayerMobile from) : base(6122, 2) // "Take as Apprentice"
            {
                _npc = npc; _from = from;
            }

            public override void OnClick()
            {
                if (NPCRelationshipSystem.TryApprentice(_from, _npc))
                    _from.SendMessage($"{_npc.Name} is now your apprentice!");
                else
                    _from.SendMessage("They are not ready to learn from you.");
            }
        }

        private class RomanceNPCEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public RomanceNPCEntry(BaseCreature npc, PlayerMobile from) : base(6123, 2) // "Propose Romance"
            {
                _npc = npc; _from = from;
            }

            public override void OnClick()
            {
                if (NPCRelationshipSystem.TryRomance(_from, _npc))
                {
                    _from.SendMessage($"{_npc.Name} accepts your courtship!");
                    _npc.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*blushes* I... I would like that very much.");
                }
                else
                {
                    _from.SendMessage("Their heart is not yet open to you.");
                }
            }
        }

        private class SetRoleEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public SetRoleEntry(BaseCreature npc, PlayerMobile from) : base(6124, 2) // "Set Role"
            {
                _npc = npc; _from = from;
            }

            public override void OnClick()
            {
                // Show submenu with roles
                _from.SendMessage("Select a role: [Guard] [Vendor] [Crafter] [Farmer] [Cook] [Entertainer]");
                // In practice, would show a gump or submenu
            }
        }

        private class MoveToHouseEntry : ContextMenuEntry
        {
            private readonly BaseCreature _npc;
            private readonly PlayerMobile _from;

            public MoveToHouseEntry(BaseCreature npc, PlayerMobile from) : base(6125, 2) // "Move to House"
            {
                _npc = npc; _from = from;
            }

            public override void OnClick()
            {
                var house = BaseHouse.FindHouseAt(_from);
                if (house != null && NPCRelationshipSystem.MoveIntoHouse(_from, _npc, house))
                    _from.SendMessage($"{_npc.Name} has moved into your house!");
                else
                    _from.SendMessage("Cannot move them there.");
            }
        }
    }

    #endregion
}