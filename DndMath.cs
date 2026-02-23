using System;

namespace _4DND
{
    /// <summary>
    /// Utility class for D&D 5e mathematical operations.
    /// Follows the core rule: "Whenever you divide a number in the game,
    /// round down if you end up with a fraction, even if the fraction is one-half or greater."
    /// </summary>
    public static class DndMath
    {
        /// <summary>
        /// Divides two integers following D&D rounding rules (always round down).
        /// </summary>
        /// <param name="dividend">The number to be divided</param>
        /// <param name="divisor">The number to divide by</param>
        /// <returns>The result rounded down to the nearest integer</returns>
        public static int Divide(int dividend, int divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException("Cannot divide by zero");
            
            // Integer division in C# already rounds towards zero
            // For positive results, this is equivalent to floor
            // For negative results, we need to explicitly floor
            if (dividend >= 0 && divisor > 0)
            {
                return dividend / divisor;
            }
            else if (dividend < 0 && divisor > 0)
            {
                // Round down (more negative) for negative dividends
                return (int)Math.Floor((double)dividend / divisor);
            }
            else
            {
                // For consistency, always round towards negative infinity
                return (int)Math.Floor((double)dividend / divisor);
            }
        }
        
        /// <summary>
        /// Calculates an ability modifier from an ability score (always round down).
        /// Formula: (ability_score - 10) / 2
        /// </summary>
        /// <param name="abilityScore">The ability score (e.g., Strength, Dexterity)</param>
        /// <returns>The ability modifier</returns>
        public static int GetAbilityModifier(int abilityScore)
        {
            return Divide(abilityScore - 10, 2);
        }
        
        /// <summary>
        /// Calculates proficiency bonus based on character level (always round down).
        /// Formula: 2 + (level - 1) / 4
        /// </summary>
        /// <param name="level">Character level (1-20)</param>
        /// <returns>The proficiency bonus</returns>
        public static int GetProficiencyBonus(int level)
        {
            return 2 + Divide(level - 1, 4);
        }
        
        /// <summary>
        /// Halves a value following D&D rounding rules (always round down).
        /// Commonly used for resistance to damage.
        /// </summary>
        /// <param name="value">The value to halve</param>
        /// <returns>Half the value, rounded down</returns>
        public static int Half(int value)
        {
            return Divide(value, 2);
        }
        
        /// <summary>
        /// Calculates how many full units fit into a value.
        /// Example: Converting movement speed to grid tiles (speed / 5 feet per tile)
        /// </summary>
        /// <param name="total">The total amount</param>
        /// <param name="unitSize">The size of each unit</param>
        /// <returns>Number of complete units, rounded down</returns>
        public static int GetUnits(int total, int unitSize)
        {
            return Divide(total, unitSize);
        }
        
        /// <summary>
        /// Standard Difficulty Class values per D&D 5e rules.
        /// </summary>
        public static class DifficultyClass
        {
            public const int VeryEasy = 5;
            public const int Easy = 10;
            public const int Medium = 15;
            public const int Hard = 20;
            public const int VeryHard = 25;
            public const int NearlyImpossible = 30;
        }
        
        /// <summary>
        /// Checks if a total roll (d20 + modifiers) meets or exceeds the target DC.
        /// </summary>
        /// <param name="total">The total of the roll plus all modifiers</param>
        /// <param name="dc">The Difficulty Class (target number)</param>
        /// <returns>True if the total equals or exceeds the DC (success), false otherwise</returns>
        public static bool MeetsDC(int total, int dc)
        {
            return total >= dc;
        }

        /// <summary>
        /// XP required to reach each level (D&D 5e Character Advancement table, PHB p.15).
        /// Index = level (1-based), value = total XP needed to reach that level.
        /// </summary>
        private static readonly int[] XPThresholds = new int[]
        {
                 0,   //  1
               300,   //  2
               900,   //  3
             2_700,   //  4
             6_500,   //  5
            14_000,   //  6
            23_000,   //  7
            34_000,   //  8
            48_000,   //  9
            64_000,   // 10
            85_000,   // 11
           100_000,   // 12
           120_000,   // 13
           140_000,   // 14
           165_000,   // 15
           195_000,   // 16
           225_000,   // 17
           265_000,   // 18
           305_000,   // 19
           355_000,   // 20
        };

        /// <summary>
        /// Returns the minimum XP required to reach <paramref name="level"/> (1–20).
        /// </summary>
        public static int GetXPThreshold(int level)
        {
            int index = Math.Clamp(level, 1, 20) - 1;
            return XPThresholds[index];
        }

        /// <summary>
        /// Returns the character level (1–20) that corresponds to <paramref name="totalXP"/>.
        /// </summary>
        public static int GetLevelForXP(int totalXP)
        {
            int level = 1;
            for (int i = XPThresholds.Length - 1; i >= 0; i--)
            {
                if (totalXP >= XPThresholds[i])
                {
                    level = i + 1;
                    break;
                }
            }
            return level;
        }
    }
}
