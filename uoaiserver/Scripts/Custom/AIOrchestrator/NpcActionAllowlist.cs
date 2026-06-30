using System;
using System.Collections.Generic;

namespace Server.AIOrchestrator
{
    public static class NpcActionAllowlist
    {
        private static readonly HashSet<string> CosmeticActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bow", "nod", "shake_head", "laugh", "point",
            "wave", "shrug", "salute", "clap", "yawn",
            "dance", "cheer", "cry", "kneel", "sit"
        };

        private static readonly HashSet<string> HirelingCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "follow", "guard", "attack", "stay", "come",
            "drop", "transfer", "friend", "unfriend",
            "equip", "craft", "patrol", "scout", "release"
        };

        public static bool IsCosmeticAction(string action)
        {
            return CosmeticActions.Contains(action?.ToLowerInvariant());
        }

        public static bool IsHirelingCommand(string command)
        {
            return HirelingCommands.Contains(command?.ToLowerInvariant());
        }

        public static int GetAnimationId(string action)
        {
            switch (action?.ToLowerInvariant())
            {
                case "bow": return 5;
                case "nod": return 6;
                case "shake_head": return 7;
                case "laugh": return 8;
                case "point": return 9;
                case "wave": return 10;
                case "shrug": return 11;
                case "salute": return 12;
                case "clap": return 13;
                case "yawn": return 14;
                case "dance": return 15;
                case "cheer": return 16;
                case "cry": return 17;
                case "kneel": return 18;
                case "sit": return 19;
                default: return 0;
            }
        }

        public static bool TryResolve(string token, out int animationId)
        {
            animationId = 0;
            if (string.IsNullOrEmpty(token))
                return false;

            var action = token.Trim().ToLowerInvariant();
            if (CosmeticActions.Contains(action))
            {
                animationId = GetAnimationId(action);
                return true;
            }
            return false;
        }
    }
}