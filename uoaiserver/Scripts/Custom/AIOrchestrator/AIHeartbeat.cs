using System;
using System.Collections.Generic;
using Server;
using Server.Network;
using Server.Mobiles;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator
{
    public static class AIHeartbeat
    {
        private static Timer _timer;
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMilliseconds(AIConfig.HeartbeatMs);

        public static void Initialize()
        {
            _timer = Timer.DelayCall(HeartbeatInterval, HeartbeatInterval, HeartbeatCallback);
            Console.WriteLine("[AIOrchestrator] Heartbeat started: " + AIConfig.HeartbeatMs + "ms interval");
        }

        private static void HeartbeatCallback()
        {
            if (!AIConfig.Enabled)
                return;

            try
            {
                var players = new List<Mobile>();
                foreach (NetState ns in NetState.Instances)
                {
                    if (ns.Mobile != null && ns.Mobile.Player)
                        players.Add(ns.Mobile);
                }

                foreach (var player in players)
                {
                    ProcessNearbyNPCs(player);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AIOrchestrator] Heartbeat error: " + ex.Message);
            }
        }

        private static void ProcessNearbyNPCs(Mobile player)
        {
            var npcs = player.GetMobilesInRange(18);
            int count = 0;

            foreach (var npc in npcs)
            {
                if (count >= AIConfig.MaxNpcsPerPlayer)
                    break;

                if (npc is IAIOrchestrator ai)
                {
                    ai.OnHeartbeat(player);
                    count++;
                }
                else if (npc is BaseCreature bc && AIComponentRegistry.HasAI(bc))
                {
                    var component = AIComponentRegistry.GetComponent(bc);
                    component?.OnHeartbeat(player);
                    count++;
                }
            }
        }

        public static void Stop()
        {
            _timer?.Stop();
            _timer = null;
        }
    }

    public interface IAIOrchestrator
    {
        void OnHeartbeat(Mobile player);
        SubagentType ActiveSubagent { get; }
    }

    public enum SubagentType
    {
        Combat,
        Dialogue,
        Environment,
        Hireling
    }
}