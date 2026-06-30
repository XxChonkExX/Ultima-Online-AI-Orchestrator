using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.AIOrchestrator
{
    /// <summary>
    /// On world load, scans ALL existing spawners and piggybacks
    /// AI creature variants (OrcShaman, etc.), greater/lesser variants,
    /// and hero hirelings onto appropriate spawners.
    /// Also places CreatureVariantSpawners at fixed world locations.
    /// Called once from AIOrchestratorInit after all spawn XMLs are loaded.
    /// </summary>
    public static class SpawnerIntegration
    {
        private static bool _hasRun = false;

        /// <summary>Mapping: base creature name → variant entries to add.</summary>
        private static readonly Dictionary<string, VariantEntry[]> VariantMap = new Dictionary<string, VariantEntry[]>
        {
            ["orc"] = new[] {
                new VariantEntry("OrcShaman", 1, 0.40),
                new VariantEntry("OrcArcher", 1, 0.35),
                new VariantEntry("OrcKnight", 1, 0.25),
                new VariantEntry("OrcBeastmaster", 1, 0.20),
                new VariantEntry("LesserOrc", 2, 0.50),
                new VariantEntry("GreaterOrc", 1, 0.10),
            },
            ["orcishlord"] = new[] {
                new VariantEntry("OrcKnight", 1, 0.30),
                new VariantEntry("OrcShaman", 1, 0.30),
                new VariantEntry("GreaterOrc", 1, 0.15),
            },
            ["orcishmage"] = new[] {
                new VariantEntry("OrcShaman", 1, 0.40),
                new VariantEntry("OrcArcher", 1, 0.20),
            },
            ["lizardman"] = new[] {
                new VariantEntry("LizardmanShaman", 1, 0.30),
                new VariantEntry("LizardmanSniper", 1, 0.25),
            },
            ["troll"] = new[] {
                new VariantEntry("TrollWitchdoctor", 1, 0.25),
                new VariantEntry("GreaterTroll", 1, 0.10),
            },
            ["skeleton"] = new[] {
                new VariantEntry("SkeletalMage", 1, 0.20),
                new VariantEntry("SkeletalArcher", 1, 0.20),
                new VariantEntry("GreaterSkeleton", 1, 0.08),
            },
            ["zombie"] = new[] {
                new VariantEntry("SkeletalMage", 1, 0.10),
                new VariantEntry("SkeletalArcher", 1, 0.10),
                new VariantEntry("GreaterSkeleton", 1, 0.05),
            },
            ["dragon"] = new[] {
                new VariantEntry("LesserDragon", 2, 0.30),
            },
            ["daemon"] = new[] {
                new VariantEntry("LesserDaemon", 2, 0.25),
            },
            ["mongbat"] = new[] {
                new VariantEntry("LesserOrc", 1, 0.15),
            },
        };

        private struct VariantEntry
        {
            public string TypeName;
            public int MaxCount;
            public double Probability; // 0.0-1.0, chance per matching spawner

            public VariantEntry(string typeName, int maxCount, double prob)
            {
                TypeName = typeName;
                MaxCount = maxCount;
                Probability = prob;
            }
        }

        public static void Initialize()
        {
            if (_hasRun) return;

            // Run after world load with a short delay to ensure all spawners exist
            Timer.DelayCall(TimeSpan.FromSeconds(5), () =>
            {
                try
                {
                    int piggybacked = 0;
                    int spawnersPlaced = 0;

                    // ── Step 1: Piggyback variants onto existing spawners ──
                    piggybacked = PiggybackExistingSpawners();

                    // ── Step 2: Place CreatureVariantSpawners at fixed locations ──
                    spawnersPlaced = PlaceVariantSpawners();

                    // ── Step 3: Place HeroHirelingSpawners in towns ──
                    int heroSpawners = PlaceHeroHirelingSpawners();

                    Console.WriteLine("[AIOrchestrator] Spawner integration: {0} spawners piggybacked, {1} variant spawners placed, {2} hero hireling spawners placed.", piggybacked, spawnersPlaced, heroSpawners);
                    _hasRun = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[AIOrchestrator] Spawner integration error: {0}", ex.Message);
                }
            });
        }

        /// <summary>
        /// Step 1: Scan all existing ISpawner items and add variant entries
        /// to spawners that contain matching base creature names.
        /// </summary>
        private static int PiggybackExistingSpawners()
        {
            int count = 0;

            foreach (Item item in World.Items.Values)
            {
                if (item is Server.Mobiles.Spawner)
                {
                    if (PiggybackSpawner((Server.Mobiles.Spawner)item))
                        count++;
                }
                else if (item is XmlSpawner)
                {
                    if (PiggybackXmlSpawner((XmlSpawner)item))
                        count++;
                }
            }

            return count;
        }

        /// <summary>Piggyback a standard Spawner.</summary>
        private static bool PiggybackSpawner(Server.Mobiles.Spawner s)
        {
            if (s == null || s.Deleted) return false;

            bool modified = false;

            // Process current spawn entries
            List<SpawnObject> spawns = new List<SpawnObject>(s.SpawnObjects);

            foreach (SpawnObject so in spawns)
            {
                string lowerName = so.SpawnName.ToLowerInvariant().Trim();

                foreach (var kvp in VariantMap)
                {
                    if (lowerName == kvp.Key || lowerName.Contains(kvp.Key))
                    {
                        double probMult = (lowerName == kvp.Key) ? 1.0 : 0.5;

                        foreach (var variant in kvp.Value)
                        {
                            if (Utility.RandomDouble() < variant.Probability * probMult)
                            {
                                // Check if already exists
                                bool alreadyExists = false;
                                foreach (SpawnObject existing in s.SpawnObjects)
                                {
                                    if (existing.SpawnName.Equals(variant.TypeName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        alreadyExists = true;
                                        break;
                                    }
                                }

                                if (!alreadyExists)
                                {
                                    s.SpawnObjects.Add(new SpawnObject(variant.TypeName, variant.MaxCount));
                                    modified = true;
                                }
                            }
                        }
                    }
                }
            }

            if (modified && !s.Running)
                s.Running = true;

            return modified;
        }

        /// <summary>Piggyback an XmlSpawner.</summary>
        private static bool PiggybackXmlSpawner(XmlSpawner xs)
        {
            if (xs == null || xs.Deleted) return false;

            bool modified = false;

            // Process current spawn entries
            List<XmlSpawner.SpawnObject> spawns = new List<XmlSpawner.SpawnObject>(xs.m_SpawnObjects);

            foreach (XmlSpawner.SpawnObject so in spawns)
            {
                string typeName = BaseXmlSpawner.ParseObjectType(so.TypeName);
                if (string.IsNullOrEmpty(typeName)) continue;

                string lowerName = typeName.ToLowerInvariant().Trim();

                foreach (var kvp in VariantMap)
                {
                    if (lowerName == kvp.Key || lowerName.Contains(kvp.Key))
                    {
                        double probMult = (lowerName == kvp.Key) ? 1.0 : 0.5;

                        foreach (var variant in kvp.Value)
                        {
                            if (Utility.RandomDouble() < variant.Probability * probMult)
                            {
                                // Check if already exists
                                bool alreadyExists = false;
                                foreach (XmlSpawner.SpawnObject existing in xs.m_SpawnObjects)
                                {
                                    string ext = BaseXmlSpawner.ParseObjectType(existing.TypeName);
                                    if (!string.IsNullOrEmpty(ext) && ext.Equals(variant.TypeName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        alreadyExists = true;
                                        break;
                                    }
                                }

                                if (!alreadyExists)
                                {
                                    xs.m_SpawnObjects.Add(new XmlSpawner.SpawnObject(variant.TypeName, variant.MaxCount));
                                    modified = true;
                                }
                            }
                        }
                    }
                }
            }

            if (modified && !xs.Running)
                xs.Running = true;

            return modified;
        }

        /// <summary>
        /// Step 2: Place CreatureVariantSpawners at fixed world locations.
        /// </summary>
        private static int PlaceVariantSpawners()
        {
            int count = 0;

            // Orc camps
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5242, 1170, 0), new Point3D(4212, 1980, 0),
                    new Point3D(6029, 3630, 0), new Point3D(3202, 654, 0),
                },
                Map.Felucca,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.OrcCamp)
                {
                    SpawnRange = 25,
                    MaxCount = 6
                }
            );
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5242, 1170, 0), new Point3D(4212, 1980, 0),
                    new Point3D(6029, 3630, 0), new Point3D(3202, 654, 0),
                },
                Map.Trammel,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.OrcCamp)
                {
                    SpawnRange = 25,
                    MaxCount = 6
                }
            );

            // Lizardman nests
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5510, 1100, 0), new Point3D(5690, 590, 0),
                },
                Map.Felucca,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.LizardmanNest)
                {
                    SpawnRange = 20,
                    MaxCount = 4
                }
            );
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5510, 1100, 0), new Point3D(5690, 590, 0),
                },
                Map.Trammel,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.LizardmanNest)
                {
                    SpawnRange = 20,
                    MaxCount = 4
                }
            );

            // Troll caves
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5590, 1360, 0), new Point3D(4290, 430, 0),
                },
                Map.Felucca,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.TrollCave)
                {
                    SpawnRange = 18,
                    MaxCount = 4
                }
            );
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5590, 1360, 0), new Point3D(4290, 430, 0),
                },
                Map.Trammel,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.TrollCave)
                {
                    SpawnRange = 18,
                    MaxCount = 4
                }
            );

            // Undead crypts
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5440, 1720, 0), new Point3D(1430, 1690, 0),
                    new Point3D(6280, 940, 0),
                },
                Map.Felucca,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.UndeadCrypt)
                {
                    SpawnRange = 22,
                    MaxCount = 6
                }
            );
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5440, 1720, 0), new Point3D(1430, 1690, 0),
                    new Point3D(6280, 940, 0),
                },
                Map.Trammel,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.UndeadCrypt)
                {
                    SpawnRange = 22,
                    MaxCount = 6
                }
            );

            // Lesser dungeon entrances
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5186, 640, 0), new Point3D(5400, 1940, 0),
                },
                Map.Felucca,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.LesserDungeon)
                {
                    SpawnRange = 15,
                    MaxCount = 4
                }
            );
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5186, 640, 0), new Point3D(5400, 1940, 0),
                },
                Map.Trammel,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.LesserDungeon)
                {
                    SpawnRange = 15,
                    MaxCount = 4
                }
            );

            // Greater threats
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5700, 630, 0), new Point3D(6500, 870, 0),
                },
                Map.Felucca,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.GreaterThreat)
                {
                    SpawnRange = 20,
                    MaxCount = 3
                }
            );
            count += PlaceSpawnersAtLocations(
                new[] {
                    new Point3D(5700, 630, 0), new Point3D(6500, 870, 0),
                },
                Map.Trammel,
                () => new CreatureVariantSpawner(CreatureVariantSpawner.VariantTheme.GreaterThreat)
                {
                    SpawnRange = 20,
                    MaxCount = 3
                }
            );

            return count;
        }

        /// <summary>Helper: place spawners at each location if one doesn't already exist.</summary>
        private static int PlaceSpawnersAtLocations(Point3D[] locations, Map map, Func<Item> factory)
        {
            if (map == null || map == Map.Internal) return 0;

            int count = 0;

            foreach (Point3D loc in locations)
            {
                if (!SpawnerExistsNear(loc.X, loc.Y, map, "Variant"))
                {
                    Item spawner = factory();
                    spawner.MoveToWorld(loc, map);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Step 3: Place HeroHirelingSpawners in towns.
        /// </summary>
        private static int PlaceHeroHirelingSpawners()
        {
            int count = 0;

            // Town center locations
            var townLocations = new[]
            {
                new { X = 1450, Y = 1772, Map = Map.Felucca },
                new { X = 2001, Y = 2800, Map = Map.Felucca },
                new { X = 4450, Y = 1122, Map = Map.Felucca },
                new { X = 2480, Y = 560,  Map = Map.Felucca },
                new { X = 1440, Y = 894,  Map = Map.Felucca },
                new { X = 570,  Y = 1575, Map = Map.Felucca },
                new { X = 650,  Y = 840,  Map = Map.Felucca },
                new { X = 2900, Y = 3400, Map = Map.Felucca },
                new { X = 3760, Y = 2240, Map = Map.Felucca },
                new { X = 1450, Y = 1772, Map = Map.Trammel },
                new { X = 2001, Y = 2800, Map = Map.Trammel },
                new { X = 4450, Y = 1122, Map = Map.Trammel },
                new { X = 2480, Y = 560,  Map = Map.Trammel },
                new { X = 570,  Y = 1575, Map = Map.Trammel },
            };

            foreach (var loc in townLocations)
            {
                if (loc.Map == null || loc.Map == Map.Internal) continue;

                if (!SpawnerExistsNear(loc.X, loc.Y, loc.Map, "Hireling"))
                {
                    var spawner = new HeroHirelingSpawner();
                    spawner.MoveToWorld(new Point3D(loc.X, loc.Y, 0), loc.Map);
                    count++;
                }
            }

            return count;
        }

        /// <summary>Check if a spawner of the given name prefix already exists nearby.</summary>
        private static bool SpawnerExistsNear(int x, int y, Map map, string namePrefix)
        {
            if (map == null || map == Map.Internal) return true;

            for (int dx = -10; dx <= 10; dx++)
            {
                for (int dy = -10; dy <= 10; dy++)
                {
                    int tileX = x + dx;
                    int tileY = y + dy;
                    if (tileX >= 0 && tileX < map.Width && tileY >= 0 && tileY < map.Height)
                    {
                        IPooledEnumerable eable = map.GetItemsInRange(new Point3D(tileX, tileY, 0), 0);
                        foreach (Item item in eable)
                        {
                            if (item != null && !item.Deleted && item.Name != null &&
                                item.Name.IndexOf(namePrefix, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                eable.Free();
                                return true;
                            }
                        }
                        eable.Free();
                    }
                }
            }

            return false;
        }
    }
}
