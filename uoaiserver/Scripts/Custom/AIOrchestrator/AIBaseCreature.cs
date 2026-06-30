using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server;
using Server.ContextMenus;
using Server.Mobiles;
using Server.AIOrchestrator;
using Server.AIOrchestrator.Subagents;

namespace Server.AIOrchestrator
{
    public class AIBaseCreature : BaseCreature, IAIOrchestrator
    {
        public AIMemory Memory { get; private set; }
        public CombatSubagent CombatAI { get; private set; }
        public DialogueSubagent DialogueAI { get; private set; }
        public EnvironmentSubagent EnvironmentAI { get; private set; }
        public HirelingSubagent HirelingAI { get; private set; }
        public SubagentType ActiveSubagent { get; private set; } = SubagentType.Dialogue;

        public AIBaseCreature(AIType ai, FightMode mode, int rangePerception, int rangeFight, double activeSpeed, double passiveSpeed)
            : base(ai, mode, rangePerception, rangeFight, activeSpeed, passiveSpeed)
        {
            InitializeAI();
        }

        public AIBaseCreature(Serial serial) : base(serial)
        {
        }

        private void InitializeAI()
        {
            Memory = new AIMemory(Serial.Value.ToString());
            Memory.Identity = NpcIdentityGenerator.Generate(this);

            CombatAI = new CombatSubagent(this, Memory);
            DialogueAI = new DialogueSubagent(this, Memory);
            EnvironmentAI = new EnvironmentSubagent(this, Memory);
            HirelingAI = new HirelingSubagent(this, Memory);
        }

        public void OnHeartbeat(Mobile player)
        {
            DetermineActiveSubagent(player);

            switch (ActiveSubagent)
            {
                case SubagentType.Combat:
                    CombatAI?.OnHeartbeat(player);
                    break;
                case SubagentType.Dialogue:
                    break;
                case SubagentType.Environment:
                    EnvironmentAI?.OnHeartbeat(player);
                    break;
                case SubagentType.Hireling:
                    HirelingAI?.OnHeartbeat(player);
                    break;
            }
        }

        private void DetermineActiveSubagent(Mobile player)
        {
            if (Combatant != null && Combatant.Alive && InRange(Combatant, RangePerception))
            {
                ActiveSubagent = SubagentType.Combat;
            }
            else if (Controlled && ControlMaster == player)
            {
                ActiveSubagent = SubagentType.Hireling;
            }
            else
            {
                ActiveSubagent = SubagentType.Environment;
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (DialogueAI != null && e.Mobile != this && e.Mobile.Player && e.Mobile.InRange(this, 5))
            {
                DialogueAI.OnSpeech(e.Mobile, e.Speech);
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from is PlayerMobile pm)
            {
                RelationshipContextMenu.AddEntries(this, pm, list);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            base.OnDoubleClick(from);

            if (Controlled && ControlMaster == from && HirelingAI != null)
            {
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(Memory != null);
            if (Memory != null)
            {
                writer.Write(Memory.NpcSerial);
                writer.Write(Memory.LastUpdated);

                writer.Write(Memory.Identity != null);
                if (Memory.Identity != null)
                {
                    writer.Write(Memory.Identity.Name);
                    writer.Write(Memory.Identity.Vocation);
                    writer.Write(Memory.Identity.Homeland);
                    writer.Write(Memory.Identity.Temperament);
                    writer.Write(Memory.Identity.Backstory);
                    writer.Write(Memory.Identity.SpeechStyle);
                    writer.Write(Memory.Identity.PrivateDrive);
                    writer.Write(Memory.Identity.Mood);
                }

                writer.Write(HirelingAI != null);
                if (HirelingAI != null)
                {
                    writer.Write(HirelingAI.Loyalty);
                    writer.Write(HirelingAI.SkillGains.Count);
                    foreach (var kvp in HirelingAI.SkillGains)
                    {
                        writer.Write((int)kvp.Key);
                        writer.Write(kvp.Value);
                    }
                }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (reader.ReadBool())
            {
                Memory = new AIMemory(reader.ReadString());
                Memory.LastUpdated = reader.ReadDateTime();

                if (reader.ReadBool())
                {
                    Memory.Identity = new NpcIdentity
                    {
                        Name = reader.ReadString(),
                        Vocation = reader.ReadString(),
                        Homeland = reader.ReadString(),
                        Temperament = reader.ReadString(),
                        Backstory = reader.ReadString(),
                        SpeechStyle = reader.ReadString(),
                        PrivateDrive = reader.ReadString(),
                        Mood = reader.ReadInt()
                    };
                }

                if (version >= 1 && reader.ReadBool())
                {
                    int loyalty = reader.ReadInt();
                    int skillCount = reader.ReadInt();
                    var skillGains = new Dictionary<SkillName, double>();
                    for (int i = 0; i < skillCount; i++)
                    {
                        var skill = (SkillName)reader.ReadInt();
                        var gain = reader.ReadDouble();
                        skillGains[skill] = gain;
                    }
                    Memory.PersistentData["HirelingLoyalty"] = loyalty;
                    Memory.PersistentData["HirelingSkillGains"] = skillGains;
                }
            }
            else
            {
                Memory = new AIMemory(Serial.Value.ToString());
            }

            InitializeAI();

            if (HirelingAI != null && Memory.PersistentData.ContainsKey("HirelingLoyalty"))
            {
                var loyalty = (int)Memory.PersistentData["HirelingLoyalty"];
                var field = HirelingAI.GetType().GetField("_loyalty", BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(HirelingAI, loyalty);

                if (Memory.PersistentData.ContainsKey("HirelingSkillGains"))
                {
                    var gains = (Dictionary<SkillName, double>)Memory.PersistentData["HirelingSkillGains"];
                    var field2 = HirelingAI.GetType().GetField("_skillGains", BindingFlags.NonPublic | BindingFlags.Instance);
                    field2?.SetValue(HirelingAI, gains);
                }
            }
        }
    }
}