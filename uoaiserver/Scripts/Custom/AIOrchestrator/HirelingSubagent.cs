using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;
using Server.AIOrchestrator;

namespace Server.AIOrchestrator.Subagents
{
    public class HirelingSubagent : IAIOrchestrator
    {
        public SubagentType ActiveSubagent => SubagentType.Hireling;
        private readonly BaseCreature _creature;
        private readonly AIMemory _memory;
        private int _loyalty = 50;
        private DateTime _lastLoyaltyGain = DateTime.MinValue;
        private Dictionary<SkillName, double> _skillGains = new Dictionary<SkillName, double>();

        public HirelingSubagent(BaseCreature creature, AIMemory memory)
        {
            _creature = creature;
            _memory = memory;
        }

        public void OnHeartbeat(Mobile player)
        {
            if (!_creature.Alive || !_creature.Controlled || _creature.ControlMaster != player)
                return;

            if (DateTime.UtcNow - _lastLoyaltyGain > TimeSpan.FromMinutes(5))
            {
                GainLoyalty(10);
                _lastLoyaltyGain = DateTime.UtcNow;
            }

            ProcessGrowth();
        }

        public void OnCommand(Mobile from, string command, string args)
        {
            if (!_creature.Controlled || _creature.ControlMaster != from)
                return;

            var loyaltyCheck = _loyalty >= 30 || from.AccessLevel > AccessLevel.Player;
            if (!loyaltyCheck)
            {
                _creature.Say("I don't trust you enough to follow that order.");
                return;
            }

            var lowerCmd = command.ToLowerInvariant();
            bool handled = false;

            switch (lowerCmd)
            {
                case "equip":
                    handled = HandleEquip(args);
                    break;
                case "craft":
                    handled = HandleCraft(args);
                    break;
                case "patrol":
                    handled = HandlePatrol(args);
                    break;
                case "scout":
                    handled = HandleScout(args);
                    break;
            }

            if (handled)
            {
                GainLoyalty(5);
                _creature.Say("As you command.");
            }
            else
            {
                _creature.Say("I don't understand that command.");
            }
        }

        private bool HandleEquip(string args)
        {
            var pack = _creature.Backpack;
            if (pack == null) return false;

            var items = pack.FindItemsByType(typeof(Item), true);
            foreach (var item in items)
            {
                if (item.Name != null && item.Name.ToLowerInvariant().Contains(args.ToLowerInvariant()))
                {
                    if (_creature.EquipItem(item))
                        return true;
                }
            }
            return false;
        }

        private bool HandleCraft(string args)
        {
            _creature.Say("I'll work on " + args + ".");
            return true;
        }

        private bool HandlePatrol(string args)
        {
            _creature.ControlOrder = OrderType.Patrol;
            return true;
        }

        private bool HandleScout(string args)
        {
            _creature.ControlOrder = OrderType.None;
            return true;
        }

        private void GainLoyalty(int amount)
        {
            _loyalty = Math.Min(100, _loyalty + amount);
        }

        private void ProcessGrowth()
        {
            foreach (Skill skill in _creature.Skills)
            {
                if (skill.Base > 0 && skill.Base < skill.Cap)
                {
                    if (Utility.RandomDouble() < 0.001)
                    {
                        skill.Base += 0.1;
                        if (!_skillGains.ContainsKey(skill.SkillName))
                            _skillGains[skill.SkillName] = 0;
                        _skillGains[skill.SkillName] += 0.1;
                    }
                }
            }

            if (Utility.RandomDouble() < 0.0005)
            {
                var stats = new[] { "Str", "Dex", "Int" };
                var stat = stats[Utility.Random(stats.Length)];
                switch (stat)
                {
                    case "Str": if (_creature.RawStr < _creature.StatCap) _creature.RawStr++; break;
                    case "Dex": if (_creature.RawDex < _creature.StatCap) _creature.RawDex++; break;
                    case "Int": if (_creature.RawInt < _creature.StatCap) _creature.RawInt++; break;
                }
            }
        }

        public int Loyalty => _loyalty;
        public Dictionary<SkillName, double> SkillGains => _skillGains;
    }
}