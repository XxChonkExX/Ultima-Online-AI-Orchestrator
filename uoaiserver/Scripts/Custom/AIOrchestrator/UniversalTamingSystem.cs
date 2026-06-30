using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Universal Taming System — makes monsters tamable with auto-calculated difficulty.
    /// Non-tamable creatures get MinTameSkill and ControlSlots set dynamically based on stats,
    /// allowing players to tame almost any creature with sufficient Animal Taming skill.
    /// </summary>
    public static class UniversalTamingSystem
    {
        /// <summary>Master toggle. Set to false to disable all universal taming.</summary>
        public static bool Enabled = true;

        /// <summary>Creatures whose type name matches any entry here will never be made tamable.</summary>
        public static readonly HashSet<string> ExcludedTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Summoned / temporary
            "SummonedDaemon",
            "SummonedEarthElemental",
            "SummonedWaterElemental",
            "SummonedAirElemental",
            "SummonedFireElemental",

            // Quest / special NPCs
            "BaseEscortable",
            "BaseVendor",
            "TownCrier",
            "BaseGuard",
            "ArcherGuard",
            "WarriorGuard",
        };

        /// <summary>AI types that should never be made tamable.</summary>
        public static readonly HashSet<AIType> ExcludedAITypes = new HashSet<AIType>()
        {
            AIType.AI_Vendor,
        };

        /// <summary>
        /// Attempt to make a non-tamable creature tamable by calculating and applying
        /// MinTameSkill and ControlSlots based on its stats.
        /// Returns true if the creature was made tamable, false if it should remain untamable.
        /// </summary>
        public static bool TryMakeTamable(BaseCreature creature)
        {
            if (!Enabled) return false;
            if (creature == null || creature.Deleted) return false;
            if (creature.Tamable) return true; // already tamable — nothing to do
            if (creature.Summoned) return false;
            if (creature.IsChampionSpawn) return false;
            if (creature.IsInvulnerable) return false;

            // Paragons are excluded by the Tamable getter (!m_Paragon), so skip early
            if (creature.IsParagon) return false;

            // Check excluded type names
            if (ExcludedTypeNames.Contains(creature.GetType().Name)) return false;

            // Check excluded AI types
            if (ExcludedAITypes.Contains(creature.AI)) return false;

            // Calculate MinTameSkill from creature stats
            double minSkill = CalculateMinTameSkill(creature);

            // Calculate ControlSlots from difficulty
            int slots = CalculateControlSlots(minSkill);

            // Apply
            creature.Tamable = true;
            creature.MinTameSkill = minSkill;
            creature.CurrentTameSkill = Math.Min(minSkill, BaseCreature.MaxTameRequirement);
            creature.ControlSlots = slots;
            creature.ControlSlotsMin = slots;
            creature.ControlSlotsMax = slots;

            return true;
        }

        /// <summary>
        /// Calculate a reasonable MinTameSkill from a creature's combat stats.
        /// Scales from ~10 (rat) to ~120 (dragon/boss).
        /// </summary>
        public static double CalculateMinTameSkill(BaseCreature creature)
        {
            // Base from Fame (higher fame = more dangerous/respected)
            double fromFame = creature.Fame / 200.0;

            // Boost from raw HP (tougher creatures are harder to tame)
            double fromHits = creature.HitsMax / 15.0;

            // Boost from damage output
            double fromDamage = (creature.DamageMin + creature.DamageMax) / 4.0;

            // Boost from resistances (magically resistant creatures are harder to dominate)
            double fromResists = (
                creature.PhysicalResistance +
                creature.FireResistance +
                creature.ColdResistance +
                creature.PoisonResistance +
                creature.EnergyResistance
            ) / 50.0;

            // Parry/anatomy bonus if present (skilled fighters are harder to tame)
            double fromSkills = 0;
            try
            {
                if (creature.Skills != null)
                {
                    double parry = creature.Skills[SkillName.Parry]?.Value ?? 0;
                    double anatomy = creature.Skills[SkillName.Anatomy]?.Value ?? 0;
                    double tactics = creature.Skills[SkillName.Tactics]?.Value ?? 0;
                    fromSkills = (parry + anatomy + tactics) / 60.0;
                }
            }
            catch
            {
                // Ignore skill access errors
            }

            double skill = 10.0 + fromFame + fromHits + fromDamage + fromResists + fromSkills;

            return Math.Max(10.0, Math.Min(120.0, skill));
        }

        /// <summary>
        /// Calculate ControlSlots based on MinTameSkill.
        /// 1 slot for easy creatures, up to 5 for endgame monsters.
        /// </summary>
        public static int CalculateControlSlots(double minTameSkill)
        {
            if (minTameSkill >= 110) return 5;
            if (minTameSkill >= 90) return 4;
            if (minTameSkill >= 60) return 3;
            if (minTameSkill >= 30) return 2;
            return 1;
        }
    }
}
