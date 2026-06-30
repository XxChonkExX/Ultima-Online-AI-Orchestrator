using System;
using Server;
using Server.Mobiles;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator
{
    public static class AnimalHirelingService
    {
        private static readonly string[] AnimalTypeNames = new[]
        {
            "Horse", "Llama", "Ostard", "Ridgeback",
            "SwampDragon", "Beetle", "Gorilla", "Bear",
            "GrizzlyBear", "PolarBear", "Tiger", "SabreToothedTiger",
            "WhiteTiger", "Panther", "Cougar", "Lion",
            "Wolf", "TimberWolf", "DireWolf", "GreyWolf",
            "Dog", "HellHound", "HellCat", "Kirin",
            "Unicorn", "Nightmare", "Reptalon", "CuSidhe",
            "RuneBeetle", "FireBeetle", "GiantBeetle",
            "Dragon", "WhiteWyrm", "AncientWyrm", "GreaterDragon",
            "Drake", "FrostDrake", "FireDrake", "ShadowDrake"
        };

        public static bool IsAnimalHireling(BaseCreature creature)
        {
            if (creature == null) return false;

            var typeName = creature.GetType().Name;
            foreach (var animalType in AnimalTypeNames)
            {
                if (typeName.Contains(animalType))
                    return true;
            }
            return false;
        }

        public static void ApplyAnimalLoyaltyBonus(BaseCreature creature)
        {
            if (!IsAnimalHireling(creature))
                return;

            if (creature is BaseHire hireling)
            {
                hireling.GainLoyalty(50);
            }
        }

        public static void ProcessAnimalLoyalty(BaseCreature creature)
        {
            if (!IsAnimalHireling(creature))
                return;

            if (creature.Controlled && creature.ControlMaster != null)
            {
                if (Utility.RandomDouble() < 0.05)
                {
                    if (creature is BaseHire hireling)
                    {
                        hireling.GainLoyalty(5);
                    }
                }
            }
        }

        public static bool CheckAnimalFoodRequirement(BaseCreature creature, Item food)
        {
            if (!IsAnimalHireling(creature))
                return true;

            return true;
        }
    }
}