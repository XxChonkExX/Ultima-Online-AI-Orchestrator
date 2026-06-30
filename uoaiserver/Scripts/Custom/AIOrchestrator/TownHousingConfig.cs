namespace Server.AIOrchestrator
{
    /// <summary>
    /// Configures whether players can place houses inside town (guarded) regions.
    /// When enabled, GuardedRegion.AllowHousing checks this flag.
    /// </summary>
    public static class TownHousingConfig
    {
        /// <summary>If true, players can place houses in guarded regions (towns).</summary>
        public static bool AllowTownHousing = true;
    }
}
