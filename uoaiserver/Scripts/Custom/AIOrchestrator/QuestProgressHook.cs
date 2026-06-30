using System;
using Server;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Hooks into ServUO events to feed crafting and gathering progress
    /// into the AI quest system (ReportCrafted, ReportGathered).
    /// </summary>
    public static class QuestProgressHook
    {
        public static void Initialize()
        {
            // Crafting: any crafted item progresses CraftItem quests
            EventSink.CraftSuccess += OnCraftSuccess;

            // Gathering: mining, lumberjacking, fishing, etc. progress GatherResource quests
            EventSink.ResourceHarvestSuccess += OnResourceHarvestSuccess;

            Console.WriteLine("[AIOrchestrator] Quest progress hooks initialized (craft + gather).");
        }

        private static void OnCraftSuccess(CraftSuccessEventArgs e)
        {
            if (e.Crafter == null || e.CraftedItem == null) return;
            if (!e.Crafter.Player) return;

            AIQuestManager.ReportCrafted(e.Crafter, e.CraftedItem);
        }

        private static void OnResourceHarvestSuccess(ResourceHarvestSuccessEventArgs e)
        {
            if (e.Harvester == null || e.Resource == null) return;
            if (!e.Harvester.Player) return;

            string resourceName = e.Resource.Name ?? e.Resource.GetType().Name;
            AIQuestManager.ReportGathered(e.Harvester, resourceName, 1);
        }
    }
}
