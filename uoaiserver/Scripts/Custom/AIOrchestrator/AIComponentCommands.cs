using System;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.AIOrchestrator;
using Server.AIOrchestrator.Subagents;

namespace Server.AIOrchestrator
{
    public static class AIComponentCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIEnable", AccessLevel.GameMaster, OnEnable);
            CommandSystem.Register("AIDisable", AccessLevel.GameMaster, OnDisable);
            CommandSystem.Register("AIEnableAll", AccessLevel.Administrator, OnEnableAll);
            CommandSystem.Register("AIDisableAll", AccessLevel.Administrator, OnDisableAll);
            CommandSystem.Register("AIInfo", AccessLevel.GameMaster, OnInfo);
            CommandSystem.Register("AIStatus", AccessLevel.GameMaster, OnStatus);
            CommandSystem.Register("AIAutoAttach", AccessLevel.Administrator, OnAutoAttach);
        }

        [Usage("AIEnable")]
        [Description("Target a creature to enable AI on it.")]
        private static void OnEnable(CommandEventArgs e)
        {
            var from = e.Mobile;
            from.Target = new AITarget((mob, target) =>
            {
                if (target is BaseCreature bc && !(bc is PlayerMobile))
                {
                    if (AIComponentRegistry.HasAI(bc))
                    {
                        from.SendMessage($"{bc.Name} already has AI enabled.");
                        return;
                    }

                    AIComponentRegistry.Register(bc);
                    from.SendMessage($"AI enabled on {bc.Name}.");
                }
                else
                {
                    from.SendMessage("That is not a valid creature.");
                }
            });
            from.SendMessage("Target a creature to enable AI on it.");
        }

        [Usage("AIDisable")]
        [Description("Target a creature to disable AI on it.")]
        private static void OnDisable(CommandEventArgs e)
        {
            var from = e.Mobile;
            from.Target = new AITarget((mob, target) =>
            {
                if (target is BaseCreature bc)
                {
                    if (!AIComponentRegistry.HasAI(bc))
                    {
                        from.SendMessage($"{bc.Name} does not have AI enabled.");
                        return;
                    }

                    AIComponentRegistry.Unregister(bc.Serial);
                    from.SendMessage($"AI disabled on {bc.Name}.");
                }
                else
                {
                    from.SendMessage("That is not a valid creature.");
                }
            });
            from.SendMessage("Target a creature to disable AI on it.");
        }

        [Usage("AIEnableAll")]
        [Description("Enables AI on every BaseCreature in the world.")]
        private static void OnEnableAll(CommandEventArgs e)
        {
            var from = e.Mobile;
            int count = AIComponentRegistry.RegisterAll();
            from.SendMessage($"AI enabled on {count} creatures.");
        }

        [Usage("AIDisableAll")]
        [Description("Disables AI on every creature.")]
        private static void OnDisableAll(CommandEventArgs e)
        {
            var from = e.Mobile;
            int count = AIComponentRegistry.Count;
            AIComponentRegistry.UnregisterAll();
            from.SendMessage($"AI disabled on {count} creatures.");
        }

        [Usage("AIInfo")]
        [Description("Target a creature to view its AI status and identity.")]
        private static void OnInfo(CommandEventArgs e)
        {
            var from = e.Mobile;
            from.Target = new AITarget((mob, target) =>
            {
                if (target is BaseCreature bc)
                {
                    if (!AIComponentRegistry.HasAI(bc))
                    {
                        from.SendMessage($"{bc.Name} does not have AI enabled.");
                        return;
                    }

                    var component = AIComponentRegistry.GetComponent(bc);
                    if (component?.Memory?.Identity != null)
                    {
                        var id = component.Memory.Identity;
                        from.SendMessage($"--- AI Info: {id.Name} ---");
                        from.SendMessage($"Vocation: {id.Vocation}");
                        from.SendMessage($"Homeland: {id.Homeland}");
                        from.SendMessage($"Temperament: {id.Temperament}");
                        from.SendMessage($"Mood: {id.Mood}/100");
                        from.SendMessage($"Backstory: {id.Backstory}");
                        from.SendMessage($"Speech Style: {id.SpeechStyle}");
                        from.SendMessage($"Drive: {id.PrivateDrive}");
                    }
                    else
                    {
                        from.SendMessage($"{bc.Name} has AI enabled but no identity data.");
                    }
                }
                else
                {
                    from.SendMessage("That is not a valid creature.");
                }
            });
            from.SendMessage("Target a creature to view its AI info.");
        }

        [Usage("AIStatus")]
        [Description("Shows global AI registry statistics.")]
        private static void OnStatus(CommandEventArgs e)
        {
            var from = e.Mobile;
            from.SendMessage($"--- AI Component Registry Status ---");
            from.SendMessage($"AI Enabled: {AIConfig.Enabled}");
            from.SendMessage($"Total AI-enabled NPCs: {AIComponentRegistry.Count}");
            from.SendMessage($"Auto-attach on spawn: {AIComponentRegistry.AutoAttach}");
            from.SendMessage($"Ollama URL: {AIConfig.OllamaBaseUrl}");
            from.SendMessage($"Dialogue Model: {AIConfig.ModelDialogue}");
        }

        [Usage("AIAutoAttach")]
        [Description("Toggles whether newly spawned creatures automatically get AI.")]
        private static void OnAutoAttach(CommandEventArgs e)
        {
            var from = e.Mobile;
            AIComponentRegistry.AutoAttach = !AIComponentRegistry.AutoAttach;
            from.SendMessage($"Auto-attach AI on new spawns: {AIComponentRegistry.AutoAttach}");
        }

        /// <summary>
        /// Simple target callback helper for AI commands.
        /// </summary>
        private class AITarget : Server.Targeting.Target
        {
            private readonly Action<Mobile, object> _callback;

            public AITarget(Action<Mobile, object> callback)
                : base(12, false, Server.Targeting.TargetFlags.None)
            {
                _callback = callback;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                _callback?.Invoke(from, targeted);
            }
        }
    }
}
