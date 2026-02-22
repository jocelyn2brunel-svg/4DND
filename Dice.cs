using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace _4DND
{
    public static class Dice
    {
        private static readonly Random _rng = new Random();

        public class RollResult
        {
            public string Notation { get; set; } = string.Empty;
            public List<int> Rolls { get; set; } = new List<int>();
            public int Modifier { get; set; }
            public int Total { get; set; }

            public override string ToString()
            {
                return $"{Notation} => Total={Total} Modifier={Modifier} Rolls=[{string.Join(",", Rolls)}]";
            }
        }

        public static int Roll(int sides)
        {
            if (sides <= 0) throw new ArgumentOutOfRangeException(nameof(sides));
            return _rng.Next(1, sides + 1);
        }
        
        /// <summary>
        /// Roll a d100 (percentile dice) using two d10s.
        /// One die gives the tens digit (0-9), the other gives the ones digit (0-9).
        /// Result ranges from 1 to 100. (0,0) = 100.
        /// </summary>
        public static int RollD100()
        {
            int tensDigit = _rng.Next(0, 10); // 0-9
            int onesDigit = _rng.Next(0, 10); // 0-9
            
            // Two 0s represent 100
            if (tensDigit == 0 && onesDigit == 0)
                return 100;
            
            return tensDigit * 10 + onesDigit;
        }

        public static int[] RollMany(int count, int sides)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var outArr = new int[count];
            for (int i = 0; i < count; i++) outArr[i] = Roll(sides);
            return outArr;
        }

        // Parse and evaluate an expression like: "3d6+2", "d20", "2d6+1d4-1", "d100"
        public static RollResult RollNotation(string notation)
        {
            if (notation == null) throw new ArgumentNullException(nameof(notation));
            var text = notation.Replace(" ", "").Trim();
            var res = new RollResult() { Notation = notation };
            if (text.Length == 0)
            {
                res.Total = 0;
                return res;
            }

            // Regex matches terms like [+|-]NdS or [+|-]N (modifier)
            var termPattern = new Regex("([+-]?)(?:(\\d*)d(\\d+)|(\\d+))", RegexOptions.IgnoreCase);
            var matches = termPattern.Matches(text);
            if (matches.Count == 0)
            {
                // try if it's a single number
                if (int.TryParse(text, out var onlyModifier))
                {
                    res.Modifier = onlyModifier;
                    res.Total = onlyModifier;
                    return res;
                }
                throw new FormatException("Invalid dice notation: '" + notation + "'");
            }

            int total = 0;
            int modifierSum = 0;

            foreach (Match m in matches)
            {
                var signStr = m.Groups[1].Value;
                int sign = signStr == "-" ? -1 : 1;

                if (m.Groups[3].Success) // dice term (groups: 2=count, 3=sides)
                {
                    var countStr = m.Groups[2].Value;
                    int count = 1;
                    if (!string.IsNullOrEmpty(countStr)) count = int.Parse(countStr);
                    int sides = int.Parse(m.Groups[3].Value);
                    if (count < 0 || sides <= 0) throw new FormatException("Invalid dice term in notation: '" + m.Value + "'");

                    int sum = 0;
                    for (int i = 0; i < count; i++)
                    {
                        int roll;
                        // Special handling for d100 (percentile dice)
                        if (sides == 100)
                        {
                            roll = RollD100();
                        }
                        else
                        {
                            roll = Roll(sides);
                        }
                        sum += roll;
                        // store signed roll so caller can see positive/negative contributions
                        res.Rolls.Add(sign * roll);
                    }

                    total += sign * sum;
                }
                else if (m.Groups[4].Success) // plain integer modifier
                {
                    int val = int.Parse(m.Groups[4].Value);
                    modifierSum += sign * val;
                }
            }

            res.Modifier = modifierSum;
            res.Total = total + modifierSum;
            return res;
        }
    }
}
