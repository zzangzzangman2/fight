using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JoseonMurimTactics
{
    public enum RollMode
    {
        Normal,
        Advantage,
        Disadvantage
    }

    public readonly struct DiceRoll
    {
        public readonly int total;
        public readonly int natural;
        public readonly string detail;

        public DiceRoll(int total, int natural, string detail)
        {
            this.total = total;
            this.natural = natural;
            this.detail = detail;
        }
    }

    public sealed class DiceRoller
    {
        private readonly System.Random random;

        public DiceRoller(int seed)
        {
            random = new System.Random(seed);
        }

        public DiceRoll RollD20(RollMode mode = RollMode.Normal)
        {
            int first = random.Next(1, 21);
            if (mode == RollMode.Normal)
            {
                return new DiceRoll(first, first, first.ToString());
            }

            int second = random.Next(1, 21);
            int picked = mode == RollMode.Advantage ? Mathf.Max(first, second) : Mathf.Min(first, second);
            return new DiceRoll(picked, picked, first + "/" + second);
        }

        public DiceRoll RollDice(string expression, int multiplier = 1)
        {
            Match match = Regex.Match(expression ?? string.Empty, @"^(\d+)d(\d+)$");
            if (!match.Success)
            {
                return new DiceRoll(0, 0, "0");
            }

            int count = int.Parse(match.Groups[1].Value) * Mathf.Max(1, multiplier);
            int sides = int.Parse(match.Groups[2].Value);
            List<int> rolls = new List<int>();
            int total = 0;
            for (int i = 0; i < count; i++)
            {
                int roll = random.Next(1, sides + 1);
                rolls.Add(roll);
                total += roll;
            }

            return new DiceRoll(total, total, string.Join("+", rolls));
        }
    }
}
