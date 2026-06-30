using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.AIOrchestrator;
using Server.AIOrchestrator.Subagents;
using Server.AIOrchestrator.Factions;

namespace Server.AIOrchestrator
{
    public static class AIEventIntegration
    {
        private static bool _registered;

        public static void Initialize()
        {
            if (_registered) return;
            _registered = true;

            EventSink.Speech += OnSpeech;
            EventSink.Movement += OnMovement;
            EventSink.AggressiveAction += OnAggressiveAction;
            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.Login += OnLogin;
            EventSink.Logout += OnLogout;

            Console.WriteLine("[AIOrchestrator] Event hooks registered.");
        }

        private static void OnSpeech(SpeechEventArgs e)
        {
            if (!AIConfig.Enabled || e.Mobile == null || !e.Mobile.Player)
                return;

            Console.WriteLine($"[AI DEBUG] Player speech detected: \"{e.Speech}\" from {e.Mobile.Name}");

            var npcs = e.Mobile.GetMobilesInRange(5);
            int count = 0, totalNpcs = 0;
            foreach (var npc in npcs)
            {
                if (npc is Mobile)
                    totalNpcs++;
                if (npc is AIBaseCreature aibc)
                {
                    aibc.DialogueAI?.OnSpeech(e.Mobile, e.Speech);
                    count++;
                }
                else if (npc is BaseCreature bc && AIComponentRegistry.HasAI(bc))
                {
                    var component = AIComponentRegistry.GetComponent(bc);
                    component?.OnSpeech(e.Mobile, e.Speech);
                    count++;
                }
            }
            if (count > 0)
                Console.WriteLine($"[AI DEBUG] Triggered AI response for {count} NPC(s) near {e.Mobile.Name}");
            else
                Console.WriteLine($"[AI DEBUG] No AI-enabled NPCs near {e.Mobile.Name} (mobiles in range: {totalNpcs})");
        }

        private static void OnMovement(MovementEventArgs e)
        {
            if (!AIConfig.Enabled || e.Mobile == null)
                return;

            var region = Region.Find(e.Mobile.Location, e.Mobile.Map);
            var oldRegion = Region.Find(new Point3D(e.Mobile.X - GetDirectionOffset(e.Direction).X,
                e.Mobile.Y - GetDirectionOffset(e.Direction).Y, e.Mobile.Z), e.Mobile.Map);

            if (region != oldRegion)
            {
                var npcs = e.Mobile.GetMobilesInRange(20);
                foreach (var npc in npcs)
                {
                    if (npc is AIBaseCreature aibc)
                    {
                        aibc.EnvironmentAI?.OnHeartbeat(e.Mobile);
                    }
                    else if (npc is BaseCreature bc && AIComponentRegistry.HasAI(bc))
                    {
                        var component = AIComponentRegistry.GetComponent(bc);
                        component?.EnvironmentAI?.OnHeartbeat(e.Mobile);
                    }
                }
            }
        }

        private static Point3D GetDirectionOffset(Direction dir)
        {
            int x = 0, y = 0;
            Server.Movement.Movement.Offset(dir, ref x, ref y);
            return new Point3D(x, y, 0);
        }

        private static void OnAggressiveAction(AggressiveActionEventArgs e)
        {
            if (!AIConfig.Enabled) return;

            Mobile attacker = e.Aggressor;
            Mobile defender = e.Aggressed;

            // If the defender (creature being attacked) has AI, trigger its combat bark
            if (defender is AIBaseCreature aibcDefender)
            {
                aibcDefender.CombatAI?.OnCombatStart(attacker);
            }
            else if (defender is BaseCreature bcDefender && AIComponentRegistry.HasAI(bcDefender))
            {
                var component = AIComponentRegistry.GetComponent(bcDefender);
                component?.CombatAI?.OnCombatStart(attacker);
            }

            // If the attacker also has AI (e.g. creature attacking player), trigger theirs too
            if (attacker is AIBaseCreature aibcAttacker)
            {
                aibcAttacker.CombatAI?.OnCombatStart(defender);
            }
            else if (attacker is BaseCreature bcAttacker && AIComponentRegistry.HasAI(bcAttacker))
            {
                var component = AIComponentRegistry.GetComponent(bcAttacker);
                component?.CombatAI?.OnCombatStart(defender);
            }
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (!AIConfig.Enabled) return;

            var killer = e.Killer;
            if (killer != null && killer.Player)
            {
                var npcs = killer.GetMobilesInRange(30);
                foreach (var npc in npcs)
                {
                    if (npc is BaseCreature creature && creature.AIObject is IAIOrchestrator ai)
                    {
                    }
                }

                // Apply faction reputation changes for the kill
                if (e.Creature is BaseCreature victim)
                {
                    FactionReputationSystem.OnCreatureKilled(killer, victim);
                }
            }
        }

        private static void OnLogin(LoginEventArgs e)
        {
            if (!AIConfig.Enabled) return;
        }

        private static void OnLogout(LogoutEventArgs e)
        {
            if (!AIConfig.Enabled) return;
        }
    }
}