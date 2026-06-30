using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Bounty system — procedural bounties on elite/named creatures.
    /// Bounties target creature variant elites (OrcWarlord, etc.) and are
    /// informed by RegionalThreatSystem (high-threat regions get more bounties).
    /// Tracks kill confirmation and awards gold.
    /// </summary>
    public static class BountySystem
    {
        public class Bounty
        {
            public string BountyId { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
            public string TargetName { get; set; }        // "OrcWarlord", "SkeletalLich", etc.
            public string DisplayName { get; set; }        // "the orc warlord", "the skeletal lich", etc.
            public string RegionName { get; set; }         // Region where bounty is active
            public int RewardGold { get; set; } = 1000;
            public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
            public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(6);
            public bool IsClaimed { get; set; }
            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
            public string ClaimedByPlayerSerial { get; set; }

            // The Type of the target creature for matching on death
            public Type TargetType { get; set; }
        }

        private static readonly List<Bounty> ActiveBounties = new List<Bounty>();
        private static Timer _bountyTimer;
        private static readonly Random _rng = new Random();

        // Elite creature types available for bounty targets
        private static readonly (Type type, string displayName, int baseReward)[] EliteTargets =
        {
            (typeof(OrcWarlord),      "the orc warlord",       1500),
            (typeof(SkeletalLich),    "the skeletal lich",     2000),
            (typeof(LizardmanHighPriest), "the lizardman high priest", 1800),
            (typeof(TrollChieftain),  "the troll chieftain",    2200),
        };

        public static void Initialize()
        {
            // Issue bounties every 15 minutes if few are active
            _bountyTimer = Timer.DelayCall(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), BountyTick);
            Console.WriteLine("[AIOrchestrator] Bounty system initialized.");
        }

        private static void BountyTick()
        {
            try
            {
                // Clean expired
                ActiveBounties.RemoveAll(b => b.IsExpired || b.IsClaimed);

                // Keep 3-5 active bounties
                int targetCount = Utility.RandomMinMax(3, 5);
                while (ActiveBounties.Count < targetCount)
                {
                    IssueBounty();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Bounty] Tick error: " + ex.Message);
            }
        }

        /// <summary>Issue a new bounty based on regional threats.</summary>
        private static void IssueBounty()
        {
            // Pick target weighted by region threat levels
            var target = PickEliteTarget();
            if (target == null) return;

            // Find a region with high threat if possible, else random
            string region = PickRegionForBounty();

            var bounty = new Bounty
            {
                TargetName = target.Value.type.Name,
                DisplayName = target.Value.displayName,
                RegionName = region,
                RewardGold = target.Value.baseReward + Utility.Random(-200, 400),
                TargetType = target.Value.type,
                ExpiresAt = DateTime.UtcNow.AddHours(3 + Utility.Random(4))
            };
            bounty.RewardGold = Math.Max(500, bounty.RewardGold);

            lock (ActiveBounties)
            {
                // Don't duplicate existing bounties on same target+region
                if (ActiveBounties.Any(b => b.TargetName == bounty.TargetName && b.RegionName == bounty.RegionName && !b.IsClaimed))
                    return;

                ActiveBounties.Add(bounty);
            }

            // Broadcast new bounty
            BroadcastToAll(
                $"[Bounty] {bounty.RewardGold} gold on {bounty.DisplayName}! Last seen near {bounty.RegionName}.",
                0x44);
            Console.WriteLine($"[Bounty] Issued: {bounty.DisplayName} @ {bounty.RegionName} — {bounty.RewardGold}g");
        }

        /// <summary>Pick an elite target, weighted by region threats if applicable.</summary>
        private static (Type type, string displayName, int baseReward)? PickEliteTarget()
        {
            if (EliteTargets.Length == 0) return null;

            // 30% chance: pick a target from a high-threat region
            if (Utility.RandomDouble() < 0.3)
            {
                string hotRegion = RegionalThreatSystem.GetThreatContext();
                if (!string.IsNullOrEmpty(hotRegion))
                {
                    // Use random elite
                    return EliteTargets[Utility.Random(EliteTargets.Length)];
                }
            }

            return EliteTargets[Utility.Random(EliteTargets.Length)];
        }

        /// <summary>Pick a region name for the bounty, preferring high-threat areas.</summary>
        private static string PickRegionForBounty()
        {
            // 40% chance: use a high-threat region from RegionalThreatSystem
            if (Utility.RandomDouble() < 0.4)
            {
                var threatCtx = RegionalThreatSystem.GetThreatContext();
                if (!string.IsNullOrEmpty(threatCtx))
                {
                    // Extract region names from threat context
                    var lines = threatCtx.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("- "))
                        {
                            var parts = line.Substring(2).Split(':');
                            if (parts.Length > 0)
                                return parts[0].Trim();
                        }
                    }
                }
            }

            // Fallback: generic region names
            var regions = new[] {
                "Britain", "Trinsic", "Moonglow", "Minoc", "Yew", "Jhelom",
                "Skara Brae", "Vesper", "Ocllo", "Magincia", "Buccaneer's Den",
                "Serpent's Hold", "Cove", "Destard", "Deceit", "Shame",
                "Hythloth", "Khaldun", "Terra Sanctum", "the Abyss"
            };
            return regions[Utility.Random(regions.Length)];
        }

        /// <summary>
        /// Called when any creature dies. Checks if it matches an active bounty.
        /// </summary>
        public static void OnCreatureKilled(Mobile killer, BaseCreature victim)
        {
            if (killer?.Player != true || victim == null) return;

            var victimType = victim.GetType();
            var killerSer = killer.Serial.Value.ToString();

            lock (ActiveBounties)
            {
                foreach (var bounty in ActiveBounties.Where(b =>
                    !b.IsClaimed && !b.IsExpired &&
                    b.TargetType.IsAssignableFrom(victimType)))
                {
                    // Verify the kill location matches the bounty region loosely
                    var regionName = victim.Region?.Name ?? "the wilds";
                    bool regionMatch = string.IsNullOrEmpty(bounty.RegionName) ||
                        regionName.IndexOf(bounty.RegionName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        bounty.RegionName.IndexOf(regionName, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!regionMatch) continue;

                    // Award bounty
                    bounty.IsClaimed = true;
                    bounty.ClaimedByPlayerSerial = killerSer;

                    int reward = bounty.RewardGold;
                    var gold = new Gold(reward);
                    if (killer.Backpack != null && killer.Backpack.TryDropItem(killer, gold, false))
                    {
                        killer.SendMessage(0x44, $"[Bounty] Claimed! You earned {reward} gold for {bounty.DisplayName}!");
                    }
                    else
                    {
                        gold.MoveToWorld(killer.Location, killer.Map);
                        killer.SendMessage(0x44, $"[Bounty] {reward} gold dropped at your feet for {bounty.DisplayName}!");
                    }

                    BroadcastToAll($"[Bounty] {killer.Name} claimed the bounty on {bounty.DisplayName}!", 0x22);
                    Console.WriteLine($"[Bounty] Claimed by {killer.Name}: {bounty.DisplayName} ({reward}g)");
                }
            }
        }

        /// <summary>Get active bounty info for display/quest prompts.</summary>
        /// <summary>Get a snapshot of currently active, unclaimed bounties.</summary>
        public static List<Bounty> GetActiveBounties()
        {
            lock (ActiveBounties)
            {
                return ActiveBounties.Where(b => !b.IsClaimed && !b.IsExpired).OrderByDescending(b => b.RewardGold).ToList();
            }
        }

        public static string GetBountyContext()
        {
            lock (ActiveBounties)
            {
                var active = ActiveBounties.Where(b => !b.IsClaimed && !b.IsExpired).ToList();
                if (active.Count == 0) return "No active bounties.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Active bounties:");
                foreach (var b in active)
                {
                    sb.AppendLine($"- {b.DisplayName} @ {b.RegionName}: {b.RewardGold}g");
                }
                return sb.ToString();
            }
        }

        private static void BroadcastToAll(string message, int hue)
        {
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null)
                    ns.Mobile.SendMessage(hue, message);
            }
        }
    }

    /// <summary>
    /// Hooks creature deaths to feed the bounty system.
    /// </summary>
    public static class BountyDeathHook
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer?.Player != true) return;
            if (!(e.Creature is BaseCreature bc)) return;

            BountySystem.OnCreatureKilled(e.Killer, bc);
        }
    }
}
