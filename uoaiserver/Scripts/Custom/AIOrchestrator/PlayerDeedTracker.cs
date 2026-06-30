using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// Tracks player quest and deed states via a static Dictionary[Serial, Dictionary[string, string]].
    /// Persisted via World Save/Load using Server.Guilds.BaseGuild-like pattern.
    /// </summary>
    public static class PlayerDeedTracker
    {
        private static readonly Dictionary<Serial, Dictionary<string, string>> _playerDeeds = new Dictionary<Serial, Dictionary<string, string>>();
        private const string SavePath = "Data/AIOrchestrator_Deeds.bin";

        static PlayerDeedTracker()
        {
            // Register save/load
            Server.EventSink.WorldSave += OnWorldSave;
            Load();
        }

        /// <summary>Record a deed/value for a player.</summary>
        public static void RecordDeed(PlayerMobile pm, string deedName, string value)
        {
            if (pm == null || string.IsNullOrEmpty(deedName)) return;
            Dictionary<string, string> deeds;
            if (!_playerDeeds.TryGetValue(pm.Serial, out deeds))
            {
                deeds = new Dictionary<string, string>();
                _playerDeeds[pm.Serial] = deeds;
            }
            deeds[deedName] = value ?? "";
        }

        /// <summary>Retrieve a deed value, or null if not set.</summary>
        public static string GetDeed(PlayerMobile pm, string deedName)
        {
            if (pm == null || string.IsNullOrEmpty(deedName)) return null;
            Dictionary<string, string> deeds;
            if (_playerDeeds.TryGetValue(pm.Serial, out deeds) && deeds.TryGetValue(deedName, out var value))
                return value;
            return null;
        }

        /// <summary>Clear a deed from the player.</summary>
        public static void ClearDeed(PlayerMobile pm, string deedName)
        {
            if (pm == null || string.IsNullOrEmpty(deedName)) return;
            Dictionary<string, string> deeds;
            if (_playerDeeds.TryGetValue(pm.Serial, out deeds))
                deeds.Remove(deedName);
        }

        /// <summary>Check if a player has completed a deed.</summary>
        public static bool HasDeed(PlayerMobile pm, string deedName, string expectedValue = "Complete")
        {
            return string.Equals(GetDeed(pm, deedName), expectedValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Get recent deeds context for AI Game Master.</summary>
        public static string GetRecentDeedsContext()
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var kvp in _playerDeeds)
            {
                if (kvp.Value.Count > 0)
                {
                    lines.Add(string.Format("Player {0}: {1} deeds", kvp.Key.Value, kvp.Value.Count));
                }
            }
            return lines.Count > 0 ? string.Join("; ", lines) : "";
        }

        // ── Persistence ──────────────────────────────────────────────

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var fs = new FileStream(SavePath, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(fs))
                {
                    writer.Write(_playerDeeds.Count);
                    foreach (var kvp in _playerDeeds)
                    {
                        var serial = kvp.Key;
                        var deeds = kvp.Value;
                        writer.Write(serial.Value);
                        writer.Write(deeds.Count);
                        foreach (var deedKvp in deeds)
                        {
                            writer.Write(deedKvp.Key ?? "");
                            writer.Write(deedKvp.Value ?? "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AIOrchestrator] PlayerDeedTracker save error: " + ex.Message);
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;

                using (var fs = new FileStream(SavePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs))
                {
                    int playerCount = reader.ReadInt32();
                    for (int i = 0; i < playerCount; i++)
                    {
                        var serial = (Serial)reader.ReadInt32();
                        int deedCount = reader.ReadInt32();
                        var deeds = new Dictionary<string, string>(deedCount);
                        for (int j = 0; j < deedCount; j++)
                        {
                            var k = reader.ReadString();
                            var v = reader.ReadString();
                            deeds[k] = v;
                        }
                        _playerDeeds[serial] = deeds;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AIOrchestrator] PlayerDeedTracker load error: " + ex.Message);
            }
        }
    }
}