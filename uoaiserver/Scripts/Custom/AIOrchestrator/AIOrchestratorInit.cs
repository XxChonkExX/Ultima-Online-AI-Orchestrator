using System;
using Server;
using Server.AIOrchestrator;
using Server.AIOrchestrator.Subagents;
using Server.AIOrchestrator.Factions;

namespace Server.AIOrchestrator
{
    public static class AIOrchestratorInit
    {
        public static void Initialize()
        {
            AIConfig.Initialize();
            AIHeartbeat.Initialize();
            AIEventIntegration.Initialize();
            AIComponentRegistry.Configure();

            // ─── New Systems ────────────────────────────────────────

            // Ambient NPC-to-NPC gossip
            AmbientGossipSubagent.Initialize();

            // Player deed tracking (bards narrate achievements)
            // DeedEventHook.Initialize();

            // Dynamic AI quests
            AIQuestManager.Initialize();
            QuestCombatHook.Initialize();
            QuestExploreHook.Initialize();

            // Living economy (supply/demand tracking)
            LivingEconomy.Initialize();
            EconomyDataHook.Initialize();

            // Regional threat system
            RegionalThreatSystem.Initialize();
            ThreatDataHook.Initialize();

            // Nemesis system
            NemesisSystem.Initialize();
            NemesisHook.Initialize();

            // NPC Relationship system (Romance, Apprentices, Household)
            NPCRelationshipSystem.Initialize();
            RelationshipGiveHook.Initialize();

            // Tame → Relationship integration (tamed creatures gain relationship entries)
            TameRelationshipIntegration.Initialize();

            // Faction reputation system
            FactionReputationSystem.Initialize();

            // NPC faction alliance behavior (dynamic alliances, combat cooperation)
            NpcAllianceBehavior.Initialize();

            // Bounty system (procedural bounties on elite creatures)
            BountySystem.Initialize();
            BountyDeathHook.Initialize();

            // AI Game Master (emergent multi-phase narrative storyline generator)
            AIGameMaster.Initialize();

            // Loot table integration for faction-themed creature drops
            LootTableIntegration.Initialize();

            // ─── AI Model-driven Subagents ─────────────────────────

            // Economy subagent (AI-driven price fluctuation)
            EconomySubagent.Initialize();
            EconomyDataHook2.Initialize();

            // Faction diplomat subagent (AI-driven diplomacy events)
            FactionDiplomatSubagent.Initialize();

            // Spawn controller subagent (AI-driven spawn management)
            SpawnControllerSubagent.Initialize();

            // Dungeon master subagent (AI-driven dungeon encounters)
            DungeonMasterSubagent.Initialize();

            // NPC Faction alignment helper (runtime faction for creatures)
            // (NpcFaction is used by other systems, no static init needed)

            // ─── Integration Hooks ──────────────────────────────────

            // QoL integration: gumps, items, commands, context menus
            QoLIntegration.Initialize();
            QuestProgressHook.Initialize();

            // Dungeon region hooks (region entry/exit → DungeonMaster)
            DungeonRegionHook.Initialize();

            // Spawner integration — piggyback variants onto existing spawners
            // and place CreatureVariantSpawners / HeroHirelingSpawners in the world.
            SpawnerIntegration.Initialize();

            // Sentient items system — very rare AI items with personality dialogue
            SentientSystem.Initialize();

            // Combat subagent death-line + morale hooks
            CombatSubagent.Initialize();

            Console.WriteLine("[AIOrchestrator] === Initialization Complete ===");
            Console.WriteLine("[AIOrchestrator] Ollama: " + AIConfig.OllamaBaseUrl);
            Console.WriteLine("[AIOrchestrator] Models - Combat: " + AIConfig.ModelCombat + ", Dialogue: " + AIConfig.ModelDialogue + ", Environment: " + AIConfig.ModelEnvironment + ", Narrator: " + AIConfig.ModelNarrator);
            Console.WriteLine("[AIOrchestrator] Systems: Gossip, Deeds, Quests, Economy, Threats, GameMaster, Factions, Loot, Heroes, CreatureVariants, EconomyAI, FactionAI, SpawnAI, DungeonAI");
        }
    }
}