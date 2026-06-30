using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Regions;
using Server.AIOrchestrator.Subagents;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Hooks into region transitions to track dungeon entry/exit
    /// and feed them into the DungeonMaster subagent.
    /// </summary>
    public static class DungeonRegionHook
    {
        // Track each player's current region name for leave detection
        private static readonly Dictionary<string, string> _playerRegions = new Dictionary<string, string>();

        public static void Initialize()
        {
            EventSink.OnEnterRegion += OnEnterRegion;
            Console.WriteLine("[AIOrchestrator] Dungeon region hooks initialized.");
        }

        private static void OnEnterRegion(OnEnterRegionEventArgs e)
        {
            if (e.From == null || !e.From.Player) return;

            var player = e.From;
            var serial = player.Serial.Value.ToString();
            var oldRegion = e.OldRegion;
            var newRegion = e.NewRegion;

            bool wasDungeon = oldRegion is DungeonRegion;
            bool isDungeon = newRegion is DungeonRegion;

            // Track region for leave detection
            string prevRegionName = null;
            lock (_playerRegions)
            {
                _playerRegions.TryGetValue(serial, out prevRegionName);
                _playerRegions[serial] = newRegion?.Name ?? "";
            }

            // Entering a dungeon
            if (isDungeon && !wasDungeon && newRegion != null)
            {
                DungeonMasterSubagent.OnPlayerEnterDungeon(player, newRegion);
                Console.WriteLine("[AI Dungeon] " + player.Name + " entered dungeon: " + newRegion.Name);
            }

            // Leaving a dungeon
            if (wasDungeon && !isDungeon)
            {
                DungeonMasterSubagent.OnPlayerLeaveDungeon(player);
                Console.WriteLine("[AI Dungeon] " + player.Name + " left dungeon.");
            }

            // If we don't have old region info but player has a known dungeon region,
            // check if they teleported out
            if (oldRegion == null && !isDungeon && !string.IsNullOrEmpty(prevRegionName))
            {
                // They might have logged in outside a dungeon they were in — that's fine, no alert needed
            }
        }
    }
}
